namespace FolderLocker
{
    partial class FormCarpetas
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormCarpetas));
            lblBienvenido = new Label();
            btnProteger = new Button();
            btnDejarDeProteger = new Button();
            PanelOpciones = new Panel();
            btnMenuAbrir = new Button();
            lblLogo = new Label();
            btnCarpetas = new Button();
            panel1 = new Panel();
            PanelOpciones.SuspendLayout();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // lblBienvenido
            // 
            lblBienvenido.AutoSize = true;
            lblBienvenido.Location = new Point(3, 11);
            lblBienvenido.Name = "lblBienvenido";
            lblBienvenido.Size = new Size(77, 15);
            lblBienvenido.TabIndex = 0;
            lblBienvenido.Text = "BIENVENIDO!";
            // 
            // btnProteger
            // 
            btnProteger.Location = new Point(19, 52);
            btnProteger.Name = "btnProteger";
            btnProteger.Size = new Size(76, 89);
            btnProteger.TabIndex = 1;
            btnProteger.Text = "Proteger";
            btnProteger.UseVisualStyleBackColor = true;
            // 
            // btnDejarDeProteger
            // 
            btnDejarDeProteger.Location = new Point(117, 52);
            btnDejarDeProteger.Name = "btnDejarDeProteger";
            btnDejarDeProteger.Size = new Size(76, 89);
            btnDejarDeProteger.TabIndex = 2;
            btnDejarDeProteger.Text = "Dejar de Proteger";
            btnDejarDeProteger.UseVisualStyleBackColor = true;
            // 
            // PanelOpciones
            // 
            PanelOpciones.BackColor = SystemColors.AppWorkspace;
            PanelOpciones.Controls.Add(btnMenuAbrir);
            PanelOpciones.Controls.Add(lblLogo);
            PanelOpciones.Controls.Add(btnCarpetas);
            PanelOpciones.Location = new Point(0, 0);
            PanelOpciones.Name = "PanelOpciones";
            PanelOpciones.Size = new Size(206, 753);
            PanelOpciones.TabIndex = 3;
            // 
            // btnMenuAbrir
            // 
            btnMenuAbrir.Location = new Point(12, 162);
            btnMenuAbrir.Name = "btnMenuAbrir";
            btnMenuAbrir.Size = new Size(167, 48);
            btnMenuAbrir.TabIndex = 2;
            btnMenuAbrir.Text = "Mostrar Carpeta";
            btnMenuAbrir.UseVisualStyleBackColor = true;
            // 
            // lblLogo
            // 
            lblLogo.AutoSize = true;
            lblLogo.Location = new Point(83, 27);
            lblLogo.Name = "lblLogo";
            lblLogo.Size = new Size(24, 15);
            lblLogo.TabIndex = 1;
            lblLogo.Text = "jpg";
            // 
            // btnCarpetas
            // 
            btnCarpetas.Location = new Point(12, 107);
            btnCarpetas.Name = "btnCarpetas";
            btnCarpetas.Size = new Size(167, 49);
            btnCarpetas.TabIndex = 0;
            btnCarpetas.Text = "CARPETAS";
            btnCarpetas.UseVisualStyleBackColor = true;
            // 
            // panel1
            // 
            panel1.Controls.Add(btnDejarDeProteger);
            panel1.Controls.Add(btnProteger);
            panel1.Controls.Add(lblBienvenido);
            panel1.Location = new Point(212, 69);
            panel1.Name = "panel1";
            panel1.Size = new Size(787, 674);
            panel1.TabIndex = 5;
            // 
            // FormCarpetas
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1011, 755);
            Controls.Add(panel1);
            Controls.Add(PanelOpciones);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "FormCarpetas";
            Text = "Folder Locker";
            PanelOpciones.ResumeLayout(false);
            PanelOpciones.PerformLayout();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Label lblBienvenido;
        private Button btnProteger;
        private Button btnDejarDeProteger;
        private Panel PanelOpciones;
        private Label lblLogo;
        private Panel panel1;
        private Button btnMenuAbrir;
        private Button btnCarpetas;
    }
}
