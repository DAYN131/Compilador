using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using Compilador.Generated;
using static Compilador.Generated.SiriusLanguageParser;

namespace Compilador
{
    public class SemanticAnalyzer : SiriusLanguageBaseVisitor<string>
    {
        #region Campos y Utilidades
        // ✅ MEJORA: Información más completa de símbolos
        private readonly Dictionary<string, SymbolInfo> _symbolTable = new Dictionary<string, SymbolInfo>();
        private readonly List<string> _errors = new List<string>();
        private readonly Stack<Dictionary<string, SymbolInfo>> _scopeStack = new Stack<Dictionary<string, SymbolInfo>>();

        // ✅ NUEVO: Información de funciones
        private readonly Dictionary<string, FunctionInfo> _functionTable = new Dictionary<string, FunctionInfo>();
        private string _currentFunctionReturnType = null;
        private bool _hasReturnStatement = false;

        // ✅ NUEVO: Clases para mejor organización
        public class SymbolInfo
        {
            public string Type { get; set; }
            public bool IsConstant { get; set; }
            public bool IsInitialized { get; set; }
            public int Line { get; set; }
            public int Column { get; set; }
        }

        public class FunctionInfo
        {
            public string ReturnType { get; set; }
            public List<ParameterInfo> Parameters { get; set; } = new List<ParameterInfo>();
            public int Line { get; set; }
            public int Column { get; set; }
        }

        public class ParameterInfo
        {
            public string Name { get; set; }
            public string Type { get; set; }
        }

        public static class DebugLogger
        {
            private static readonly string LogPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "compilador_log.txt");

            public static void Log(string message)
            {
                var fullMessage = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
                System.Diagnostics.Debug.WriteLine(fullMessage);
                File.AppendAllText(LogPath, fullMessage + Environment.NewLine);
            }
        }

        private void AddError(IToken token, string message)
        {
            string error = $"Línea {token.Line}:{token.Column} - {message}";
            DebugLogger.Log("Error semántico: " + error);
            _errors.Add(error);
        }

        public List<string> GetErrors() => _errors;

        // ✅ NUEVO: Gestión de ámbitos
        private void EnterScope()
        {
            _scopeStack.Push(new Dictionary<string, SymbolInfo>());
            DebugLogger.Log("Entrando a nuevo ámbito");
        }

        private void ExitScope()
        {
            if (_scopeStack.Count > 0)
            {
                _scopeStack.Pop();
                DebugLogger.Log("Saliendo del ámbito");
            }
        }

        private bool DeclareVariable(string name, SymbolInfo info)
        {
            var currentScope = _scopeStack.Count > 0 ? _scopeStack.Peek() : _symbolTable;

            if (currentScope.ContainsKey(name))
            {
                return false; // Ya existe en el ámbito actual
            }

            currentScope[name] = info;
            return true;
        }

        private SymbolInfo LookupVariable(string name)
        {
            // Buscar en ámbitos de dentro hacia fuera
            foreach (var scope in _scopeStack)
            {
                if (scope.TryGetValue(name, out var info))
                    return info;
            }

            // Buscar en ámbito global
            return _symbolTable.TryGetValue(name, out var globalInfo) ? globalInfo : null;
        }

        private void PrintTree(IParseTree node, int indent = 0)
        {
            var indentStr = new string(' ', indent * 2);
            DebugLogger.Log($"{indentStr}{node.GetType().Name}: {node.GetText()}");

            for (int i = 0; i < node.ChildCount; i++)
            {
                PrintTree(node.GetChild(i), indent + 1);
            }
        }
        #endregion

        #region Métodos Principales
        public override string VisitProgram(ProgramContext context)
        {
            DebugLogger.Log($"VisitProgram: Iniciando análisis del programa con {context.ChildCount} hijos");

            PrintTree(context);
            foreach (var child in context.children)
            {
                if (child is DeclarationContext decl)
                {
                    VisitDeclaration(decl);
                }
                else if (child is StatementContext stmt)
                {
                    VisitStatement(stmt);
                }
            }

            DebugLogger.Log("VisitProgram: Análisis semántico finalizado");
            return "Análisis semántico finalizado.";
        }


        public override string VisitStatement(StatementContext context)
        {
            DebugLogger.Log($"VisitStatement: {context.GetText()}");

            // Manejar todos los tipos de statement
            if (context.printStatement() != null)
            {
                return VisitPrintStatement(context.printStatement());
            }
            else if (context.ifStatement() != null)
            {
                return VisitIfStatement(context.ifStatement());
            }
            else if (context.forStatement() != null)
            {
                return VisitForStatement(context.forStatement());
            }
            else if (context.whileStatement() != null)
            {
                return VisitWhileStatement(context.whileStatement());
            }
            else if (context.returnStatement() != null)
            {
                return VisitReturnStatement(context.returnStatement());
            }
            else if (context.variableDeclaration() != null)
            {
                return VisitVariableDeclaration(context.variableDeclaration());
            }
            else if (context.block() != null)
            {
                return VisitBlock(context.block());
            }
            else if (context.expression() != null)
            {
                return GetExpressionType(context.expression());
            }

            return null;
        }
        #endregion

        #region Visitantes de Expresiones
        private string GetExpressionType(ExpressionContext exprContext)
        {
            DebugLogger.Log("Visitando Get Expression Type");
            DebugLogger.Log($"\n[GetExpressionType] INI - Recibí ExpressionContext: {exprContext?.GetText() ?? "null"}");
            DebugLogger.Log($"Tipo concreto: {exprContext?.GetType().Name ?? "null"}");

            if (exprContext == null)
            {
                DebugLogger.Log("[GetExpressionType] Contexto nulo");
                return null;
            }
            try
            {
                string type = VisitExpression(exprContext);
                DebugLogger.Log($"[GetExpressionType] END - Tipo obtenido: {type ?? "null"}");
                return type;
            }
            catch (Exception ex)
            {
                DebugLogger.Log($"[GetExpressionType] ERROR: {ex.Message}");
                return null;
            }
        }

        public override string VisitExpression(SiriusLanguageParser.ExpressionContext context)
        {
            DebugLogger.Log($"\n[VisitExpression] INI - Texto: {context.GetText()}");

            // Prioridad 1: Asignaciones
            if (context.assignment() != null)
            {
                return VisitAssignment(context.assignment());
            }

            // Prioridad 2: Lógica OR (incluye todos los casos de expresiones simples)
            return VisitLogicOr(context.logicOr());
        }

        // ✅ MEJORA: Validación de asignaciones
        public override string VisitAssignment(SiriusLanguageParser.AssignmentContext context)
        {
            DebugLogger.Log($"\n[VisitAssignment] INI - Texto: {context.GetText()}");

            var varName = context.IDENTIFIER().GetText();
            var variable = LookupVariable(varName);

            if (variable == null)
            {
                AddError(context.IDENTIFIER().Symbol, $"Variable no declarada: '{varName}'");
                return "error";
            }

            // ✅ VALIDACIÓN: No se pueden reasignar constantes
            if (variable.IsConstant)
            {
                AddError(context.IDENTIFIER().Symbol, $"No se puede reasignar la constante '{varName}'");
                return "error";
            }

            var exprType = GetExpressionType(context.expression());

            if (exprType != variable.Type)
            {
                AddError(context.Start, $"No se puede asignar '{exprType}' a variable de tipo '{variable.Type}'");
                return "error";
            }

            return variable.Type;
        }

        public override string VisitLogicOr(SiriusLanguageParser.LogicOrContext context)
        {
            DebugLogger.Log("Visitando LogicOr");
            DebugLogger.Log($"\n[VisitLogicOr] INI - Texto: {context.GetText()}");

            if (context.OR() == null || context.OR().Length == 0)
            {
                DebugLogger.Log("[VisitLogicOr] Sin operadores, visitando LogicAnd");
                return VisitLogicAnd((SiriusLanguageParser.LogicAndContext)context.logicAnd(0));
            }

            string leftType = VisitLogicAnd((SiriusLanguageParser.LogicAndContext)context.logicAnd(0));
            string rightType = VisitLogicAnd((SiriusLanguageParser.LogicAndContext)context.logicAnd(1));

            if (leftType != "bool" || rightType != "bool")
            {
                AddError(context.Start, "Los operandos del OR deben ser booleanos");
                return "error";
            }

            return "bool";
        }

        public override string VisitLogicAnd(SiriusLanguageParser.LogicAndContext context)
        {
            DebugLogger.Log("Visitando LogicAnd");
            DebugLogger.Log($"\n[VisitLogicAnd] INI - Texto: {context.GetText()}");

            if (context.AND() == null || context.AND().Length == 0)
            {
                DebugLogger.Log("[VisitLogicAnd] Sin operadores, visitando Equality");
                var result = VisitEquality((SiriusLanguageParser.EqualityContext)context.equality(0));
                DebugLogger.Log($"[VisitLogicAnd] Resultado de Equality: {result ?? "null"}");
                return result;
            }

            var leftType = VisitEquality((SiriusLanguageParser.EqualityContext)context.equality(0));
            var rightType = VisitEquality((SiriusLanguageParser.EqualityContext)context.equality(1));

            if (leftType != "bool" || rightType != "bool")
            {
                AddError(context.Start, "Los operandos del AND lógico deben ser booleanos");
                return "error";
            }

            return "bool";
        }

        public override string VisitEquality(SiriusLanguageParser.EqualityContext context)
        {
            DebugLogger.Log($"\n[VisitEquality] INI - Texto: {context.GetText()}");

            if (context.EQUAL() == null && context.NOTEQUAL() == null)
            {
                DebugLogger.Log("[VisitEquality] Sin operadores, visitando Comparison");
                var result = VisitComparison((SiriusLanguageParser.ComparisonContext)context.comparison(0));
                DebugLogger.Log($"[VisitEquality] Resultado de Comparison: {result ?? "null"}");
                return result;
            }

            var leftType = VisitComparison((SiriusLanguageParser.ComparisonContext)context.comparison(0));
            var rightType = VisitComparison((SiriusLanguageParser.ComparisonContext)context.comparison(1));

            if (leftType != rightType)
            {
                AddError(context.Start, $"No se pueden comparar tipos {leftType} y {rightType}");
                return "error";
            }

            return "bool";
        }

        public override string VisitComparison(SiriusLanguageParser.ComparisonContext context)
        {
            DebugLogger.Log($"\n[VisitComparison] INI - Texto: {context.GetText()}");

            if (context.LT() == null && context.GT() == null &&
                context.LTEQ() == null && context.GTEQ() == null)
            {
                DebugLogger.Log("[VisitComparison] Sin operadores, visitando Additive");
                var result = VisitAdditive((SiriusLanguageParser.AdditiveContext)context.additive(0));
                DebugLogger.Log($"[VisitComparison] Resultado de Additive: {result ?? "null"}");
                return result;
            }

            var leftType = VisitAdditive((SiriusLanguageParser.AdditiveContext)context.additive(0));
            var rightType = VisitAdditive((SiriusLanguageParser.AdditiveContext)context.additive(1));

            if ((leftType != "int" && leftType != "float") ||
                (rightType != "int" && rightType != "float"))
            {
                AddError(context.Start, "Las comparaciones solo funcionan con tipos numéricos");
                return "error";
            }

            return "bool";
        }

        public override string VisitAdditive(SiriusLanguageParser.AdditiveContext context)
        {
            DebugLogger.Log($"\n[VisitAdditive] INI - Texto: {context.GetText()}");

            var hasPlus = context.PLUS()?.Length > 0;
            var hasMinus = context.MINUS()?.Length > 0;
            var hasOperators = hasPlus || hasMinus;

            if (!hasOperators)
            {
                DebugLogger.Log("[VisitAdditive] Sin operadores, visitando Multiplicative");
                var result = VisitMultiplicative(context.multiplicative(0));
                DebugLogger.Log($"[VisitAdditive] Resultado de Multiplicative: {result ?? "null"}");
                return result;
            }

            var multiplicativeNodes = context.multiplicative();
            var operators = new List<ITerminalNode>();

            if (hasPlus) operators.AddRange(context.PLUS());
            if (hasMinus) operators.AddRange(context.MINUS());

            string currentType = VisitMultiplicative(multiplicativeNodes[0]);

            for (int i = 0; i < operators.Count; i++)
            {
                var rightType = VisitMultiplicative(multiplicativeNodes[i + 1]);

                if (currentType == "error" || rightType == "error")
                {
                    return "error";
                }

                if (currentType != rightType ||
                    (currentType != "int" && currentType != "float" && currentType != "str"))
                {
                    AddError(context.Start, $"Operación no válida entre {currentType} y {rightType}");
                    return "error";
                }

                currentType = rightType;
            }

            return currentType;
        }

        public override string VisitMultiplicative(SiriusLanguageParser.MultiplicativeContext context)
        {
            if (context == null)
            {
                DebugLogger.Log("[VisitMultiplicative] Error: Contexto nulo recibido");
                return "error";
            }

            DebugLogger.Log($"\n[VisitMultiplicative] INI - Texto: {context.GetText()}");

            var hasMultiply = context.MULTIPLY()?.Length > 0;
            var hasDivide = context.DIVIDE()?.Length > 0;
            var hasOperators = hasMultiply || hasDivide;

            DebugLogger.Log($"Operadores encontrados: MULT={hasMultiply}, DIV={hasDivide}");
            DebugLogger.Log($"Número de nodos unary: {context.unary().Length}");

            if (!hasOperators)
            {
                DebugLogger.Log("[VisitMultiplicative] Caso sin operadores - delegando a Unary");

                if (context.unary().Length != 1)
                {
                    DebugLogger.Log("[VisitMultiplicative] Error: Se esperaba exactamente 1 nodo unary");
                    AddError(context.Start, "Expresión multiplicativa incompleta");
                    return "error";
                }

                var result = VisitUnary(context.unary(0));
                DebugLogger.Log($"[VisitMultiplicative] Resultado de Unary: {result}");
                return result;
            }

            DebugLogger.Log("[VisitMultiplicative] Procesando operaciones múltiples");

            var unaryNodes = context.unary();
            var operators = new List<ITerminalNode>();

            if (hasMultiply) operators.AddRange(context.MULTIPLY());
            if (hasDivide) operators.AddRange(context.DIVIDE());

            if (unaryNodes.Length != operators.Count + 1)
            {
                AddError(context.Start, "Número incorrecto de operandos para operaciones multiplicativas");
                return "error";
            }

            string currentType = VisitUnary(unaryNodes[0]);

            for (int i = 0; i < operators.Count; i++)
            {
                var rightType = VisitUnary(unaryNodes[i + 1]);

                if (currentType == "error" || rightType == "error")
                {
                    return "error";
                }

                if ((currentType != "int" && currentType != "float") ||
                    (rightType != "int" && rightType != "float"))
                {
                    AddError(context.Start, $"Operación no válida entre {currentType} y {rightType}");
                    return "error";
                }

                currentType = (currentType == "float" || rightType == "float") ? "float" : "int";
            }

            DebugLogger.Log($"[VisitMultiplicative] Resultado final: {currentType}");
            return currentType;
        }

        public override string VisitUnary(SiriusLanguageParser.UnaryContext context)
        {
            DebugLogger.Log($"\n[VisitUnary] INI - Texto: {context.GetText()}");

            if (context.NOT() != null || context.MINUS() != null)
            {
                var operandType = VisitPrimary(context.primary());

                if (context.NOT() != null)
                {
                    if (operandType != "bool")
                    {
                        AddError(context.Start, "El operador NOT (!) requiere un booleano");
                        return "error";
                    }
                    return "bool";
                }
                else // MINUS
                {
                    if (operandType != "int" && operandType != "float")
                    {
                        AddError(context.Start, "El operador negativo (-) requiere un número");
                        return "error";
                    }
                    return operandType;
                }
            }

            return VisitPrimary(context.primary());
        }

        public override string VisitPrimary(SiriusLanguageParser.PrimaryContext context)
        {
            DebugLogger.Log($"\n[VisitPrimary] INI - Texto: {context.GetText()}");

            if (context.literal() != null)
            {
                var literalContext = context.literal();
                DebugLogger.Log($"[VisitPrimary] Tipo de literal encontrado: {literalContext.GetType().Name}");

                if (literalContext is SiriusLanguageParser.BooleanLiteralContext boolCtx)
                {
                    DebugLogger.Log("[VisitPrimary] Delegando a BooleanLiteral");
                    return VisitBooleanLiteral(boolCtx);
                }

                if (literalContext is SiriusLanguageParser.NumberLiteralContext numCtx)
                {
                    DebugLogger.Log("[VisitPrimary] Delegando a NumberLiteral");
                    return VisitNumberLiteral(numCtx);
                }

                if (literalContext is SiriusLanguageParser.StringLiteralContext strCtx)
                {
                    DebugLogger.Log("[VisitPrimary] Delegando a StringLiteral");
                    return VisitStringLiteral(strCtx);
                }

                DebugLogger.Log("[VisitPrimary] Fallback: usando Visit genérico");
                var result = Visit(literalContext);
                DebugLogger.Log($"[VisitPrimary] Resultado del Visit genérico: {result}");
                return result;
            }

            if (context.IDENTIFIER() != null)
            {
                var varName = context.IDENTIFIER().GetText();
                DebugLogger.Log($"[VisitPrimary] Buscando variable: {varName}");

                var symbol = LookupVariable(varName);
                if (symbol == null)
                {
                    AddError(context.Start, $"Variable no declarada: '{varName}'");
                    return "error";
                }

                DebugLogger.Log($"[VisitPrimary] Variable encontrada: {varName} -> {symbol.Type}");
                return symbol.Type;
            }

            if (context.expression() != null)
            {
                DebugLogger.Log("[VisitPrimary] Delegando a Expression");
                return VisitExpression(context.expression());
            }

            if (context.functionCall() != null)
            {
                DebugLogger.Log("[VisitPrimary] Delegando a FunctionCall");
                return VisitFunctionCall(context.functionCall());
            }

            DebugLogger.Log("[VisitPrimary] ERROR: Expresión primaria no reconocida");
            AddError(context.Start, "Expresión primaria no reconocida");
            return "error";
        }

        public override string VisitBooleanLiteral(SiriusLanguageParser.BooleanLiteralContext context)
        {
            DebugLogger.Log($"\n[VisitBooleanLiteral] INI - Texto: {context.GetText()}");
            return "bool";
        }

        public override string VisitNumberLiteral(SiriusLanguageParser.NumberLiteralContext context)
        {
            DebugLogger.Log($"\n[VisitNumberLiteral] INI - Texto: {context.GetText()}");
            var text = context.GetText();
            return text.Contains(".") ? "float" : "int";
        }

        public override string VisitStringLiteral(SiriusLanguageParser.StringLiteralContext context)
        {
            DebugLogger.Log($"\n[VisitStringLiteral] INI - Texto: {context.GetText()}");
            return "str";
        }
        #endregion

        #region Visitantes de Declaraciones
        public override string VisitDeclaration(DeclarationContext context)
        {
            DebugLogger.Log("VisitDeclaration: Visitando declaración");

            if (context.importDeclaration() != null)
                return VisitImportDeclaration(context.importDeclaration());
            if (context.variableDeclaration() != null)
                return VisitVariableDeclaration(context.variableDeclaration());
            if (context.functionDeclaration() != null)
                return VisitFunctionDeclaration(context.functionDeclaration());

            DebugLogger.Log("VisitDeclaration: Tipo de declaración no reconocido");
            return "Error: Tipo de declaración no reconocido";
        }

        // ✅ MEJORA: Validación mejorada de variables
        public override string VisitVariableDeclaration(VariableDeclarationContext context)
        {
            var isConstant = context.VAL() != null;
            var varName = context.IDENTIFIER().GetText();
            var declaredType = context.type()?.GetText();

            DebugLogger.Log($"VisitVariableDeclaration: {(isConstant ? "Constante" : "Variable")} {varName} de tipo {declaredType ?? "inferido"}");

            // ✅ VALIDACIÓN: Variable ya declarada en ámbito actual
            if (!DeclareVariable(varName, null)) // Temporal, se actualiza después
            {
                var errorMsg = $"La variable '{varName}' ya fue declarada en este ámbito";
                DebugLogger.Log(errorMsg);
                AddError(context.IDENTIFIER().Symbol, errorMsg);
                return null;
            }

            string exprType = GetExpressionType(context.expression());
            DebugLogger.Log($"VisitVariableDeclaration: Tipo inferido de expresión: {exprType}");

            if (declaredType != null && exprType != null && declaredType != exprType)
            {
                var errorMsg = $"Tipo declarado '{declaredType}' no coincide con tipo de expresión '{exprType}'";
                DebugLogger.Log(errorMsg);
                AddError(context.type().Start, errorMsg);
                return null;
            }

            var finalType = declaredType ?? exprType;
            if (finalType != null)
            {
                var symbolInfo = new SymbolInfo
                {
                    Type = finalType,
                    IsConstant = isConstant,
                    IsInitialized = true,
                    Line = context.Start.Line,
                    Column = context.Start.Column
                };

                // Actualizar la entrada
                var currentScope = _scopeStack.Count > 0 ? _scopeStack.Peek() : _symbolTable;
                currentScope[varName] = symbolInfo;

                DebugLogger.Log($"VisitVariableDeclaration: {varName} registrada como {finalType}");
            }

            return finalType;
        }

        // ✅ NUEVO: Validación de funciones
        public override string VisitFunctionDeclaration(FunctionDeclarationContext context)
        {
            var functionName = context.IDENTIFIER().GetText();
            var returnType = context.type()?.GetText() ?? "void";

            DebugLogger.Log($"VisitFunctionDeclaration: Función {functionName} con retorno {returnType}");

            // Verificar si la función ya existe
            if (_functionTable.ContainsKey(functionName))
            {
                AddError(context.IDENTIFIER().Symbol, $"La función '{functionName}' ya fue declarada");
                return "error";
            }

            // Crear información de la función
            var functionInfo = new FunctionInfo
            {
                ReturnType = returnType,
                Line = context.Start.Line,
                Column = context.Start.Column
            };

            // Procesar parámetros si existen
            if (context.parameterList() != null)
            {
                foreach (var param in context.parameterList().parameter())
                {
                    var paramName = param.IDENTIFIER().GetText();
                    var paramType = param.type().GetText();

                    // Verificar parámetros duplicados
                    if (functionInfo.Parameters.Any(p => p.Name == paramName))
                    {
                        AddError(param.IDENTIFIER().Symbol, $"Parámetro duplicado: '{paramName}'");
                    }
                    else
                    {
                        functionInfo.Parameters.Add(new ParameterInfo
                        {
                            Name = paramName,
                            Type = paramType
                        });
                    }
                }
            }

            _functionTable[functionName] = functionInfo;

            // Entrar a nuevo ámbito para la función
            EnterScope();
            _currentFunctionReturnType = returnType;
            _hasReturnStatement = false;

            // Declarar parámetros en el ámbito de la función
            foreach (var param in functionInfo.Parameters)
            {
                DeclareVariable(param.Name, new SymbolInfo
                {
                    Type = param.Type,
                    IsConstant = false,
                    IsInitialized = true,
                    Line = context.Start.Line,
                    Column = context.Start.Column
                });
            }

            // Visitar el cuerpo de la función
            if (context.block() != null)
            {
                VisitBlock(context.block());
            }

            // Verificar que funciones no-void tengan return
            if (returnType != "void" && !_hasReturnStatement)
            {
                AddError(context.Start, $"La función '{functionName}' debe tener una declaración return");
            }

            // Salir del ámbito
            ExitScope();
            _currentFunctionReturnType = null;
            _hasReturnStatement = false;

            return returnType;
        }

        // ✅ NUEVO: Validación de llamadas a funciones
        public override string VisitFunctionCall(FunctionCallContext context)
        {
            var functionName = context.IDENTIFIER().GetText();
            DebugLogger.Log($"VisitFunctionCall: Llamada a función {functionName}");

            if (!_functionTable.TryGetValue(functionName, out var functionInfo))
            {
                AddError(context.IDENTIFIER().Symbol, $"Función no declarada: '{functionName}'");
                return "error";
            }

            // Obtener los argumentos directamente del contexto
            var providedArgs = context.expression() ?? new ExpressionContext[0];
            var expectedArgs = functionInfo.Parameters.Count;

            // Verificar número de argumentos
            if (providedArgs.Length != expectedArgs)
            {
                AddError(context.Start, $"La función '{functionName}' espera {expectedArgs} argumentos, pero se proporcionaron {providedArgs.Length}");
                return "error";
            }

            // Verificar tipos de argumentos
            for (int i = 0; i < providedArgs.Length; i++)
            {
                var argType = GetExpressionType(providedArgs[i]);
                var expectedType = functionInfo.Parameters[i].Type;

                if (argType != expectedType)
                {
                    AddError(providedArgs[i].Start,
                        $"Argumento {i + 1}: se esperaba '{expectedType}' pero se recibió '{argType}'");
                }
            }

            return functionInfo.ReturnType;
        }
        #endregion

        #region Visitantes de Statements

        // ✅ MEJORA: Validación completa de if/else
        public override string VisitIfStatement(IfStatementContext context)
        {
            DebugLogger.Log($"\n===== Visitando el IfStatement =====");
            DebugLogger.Log($"Texto completo: {context.GetText()}");

            // 1. Validar condición del if
            var ifCondition = context.expression(0);
            string ifCondType = GetExpressionType(ifCondition);

            DebugLogger.Log($"[VisitIfStatement] Tipo obtenido de la condición: {ifCondType ?? "null"}");

            if (ifCondType != "bool")
            {
                AddError(ifCondition.Start,
                    ifCondType == null
                        ? "No se pudo determinar el tipo de la condición"
                        : $"La condición debe ser booleana, no {ifCondType}");
                return "error";
            }

            // 2. Visitar bloque del if
            EnterScope();
            VisitBlock(context.block(0));
            ExitScope();

            // 3. Si hay else, validarlo también
            if (context.block().Length > 1) // Tiene else
            {
                EnterScope();
                VisitBlock(context.block(1));
                ExitScope();
            }

            return "void";
        }

        // ✅ NUEVO: Validación de loops for
        public override string VisitForStatement(ForStatementContext context)
        {
            DebugLogger.Log($"VisitForStatement: {context.GetText()}");
            EnterScope(); // Nuevo ámbito para el for

            int expressionIndex = 0; // Índice para rastrear qué expresión estamos procesando

            // 1. Validar inicialización (si existe)
            if (context.variableDeclaration() != null)
            {
                // Si hay variableDeclaration, las expresiones empiezan desde índice 0
                VisitVariableDeclaration(context.variableDeclaration());
                DebugLogger.Log("Inicialización: variableDeclaration procesada");
            }
            else if (context.expression().Length > expressionIndex)
            {
                // Si no hay variableDeclaration, la primera expresión es la inicialización
                var firstExpr = context.expression(expressionIndex);
                if (IsAssignmentExpression(firstExpr))
                {
                    VisitExpression(firstExpr);
                    DebugLogger.Log($"Inicialización: expresión procesada - {firstExpr.GetText()}");
                }
                else
                {
                    GetExpressionType(firstExpr);
                    DebugLogger.Log($"Inicialización: expresión evaluada - {firstExpr.GetText()}");
                }
                expressionIndex++; // Incrementar índice porque usamos una expresión
            }

            // 2. Validar condición (la siguiente expresión disponible)
            if (context.expression().Length > expressionIndex)
            {
                var conditionExpr = context.expression(expressionIndex);
                var conditionType = GetExpressionType(conditionExpr);
                DebugLogger.Log($"Condición: {conditionExpr.GetText()} -> tipo: {conditionType}");

                if (conditionType != "bool")
                {
                    AddError(conditionExpr.Start, "La condición del for debe ser booleana");
                }
                expressionIndex++; // Incrementar índice
            }

            // 3. Validar incremento (la última expresión disponible)
            if (context.expression().Length > expressionIndex)
            {
                var incrementExpr = context.expression(expressionIndex);
                GetExpressionType(incrementExpr); // Solo validar, no importa el tipo
                DebugLogger.Log($"Incremento: {incrementExpr.GetText()} procesado");
            }

            // 4. Validar cuerpo del loop
            if (context.block() != null)
            {
                VisitBlock(context.block());
            }

            ExitScope();
            return "void";
        }

        // Método auxiliar para verificar si una expresión es un assignment
        private bool IsAssignmentExpression(ExpressionContext context)
        {
            // Verificar si la expresión contiene la regla assignment
            return context.assignment() != null;
        }

        // ✅ NUEVO: Validación de loops while
        public override string VisitWhileStatement(WhileStatementContext context)
        {
            DebugLogger.Log($"VisitWhileStatement: {context.GetText()}");

            // 1. Validar condición
            var conditionType = GetExpressionType(context.expression());
            if (conditionType != "bool")
            {
                AddError(context.expression().Start, "La condición del while debe ser booleana");
                return "error";
            }

            // 2. Validar cuerpo del loop
            EnterScope();
            if (context.block() != null)
            {
                VisitBlock(context.block());
            }
            ExitScope();

            return "void";
        }

        // ✅ NUEVO: Validación de statements return
        public override string VisitReturnStatement(ReturnStatementContext context)
        {
            DebugLogger.Log($"VisitReturnStatement: {context.GetText()}");

            _hasReturnStatement = true;

            if (_currentFunctionReturnType == null)
            {
                AddError(context.Start, "Return statement fuera de una función");
                return "error";
            }

            // Caso 1: return sin valor
            if (context.expression() == null)
            {
                if (_currentFunctionReturnType != "void")
                {
                    AddError(context.Start, $"Se esperaba retornar un valor de tipo '{_currentFunctionReturnType}'");
                }
                return "void";
            }

            // Caso 2: return con valor
            var returnType = GetExpressionType(context.expression());

            if (_currentFunctionReturnType == "void")
            {
                AddError(context.Start, "No se puede retornar un valor en una función void");
                return "error";
            }

            if (returnType != _currentFunctionReturnType)
            {
                AddError(context.expression().Start,
                    $"Tipo de retorno incorrecto: se esperaba '{_currentFunctionReturnType}' pero se obtuvo '{returnType}'");
                return "error";
            }

            return returnType;
        }

        // ✅ NUEVO: Validación de print statements
        public override string VisitPrintStatement(PrintStatementContext context)
        {
            DebugLogger.Log($"VisitPrintStatement: {context.GetText()}");

            if (context.expression() != null)
            {
                var exprType = GetExpressionType(context.expression());
                DebugLogger.Log($"Print de expresión tipo: {exprType}");
            }

            return "void";
        }

        public override string VisitBlock(BlockContext context)
        {
            if (context == null) return null;

            DebugLogger.Log($"\n[Bloque] Inicio - Texto: {context.GetText()}");

            foreach (var stmt in context.statement())
            {
                DebugLogger.Log($"Procesando statement: {stmt.GetText()}");
                VisitStatement(stmt);
            }

            return null;
        }

        // ✅ NUEVO: Placeholder para import (si lo necesitas)
        public override string VisitImportDeclaration(ImportDeclarationContext context)
        {
            DebugLogger.Log($"VisitImportDeclaration: {context.GetText()}");
            // TODO: Implementar validación de imports según tu gramática
            return "void";
        }
        #endregion

        #region Métodos de Utilidad para Testing

        // ✅ NUEVO: Métodos útiles para debugging y testing
        public void PrintSymbolTable()
        {
            DebugLogger.Log("\n=== TABLA DE SÍMBOLOS ===");
            foreach (var symbol in _symbolTable)
            {
                DebugLogger.Log($"{symbol.Key}: {symbol.Value.Type} (Const: {symbol.Value.IsConstant}, Línea: {symbol.Value.Line})");
            }
        }

        public void PrintFunctionTable()
        {
            DebugLogger.Log("\n=== TABLA DE FUNCIONES ===");
            foreach (var func in _functionTable)
            {
                var paramStr = string.Join(", ", func.Value.Parameters.Select(p => $"{p.Type} {p.Name}"));
                DebugLogger.Log($"{func.Key}({paramStr}) -> {func.Value.ReturnType}");
            }
        }

        public bool HasErrors() => _errors.Count > 0;

        public void ClearErrors() => _errors.Clear();
        #endregion
    }
}