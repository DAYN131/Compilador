using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Antlr4.Runtime.Tree;
using Compilador.Generated;
using static Compilador.Generated.SiriusLanguageParser;

namespace Compilador
{
    public class ThreeAddressCodeGenerator : SiriusLanguageBaseVisitor<string>
    {
        #region Campos y Propiedades
        private int _tempCounter = 0;
        private int _labelCounter = 0;
        private readonly List<string> _instructions = new List<string>();

        // Reutilizar la información del análisis semántico
        private readonly SemanticAnalyzer _semanticAnalyzer;
        private string _currentFunction = null;

        // Cache para temporales de expresiones (evitar recalcular)
        private readonly Dictionary<string, string> _expressionTemps = new Dictionary<string, string>();
        #endregion

        #region Constructor
        public ThreeAddressCodeGenerator(SemanticAnalyzer semanticAnalyzer)
        {
            _semanticAnalyzer = semanticAnalyzer ?? throw new ArgumentNullException(nameof(semanticAnalyzer));

            // Verificar que el análisis semántico fue exitoso
            if (_semanticAnalyzer.HasErrors())
            {
                throw new InvalidOperationException("No se puede generar código intermedio con errores semánticos");
            }
        }
        #endregion

        #region Métodos de Utilidad
        private string NewTemp()
        {
            return $"t{++_tempCounter}";
        }

        private string NewLabel()
        {
            return $"L{++_labelCounter}";
        }

        private void Emit(string instruction)
        {
            _instructions.Add(instruction);
            SemanticAnalyzer.DebugLogger.Log($"[CodeGen] {instruction}");
        }

        public List<string> GetGeneratedCode() => _instructions;

        public void SaveToFile()
        {
            // Obtener la ruta del escritorio
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string filePath = Path.Combine(desktopPath, "output.tac");

            try
            {
                File.WriteAllLines(filePath, _instructions);
                SemanticAnalyzer.DebugLogger.Log($"Código intermedio guardado automáticamente en: {filePath}");
            }
            catch (Exception ex)
            {
                SemanticAnalyzer.DebugLogger.Log($"Error al guardar archivo: {ex.Message}");
                throw;
            }
        }
        #endregion

        #region Visitantes Principales
        public override string VisitProgram(ProgramContext context)
        {
            SemanticAnalyzer.DebugLogger.Log("=== GENERACIÓN DE CÓDIGO INTERMEDIO ===");

            // Generar código para todas las declarations y statements
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

            return "Code generation completed";
        }

        public override string VisitDeclaration(DeclarationContext context)
        {
            if (context.variableDeclaration() != null)
                return VisitVariableDeclaration(context.variableDeclaration());
            else if (context.functionDeclaration() != null)
                return VisitFunctionDeclaration(context.functionDeclaration());
            else if (context.importDeclaration() != null)
                return VisitImportDeclaration(context.importDeclaration());

            return null;
        }

        public override string VisitVariableDeclaration(VariableDeclarationContext context)
        {
            var varName = context.IDENTIFIER().GetText();

            // Si hay inicialización, generar código para la expresión
            if (context.expression() != null)
            {
                string temp = VisitExpression(context.expression());
                Emit($"{varName} = {temp}");
            }
            else
            {
                // Inicialización por defecto según el tipo
                var varType = context.type()?.GetText();
                string defaultValue = GetDefaultValue(varType);
                if (defaultValue != null)
                {
                    Emit($"{varName} = {defaultValue}");
                }
            }

            return varName;
        }

        public override string VisitFunctionDeclaration(FunctionDeclarationContext context)
        {
            var funcName = context.IDENTIFIER().GetText();
            _currentFunction = funcName;

            Emit($"FUNC_BEGIN {funcName}");

            // Parámetros
            if (context.parameterList() != null)
            {
                foreach (var param in context.parameterList().parameter())
                {
                    var paramName = param.IDENTIFIER().GetText();
                    Emit($"PARAM {paramName}");
                }
            }

            // Cuerpo de la función
            if (context.block() != null)
            {
                VisitBlock(context.block());
            }

            Emit($"FUNC_END {funcName}");
            _currentFunction = null;

            return funcName;
        }

        public override string VisitImportDeclaration(ImportDeclarationContext context)
        {
            // Los imports no generan código intermedio, solo un comentario
            var importName = context.IDENTIFIER().GetText();
            Emit($"COMMENT Import: {importName}");
            return null;
        }
        #endregion

        #region Visitantes de Statements
        public override string VisitStatement(StatementContext context)
        {
            if (context.printStatement() != null)
                return VisitPrintStatement(context.printStatement());
            else if (context.ifStatement() != null)
                return VisitIfStatement(context.ifStatement());
            else if (context.forStatement() != null)
                return VisitForStatement(context.forStatement());
            else if (context.whileStatement() != null)
                return VisitWhileStatement(context.whileStatement());
            else if (context.returnStatement() != null)
                return VisitReturnStatement(context.returnStatement());
            else if (context.variableDeclaration() != null)
                return VisitVariableDeclaration(context.variableDeclaration());
            else if (context.block() != null)
                return VisitBlock(context.block());
            else if (context.expression() != null)
                return VisitExpression(context.expression());

            return null;
        }

        public override string VisitPrintStatement(PrintStatementContext context)
        {
            string temp = VisitExpression(context.expression());
            string printFunc = context.PRINT() != null ? "print" : "println";
            Emit($"{printFunc} {temp}");
            return null;
        }

        public override string VisitIfStatement(IfStatementContext context)
        {
            // Generar código para la condición
            string conditionTemp = VisitExpression(context.expression(0));
            string falseLabel = NewLabel();
            string endLabel = NewLabel();

            Emit($"IF_FALSE {conditionTemp} GOTO {falseLabel}");

            // Código del bloque then
            VisitBlock(context.block(0));

            // Si hay else
            if (context.block().Length > 1)
            {
                Emit($"GOTO {endLabel}");
                Emit($"{falseLabel}:");
                VisitBlock(context.block(1));
                Emit($"{endLabel}:");
            }
            else
            {
                Emit($"{falseLabel}:");
            }

            return null;
        }

        public override string VisitForStatement(ForStatementContext context)
        {
            string startLabel = NewLabel();
            string endLabel = NewLabel();
            string continueLabel = NewLabel();

            // Inicialización
            if (context.variableDeclaration() != null)
            {
                VisitVariableDeclaration(context.variableDeclaration());
            }
            else if (context.expression().Length > 0)
            {
                VisitExpression(context.expression(0));
            }

            Emit($"{startLabel}:");

            // Condición (segunda expresión o primera si no hay inicialización)
            int conditionIndex = context.variableDeclaration() != null ? 0 : 1;
            if (context.expression().Length > conditionIndex)
            {
                string condTemp = VisitExpression(context.expression(conditionIndex));
                Emit($"IF_FALSE {condTemp} GOTO {endLabel}");
            }

            // Cuerpo del for
            VisitBlock(context.block());

            Emit($"{continueLabel}:");

            // Incremento (última expresión)
            if (context.expression().Length > 0)
            {
                int incrementIndex = context.expression().Length - 1;
                if (incrementIndex != conditionIndex) // Evitar duplicar la condición
                {
                    VisitExpression(context.expression(incrementIndex));
                }
            }

            Emit($"GOTO {startLabel}");
            Emit($"{endLabel}:");

            return null;
        }

        public override string VisitWhileStatement(WhileStatementContext context)
        {
            string startLabel = NewLabel();
            string endLabel = NewLabel();

            Emit($"{startLabel}:");

            string condTemp = VisitExpression(context.expression());
            Emit($"IF_FALSE {condTemp} GOTO {endLabel}");

            VisitBlock(context.block());

            Emit($"GOTO {startLabel}");
            Emit($"{endLabel}:");

            return null;
        }

        public override string VisitReturnStatement(ReturnStatementContext context)
        {
            if (context.expression() != null)
            {
                string temp = VisitExpression(context.expression());
                Emit($"RETURN {temp}");
            }
            else
            {
                Emit("RETURN");
            }

            return null;
        }

        public override string VisitBlock(BlockContext context)
        {
            foreach (var stmt in context.statement())
            {
                VisitStatement(stmt);
            }
            return null;
        }
        #endregion

        #region Visitantes de Expresiones
        public override string VisitExpression(ExpressionContext context)
        {
            // Usar cache para evitar recalcular expresiones
            string contextKey = context.GetText();
            if (_expressionTemps.TryGetValue(contextKey, out string cachedTemp))
            {
                return cachedTemp;
            }

            string result;

            if (context.assignment() != null)
            {
                result = VisitAssignment(context.assignment());
            }
            else
            {
                result = VisitLogicOr(context.logicOr());
            }

            // Cachear el resultado
            if (result != null)
            {
                _expressionTemps[contextKey] = result;
            }

            return result;
        }

        public override string VisitAssignment(AssignmentContext context)
        {
            var varName = context.IDENTIFIER().GetText();
            string temp = VisitExpression(context.expression());
            Emit($"{varName} = {temp}");
            return varName;
        }

        public override string VisitLogicOr(LogicOrContext context)
        {
            string left = VisitLogicAnd(context.logicAnd(0));

            for (int i = 1; i < context.logicAnd().Length; i++)
            {
                string right = VisitLogicAnd(context.logicAnd(i));
                string temp = NewTemp();
                Emit($"{temp} = {left} OR {right}");
                left = temp;
            }

            return left;
        }

        public override string VisitLogicAnd(LogicAndContext context)
        {
            string left = VisitEquality(context.equality(0));

            for (int i = 1; i < context.equality().Length; i++)
            {
                string right = VisitEquality(context.equality(i));
                string temp = NewTemp();
                Emit($"{temp} = {left} AND {right}");
                left = temp;
            }

            return left;
        }

        public override string VisitEquality(EqualityContext context)
        {
            string left = VisitComparison(context.comparison(0));

            for (int i = 1; i < context.comparison().Length; i++)
            {
                string op = context.EQUAL() != null ? "==" : "!=";
                string right = VisitComparison(context.comparison(i));
                string temp = NewTemp();
                Emit($"{temp} = {left} {op} {right}");
                left = temp;
            }

            return left;
        }

        public override string VisitComparison(ComparisonContext context)
        {
            string left = VisitAdditive(context.additive(0));

            for (int i = 1; i < context.additive().Length; i++)
            {
                // Determinar el operador
                string op = "?";
                if (context.LT() != null) op = "<";
                else if (context.GT() != null) op = ">";
                else if (context.LTEQ() != null) op = "<=";
                else if (context.GTEQ() != null) op = ">=";

                string right = VisitAdditive(context.additive(i));
                string temp = NewTemp();
                Emit($"{temp} = {left} {op} {right}");
                left = temp;
            }

            return left;
        }

        public override string VisitAdditive(AdditiveContext context)
        {
            string left = VisitMultiplicative(context.multiplicative(0));

            var operators = new List<string>();
            if (context.PLUS() != null) operators.AddRange(context.PLUS().Select(p => "+"));
            if (context.MINUS() != null) operators.AddRange(context.MINUS().Select(m => "-"));

            for (int i = 1; i < context.multiplicative().Length; i++)
            {
                string op = operators[i - 1];
                string right = VisitMultiplicative(context.multiplicative(i));
                string temp = NewTemp();
                Emit($"{temp} = {left} {op} {right}");
                left = temp;
            }

            return left;
        }

        public override string VisitMultiplicative(MultiplicativeContext context)
        {
            string left = VisitUnary(context.unary(0));

            var operators = new List<string>();
            if (context.MULTIPLY() != null) operators.AddRange(context.MULTIPLY().Select(m => "*"));
            if (context.DIVIDE() != null) operators.AddRange(context.DIVIDE().Select(d => "/"));

            for (int i = 1; i < context.unary().Length; i++)
            {
                string op = operators[i - 1];
                string right = VisitUnary(context.unary(i));
                string temp = NewTemp();
                Emit($"{temp} = {left} {op} {right}");
                left = temp;
            }

            return left;
        }

        public override string VisitUnary(UnaryContext context)
        {
            string operand = VisitPrimary(context.primary());

            if (context.NOT() != null)
            {
                string temp = NewTemp();
                Emit($"{temp} = NOT {operand}");
                return temp;
            }
            else if (context.MINUS() != null)
            {
                string temp = NewTemp();
                Emit($"{temp} = -{operand}");
                return temp;
            }

            return operand;
        }

        public override string VisitPrimary(PrimaryContext context)
        {
            // Manejo de literales como en tu solución semántica
            if (context.literal() != null)
            {
                if (context.literal() is BooleanLiteralContext boolCtx)
                {
                    return boolCtx.GetText(); // Devuelve "true" o "false" directamente
                }
                else if (context.literal() is NumberLiteralContext numCtx)
                {
                    string temp = NewTemp();
                    Emit($"{temp} = {numCtx.NUMBER().GetText()}");
                    return temp;
                }
                else if (context.literal() is StringLiteralContext strCtx)
                {
                    string temp = NewTemp();
                    Emit($"{temp} = {strCtx.STRING().GetText()}");
                    return temp;
                }
            }
            // Para identificadores (variables)
            else if (context.IDENTIFIER() != null)
            {
                return context.IDENTIFIER().GetText();
            }
            // Para expresiones entre paréntesis
            else if (context.expression() != null)
            {
                return VisitExpression(context.expression());
            }
            // Para llamadas a función
            else if (context.functionCall() != null)
            {
                return VisitFunctionCall(context.functionCall());
            }

            return null;
        }

        public override string VisitFunctionCall(FunctionCallContext context)
        {
            string funcName = context.IDENTIFIER().GetText();
            List<string> args = new List<string>();

            // Procesar argumentos
            if (context.expression() != null && context.expression().Length > 0)
            {
                foreach (var expr in context.expression())
                {
                    args.Add(VisitExpression(expr));
                }
            }

            // Generar llamada a función
            string temp = NewTemp();
            string argsStr = args.Count > 0 ? string.Join(", ", args) : "";
            Emit($"{temp} = CALL {funcName}({argsStr})");

            return temp;
        }
        #endregion

        #region Métodos de Utilidad
        private string GetDefaultValue(string type)
        {
            if (type == "int")
                return "0";
            else if (type == "float")
                return "0.0";
            else if (type == "bool")
                return "false";
            else if (type == "str")
                return "\"\"";
            else
                return null;
        }

        public void PrintGeneratedCode()
        {
            SemanticAnalyzer.DebugLogger.Log("\n=== CÓDIGO INTERMEDIO GENERADO ===");
            for (int i = 0; i < _instructions.Count; i++)
            {
                SemanticAnalyzer.DebugLogger.Log($"{i + 1:D3}: {_instructions[i]}");
            }
        }

        public void Reset()
        {
            _tempCounter = 0;
            _labelCounter = 0;
            _instructions.Clear();
            _expressionTemps.Clear();
            _currentFunction = null;
        }
        #endregion
    }
}