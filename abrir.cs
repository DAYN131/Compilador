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
            int position = 0;
            int currentLine = 1;
            int currentColumn = 1;

            while (currentLine < line && position < codigo.Text.Length)
            {
                if (codigo.Text[position] == '\n')
                {
                    currentLine++;
                    currentColumn = 1;
                }
                else
                {
                    currentColumn++;
                }
                position++;
            }

            return position + (column - 1);
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

        private void btnTokenize_Click(object sender, EventArgs e)
        {
            try
            {
                Tokenizer tokenizer = new Tokenizer(codigo.Text);
                List<Token> tokens = tokenizer.Tokenize();

                // Mostrar los tokens en un ListBox o similar
                tokentipebox.Items.Clear();
                tokenbox.Items.Clear();
                posicion.Items.Clear();
                foreach (Token token in tokens)
                {
                    string[] partes = token.ToString().Split(' ');
                    tokentipebox.Items.Add(partes[0]);
                    tokenbox.Items.Add(partes[1]);
                    posicion.Items.Add(partes[2] + " " + partes[3] + " " + partes[4] + " "+ partes[5]);
                }

                // Aplicar colores al código fuente
                ColorizeTokens(tokens);
                MessageBox.Show("Tokenización completada con colores aplicados");

                MessageBox.Show($"Tokenización completada. Se encontraron {tokens.Count} tokens.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error durante la tokenización: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


    }
}
