﻿namespace Compilador
{
    partial class abrir
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(abrir));
            this.codigo = new System.Windows.Forms.RichTextBox();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.guardarToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.guardarToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.guardarToolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
            this.guardarToolStripMenuItem3 = new System.Windows.Forms.ToolStripMenuItem();
            this.guardarToolStripMenuItem4 = new System.Windows.Forms.ToolStripMenuItem();
            this.compilarToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.compilarToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.detenerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.label1 = new System.Windows.Forms.Label();
            this.tokentipebox = new System.Windows.Forms.ListBox();
            this.tokenbox = new System.Windows.Forms.ListBox();
            this.posicion = new System.Windows.Forms.ListBox();
            this.tokenizarToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.parserToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // codigo
            // 
            this.codigo.BackColor = System.Drawing.Color.Black;
            this.codigo.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.codigo.ForeColor = System.Drawing.SystemColors.Menu;
            this.codigo.Location = new System.Drawing.Point(4, 62);
            this.codigo.Margin = new System.Windows.Forms.Padding(4);
            this.codigo.Name = "codigo";
            this.codigo.Size = new System.Drawing.Size(755, 474);
            this.codigo.TabIndex = 2;
            this.codigo.Text = "";
            // 
            // menuStrip1
            // 
            this.menuStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Visible;
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.guardarToolStripMenuItem,
            this.compilarToolStripMenuItem,
            this.tokenizarToolStripMenuItem,
            this.parserToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
            this.menuStrip1.Size = new System.Drawing.Size(764, 28);
            this.menuStrip1.TabIndex = 3;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // guardarToolStripMenuItem
            // 
            this.guardarToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.guardarToolStripMenuItem1,
            this.guardarToolStripMenuItem2,
            this.guardarToolStripMenuItem3,
            this.guardarToolStripMenuItem4});
            this.guardarToolStripMenuItem.Name = "guardarToolStripMenuItem";
            this.guardarToolStripMenuItem.Size = new System.Drawing.Size(73, 24);
            this.guardarToolStripMenuItem.Text = "Archivo";
            // 
            // guardarToolStripMenuItem1
            // 
            this.guardarToolStripMenuItem1.Image = ((System.Drawing.Image)(resources.GetObject("guardarToolStripMenuItem1.Image")));
            this.guardarToolStripMenuItem1.Name = "guardarToolStripMenuItem1";
            this.guardarToolStripMenuItem1.Size = new System.Drawing.Size(189, 26);
            this.guardarToolStripMenuItem1.Text = "Abrir";
            this.guardarToolStripMenuItem1.Click += new System.EventHandler(this.guardarToolStripMenuItem1_Click_1);
            // 
            // guardarToolStripMenuItem2
            // 
            this.guardarToolStripMenuItem2.Image = ((System.Drawing.Image)(resources.GetObject("guardarToolStripMenuItem2.Image")));
            this.guardarToolStripMenuItem2.Name = "guardarToolStripMenuItem2";
            this.guardarToolStripMenuItem2.Size = new System.Drawing.Size(189, 26);
            this.guardarToolStripMenuItem2.Text = "Crear Nuevo";
            this.guardarToolStripMenuItem2.Click += new System.EventHandler(this.guardarToolStripMenuItem2_Click_1);
            // 
            // guardarToolStripMenuItem3
            // 
            this.guardarToolStripMenuItem3.Image = ((System.Drawing.Image)(resources.GetObject("guardarToolStripMenuItem3.Image")));
            this.guardarToolStripMenuItem3.Name = "guardarToolStripMenuItem3";
            this.guardarToolStripMenuItem3.Size = new System.Drawing.Size(189, 26);
            this.guardarToolStripMenuItem3.Text = "Guardar Como";
            this.guardarToolStripMenuItem3.Click += new System.EventHandler(this.guardarToolStripMenuItem3_Click_1);
            // 
            // guardarToolStripMenuItem4
            // 
            this.guardarToolStripMenuItem4.Image = ((System.Drawing.Image)(resources.GetObject("guardarToolStripMenuItem4.Image")));
            this.guardarToolStripMenuItem4.Name = "guardarToolStripMenuItem4";
            this.guardarToolStripMenuItem4.Size = new System.Drawing.Size(189, 26);
            this.guardarToolStripMenuItem4.Text = "Guardar";
            this.guardarToolStripMenuItem4.Click += new System.EventHandler(this.guardarToolStripMenuItem4_Click);
            // 
            // compilarToolStripMenuItem
            // 
            this.compilarToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.compilarToolStripMenuItem1,
            this.detenerToolStripMenuItem});
            this.compilarToolStripMenuItem.Name = "compilarToolStripMenuItem";
            this.compilarToolStripMenuItem.Size = new System.Drawing.Size(84, 24);
            this.compilarToolStripMenuItem.Text = "Compilar";
            // 
            // compilarToolStripMenuItem1
            // 
            this.compilarToolStripMenuItem1.Image = ((System.Drawing.Image)(resources.GetObject("compilarToolStripMenuItem1.Image")));
            this.compilarToolStripMenuItem1.Name = "compilarToolStripMenuItem1";
            this.compilarToolStripMenuItem1.Size = new System.Drawing.Size(153, 26);
            this.compilarToolStripMenuItem1.Text = "Compilar";
            // 
            // detenerToolStripMenuItem
            // 
            this.detenerToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("detenerToolStripMenuItem.Image")));
            this.detenerToolStripMenuItem.Name = "detenerToolStripMenuItem";
            this.detenerToolStripMenuItem.Size = new System.Drawing.Size(153, 26);
            this.detenerToolStripMenuItem.Text = "Detener";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(1, 28);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(0, 16);
            this.label1.TabIndex = 4;
            // 
            // tokentipebox
            // 
            this.tokentipebox.FormattingEnabled = true;
            this.tokentipebox.ItemHeight = 16;
            this.tokentipebox.Location = new System.Drawing.Point(767, 63);
            this.tokentipebox.Margin = new System.Windows.Forms.Padding(4);
            this.tokentipebox.Name = "tokentipebox";
            this.tokentipebox.Size = new System.Drawing.Size(98, 468);
            this.tokentipebox.TabIndex = 6;
            this.tokentipebox.Visible = false;
            // 
            // tokenbox
            // 
            this.tokenbox.FormattingEnabled = true;
            this.tokenbox.ItemHeight = 16;
            this.tokenbox.Location = new System.Drawing.Point(868, 63);
            this.tokenbox.Margin = new System.Windows.Forms.Padding(4);
            this.tokenbox.Name = "tokenbox";
            this.tokenbox.Size = new System.Drawing.Size(98, 468);
            this.tokenbox.TabIndex = 7;
            this.tokenbox.Visible = false;
            // 
            // posicion
            // 
            this.posicion.FormattingEnabled = true;
            this.posicion.ItemHeight = 16;
            this.posicion.Location = new System.Drawing.Point(969, 63);
            this.posicion.Margin = new System.Windows.Forms.Padding(4);
            this.posicion.Name = "posicion";
            this.posicion.Size = new System.Drawing.Size(220, 468);
            this.posicion.TabIndex = 8;
            this.posicion.Visible = false;
            // 
            // tokenizarToolStripMenuItem
            // 
            this.tokenizarToolStripMenuItem.Name = "tokenizarToolStripMenuItem";
            this.tokenizarToolStripMenuItem.Size = new System.Drawing.Size(86, 24);
            this.tokenizarToolStripMenuItem.Text = "Tokenizar";
            this.tokenizarToolStripMenuItem.Click += new System.EventHandler(this.tokenizarToolStripMenuItem_Click);
            // 
            // parserToolStripMenuItem
            // 
            this.parserToolStripMenuItem.Name = "parserToolStripMenuItem";
            this.parserToolStripMenuItem.Size = new System.Drawing.Size(62, 24);
            this.parserToolStripMenuItem.Text = "Parser";
            this.parserToolStripMenuItem.Click += new System.EventHandler(this.parserToolStripMenuItem_Click);
            // 
            // abrir
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.ClientSize = new System.Drawing.Size(764, 554);
            this.Controls.Add(this.posicion);
            this.Controls.Add(this.tokenbox);
            this.Controls.Add(this.tokentipebox);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.codigo);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "abrir";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "abrir";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.RichTextBox codigo;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem guardarToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem guardarToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem guardarToolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem guardarToolStripMenuItem3;
        private System.Windows.Forms.ToolStripMenuItem guardarToolStripMenuItem4;
        private System.Windows.Forms.ToolStripMenuItem compilarToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem compilarToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem detenerToolStripMenuItem;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ListBox tokentipebox;
        private System.Windows.Forms.ListBox tokenbox;
        private System.Windows.Forms.ListBox posicion;
        private System.Windows.Forms.ToolStripMenuItem tokenizarToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem parserToolStripMenuItem;
    }
}