using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using Compilador.Generated;
using Microsoft.Win32;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static Compilador.abrir;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;
using static Compilador.SemanticAnalyzer;
using static Compilador.Generated.SiriusLanguageParser;
using System.Diagnostics;




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
            var tokenStream = new CommonTokenStream(lexer);

            tokenStream.Fill(); // Carga todos los tokens

            var parser = new SiriusLanguageParser(tokenStream);

            // Configuración importante para capturar errores
            parser.RemoveErrorListeners();
            parser.AddErrorListener(new AntlrErrorListener());

            return parser;
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

                var analyzer = new SemanticAnalyzer();
                analyzer.VisitProgram(tree);

                if (!analyzer.HasErrors())
                {
                    ShowSemanticSuccess();
                }
                else
                {
                    ShowSemanticErrors(analyzer.GetErrors());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error durante análisis semántico: {ex.Message}",
                               "Error",
                               MessageBoxButtons.OK,
                               MessageBoxIcon.Error);
            }
        }

        private void ShowSemanticSuccess()
        {
            MessageBox.Show("✔ El análisis semántico se completó sin errores",
                           "Éxito",
                           MessageBoxButtons.OK,
                           MessageBoxIcon.Information);
        }

        private void ShowSemanticErrors(List<string> errors)
        {
            var form = new Form
            {
                Text = "Errores Semánticos",
                Width = 800,
                Height = 500,
                StartPosition = FormStartPosition.CenterParent
            };

            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false
            };

            grid.Columns.Add("Line", "Línea");
            grid.Columns.Add("Column", "Columna");
            grid.Columns.Add("Message", "Mensaje de Error");

            foreach (var error in errors)
            {
                // Parsear línea y columna del mensaje de error
                var parts = error.Split(new[] { ": - " }, 2, StringSplitOptions.None);
                var location = parts[0].Replace("Línea ", "").Split(':');
                var message = parts.Length > 1 ? parts[1] : error;

                if (location.Length >= 2)
                {
                    grid.Rows.Add(location[0], location[1], message);
                }
                else
                {
                    grid.Rows.Add("", "", message);
                }
            }

            form.Controls.Add(grid);
            form.Show(); // Cambiado de ShowDialog() a Show()
        }

        private void intermedioToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                // Primero realizar análisis semántico
                var parser = SetupAntlrParser();
                var tree = parser.program();

                var analyzer = new SemanticAnalyzer();
                analyzer.VisitProgram(tree);

                if (analyzer.HasErrors())
                {
                    ShowSemanticErrors(analyzer.GetErrors());
                    return;
                }

                // Si no hay errores semánticos, generar código intermedio
                var codeGenerator = new ThreeAddressCodeGenerator(analyzer);
                codeGenerator.VisitProgram(tree);

                // Guardar automáticamente en el escritorio
                codeGenerator.SaveToFile();

                // Mostrar el código generado
                ShowIntermediateCode(codeGenerator.GetGeneratedCode());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error durante generación de código intermedio: {ex.Message}",
                              "Error",
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Error);
            }
        }

        private void ShowIntermediateCode(List<string> intermediateCode)
        {
            var form = new Form
            {
                Text = "Código Intermedio Generado",
                Width = 800,
                Height = 500,
                StartPosition = FormStartPosition.CenterParent
            };

            var textBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 10),
                ReadOnly = true,
                Text = string.Join(Environment.NewLine, intermediateCode)
            };

            var saveButton = new System.Windows.Forms.Button
            {
                Text = "Guardar Código",
                Dock = DockStyle.Bottom
            };

            saveButton.Click += (s, args) =>
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "Archivos TAC|*.tac|Todos los archivos|*.*",
                    Title = "Guardar código intermedio"
                };

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllLines(saveDialog.FileName, intermediateCode);
                    MessageBox.Show("Código intermedio guardado exitosamente",
                                  "Éxito",
                                  MessageBoxButtons.OK,
                                  MessageBoxIcon.Information);
                }
            };

            form.Controls.Add(textBox);
            form.Controls.Add(saveButton);
            form.Show();
        }

        private void roslynToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                // 1. Primero realizar análisis semántico
                var parser = SetupAntlrParser();
                var tree = parser.program();

                var analyzer = new SemanticAnalyzer();
                analyzer.VisitProgram(tree);

                if (analyzer.HasErrors())
                {
                    ShowSemanticErrors(analyzer.GetErrors());
                    return;
                }

                // 2. Generar código intermedio
                var codeGenerator = new ThreeAddressCodeGenerator(analyzer);
                codeGenerator.VisitProgram(tree);
                var intermediateCode = codeGenerator.GetGeneratedCode();

                // 3. Compilar con Roslyn
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string exePath = Path.Combine(desktopPath, "output.exe");

                var roslynCompiler = new RoslynCompiler();
                bool success = roslynCompiler.CompileFromTAC(intermediateCode, exePath);

                if (success)
                {
                    // Mostrar mensaje de éxito
                    var result = MessageBox.Show($"✔ Compilación exitosa!\nEjecutable generado en: {exePath}\n\n¿Deseas ejecutar el programa ahora?",
                                              "Éxito",
                                              MessageBoxButtons.YesNo,
                                              MessageBoxIcon.Information);

                    if (result == DialogResult.Yes)
                    {
                        ProcessStartInfo startInfo = new ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            Arguments = $"/K \"{exePath}\" && pause",  // Ejecuta el programa Y LUEGO hace una pausa
                            UseShellExecute = true,
                            CreateNoWindow = false
                        };
                        Process.Start(startInfo);
                    }
                }
                else
                {
                    MessageBox.Show("✖ Error durante la compilación",
                                  "Error",
                                  MessageBoxButtons.OK,
                                  MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error durante compilación: {ex.Message}",
                              "Error",
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Error);
            }
        }
    }
}