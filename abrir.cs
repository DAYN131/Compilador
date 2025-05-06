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


namespace Compilador
{
    public partial class abrir: Form
    {

        // Añade este método para colorear los tokens
        private void ColorizeTokens(List<Token> tokens)
        {
            // Guardar posición actual del cursor
            int originalPosition = codigo.SelectionStart;

            // Limpiar formatos previos
            codigo.SelectAll();
            codigo.SelectionColor = Color.White; // Color por defecto
            codigo.DeselectAll();

            foreach (Token token in tokens)
            {
                int startPos = GetPositionInTextBox(token.Line, token.Position);
                int length = token.Lexeme.Length;

                // Seleccionar el texto del token
                codigo.Select(startPos, length);

                // Asignar color según tipo de token
                switch (token.Type)
                {
                    case TokenType.Var:
                    case TokenType.Val:
                    case TokenType.Fun:
                    case TokenType.For:
                    case TokenType.While:
                    case TokenType.Print:
                    case TokenType.Println:
                        codigo.SelectionColor = Color.DodgerBlue;
                        break;

                    case TokenType.TypeInt:
                    case TokenType.TypeStr:
                    case TokenType.TypeBool:
                        codigo.SelectionColor = Color.LightSkyBlue;
                        break;

                    case TokenType.Number:
                        codigo.SelectionColor = Color.White;
                        break;

                    case TokenType.String:
                        codigo.SelectionColor = Color.LimeGreen;
                        break;

                    case TokenType.CommentLine:
                    case TokenType.CommentBlock:
                        codigo.SelectionColor = Color.Gray;
                        break;

                    case TokenType.Plus:
                    case TokenType.Minus:
                    case TokenType.Multiply:
                    case TokenType.Divide:
                    case TokenType.Assign:
                        codigo.SelectionColor = Color.Gold;
                        break;

                    case TokenType.Identifier:
                        codigo.SelectionColor = Color.Cyan;
                        break;

                    case TokenType.LParen:
                    case TokenType.RParen:
                    case TokenType.LBrace:
                    case TokenType.RBrace:
                    case TokenType.Comma:
                    case TokenType.Semicolon:
                        codigo.SelectionColor = Color.Magenta;
                        break;

                    case TokenType.EOF:
                        codigo.SelectionColor = Color.White;
                        break;

                    default:
                        codigo.SelectionColor = Color.White;
                        break;
                }
            }

            // Restaurar posición original
            codigo.SelectionStart = originalPosition;
            codigo.SelectionLength = 0;
        }

        // Método auxiliar para convertir posición (línea, columna) a posición en TextBox
        private int GetPositionInTextBox(int line, int column)
        {
            // Asegurarse que las líneas y columnas no sean negativas
            line = Math.Max(1, line);
            column = Math.Max(0, column); // ANTLR usa columnas base 0

            string[] lines = codigo.Text.Split('\n');
            int position = 0;

            // Sumar las longitudes de las líneas anteriores
            for (int i = 0; i < line - 1 && i < lines.Length; i++)
            {
                position += lines[i].Length + 1; // +1 por el carácter \n
            }

            // Asegurar que la columna no exceda la longitud de la línea
            if (line - 1 < lines.Length)
            {
                column = Math.Min(column, lines[line - 1].Length);
            }

            return position + column;
        }


        public abrir(string contenido, string rutaArchivo)
        {
            {
                InitializeComponent();
                label1.Text = rutaArchivo;

                if (contenido != null)
                    codigo.Text = contenido; // Asumiendo que tienes un TextBox llamado txtContenido
                else{
                codigo.Text = "";
                }


            }

        }

     
        private void guardarToolStripMenuItem1_Click_1(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Seleccionar Archivo",
                Filter = "Archivos SIR (*.sir)|*.sir",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string rutaArchivo = openFileDialog.FileName;
                if (rutaArchivo.EndsWith(".sir"))
                {
                    string contenido;
                    using (StreamReader reader = new StreamReader(rutaArchivo))
                    {
                        contenido = reader.ReadToEnd();
                    }

                    abrir f1 = new abrir(contenido, rutaArchivo);
                    f1.Visible = true;
                    this.Visible = false;
                    f1.Text = rutaArchivo;

                }
                else
                {
                    MessageBox.Show("El archivo seleccionado no es un archivo .sir", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void guardarToolStripMenuItem2_Click_1(object sender, EventArgs e)
        {

            string contenido = "";
            string rutaArchivo = "Nuevo archivo";
            abrir f1 = new abrir(contenido, rutaArchivo);
            f1.Text = rutaArchivo;
            f1.Visible = true;
            this.Visible = false;


        }

        private void guardarToolStripMenuItem3_Click_1(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Title = "Guardar Archivo",
                Filter = "Todos los archivos (*.sir*)|*.sir*",
                DefaultExt = "sir",
                AddExtension = true,
            };
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                string rutaArchivo = saveFileDialog.FileName;
                using (StreamWriter writer = new StreamWriter(rutaArchivo))
                {
                    writer.WriteLine(codigo.Text);
                    label1.Text = rutaArchivo;
                    this.Text = rutaArchivo;

                }
            }
        }

        private void guardarToolStripMenuItem4_Click(object sender, EventArgs e)
        {
            string rutaArchivo = label1.Text;
            using (StreamWriter writer = new StreamWriter(rutaArchivo))
            {
                writer.WriteLine(codigo.Text);
            }
        }

        // USO DE ANTLR
        private SiriusLanguageParser SetupAntlrParser()
        {
            var inputStream = new AntlrInputStream(codigo.Text);
            var lexer = new SiriusLanguageLexer(inputStream);
            var tokenStream = new CommonTokenStream(lexer);
            return new SiriusLanguageParser(tokenStream);
        }



        private void tokenizarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var parser = SetupAntlrParser();
                var tokenStream = (CommonTokenStream)parser.InputStream;
                tokenStream.Fill();

                // Configurar visualización de tokens
                tokentipebox.Items.Clear();
                tokenbox.Items.Clear();
                posicion.Items.Clear();

                foreach (var token in tokenStream.GetTokens())
                {
                    if (token.Type == -1) continue; // Omitir EOF

                    var tokenName = SiriusLanguageLexer.DefaultVocabulary.GetSymbolicName(token.Type);
                    tokentipebox.Items.Add(tokenName);
                    tokenbox.Items.Add(token.Text);
                    posicion.Items.Add($"Línea {token.Line}:{token.Column}");
                }

                ColorizeTokensAntlr(tokenStream.GetTokens());
                MessageBox.Show("Tokenización con ANTLR completada");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ColorizeTokensAntlr(IList<IToken> tokens)
        {
            int originalPos = codigo.SelectionStart;
            codigo.SelectAll();
            codigo.SelectionColor = Color.White; // Color por defecto
            codigo.DeselectAll();

            foreach (var token in tokens)
            {
                if (token.Type == -1) continue; // Omitir EOF

                int start = GetPositionInTextBox(token.Line, token.Column);
                int length = token.StopIndex - token.StartIndex + 1;

                // Validar que la selección esté dentro de los límites
                if (start >= 0 && start + length <= codigo.Text.Length)
                {
                    codigo.Select(start, length);

                    // Mapeo completo de tokens a colores
                    var tokenType = SiriusLanguageLexer.DefaultVocabulary.GetSymbolicName(token.Type);
                    switch (tokenType)
                    {
                        case "VAR":
                        case "VAL":
                            codigo.SelectionColor = Color.DodgerBlue;
                            break;
                        case "FUN":
                        case "FOR":
                        case "WHILE":
                        case "IF":
                        case "ELSE":
                            codigo.SelectionColor = Color.Blue;
                            break;
                        case "PRINT":
                        case "PRINTLN":
                            codigo.SelectionColor = Color.Cyan;
                            break;
                        case "NUMBER":
                            codigo.SelectionColor = Color.LightGreen;
                            break;
                        case "STRING":
                            codigo.SelectionColor = Color.Orange;
                            break;
                        case "TYPE_INT":
                        case "TYPE_STR":
                        case "TYPE_BOOL":
                            codigo.SelectionColor = Color.LightSkyBlue;
                            break;
                        case "PLUS":
                        case "MINUS":
                        case "MULTIPLY":
                        case "DIVIDE":
                            codigo.SelectionColor = Color.Gold;
                            break;
                        case "LPAREN":
                        case "RPAREN":
                        case "LBRACE":
                        case "RBRACE":
                        case "SEMICOLON":
                        case "COMMA":
                            codigo.SelectionColor = Color.Magenta;
                            break;
                        case "COMMENT_LINE":
                        case "COMMENT_BLOCK":
                            codigo.SelectionColor = Color.Gray;
                            break;
                        default:
                            codigo.SelectionColor = Color.White;
                            break;
                    }
                }
            }

            codigo.SelectionStart = originalPos;
            codigo.SelectionLength = 0;
        }

        private void parserToolStripMenuItem_Click(object sender, EventArgs e)
        {

            try
            {
                var parser = SetupAntlrParser();
                parser.RemoveErrorListeners();
                parser.AddErrorListener(new AntlrErrorListener());

                var tree = parser.program(); // Usa la regla inicial de tu gramática

                MessageBox.Show("Análisis sintáctico exitoso!\n" +
                              $"Árbol: {tree.ToStringTree(parser)}",
                              "Parser ANTLR", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error de sintaxis:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Clase para manejo de errores
        public class AntlrErrorListener : BaseErrorListener
        {
            public override void SyntaxError(TextWriter output, IRecognizer recognizer,
                IToken offendingSymbol, int line, int charPositionInLine,
                string msg, RecognitionException e)
            {
                // Cambia el parámetro 'output' a '_output' si es necesario
                throw new Exception($"Error en línea {line}, posición {charPositionInLine}: {msg}");
            }
        }



       
    }
}
