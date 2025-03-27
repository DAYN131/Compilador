using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace Compilador
{
    public partial class Form1: Form
    {
        public Form1()
        {
            InitializeComponent();
        }


        // Boton para Crear Archivo
        private void Crear_Click(object sender, EventArgs e)
        {
            string contenido = "";
            string rutaArchivo = "Nuevo";
            abrir f1 = new abrir(contenido, rutaArchivo);
            f1.Visible = true;
            this.Visible = false;
            f1.Text = rutaArchivo;
        }

        // Boton para Abrir Archivo
        private void Abrir_Click(object sender, EventArgs e)
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
                        contenido = reader.ReadToEnd(); // Leer todo el archivo como texto
                    }

                    abrir f1 = new abrir(contenido, rutaArchivo); // Pasar el contenido como string
                    f1.Visible = true;
                    this.Visible = false;
                    f1.Text = rutaArchivo;
                }
                else
                {
                    MessageBox.Show("El archivo seleccionado no es un archivo .sir", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        } // End of Abrir_Click
    }

}
