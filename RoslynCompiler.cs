using System;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Compilador
{
    public class RoslynCompiler
    {
        public bool CompileFromTAC(List<string> tacCode, string outputPath)
        {
            try
            {
                string csharpCode = ConvertTacToCSharp(tacCode);
                SemanticAnalyzer.DebugLogger.Log("Generated C# code:\n" + csharpCode);

                SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(csharpCode);
                var references = GetRequiredReferences();

                CSharpCompilation compilation = CSharpCompilation.Create(
                    Path.GetFileNameWithoutExtension(outputPath),
                    new[] { syntaxTree },
                    references,
                    new CSharpCompilationOptions(
                        OutputKind.ConsoleApplication,
                        optimizationLevel: OptimizationLevel.Debug,
                        platform: Platform.AnyCpu)
                );

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
            references.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
            references.Add(MetadataReference.CreateFromFile(typeof(Console).Assembly.Location));
            references.Add(MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location));

            var runtimePath = RuntimeEnvironment.GetRuntimeDirectory();
            references.Add(MetadataReference.CreateFromFile(Path.Combine(runtimePath, "System.Runtime.dll")));
            references.Add(MetadataReference.CreateFromFile(Path.Combine(runtimePath, "mscorlib.dll")));
            references.Add(MetadataReference.CreateFromFile(Path.Combine(runtimePath, "netstandard.dll")));

            return references;
        }

        private string ConvertTacToCSharp(List<string> tacCode)
        {
            var code = new StringBuilder();
            code.AppendLine("using System;");
            code.AppendLine("namespace GeneratedProgram");
            code.AppendLine("{");
            code.AppendLine("    public static class Program");
            code.AppendLine("    {");

            var functions = new Dictionary<string, StringBuilder>();
            string currentFunction = "Main";
            functions[currentFunction] = new StringBuilder();
            functions[currentFunction].AppendLine("        public static void Main(string[] args)");
            functions[currentFunction].AppendLine("        {");

            var variables = new Dictionary<string, VariableInfo>();
            var labels = new HashSet<string>();

            // Primera pasada: recolectar información de tipos
            AnalyzeVariableTypesMultiPass(tacCode, variables);

            // Segunda pasada: generar código
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
                    ProcessTacLine(line, variables, labels, functions[currentFunction]);
                }
            }

            InsertVariableDeclarations(functions, variables);

            functions["Main"].AppendLine("        }");

            foreach (var func in functions.Values)
            {
                code.AppendLine(func.ToString());
            }

            code.AppendLine("    }");
            code.AppendLine("}");

            return code.ToString();
        }

        private void AnalyzeVariableTypesMultiPass(List<string> tacCode, Dictionary<string, VariableInfo> variables)
        {
            // Múltiples pasadas para resolver tipos correctamente
            bool typesChanged;
            int maxPasses = 10; // Evitar bucles infinitos
            int currentPass = 0;

            do
            {
                typesChanged = false;
                currentPass++;

                foreach (string line in tacCode)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    if (AnalyzeVariableTypes(line, variables))
                    {
                        typesChanged = true;
                    }
                }
            } while (typesChanged && currentPass < maxPasses);

            // Establecer tipo por defecto para variables sin tipo determinado
            foreach (var variable in variables.Values)
            {
                if (string.IsNullOrEmpty(variable.Type))
                {
                    variable.Type = "int"; // Tipo por defecto
                }
            }
        }

        private bool AnalyzeVariableTypes(string line, Dictionary<string, VariableInfo> variables)
        {
            line = line.Split('#')[0].Trim();
            if (string.IsNullOrEmpty(line)) return false;

            bool typeChanged = false;
            int assignIndex = FindAssignmentOperator(line);

            if (assignIndex > 0)
            {
                string left = line.Substring(0, assignIndex).Trim();
                string right = line.Substring(assignIndex + 1).Trim();

                // Crear variable si no existe
                if (!variables.ContainsKey(left))
                {
                    variables[left] = new VariableInfo { Name = left };
                }

                string inferredType = InferTypeFromExpression(right, variables);

                // Solo actualizar si el tipo cambió o no estaba establecido
                if (string.IsNullOrEmpty(variables[left].Type) || variables[left].Type != inferredType)
                {
                    variables[left].Type = inferredType;
                    typeChanged = true;
                }
            }

            return typeChanged;
        }

        private string InferTypeFromExpression(string expression, Dictionary<string, VariableInfo> variables)
        {
            expression = expression.Trim();

            // Literal de cadena
            if (IsStringLiteral(expression))
            {
                return "string";
            }

            // Literal booleano
            if (expression == "true" || expression == "false")
            {
                return "bool";
            }

            // Literal numérico entero
            if (int.TryParse(expression, out _))
            {
                return "int";
            }

            // Literal numérico decimal
            if (double.TryParse(expression, out _) && expression.Contains("."))
            {
                return "double";
            }

            // Variable simple (asignación directa)
            if (variables.ContainsKey(expression) && !string.IsNullOrEmpty(variables[expression].Type))
            {
                return variables[expression].Type;
            }

            // CORRECCIÓN: Normalizar la expresión para el análisis
            string normalizedExpression = expression
                .Replace("AND", "&&")
                .Replace("OR", "||")
                .Replace("NOT", "!");

            // Verificar expresiones booleanas ANTES que aritméticas
            if (IsBooleanExpression(normalizedExpression))
            {
                return "bool";
            }

            // Expresión aritmética (a + b, a - b, etc.)
            if (IsArithmeticExpression(normalizedExpression))
            {
                var operands = ExtractOperandsFromArithmetic(normalizedExpression);

                // Si todos los operandos son int, el resultado es int
                bool allInt = true;
                bool hasDouble = false;
                bool hasString = false;

                foreach (var operand in operands)
                {
                    string operandType = InferTypeFromSingleOperand(operand, variables);
                    if (operandType == "string")
                    {
                        hasString = true;
                        break;
                    }
                    else if (operandType == "double")
                    {
                        hasDouble = true;
                        allInt = false;
                    }
                    else if (operandType != "int")
                    {
                        allInt = false;
                    }
                }

                if (hasString) return "string"; // Concatenación
                if (hasDouble) return "double";
                if (allInt) return "int";
            }

            // Por defecto, asumir int si no se puede determinar
            return "int";
        }

        private string InferTypeFromSingleOperand(string operand, Dictionary<string, VariableInfo> variables)
        {
            operand = operand.Trim();

            if (IsStringLiteral(operand)) return "string";
            if (operand == "true" || operand == "false") return "bool";
            if (int.TryParse(operand, out _)) return "int";
            if (double.TryParse(operand, out _) && operand.Contains(".")) return "double";

            if (variables.ContainsKey(operand) && !string.IsNullOrEmpty(variables[operand].Type))
            {
                return variables[operand].Type;
            }

            return "int"; // Por defecto
        }

        private bool IsArithmeticExpression(string expression)
        {
            return expression.Contains("+") || expression.Contains("-") ||
                   expression.Contains("*") || expression.Contains("/") ||
                   expression.Contains("%");
        }

        private List<string> ExtractOperandsFromArithmetic(string expression)
        {
            var operands = new List<string>();
            var operators = new[] { "+", "-", "*", "/", "%" };

            string[] parts = expression.Split(operators, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                string operand = part.Trim();
                if (!string.IsNullOrEmpty(operand))
                {
                    operands.Add(operand);
                }
            }

            return operands;
        }

        private void ProcessTacLine(string line, Dictionary<string, VariableInfo> variables,
                                  HashSet<string> labels, StringBuilder functionBuilder)
        {
            line = line.Split('#')[0].Trim();
            if (string.IsNullOrEmpty(line)) return;

            if (line.EndsWith(":"))
            {
                string labelName = line.TrimEnd(':');
                labels.Add(labelName);
                functionBuilder.AppendLine($"            {line};");
                return;
            }

            if (line.StartsWith("GOTO"))
            {
                string label = line.Substring("GOTO".Length).Trim();
                functionBuilder.AppendLine($"            goto {label};");
                return;
            }

            if (line.StartsWith("IF_FALSE"))
            {
                var parts = line.Split(new[] { "GOTO" }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    string cond = parts[0].Replace("IF_FALSE", "").Trim();
                    string label = parts[1].Trim();
                    functionBuilder.AppendLine($"            if (!{cond}) goto {label};");
                }
                return;
            }

            if (line.StartsWith("print ") || line.StartsWith("println "))
            {
                bool isPrintLn = line.StartsWith("println ");
                string content = line.Substring(isPrintLn ? "println ".Length : "print ".Length).Trim();
                functionBuilder.AppendLine($"            Console.{(isPrintLn ? "WriteLine" : "Write")}({content});");
                return;
            }

            int assignIndex = FindAssignmentOperator(line);
            if (assignIndex > 0)
            {
                string left = line.Substring(0, assignIndex).Trim();
                string right = line.Substring(assignIndex + 1).Trim();

                right = right.Replace("AND", "&&")
                             .Replace("OR", "||")
                             .Replace("NOT", "!");

                functionBuilder.AppendLine($"            {left} = {right};");
                return;
            }

            if (line.StartsWith("RETURN"))
            {
                if (line.Length > "RETURN".Length)
                {
                    functionBuilder.AppendLine($"            return {line.Substring("RETURN".Length).Trim()};");
                }
                else
                {
                    functionBuilder.AppendLine("            return;");
                }
            }
        }

        private void InsertVariableDeclarations(Dictionary<string, StringBuilder> functions,
                                              Dictionary<string, VariableInfo> variables)
        {
            foreach (var funcName in functions.Keys.ToList())
            {
                var funcContent = functions[funcName].ToString();
                var lines = funcContent.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                int openingBraceIndex = Array.FindIndex(lines, l => l.Contains("{"));

                if (openingBraceIndex >= 0)
                {
                    var newLines = lines.Take(openingBraceIndex + 1).ToList();

                    // Agrupar variables por tipo y ordenar para tener un output consistente
                    var varsByType = variables.Values
                        .Where(v => !IsKeyword(v.Name))
                        .GroupBy(v => v.Type ?? "int")
                        .OrderBy(g => GetTypePriority(g.Key)); // Ordenar tipos: string, bool, int, double

                    foreach (var group in varsByType)
                    {
                        string typeName = group.Key;
                        var varNames = group.Select(v => v.Name).OrderBy(n => n);
                        newLines.Add($"            {typeName} {string.Join(", ", varNames)};");
                    }

                    newLines.AddRange(lines.Skip(openingBraceIndex + 1));
                    functions[funcName] = new StringBuilder(string.Join(Environment.NewLine, newLines));
                }
            }
        }

        private int GetTypePriority(string type)
        {
            switch (type)
            {
                case "string": return 1;
                case "bool": return 2;
                case "int": return 3;
                case "double": return 4;
                default: return 5;
            }
        }

        private bool IsStringLiteral(string expression)
        {
            return expression.StartsWith("\"") && expression.EndsWith("\"");
        }

        private bool IsBooleanExpression(string expression)
        {
            expression = expression.Trim();

            // Verificar si es un literal booleano
            if (expression == "true" || expression == "false")
                return true;

            // Verificar operadores lógicos (&&, ||, !) - incluyendo versiones TAC
            if (expression.Contains("&&") || expression.Contains("||") ||
                expression.Contains("AND") || expression.Contains("OR"))
                return true;

            // Verificar operadores de comparación
            if (expression.Contains("==") || expression.Contains("!=") ||
                expression.Contains("<=") || expression.Contains(">=") ||
                expression.Contains("<") || expression.Contains(">"))
                return true;

            // Verificar operador NOT al inicio (tanto ! como NOT)
            if (expression.StartsWith("!") || expression.StartsWith("NOT "))
                return true;

            return false;
        }

        private bool IsNumericExpression(string expression)
        {
            return expression.Any(char.IsDigit) && !IsStringLiteral(expression) && !IsBooleanExpression(expression);
        }

        private int FindAssignmentOperator(string line)
        {
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == '=')
                {
                    bool isComparison = (i > 0 && (line[i - 1] == '!' || line[i - 1] == '<' || line[i - 1] == '>')) ||
                                      (i < line.Length - 1 && line[i + 1] == '=');
                    if (!isComparison) return i;
                }
            }
            return -1;
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

        private class VariableInfo
        {
            public string Name { get; set; }
            public string Type { get; set; }
        }
    }
}