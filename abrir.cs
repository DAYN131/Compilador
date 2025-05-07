using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Compilador.Generated;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;


namespace Compilador
{
    public partial class abrir : Form
    {
        // Constructor y manejo de archivos
        public abrir(string contenido, string rutaArchivo)
        {
  
            InitializeComponent();
            label1.Text = rutaArchivo;
            codigo.Text = contenido ?? "";

       

            // Ajusta el tamaño del formulario
            this.WindowState = FormWindowState.Maximized;
            this.MinimumSize = new Size(900, 600);
        }



        #region Manejo de Archivos
        private void guardarToolStripMenuItem1_Click_1(object sender, EventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Seleccionar Archivo",
                Filter = "Archivos SIR (*.sir)|*.sir",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK && openFileDialog.FileName.EndsWith(".sir"))
            {
                string contenido = File.ReadAllText(openFileDialog.FileName);
                var f1 = new abrir(contenido, openFileDialog.FileName)
                {
                    Visible = true,
                    Text = openFileDialog.FileName
                };
                this.Visible = false;
            }
            else
            {
                MessageBox.Show("El archivo seleccionado no es un archivo .sir", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void guardarToolStripMenuItem2_Click_1(object sender, EventArgs e)
        {
            var f1 = new abrir("", "Nuevo archivo")
            {
                Visible = true,
                Text = "Nuevo archivo"
            };
            this.Visible = false;
        }

        private void guardarToolStripMenuItem3_Click_1(object sender, EventArgs e)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Title = "Guardar Archivo",
                Filter = "Todos los archivos (*.sir*)|*.sir*",
                DefaultExt = "sir",
                AddExtension = true,
            };

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(saveFileDialog.FileName, codigo.Text);
                label1.Text = saveFileDialog.FileName;
                this.Text = saveFileDialog.FileName;
            }
        }

        private void guardarToolStripMenuItem4_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(label1.Text))
            {
                File.WriteAllText(label1.Text, codigo.Text);
            }
        }
        #endregion

        #region ANTLR Integration
        private SiriusLanguageParser SetupAntlrParser()
        {
            var inputStream = new AntlrInputStream(codigo.Text);
            var lexer = new SiriusLanguageLexer(inputStream);
            return new SiriusLanguageParser(new CommonTokenStream(lexer));
        }
        #endregion

        #region Tokenización y Coloreado
        private void tokenizarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var parser = SetupAntlrParser();
                var tokenStream = (CommonTokenStream)parser.InputStream;
                tokenStream.Fill();

                DisplayTokens(tokenStream);
                ColorizeTokensAntlr(tokenStream.GetTokens());
                MessageBox.Show($"Tokenización completada. Total tokens: {tokenStream.GetTokens().Count - 1}",
                              "Tokenización", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DisplayTokens(CommonTokenStream tokenStream)
        {
            tokentipebox.Items.Clear();
            tokenbox.Items.Clear();
            posicion1.Items.Clear();

            foreach (var token in tokenStream.GetTokens())
            {
                if (token.Type == -1) continue;

                tokentipebox.Items.Add(SiriusLanguageLexer.DefaultVocabulary.GetSymbolicName(token.Type));
                tokenbox.Items.Add(token.Text);
                posicion1.Items.Add($"Línea {token.Line}:{token.Column}");
            }
        }

        private void ColorizeTokensAntlr(IList<IToken> tokens)
        {
            int originalPos = codigo.SelectionStart;
            codigo.SelectAll();
            codigo.SelectionColor = Color.White;
            codigo.DeselectAll();

            foreach (var token in tokens)
            {
                if (token.Type == -1) continue;

                int start = GetPositionInTextBox(token.Line, token.Column);
                int length = token.StopIndex - token.StartIndex + 1;

                if (start >= 0 && start + length <= codigo.Text.Length)
                {
                    codigo.Select(start, length);
                    codigo.SelectionColor = GetTokenColor(token);
                }
            }

            codigo.SelectionStart = originalPos;
            codigo.SelectionLength = 0;
        }

        private Color GetTokenColor(IToken token)
        {
            var tokenType = SiriusLanguageLexer.DefaultVocabulary.GetSymbolicName(token.Type);

            switch (tokenType)
            {
                case "VAR":
                case "VAL":
                    return Color.DodgerBlue;

                case "FUN":
                case "FOR":
                case "WHILE":
                case "IF":
                case "ELSE":
                case "RETURN":
                    return Color.Blue;

                case "PRINT":
                case "PRINTLN":
                    return Color.Cyan;

                case "NUMBER":
                    return Color.LightGreen;

                case "STRING":
                    return Color.Orange;

                case "TYPE_INT":
                case "TYPE_STR":
                case "TYPE_BOOL":
                    return Color.LightSkyBlue;

                case "PLUS":
                case "MINUS":
                case "MULTIPLY":
                case "DIVIDE":
                case "ASSIGN":
                    return Color.Gold;

                case "LPAREN":
                case "RPAREN":
                case "LBRACE":
                case "RBRACE":
                case "SEMICOLON":
                case "COMMA":
                    return Color.Magenta;

                case "COMMENT_LINE":
                case "COMMENT_BLOCK":
                    return Color.Gray;

                case "IDENTIFIER":
                    return Color.Cyan;

                default:
                    return Color.White;
            }
        }

        private int GetPositionInTextBox(int line, int column)
        {
            line = Math.Max(1, line);
            column = Math.Max(0, column);

            string[] lines = codigo.Text.Split('\n');
            int position = 0;

            for (int i = 0; i < line - 1 && i < lines.Length; i++)
            {
                position += lines[i].Length + 1;
            }

            if (line - 1 < lines.Length)
            {
                column = Math.Min(column, lines[line - 1].Length);
            }

            return position + column;
        }
        #endregion

        #region Análisis Sintáctico y Árbol
        private void parserToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var parser = SetupAntlrParser();
                parser.RemoveErrorListeners();
                parser.AddErrorListener(new AntlrErrorListener());

                var tree = parser.program();
                MessageBox.Show("Análisis sintáctico exitoso!", "Parser ANTLR", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error de sintaxis:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //private void ArbolBtn_Click(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        var parser = SetupAntlrParser();
        //        var tree = parser.program();

        //        treeView1.BeginUpdate();
        //        treeView1.Nodes.Clear();
        //        BuildTreeView(tree, treeView1.Nodes.Add("Programa"));
        //        treeView1.ExpandAll();
        //        treeView1.EndUpdate();

              
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //    }
        //}

        private void BuildTreeView(IParseTree tree, TreeNode parentNode)
        {
            if (tree is TerminalNodeImpl terminal)
            {
                string tokenName = SiriusLanguageLexer.DefaultVocabulary.GetSymbolicName(terminal.Symbol.Type);
                parentNode.Nodes.Add($"[TOKEN] {tokenName}: {terminal.GetText()}");
            }
            else if (tree is ParserRuleContext ruleNode)
            {
                var newNode = parentNode.Nodes.Add($"[REGLA] {SiriusLanguageParser.ruleNames[ruleNode.RuleIndex]}");
                for (int i = 0; i < ruleNode.ChildCount; i++)
                {
                    BuildTreeView(ruleNode.GetChild(i), newNode);
                }
            }
        }

        public class AntlrErrorListener : BaseErrorListener
        {
            public override void SyntaxError(TextWriter output, IRecognizer recognizer,
                IToken offendingSymbol, int line, int charPositionInLine,
                string msg, RecognitionException e)
            {
                string errorType = msg.Contains("extraneous input") ? "Token inesperado" :
                                 msg.Contains("missing") ? "Falta token" : "Error de Sintaxis";

                throw new Exception($"{errorType} en línea {line}, columna {charPositionInLine + 1}:\n" +
                                   $"→ Token: '{offendingSymbol?.Text ?? "null"}' \n" +
                                   $"→ Detalle: {msg.Replace("'", "`")}");
            }
        }



        #endregion


        public class SemanticAnalyzer : SiriusLanguageBaseListener
        {
            private readonly Stack<Dictionary<string, string>> _scopeStack;
            private readonly List<string> _errors;

            public SemanticAnalyzer()
            {
                _scopeStack = new Stack<Dictionary<string, string>>();
                _scopeStack.Push(new Dictionary<string, string>()); // Ámbito global
                _errors = new List<string>();
            }

            public override void EnterVariableDeclaration(SiriusLanguageParser.VariableDeclarationContext context)
            {
                var identifier = context.IDENTIFIER();
                if (identifier == null)
                {
                    AddError(context.Start, "Declaración de variable sin identificador");
                    return;
                }

                string varName = identifier.GetText();
                var currentScope = _scopeStack.Peek();

                if (currentScope.ContainsKey(varName))
                {
                    AddError(identifier.Symbol, $"La variable '{varName}' ya está declarada en este ámbito");
                }
                else
                {
                    string type = context.type() != null ? context.type().GetText() : "infer";
                    currentScope[varName] = type;
                }
            }

            public override void EnterAssignment(SiriusLanguageParser.AssignmentContext context)
            {
                var identifier = context.IDENTIFIER();
                if (identifier == null) return;

                string varName = identifier.GetText();
                bool variableDeclared = false;

                foreach (var scope in _scopeStack)
                {
                    if (scope.ContainsKey(varName))
                    {
                        variableDeclared = true;
                        break;
                    }
                }

                if (!variableDeclared)
                {
                    AddError(identifier.Symbol, $"Variable no declarada '{varName}'");
                }
            }

            public override void EnterFunctionDeclaration(SiriusLanguageParser.FunctionDeclarationContext context)
            {
                var funcName = context.IDENTIFIER();
                if (funcName == null)
                {
                    AddError(context.Start, "Función sin nombre");
                    return;
                }

                // Crear nuevo ámbito para la función
                _scopeStack.Push(new Dictionary<string, string>());

                // Registrar parámetros
                if (context.parameterList() != null)
                {
                    foreach (var param in context.parameterList().parameter())
                    {
                        var paramNameToken = param.IDENTIFIER();
                        if (paramNameToken == null) continue;

                        string paramName = paramNameToken.GetText();
                        if (_scopeStack.Peek().ContainsKey(paramName))
                        {
                            AddError(paramNameToken.Symbol, $"Parámetro duplicado '{paramName}'");
                        }
                        else
                        {
                            string paramType = param.type() != null ? param.type().GetText() : "infer";
                            _scopeStack.Peek()[paramName] = paramType;
                        }
                    }
                }
            }

            public override void ExitFunctionDeclaration(SiriusLanguageParser.FunctionDeclarationContext context)
            {
                if (_scopeStack.Count > 1)
                {
                    _scopeStack.Pop();
                }
            }

            private void AddError(IToken token, string message)
            {
                string errorMsg = $"Línea {token.Line}:{token.Column} - {message}";
                _errors.Add(errorMsg);
            }

            public void ValidateSemantics()
            {
                if (_errors.Count > 0)
                {
                    throw new Exception(string.Join("\n", _errors));
                }
            }
        }

        private void arbolToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var parser = SetupAntlrParser();
                var tree = parser.program();

                treeView1.BeginUpdate();
                treeView1.Nodes.Clear();
                BuildTreeView(tree, treeView1.Nodes.Add("Programa"));
                treeView1.ExpandAll();
                treeView1.EndUpdate();


            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void semanticoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var parser = SetupAntlrParser();
                var tree = parser.program();

                var walker = new ParseTreeWalker();
                var analyzer = new SemanticAnalyzer();
                walker.Walk(analyzer, tree);

                // Validar después de recorrer todo el árbol
                analyzer.ValidateSemantics();

                MessageBox.Show("✔ Análisis semántico completado sin errores",
                              "Éxito",
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Errores semánticos encontrados:\n{ex.Message}",
                               "Error",
                               MessageBoxButtons.OK,
                               MessageBoxIcon.Error);
            }
        }


    }
}