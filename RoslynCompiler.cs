using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Antlr4.Runtime.Tree;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace Compilador
{
    public class RoslynCompiler
    {
        public bool CompileFromTAC(List<string> tacCode, string outputPath)
        {
            try
            {
                // 1. Convertir TAC a C#
                string csharpCode = ConvertTacToCSharp(tacCode);
                SemanticAnalyzer.DebugLogger.Log("Generated C# code:\n" + csharpCode);

                // 2. Configurar sintaxis
                SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(csharpCode);

                // 3. Configurar referencias
                var references = GetRequiredReferences();

                // 4. Configurar compilación
                CSharpCompilation compilation = CSharpCompilation.Create(
                    Path.GetFileNameWithoutExtension(outputPath),
                    new[] { syntaxTree },
                    references,
                    new CSharpCompilationOptions(
                        OutputKind.ConsoleApplication,
                        optimizationLevel: OptimizationLevel.Debug,
                        platform: Platform.AnyCpu)
                );

                // 5. Emitir ejecutable
                EmitResult result = compilation.Emit(outputPath);

                if (!result.Success)
                {
                    foreach (Diagnostic diagnostic in result.Diagnostics
                        .Where(d => d.Severity == DiagnosticSeverity.Error))
                    {
                        SemanticAnalyzer.DebugLogger.Log($"{diagnostic.Severity}: {diagnostic.Id} - {diagnostic.GetMessage()}");
                        SemanticAnalyzer.DebugLogger.Log($"Location: {diagnostic.Location.GetLineSpan()}");
                    }
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                SemanticAnalyzer.DebugLogger.Log($"Roslyn compilation failed: {ex}");
                return false;
            }
        }

        private List<MetadataReference> GetRequiredReferences()
        {
            var references = new List<MetadataReference>();

            // Ensamblados básicos
            references.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
            references.Add(MetadataReference.CreateFromFile(typeof(Console).Assembly.Location));
            references.Add(MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location));

            // Ensamblados del runtime
            var runtimePath = RuntimeEnvironment.GetRuntimeDirectory();
            references.Add(MetadataReference.CreateFromFile(Path.Combine(runtimePath, "System.Runtime.dll")));
            references.Add(MetadataReference.CreateFromFile(Path.Combine(runtimePath, "mscorlib.dll")));
            references.Add(MetadataReference.CreateFromFile(Path.Combine(runtimePath, "netstandard.dll")));

            return references;
        }

        // MÉTODO PRINCIPAL CORREGIDO
        private string ConvertTacToCSharp(List<string> tacCode)
        {
            var code = new StringBuilder();
            code.AppendLine("using System;");
            code.AppendLine("namespace GeneratedProgram");
            code.AppendLine("{");
            code.AppendLine("    public static class Program");
            code.AppendLine("    {");

            // Procesar funciones y main
            var functions = new Dictionary<string, StringBuilder>();
            string currentFunction = "Main";
            functions[currentFunction] = new StringBuilder();
            functions[currentFunction].AppendLine("        public static void Main(string[] args)");
            functions[currentFunction].AppendLine("        {");

            // Variables para rastrear las variables usadas, labels y tipos
            var variables = new HashSet<string>();
            var labels = new HashSet<string>();
            var booleanVariables = new HashSet<string>();
            var stringVariables = new HashSet<string>();

            foreach (string line in tacCode)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                if (line.StartsWith("FUNC_BEGIN"))
                {
                    string funcName = line.Split(' ')[1];
                    currentFunction = funcName;
                    functions[funcName] = new StringBuilder();
                    functions[funcName].AppendLine($"        public static void {funcName}()");
                    functions[funcName].AppendLine("        {");
                }
                else if (line.StartsWith("FUNC_END"))
                {
                    functions[currentFunction].AppendLine("        }");
                    currentFunction = "Main";
                }
                else
                {
                    // PRIMERO: Extraer variables directamente del TAC
                    ExtractVariablesFromTacLine(line, variables, booleanVariables, stringVariables);

                    // SEGUNDO: Convertir la línea
                    string csharpLine = ConvertTacLineToCSharp(line, labels, booleanVariables, stringVariables);
                    if (!string.IsNullOrEmpty(csharpLine))
                    {
                        functions[currentFunction].AppendLine($"            {csharpLine}");
                    }
                }
            }

            // Insertar declaraciones de variables al inicio de cada función
            foreach (var funcName in functions.Keys.ToList())
            {
                var funcContent = functions[funcName].ToString();
                var lines = funcContent.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

                // Encontrar la línea que contiene el {
                int openingBraceIndex = Array.FindIndex(lines, l => l.Contains("{"));

                if (openingBraceIndex >= 0)
                {
                    // Insertar declaraciones de variables después del {
                    var newLines = lines.Take(openingBraceIndex + 1).ToList();

                    // Filtrar variables válidas
                    var validVariables = variables.Where(v =>
                        !labels.Contains(v) &&
                        !IsKeyword(v) &&
                        !v.Equals("Console") &&
                        !v.Equals("WriteLine") &&
                        !v.Equals("Write") &&
                        !v.Equals("args") &&
                        IsValidVariableName(v))
                        .OrderBy(v => v);

                    // Declarar variables con el tipo correcto
                    foreach (var varName in validVariables)
                    {
                        if (booleanVariables.Contains(varName))
                        {
                            newLines.Add($"            bool {varName};");
                        }
                        else if (stringVariables.Contains(varName))
                        {
                            newLines.Add($"            string {varName};");
                        }
                        else
                        {
                            newLines.Add($"            int {varName};");
                        }
                    }

                    newLines.AddRange(lines.Skip(openingBraceIndex + 1));
                    functions[funcName] = new StringBuilder(string.Join(Environment.NewLine, newLines));
                }
            }

            functions["Main"].AppendLine("        }");

            // Agregar todas las funciones al código
            foreach (var func in functions.Values)
            {
                code.AppendLine(func.ToString());
            }

            code.AppendLine("    }");
            code.AppendLine("}");

            return code.ToString();
        }

        // NUEVO MÉTODO: Extraer variables directamente del TAC
        private void ExtractVariablesFromTacLine(string line, HashSet<string> variables,
            HashSet<string> booleanVariables, HashSet<string> stringVariables)
        {
            line = line.Split(new[] { '#' }, 2)[0].Trim();
            if (string.IsNullOrEmpty(line)) return;

            // Solo procesar líneas de asignación
            int assignIndex = FindAssignmentOperator(line);
            if (assignIndex > 0)
            {
                string left = line.Substring(0, assignIndex).Trim();
                string right = line.Substring(assignIndex + 1).Trim();

                // Agregar la variable del lado izquierdo
                if (IsValidVariableName(left))
                {
                    variables.Add(left);

                    // Determinar el tipo
                    if (IsBooleanExpression(right) || IsComparisonExpression(right))
                    {
                        booleanVariables.Add(left);
                    }
                    else if (IsStringExpression(right))
                    {
                        stringVariables.Add(left);
                    }
                }

                // Extraer variables del lado derecho
                ExtractVariablesFromExpression(right, variables);
            }
        }

        private string ConvertTacLineToCSharp(string line, HashSet<string> labels, HashSet<string> booleanVariables, HashSet<string> stringVariables)
        {
            line = line.Split(new[] { '#' }, 2)[0].Trim();
            if (string.IsNullOrEmpty(line)) return string.Empty;

            // Manejar labels
            if (line.EndsWith(":"))
            {
                string labelName = line.TrimEnd(':');
                labels.Add(labelName);
                return line + " ;";
            }

            // Manejar GOTO
            if (line.StartsWith("GOTO"))
            {
                string label = line.Substring("GOTO".Length).Trim();
                return $"goto {label};";
            }

            // Manejar IF_FALSE
            if (line.StartsWith("IF_FALSE"))
            {
                var parts = line.Split(new[] { "GOTO" }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    string cond = parts[0].Replace("IF_FALSE", "").Trim();
                    string label = parts[1].Trim();
                    return $"if (!{cond}) goto {label};";
                }
            }

            // Manejar prints
            if (line.StartsWith("print ") || line.StartsWith("println "))
            {
                bool isPrintLn = line.StartsWith("println ");
                string content = line.Substring(isPrintLn ? "println ".Length : "print ".Length).Trim();
                return $"Console.{(isPrintLn ? "WriteLine" : "Write")}({content});";
            }

            // Manejar asignaciones
            int assignIndex = FindAssignmentOperator(line);
            if (assignIndex > 0)
            {
                string left = line.Substring(0, assignIndex).Trim();
                string right = line.Substring(assignIndex + 1).Trim();

                // Manejar operaciones
                right = right.Replace("AND", "&&")
                             .Replace("OR", "||")
                             .Replace("NOT", "!");

                return $"{left} = {right};";
            }

            // Manejar RETURN
            if (line.StartsWith("RETURN"))
            {
                if (line.Length > "RETURN".Length)
                {
                    return $"return {line.Substring("RETURN".Length).Trim()};";
                }
                return "return;";
            }

            return "";
        }

        private int FindAssignmentOperator(string line)
        {
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == '=')
                {
                    // Verificar que no sea parte de == o !=
                    bool isPartOfComparison = false;

                    // Verificar si hay otro = después
                    if (i + 1 < line.Length && line[i + 1] == '=')
                    {
                        isPartOfComparison = true;
                    }

                    // Verificar si hay ! antes
                    if (i > 0 && line[i - 1] == '!')
                    {
                        isPartOfComparison = true;
                    }

                    // Verificar si hay < antes
                    if (i > 0 && line[i - 1] == '<')
                    {
                        isPartOfComparison = true;
                    }

                    // Verificar si hay > antes
                    if (i > 0 && line[i - 1] == '>')
                    {
                        isPartOfComparison = true;
                    }

                    // Si no es parte de una comparación, este es nuestro operador de asignación
                    if (!isPartOfComparison)
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        private bool IsComparisonExpression(string expression)
        {
            var comparisonOperators = new[] { "<", ">", "<=", ">=", "==", "!=" };
            return comparisonOperators.Any(op => expression.Contains(op));
        }

        private bool IsBooleanExpression(string expression)
        {
            expression = expression.Trim();
            return expression == "true" || expression == "false" || IsComparisonExpression(expression);
        }

        private bool IsStringExpression(string expression)
        {
            expression = expression.Trim();
            return expression.StartsWith("\"") && expression.EndsWith("\"");
        }

        // MÉTODO ACTUALIZADO: No se usa más para extraer del C#, pero se mantiene para compatibilidad
        private void ExtractVariables(string line, HashSet<string> variables)
        {
            // Este método ya no se usa en el flujo principal
            // Se mantiene por compatibilidad
        }

        private void ExtractVariablesFromExpression(string expression, HashSet<string> variables)
        {
            // Tokenizar la expresión
            var tokens = expression.Split(new char[] {
                ' ', '+', '-', '*', '/', '<', '>', '=', '!', '(', ')', '&', '|', ',', ';'
            }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var token in tokens)
            {
                string cleanToken = token.Trim();
                if (IsValidVariableName(cleanToken) && !IsNumericLiteral(cleanToken))
                {
                    variables.Add(cleanToken);
                }
            }
        }

        private bool IsValidVariableName(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            if (IsKeyword(name)) return false;
            if (IsNumericLiteral(name)) return false;

            // Debe comenzar con letra o _ y contener solo letras, números y _
            if (!char.IsLetter(name[0]) && name[0] != '_') return false;

            return name.All(c => char.IsLetterOrDigit(c) || c == '_');
        }

        private bool IsNumericLiteral(string token)
        {
            return int.TryParse(token, out _) || double.TryParse(token, out _);
        }

        private bool IsKeyword(string token)
        {
            var keywords = new HashSet<string> {
                "if", "else", "while", "for", "return", "goto",
                "true", "false", "null", "Console", "WriteLine", "Write",
                "int", "string", "bool", "void", "public", "static", "class"
            };
            return keywords.Contains(token);
        }
    }
}