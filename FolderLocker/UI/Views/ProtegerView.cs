using FolderLocker.UI.Common;

namespace FolderLocker.UI.Views
{
    public class ProtegerView : UserControl
    {
        private Panel card = null!;
        private Label lblTituloProteger = null!;
        private Label lblDropHint = null!;
        private Label lblPassProteger = null!;
        private Label lblPassHint = null!;
        private Label lblGarantia = null!;

        private Panel pnlRutaGroup = null!;
        private TextBox txtRuta = null!;
        private Button btnBuscarRuta = null!;

        private Panel pnlPassGroup = null!;
        private TextBox txtContrasena = null!;
        private Button btnEye = null!;
        private Button btnOlvide = null!;

        private Button btnAccionGuardar = null!;

        private bool _rutaFocused = false;
        private bool _passFocused = false;
        private bool _isDragOver = false;

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
            // Tarjeta principal ampliada (680 x 380) con doble nivel de profundidad y borde superior rojo
            card = new Panel
            {
                Size = new Size(680, 380),
                BackColor = UITheme.cSurface
            };
            card.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, card.ClientRectangle, UITheme.cBorder, ButtonBorderStyle.Solid);
                using var b = new SolidBrush(UITheme.cAccentRed);
                e.Graphics.FillRectangle(b, 0, 0, card.Width, 3);
            };
            this.Controls.Add(card);

            // Badges superiores de seguridad
            int badgeY = 18;
            var badge1 = CrearBadge("🔒 AES-256 CTR", Color.FromArgb(50, 22, 22), Color.FromArgb(252, 165, 165), 40, badgeY);
            var badge2 = CrearBadge("🛡 ZERO-KNOWLEDGE", Color.FromArgb(36, 33, 33), Color.FromArgb(209, 213, 219), 165, badgeY);
            var badge3 = CrearBadge("⚡ TWO-PHASE COMMIT", Color.FromArgb(36, 33, 33), Color.FromArgb(209, 213, 219), 310, badgeY);
            card.Controls.AddRange(new Control[] { badge1, badge2, badge3 });

            // 1. Sección: Carpeta a proteger
            lblTituloProteger = new Label
            {
                Text = Localization.Get("lbl_target").ToUpper(),
                Location = new Point(40, 56),
                ForeColor = UITheme.cTextSecondary,
                AutoSize = true,
                Font = new Font("Segoe UI", 8, FontStyle.Bold)
            };
            card.Controls.Add(lblTituloProteger);

            lblDropHint = new Label
            {
                Text = Localization.Get("lbl_drop_hint"),
                Location = new Point(320, 57),
                ForeColor = Color.FromArgb(140, 130, 130),
                AutoSize = true,
                Font = new Font("Segoe UI", 7.8f, FontStyle.Regular)
            };
            card.Controls.Add(lblDropHint);

            // Input Group Unificado para la Ruta
            pnlRutaGroup = new Panel
            {
                Location = new Point(40, 80),
                Size = new Size(600, 44),
                BackColor = UITheme.cInputBackground
            };
            pnlRutaGroup.Paint += (s, e) =>
            {
                Color borderColor = (_rutaFocused || _isDragOver) ? UITheme.cAccentRed : Color.FromArgb(52, 47, 47);
                ControlPaint.DrawBorder(e.Graphics, pnlRutaGroup.ClientRectangle, borderColor, ButtonBorderStyle.Solid);
            };

            var lblRutaIcon = new Label
            {
                Text = "📁",
                Location = new Point(10, 10),
                Size = new Size(26, 24),
                Font = new Font("Segoe UI", 12),
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };
            pnlRutaGroup.Controls.Add(lblRutaIcon);

            txtRuta = new TextBox
            {
                Location = new Point(40, 11),
                Size = new Size(445, 24),
                BorderStyle = BorderStyle.None,
                BackColor = UITheme.cInputBackground,
                ForeColor = UITheme.cTextPrimary,
                Font = new Font("Segoe UI", 10.5f)
            };
            txtRuta.GotFocus += (s, e) => { _rutaFocused = true; pnlRutaGroup.Invalidate(); };
            txtRuta.LostFocus += (s, e) => { _rutaFocused = false; pnlRutaGroup.Invalidate(); };
            pnlRutaGroup.Controls.Add(txtRuta);

            btnBuscarRuta = new Button
            {
                Text = Localization.Get("btn_browse"),
                Location = new Point(490, 5),
                Size = new Size(104, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(52, 46, 46),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnBuscarRuta.FlatAppearance.BorderSize = 0;
            btnBuscarRuta.FlatAppearance.MouseOverBackColor = Color.FromArgb(72, 64, 64);
            btnBuscarRuta.Click += (s, e) =>
            {
                using var fbd = new FolderBrowserDialog();
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtRuta.Text = fbd.SelectedPath;
                }
            };
            pnlRutaGroup.Controls.Add(btnBuscarRuta);
            card.Controls.Add(pnlRutaGroup);

            // 2. Sección: Contraseña
            lblPassProteger = new Label
            {
                Text = Localization.Get("lbl_pass").ToUpper(),
                Location = new Point(40, 148),
                ForeColor = UITheme.cTextSecondary,
                AutoSize = true,
                Font = new Font("Segoe UI", 8, FontStyle.Bold)
            };
            card.Controls.Add(lblPassProteger);

            btnOlvide = new Button
            {
                Text = Localization.Get("link_forgot"),
                AutoSize = true,
                Location = new Point(430, 144),
                Size = new Size(210, 22),
                FlatStyle = FlatStyle.Flat,
                ForeColor = UITheme.cAccentRed,
                Font = new Font("Segoe UI", 8, FontStyle.Regular),
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleRight
            };
            btnOlvide.FlatAppearance.BorderSize = 0;
            btnOlvide.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btnOlvide.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btnOlvide.Click += (s, e) => ForgotPasswordRequested?.Invoke();
            card.Controls.Add(btnOlvide);

            // Input Group Unificado para la Contraseña
            pnlPassGroup = new Panel
            {
                Location = new Point(40, 172),
                Size = new Size(600, 44),
                BackColor = UITheme.cInputBackground
            };
            pnlPassGroup.Paint += (s, e) =>
            {
                Color borderColor = _passFocused ? UITheme.cAccentRed : Color.FromArgb(52, 47, 47);
                ControlPaint.DrawBorder(e.Graphics, pnlPassGroup.ClientRectangle, borderColor, ButtonBorderStyle.Solid);
            };

            var lblPassIcon = new Label
            {
                Text = "🔑",
                Location = new Point(10, 10),
                Size = new Size(26, 24),
                Font = new Font("Segoe UI", 12),
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };
            pnlPassGroup.Controls.Add(lblPassIcon);

            txtContrasena = new TextBox
            {
                Location = new Point(40, 11),
                Size = new Size(512, 24),
                BorderStyle = BorderStyle.None,
                BackColor = UITheme.cInputBackground,
                ForeColor = UITheme.cTextPrimary,
                Font = new Font("Segoe UI", 10.5f),
                UseSystemPasswordChar = true
            };
            txtContrasena.GotFocus += (s, e) => { _passFocused = true; pnlPassGroup.Invalidate(); };
            txtContrasena.LostFocus += (s, e) => { _passFocused = false; pnlPassGroup.Invalidate(); };
            pnlPassGroup.Controls.Add(txtContrasena);

            btnEye = new Button
            {
                Text = "👁",
                Location = new Point(558, 5),
                Size = new Size(38, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(160, 150, 150),
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 11)
            };
            btnEye.FlatAppearance.BorderSize = 0;
            btnEye.MouseDown += (s, e) => { txtContrasena.UseSystemPasswordChar = false; btnEye.ForeColor = UITheme.cAccentRed; };
            btnEye.MouseUp += (s, e) => { txtContrasena.UseSystemPasswordChar = true; btnEye.ForeColor = Color.FromArgb(160, 150, 150); };
            pnlPassGroup.Controls.Add(btnEye);
            card.Controls.Add(pnlPassGroup);

            lblPassHint = new Label
            {
                Text = Localization.Get("lbl_pass_hint"),
                Location = new Point(40, 222),
                ForeColor = Color.FromArgb(130, 120, 120),
                AutoSize = true,
                Font = new Font("Segoe UI", 7.8f, FontStyle.Regular)
            };
            card.Controls.Add(lblPassHint);

            // 3. Botón de Acción Principal
            btnAccionGuardar = new Button
            {
                Text = "🔒 " + (Localization.Get("btn_lock") ?? "BLOQUEAR / CIFRAR CARPETA"),
                Size = new Size(600, 52),
                Location = new Point(40, 260),
                BackColor = UITheme.cAccentRed,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAccionGuardar.FlatAppearance.BorderSize = 0;
            btnAccionGuardar.FlatAppearance.MouseOverBackColor = UITheme.cAccentRedHover;
            btnAccionGuardar.Click += (s, e) => BloquearRequested?.Invoke(txtRuta.Text, txtContrasena.Text);
            card.Controls.Add(btnAccionGuardar);

            // Pie de garantía atómica
            lblGarantia = new Label
            {
                Text = Localization.Get("lbl_guarantee_atomic"),
                Location = new Point(40, 324),
                Size = new Size(600, 20),
                ForeColor = Color.FromArgb(120, 110, 110),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 7.8f, FontStyle.Regular)
            };
            card.Controls.Add(lblGarantia);

            Recentrar();
        }

        private static Label CrearBadge(string text, Color bg, Color fg, int x, int y)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                BackColor = bg,
                ForeColor = fg,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                Padding = new Padding(6, 3, 6, 3),
                AutoSize = true
            };
        }

        private void ConfigurarDragAndDrop()
        {
            this.DragEnter += (s, e) =>
            {
                if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    e.Effect = DragDropEffects.Copy;
                    _isDragOver = true;
                    pnlRutaGroup.Invalidate();
                }
            };

            this.DragLeave += (s, e) =>
            {
                _isDragOver = false;
                pnlRutaGroup.Invalidate();
            };

            this.DragDrop += (s, e) =>
            {
                _isDragOver = false;
                pnlRutaGroup.Invalidate();

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
            btnAccionGuardar.Text = procesando ? Localization.Get("status_processing") : "🔒 " + (Localization.Get("btn_lock") ?? "BLOQUEAR / CIFRAR CARPETA");
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
            lblPassProteger.Text = Localization.Get("lbl_pass").ToUpper();
            btnOlvide.Text = Localization.Get("link_forgot");
            btnAccionGuardar.Text = "🔒 " + (Localization.Get("btn_lock") ?? "BLOQUEAR / CIFRAR CARPETA");
            lblDropHint.Text = Localization.Get("lbl_drop_hint");
            lblPassHint.Text = Localization.Get("lbl_pass_hint");
            lblGarantia.Text = Localization.Get("lbl_guarantee_atomic");
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
            int startY = Math.Max(25, (this.ClientSize.Height - totalH) / 2);
            int x = Math.Max(20, (this.ClientSize.Width - card.Width) / 2);
            card.Location = new Point(x, startY + 100);
        }
    }
}
