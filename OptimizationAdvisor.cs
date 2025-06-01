using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Compilador
{
    class SimpleTACOptimizer
    {
        public List<string> AnalyzeCode(List<string> tacLines)
        {
            var advice = new List<string>();
            var variableUsage = new Dictionary<string, VariableInfo>();
            var functionCalls = new Dictionary<string, int>();
            var constantExpressions = new Dictionary<string, string>();

            // Primera pasada: recopilar información básica
            for (int i = 0; i < tacLines.Count; i++)
            {
                string line = tacLines[i].Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;

                ProcessLine(line, variableUsage, functionCalls, constantExpressions, i);
            }

            // Segunda pasada: análisis más profundo con contexto
            for (int i = 0; i < tacLines.Count; i++)
            {
                string line = tacLines[i].Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;

                AnalyzeLine(line, i, variableUsage, functionCalls, constantExpressions, advice, tacLines);
            }

            // Análisis post-pasada
            PostAnalysis(variableUsage, functionCalls, advice);

            return advice;
        }

        private void ProcessLine(string line,
                               Dictionary<string, VariableInfo> variableUsage,
                               Dictionary<string, int> functionCalls,
                               Dictionary<string, string> constantExpressions,
                               int lineNumber)
        {
            // Procesar asignaciones (formato: variable = expresión)
            if (line.Contains("=") && !line.StartsWith("println") && !line.StartsWith("print"))
            {
                var parts = line.Split(new[] { '=' }, 2);
                if (parts.Length == 2)
                {
                    string left = parts[0].Trim();
                    string right = parts[1].Trim();

                    // Registrar variable del lado izquierdo (se está asignando)
                    if (!variableUsage.ContainsKey(left))
                    {
                        variableUsage[left] = new VariableInfo
                        {
                            Name = left,
                            IsTemp = IsTempVariable(left),
                            DeclarationLine = lineNumber,
                            UsageCount = 0,
                            IsAssigned = true,
                            IsPrinted = false
                        };
                    }
                    else
                    {
                        variableUsage[left].IsAssigned = true;
                        // Si ya existía, actualizar línea de declaración solo si no tenía una
                        if (variableUsage[left].DeclarationLine == -1)
                        {
                            variableUsage[left].DeclarationLine = lineNumber;
                        }
                    }

                    // Analizar expresión del lado derecho (variables que se están usando)
                    AnalyzeExpression(right, left, variableUsage, constantExpressions, lineNumber);
                }
            }
            // Procesar llamadas a println/print
            else if (line.StartsWith("println") || line.StartsWith("print"))
            {
                string funcName = ExtractFunctionName(line);
                if (!string.IsNullOrEmpty(funcName))
                {
                    if (!functionCalls.ContainsKey(funcName))
                        functionCalls[funcName] = 0;
                    functionCalls[funcName]++;

                    // Extraer variable usada en println/print
                    var match = Regex.Match(line, @"(println|print)\s+(\w+)");
                    if (match.Success)
                    {
                        string usedVar = match.Groups[2].Value;
                        if (!variableUsage.ContainsKey(usedVar))
                        {
                            variableUsage[usedVar] = new VariableInfo
                            {
                                Name = usedVar,
                                IsTemp = IsTempVariable(usedVar),
                                DeclarationLine = -1,
                                UsageCount = 0,
                                IsAssigned = false,
                                IsPrinted = false
                            };
                        }

                        variableUsage[usedVar].UsageCount++;
                        variableUsage[usedVar].UsedInLines.Add(lineNumber);
                        variableUsage[usedVar].IsPrinted = true; // Marcar que se imprime
                    }
                }
            }
        }

        private void AnalyzeLine(string line,
                                int lineNumber,
                                Dictionary<string, VariableInfo> variableUsage,
                                Dictionary<string, int> functionCalls,
                                Dictionary<string, string> constantExpressions,
                                List<string> advice,
                                List<string> allLines)
        {
            // Optimización de expresiones constantes
            if (line.Contains("=") && !line.StartsWith("println") && !line.StartsWith("print"))
            {
                var parts = line.Split(new[] { '=' }, 2);
                if (parts.Length == 2)
                {
                    string left = parts[0].Trim();
                    string right = parts[1].Trim();

                    // Plegado de constantes
                    if (IsConstantExpression(right) && (right.Contains("+") || right.Contains("-") || right.Contains("*") || right.Contains("/")))
                    {
                        try
                        {
                            string simplified = SimplifyConstantExpression(right);
                            if (simplified != right)
                            {
                                advice.Add($"🔢 Plegado de constantes: Línea {lineNumber + 1}: '{line}' → podría reemplazarse por '{left} = {simplified}'");
                            }
                        }
                        catch
                        {
                            // Ignorar errores de evaluación
                        }
                    }

                    // Eliminación de asignaciones redundantes
                    if (right == left)
                    {
                        advice.Add($"🗑️ Asignación redundante: Línea {lineNumber + 1}: '{line}' → asigna una variable a sí misma");
                    }

                    // Optimización de operaciones matemáticas
                    CheckMathOperations(right, line, lineNumber, advice);
                }
            }
        }

        private void PostAnalysis(Dictionary<string, VariableInfo> variableUsage,
                                Dictionary<string, int> functionCalls,
                                List<string> advice)
        {
            // Análisis de variables no utilizadas - Más específico
            foreach (var varEntry in variableUsage)
            {
                string varName = varEntry.Key;
                var info = varEntry.Value;

                // Solo analizar variables que fueron asignadas
                if (info.IsAssigned)
                {
                    // Variable asignada pero nunca usada ni impresa
                    if (info.UsageCount == 0 && !info.IsPrinted)
                    {
                        if (info.IsTemp)
                        {
                            advice.Add($"🗑️ Variable temporal '{varName}' se asigna en línea {info.DeclarationLine + 1} pero nunca se usa");
                        }
                        else
                        {
                            advice.Add($"🗑️ Variable '{varName}' se asigna en línea {info.DeclarationLine + 1} pero nunca se usa ni se imprime");
                        }
                    }
                    // Variable usada en expresiones pero nunca impresa (solo para variables no temporales)
                    else if (!info.IsTemp && info.UsageCount > 0 && !info.IsPrinted)
                    {
                        // Verificar si solo se usa en asignaciones de otras variables que tampoco se usan
                        bool isUsedMeaningfully = CheckIfUsedMeaningfully(varName, variableUsage);
                        if (!isUsedMeaningfully)
                        {
                            advice.Add($"⚠️ Variable '{varName}' se usa en cálculos pero el resultado nunca se imprime o utiliza efectivamente");
                        }
                    }
                    // Variable usada solo una vez (para análisis de eficiencia)
                    else if (info.UsageCount == 1 && info.IsPrinted)
                    {
                        advice.Add($"ℹ️ Variable '{varName}' asignada en línea {info.DeclarationLine + 1} y usada solo una vez en línea {string.Join(", ", info.UsedInLines.Select(x => x + 1))}");
                    }
                }
            }

            // Análisis mejorado de variables temporales
            var tempVars = variableUsage.Where(v => v.Value.IsTemp && v.Value.IsAssigned).ToList();
            foreach (var tempVar in tempVars)
            {
                var info = tempVar.Value;
                if (info.UsageCount == 1)
                {
                    advice.Add($"🔄 Variable temporal '{tempVar.Key}' podría eliminarse mediante propagación de copia - solo se usa una vez");
                }
                else if (info.UsageCount == 0)
                {
                    advice.Add($"🗑️ Variable temporal '{tempVar.Key}' es completamente innecesaria - se calcula pero nunca se usa");
                }
            }

            // Análisis de funciones
            foreach (var funcEntry in functionCalls)
            {
                if (funcEntry.Value == 1 && !IsStandardFunction(funcEntry.Key))
                {
                    advice.Add($"⚠️ Función '{funcEntry.Key}' llamada solo una vez (considera inline si es pequeña)");
                }
            }
        }

        private bool CheckIfUsedMeaningfully(string varName, Dictionary<string, VariableInfo> variableUsage)
        {
            // Esta función verifica si una variable se usa de manera significativa
            // Por ahora, consideramos que si se imprime, se usa significativamente
            // En el futuro se podría expandir para verificar si se usa en cálculos que eventualmente se imprimen

            var info = variableUsage[varName];
            return info.IsPrinted;
        }

        private void AnalyzeExpression(string expression,
                                     string targetVar,
                                     Dictionary<string, VariableInfo> variableUsage,
                                     Dictionary<string, string> constantExpressions,
                                     int lineNumber)
        {
            // Contar usos de variables en la expresión
            var variablesInExpr = Regex.Matches(expression, @"\b([a-zA-Z_]\w*|t\d+)\b")
                                    .Cast<Match>()
                                    .Select(m => m.Value)
                                    .Where(v => !int.TryParse(v, out _) && !IsOperator(v))
                                    .Distinct(); // Evitar contar la misma variable múltiples veces en una expresión

            foreach (var varName in variablesInExpr)
            {
                if (!variableUsage.ContainsKey(varName))
                {
                    // Crear entrada para variable que se usa pero no se ha visto su asignación aún
                    variableUsage[varName] = new VariableInfo
                    {
                        Name = varName,
                        IsTemp = IsTempVariable(varName),
                        DeclarationLine = -1, // Marca que no se ha visto su declaración
                        UsageCount = 0,
                        IsAssigned = false,
                        IsPrinted = false
                    };
                }

                variableUsage[varName].UsageCount++;
                variableUsage[varName].UsedInLines.Add(lineNumber);
            }
        }

        private void CheckMathOperations(string expression, string originalLine, int lineNumber, List<string> advice)
        {
            // Patrones para operaciones matemáticas
            var mathPattern = @"(\w+|\d+)\s*([\+\-\*/])\s*(\w+|\d+)";
            var matches = Regex.Matches(expression, mathPattern);

            foreach (Match match in matches)
            {
                string op1 = match.Groups[1].Value;
                string oper = match.Groups[2].Value;
                string op2 = match.Groups[3].Value;

                switch (oper)
                {
                    case "*":
                        if (op1 == "1" || op2 == "1")
                        {
                            advice.Add($"🧮 Multiplicación redundante: Línea {lineNumber + 1}: '{originalLine}' → puede simplificarse eliminando la multiplicación por 1");
                        }
                        else if (op1 == "0" || op2 == "0")
                        {
                            advice.Add($"🧮 Multiplicación por cero: Línea {lineNumber + 1}: '{originalLine}' → resultado siempre será 0");
                        }
                        break;

                    case "/":
                        if (op2 == "1")
                        {
                            advice.Add($"🧮 División redundante: Línea {lineNumber + 1}: '{originalLine}' → puede simplificarse eliminando la división por 1");
                        }
                        else if (op2 == "0")
                        {
                            advice.Add($"⚠️ Peligro: División por cero en línea {lineNumber + 1}: '{originalLine}'");
                        }
                        else if (op1 == "0")
                        {
                            advice.Add($"🧮 División de cero: Línea {lineNumber + 1}: '{originalLine}' → resultado siempre será 0");
                        }
                        break;

                    case "+":
                        if (op1 == "0" || op2 == "0")
                        {
                            advice.Add($"🧮 Suma redundante: Línea {lineNumber + 1}: '{originalLine}' → puede simplificarse eliminando la suma con 0");
                        }
                        break;

                    case "-":
                        if (op2 == "0")
                        {
                            advice.Add($"🧮 Resta redundante: Línea {lineNumber + 1}: '{originalLine}' → puede simplificarse eliminando la resta de 0");
                        }
                        else if (op1 == op2)
                        {
                            advice.Add($"🧮 Resta de iguales: Línea {lineNumber + 1}: '{originalLine}' → resultado siempre será 0");
                        }
                        break;
                }
            }
        }

        private bool IsTempVariable(string varName)
        {
            return Regex.IsMatch(varName, @"^t\d+$");
        }

        private bool IsStandardFunction(string funcName)
        {
            return funcName == "println" || funcName == "print" || funcName == "main";
        }

        private bool IsOperator(string token)
        {
            return token == "+" || token == "-" || token == "*" || token == "/";
        }

        private string ExtractFunctionName(string line)
        {
            var match = Regex.Match(line, @"(println|print|\w+)");
            return match.Success ? match.Groups[1].Value : null;
        }

        private bool IsConstantExpression(string expression)
        {
            // Verifica si es una expresión que contiene solo números y operadores
            return Regex.IsMatch(expression, @"^(\d+(\s*[\+\-\*/]\s*\d+)*|\d+)$");
        }

        private string SimplifyConstantExpression(string expression)
        {
            try
            {
                var dataTable = new System.Data.DataTable();
                var result = dataTable.Compute(expression, null);
                return result.ToString();
            }
            catch
            {
                return expression;
            }
        }

        class VariableInfo
        {
            public string Name { get; set; }
            public bool IsTemp { get; set; }
            public int DeclarationLine { get; set; }
            public int UsageCount { get; set; }
            public bool IsAssigned { get; set; } = false;
            public bool IsPrinted { get; set; } = false; // Nueva propiedad
            public List<int> UsedInLines { get; set; } = new List<int>();
        }
    }
}