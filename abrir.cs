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
                listBoxTokens.Items.Clear();
                foreach (Token token in tokens)
                {
                    listBoxTokens.Items.Add(token.ToString());
                }

                MessageBox.Show($"Tokenización completada. Se encontraron {tokens.Count} tokens.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error durante la tokenización: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
