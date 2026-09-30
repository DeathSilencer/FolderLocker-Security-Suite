using FolderLocker.UI.Common;

namespace FolderLocker.UI.Views
{
    public class ProtegerView : UserControl
    {
        private Panel card = null!;
        private Label lblTituloProteger = null!;
        private Label lblPassProteger = null!;
        private TextBox txtRuta = null!;
        private TextBox txtContrasena = null!;
        private Button btnBuscarRuta = null!;
        private Button btnOlvide = null!;
        private Button btnAccionGuardar = null!;

        public string Ruta
        {
            get => txtRuta.Text;
            set => txtRuta.Text = value;
        }

        public string Contrasena
        {
            get => txtContrasena.Text;
            set => txtContrasena.Text = value;
        }

        public Panel CardPanel => card;

        public event Action<string, string>? BloquearRequested;
        public event Action? ForgotPasswordRequested;

        public ProtegerView()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = UITheme.cBackground;
            this.AllowDrop = true;

            InicializarComponentes();
            ConfigurarDragAndDrop();
        }

        private void InicializarComponentes()
        {
            card = UITheme.CrearTarjetaBase(550, 350);
            this.Controls.Add(card);

            lblTituloProteger = UITheme.CrearEtiqueta(card, Localization.Get("lbl_target"), 40, 30);
            txtRuta = UITheme.CrearInput(card, 40, 55, 330);

            btnBuscarRuta = new Button
            {
                Text = Localization.Get("btn_browse"),
                Size = new Size(100, 30),
                Location = new Point(380, 55)
            };
            UITheme.EstilarBotonSecundario(btnBuscarRuta);
            btnBuscarRuta.Click += (s, e) =>
            {
                using var fbd = new FolderBrowserDialog();
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtRuta.Text = fbd.SelectedPath;
                }
            };
            card.Controls.Add(btnBuscarRuta);

            UITheme.CrearSeparador(card, 110);

            lblPassProteger = new Label
            {
                Text = Localization.Get("lbl_pass"),
                Location = new Point(40, 140),
                ForeColor = UITheme.cTextSecondary,
                AutoSize = true,
                Font = new Font("Segoe UI", 8, FontStyle.Bold)
            };
            card.Controls.Add(lblPassProteger);

            txtContrasena = UITheme.CrearInputPassword(card, 40, 165, 440);

            btnOlvide = new Button
            {
                Text = Localization.Get("link_forgot"),
                AutoSize = true,
                Location = new Point(350, 135),
                FlatStyle = FlatStyle.Flat,
                ForeColor = UITheme.cAccentRed,
                Font = new Font("Segoe UI", 8, FontStyle.Regular),
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent
            };
            btnOlvide.FlatAppearance.BorderSize = 0;
            btnOlvide.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btnOlvide.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btnOlvide.Click += (s, e) => ForgotPasswordRequested?.Invoke();
            card.Controls.Add(btnOlvide);

            btnAccionGuardar = new Button
            {
                Text = Localization.Get("btn_lock"),
                Size = new Size(440, 50),
                Location = new Point(40, 250)
            };
            UITheme.EstilarBotonAccion(btnAccionGuardar);
            btnAccionGuardar.Click += (s, e) => BloquearRequested?.Invoke(txtRuta.Text, txtContrasena.Text);
            card.Controls.Add(btnAccionGuardar);

            Recentrar();
        }

        private void ConfigurarDragAndDrop()
        {
            this.DragEnter += (s, e) =>
            {
                if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    e.Effect = DragDropEffects.Copy;
                }
            };

            this.DragDrop += (s, e) =>
            {
                if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
                    if (files.Length > 0 && Directory.Exists(files[0]))
                    {
                        txtRuta.Text = files[0];
                    }
                }
            };
        }

        public void ConfigurarProcesando(bool procesando)
        {
            btnAccionGuardar.Enabled = !procesando;
            btnAccionGuardar.Text = procesando ? Localization.Get("status_processing") : Localization.Get("btn_lock");
            this.Cursor = procesando ? Cursors.WaitCursor : Cursors.Default;
        }

        public void LimpiarCampos()
        {
            txtRuta.Text = "";
            txtContrasena.Text = "";
        }

        public void ActualizarIdioma()
        {
            lblTituloProteger.Text = Localization.Get("lbl_target").ToUpper();
            btnBuscarRuta.Text = Localization.Get("btn_browse");
            lblPassProteger.Text = Localization.Get("lbl_pass");
            btnOlvide.Text = Localization.Get("link_forgot");
            btnAccionGuardar.Text = Localization.Get("btn_lock");
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Recentrar();
        }

        private void Recentrar()
        {
            if (card == null) return;
            int totalH = 100 + card.Height;
            int startY = Math.Max(30, (this.ClientSize.Height - totalH) / 2);
            int x = Math.Max(20, (this.ClientSize.Width - card.Width) / 2);
            card.Location = new Point(x, startY + 100);
        }
    }
}
