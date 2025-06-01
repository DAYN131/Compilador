using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Compilador
{
    class SimpleTACOptimizer
    {
        public List<string> AnalyzeCode(List<string> tacLines)
        {
            var advice = new List<string>();
            var variableUsage = new Dictionary<string, int>();
            var tempVars = new HashSet<string>();

            foreach (var line in tacLines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var assignmentParts = line.Split('=');
                if (assignmentParts.Length == 2)
                {
                    string leftVar = assignmentParts[0].Trim();
                    string rightExpr = assignmentParts[1].Trim();

                    if (!variableUsage.ContainsKey(leftVar))
                        variableUsage[leftVar] = 0;

                    if (Regex.IsMatch(leftVar, @"^t\d+$"))
                        tempVars.Add(leftVar);

                    CheckMathOperations(rightExpr, line, advice);
                }

                string lineWithoutStrings = Regex.Replace(line, @"""[^""]*""", "");
                var tokens = Regex.Matches(lineWithoutStrings, @"\b[a-zA-Z_]\w*\b")
                                .Cast<Match>()
                                .Select(m => m.Value)
                                .Where(v => !int.TryParse(v, out _) && v != "true" && v != "false");

                foreach (var token in tokens)
                {
                    if (!variableUsage.ContainsKey(token))
                        variableUsage[token] = 1;
                    else
                        variableUsage[token]++;
                }
            }

            foreach (var varEntry in variableUsage)
            {
                string varName = varEntry.Key;
                int usageCount = varEntry.Value;

                if (tempVars.Contains(varName)) continue;
                if (usageCount > 1) continue;

                if (usageCount == 1)
                {
                    advice.Add($"⚠️ Variable '{varName}' se declara pero solo se usa una vez (¿asignación sin uso?)");
                }
                else if (usageCount == 0)
                {
                    advice.Add($"🗑️ Variable '{varName}' se declara pero nunca se usa");
                }
            }

            return advice;
        }

        private void CheckMathOperations(string expression, string originalLine, List<string> advice)
        {
            // Patrones para detectar operaciones matemáticas
            var mathPattern = @"(\w+|\d+)\s*([\*/])\s*(\w+|\d+)";
            var matches = Regex.Matches(expression, mathPattern);

            foreach (Match match in matches)
            {
                string op1 = match.Groups[1].Value;
                string oper = match.Groups[2].Value;
                string op2 = match.Groups[3].Value;

                if (oper == "*")
                {
                    if (op1 == "1" || op2 == "1")
                    {
                        string replacement = op1 == "1" ? op2 : op1;
                        advice.Add($"🧮 Multiplicación redundante: '{originalLine}' → puede simplificarse a '{replacement}'");
                    }
                    else if (op1 == "0" || op2 == "0")
                    {
                        advice.Add($"🧮 Multiplicación por cero: '{originalLine}' → resultado siempre será 0");
                    }
                }
                else if (oper == "/")
                {
                    if (op2 == "1")
                    {
                        advice.Add($"🧮 División redundante: '{originalLine}' → puede simplificarse a '{op1}'");
                    }
                    else if (op2 == "0")
                    {
                        advice.Add($"⚠️ Peligro: División por cero en '{originalLine}' → error en tiempo de ejecución");
                    }
                    else if (op1 == "0")
                    {
                        advice.Add($"🧮 División de cero: '{originalLine}' → resultado siempre será 0 (si {op2} ≠ 0)");
                    }
                }
            }

            // Detectar plegado de constantes (operaciones con números constantes)
            var constPattern = @"(\d+)\s*([\+\-\*/])\s*(\d+)";
            var constMatch = Regex.Match(expression, constPattern);
            if (constMatch.Success)
            {
                int n1 = int.Parse(constMatch.Groups[1].Value);
                int n2 = int.Parse(constMatch.Groups[3].Value);
                string op = constMatch.Groups[2].Value;
                int result;

                try
                {
                    switch (op)
                    {
                        case "+":
                            result = n1 + n2;
                            break;
                        case "-":
                            result = n1 - n2;
                            break;
                        case "*":
                            result = n1 * n2;
                            break;
                        case "/":
                            if (n2 == 0)
                                throw new DivideByZeroException();
                            result = n1 / n2;
                            break;
                        default:
                            throw new NotImplementedException();
                    }

                    advice.Add($"🔢 Plegado de constantes posible: '{originalLine}' → podría simplificarse a '= {result}'");
                }
                catch (DivideByZeroException)
                {
                    advice.Add($"⚠️ Peligro: División por cero constante en '{originalLine}'");
                }
            }
        }
    }

}
