using System.Runtime.InteropServices;

namespace FolderLocker
{
    public static class DarkDialogs
    {
        #region CONFIGURACIÓN VISUAL (THEME)

        private struct Theme
        {
            public static readonly Color Background = Color.FromArgb(20, 18, 18);
            public static readonly Color Surface = Color.FromArgb(35, 28, 28);
            public static readonly Color InputBg = Color.FromArgb(50, 40, 40);
            public static readonly Color Text = Color.FromArgb(245, 245, 245);
            public static readonly Color AccentRed = Color.FromArgb(198, 40, 40);
            public static readonly Color NeutralButton = Color.FromArgb(50, 50, 50);
            public static readonly Color SuccessGreen = Color.FromArgb(100, 200, 100);
            public static readonly Font BaseFont = new Font("Segoe UI", 11);
            public static readonly Font BoldFont = new Font("Segoe UI Semibold", 10, FontStyle.Bold);
        }

        // Win32 para mover ventanas modales
        [DllImport("user32.dll", EntryPoint = "ReleaseCapture")]
        private extern static void ReleaseCapture();
        [DllImport("user32.dll", EntryPoint = "SendMessage")]
        private extern static void SendMessage(System.IntPtr hwnd, int wmsg, int wparam, int lparam);

        private static void MoverVentana(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                if (sender is Control c && c.TopLevelControl is Form f)
                    SendMessage(f.Handle, 0xA1, 0x2, 0);
            }
        }

        #endregion

        #region MÉTODOS PÚBLICOS

        // 1. INFORMACIÓN (Enter o Esc cierran)
        public static void ShowInfo(string mensaje, string titulo = "Info")
        {
            var lbl = CrearLabel(mensaje, 20, 55, 360);
            int altura = Math.Max(200, 120 + lbl.PreferredHeight);

            using var form = CrearBase(titulo, 400, altura);
            form.Controls.Add(lbl);

            var btnOk = CrearBoton(Localization.Get("btn_accept"), Theme.AccentRed, 125, altura - 60);
            btnOk.Click += (s, e) => form.Close();
            form.Controls.Add(btnOk);

            // --- KEYBOARD SUPPORT ---
            form.AcceptButton = btnOk; // Enter activa este botón
            form.CancelButton = btnOk; // Esc activa este botón (porque solo hay uno)

            form.ShowDialog();
        }

        // 2. CONFIRMACIÓN (Enter = Sí, Esc = No)
        public static DialogResult ShowConfirm(string mensaje, string titulo = "Confirm")
        {
            var lbl = CrearLabel(mensaje, 20, 55, 360);
            int altura = Math.Max(220, 120 + lbl.PreferredHeight);

            using var form = CrearBase(titulo, 400, altura);
            form.Controls.Add(lbl);

            int yBotones = altura - 60;

            var btnSi = CrearBoton(Localization.Get("btn_yes"), Theme.AccentRed, 40, yBotones);
            btnSi.DialogResult = DialogResult.Yes;

            var btnNo = CrearBoton(Localization.Get("btn_cancel"), Theme.NeutralButton, 210, yBotones);
            btnNo.DialogResult = DialogResult.No;

            form.Controls.AddRange(new Control[] { btnSi, btnNo });

            // --- KEYBOARD SUPPORT ---
            form.AcceptButton = btnSi; // Enter confirma
            form.CancelButton = btnNo; // Esc cancela

            // Por seguridad, el foco inicial va al "No", pero Enter sigue activando el "Sí"
            // (Windows prefiere que el foco coincida con AcceptButton, pero esto es más seguro para datos)
            form.ActiveControl = btnNo;

            return form.ShowDialog();
        }

        // 3. ENTRADA DE DATOS (Enter en TextBox envía, Esc cancela)
        public static string ShowInput(string mensaje, string titulo = "Input", bool esPassword = false)
        {
            using var form = CrearBase(titulo, 400, 240);

            var lbl = CrearLabel(mensaje, 20, 50, 360);
            form.Controls.Add(lbl);

            var txt = new TextBox
            {
                Location = new Point(40, 110),
                Size = new Size(320, 30),
                BackColor = Theme.InputBg,
                ForeColor = Theme.Text,
                BorderStyle = BorderStyle.FixedSingle,
                Font = Theme.BaseFont,
                UseSystemPasswordChar = esPassword
            };
            form.Controls.Add(txt);

            var btnOk = CrearBoton(Localization.Get("btn_accept"), Theme.AccentRed, 125, 170);
            btnOk.DialogResult = DialogResult.OK;
            form.Controls.Add(btnOk);

            // Botón fantasma para manejar Cancel (Esc)
            var btnCancel = new Button { DialogResult = DialogResult.Cancel, Location = new Point(-100, -100) };
            form.Controls.Add(btnCancel);

            // --- KEYBOARD SUPPORT ---
            form.AcceptButton = btnOk;     // Enter activa "Aceptar"
            form.CancelButton = btnCancel; // Esc activa "Cancelar"

            // Evento KeyDown explícito en el TextBox para asegurar que Enter no haga saltos de línea (si fuera multiline)
            // y dispare el botón inmediatamente.
            txt.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true; // Evita el "beep" de Windows
                    btnOk.PerformClick();
                }
            };

            // Poner foco en el input al mostrar
            form.Shown += (s, e) => txt.Focus();

            return form.ShowDialog() == DialogResult.OK ? txt.Text : "";
        }

        // 4. RESULTADO CON COPIA (Enter o Esc cierran)
        public static void ShowResultWithCopy(string mensaje, string textoParaCopiar, IWin32Window owner = null)
        {
            using var form = CrearBase("Result", 450, 320);

            // Si nos pasan un dueño, nos centramos en él. Si no, al centro de la pantalla.
            if (owner != null)
            {
                form.StartPosition = FormStartPosition.CenterParent;
            }

            var lbl = CrearLabel(mensaje, 20, 50, 410);
            form.Controls.Add(lbl);

            var txtRuta = new TextBox
            {
                Location = new Point(40, 160),
                Size = new Size(370, 30),
                Text = textoParaCopiar,
                BackColor = Theme.InputBg,
                ForeColor = Theme.SuccessGreen,
                Font = new Font("Consolas", 11),
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = true,
                TextAlign = HorizontalAlignment.Center
            };
            form.Controls.Add(txtRuta);

            var btnOk = CrearBoton(Localization.Get("btn_ready"), Theme.AccentRed, 150, 230);
            btnOk.Click += (s, e) => form.Close();
            form.Controls.Add(btnOk);

            form.AcceptButton = btnOk;
            form.CancelButton = btnOk;

            form.Shown += (s, e) => { txtRuta.Focus(); txtRuta.SelectAll(); };

            // MOSTRAR: Si hay dueño, usamos ShowDialog(owner)
            if (owner != null) form.ShowDialog(owner);
            else form.ShowDialog();
        }

        #endregion

        #region FACTORY HELPERS

        private static Form CrearBase(string titulo, int w, int h)
        {
            var f = new Form
            {
                Text = titulo,
                Size = new Size(w, h),
                BackColor = Theme.Background,
                ForeColor = Theme.Text,
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.CenterScreen,
                ShowInTaskbar = false,
                KeyPreview = true // IMPORTANTE: Permite al Form interceptar teclas antes que los controles
            };

            // Borde exterior simple
            f.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, f.ClientRectangle, Color.FromArgb(60, 60, 60), ButtonBorderStyle.Solid);

            var header = new Panel { Height = 40, Dock = DockStyle.Top, BackColor = Theme.Surface };
            header.MouseDown += MoverVentana; // Habilitar arrastre desde el header

            var titleLbl = new Label
            {
                Text = titulo,
                AutoSize = true,
                Location = new Point(15, 10),
                Font = Theme.BoldFont,
                ForeColor = Theme.AccentRed
            };
            titleLbl.MouseDown += MoverVentana; // Habilitar arrastre desde el título

            header.Controls.Add(titleLbl);
            f.Controls.Add(header);

            return f;
        }

        private static Label CrearLabel(string texto, int x, int y, int w)
        {
            return new Label
            {
                Text = texto,
                Location = new Point(x, y),
                Width = w,
                Font = Theme.BaseFont,
                ForeColor = Theme.Text,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.TopCenter,
                AutoSize = true,
                MaximumSize = new Size(w, 0)
            };
        }

        private static Button CrearBoton(string texto, Color color, int x, int y)
        {
            var btn = new Button
            {
                Text = texto,
                Location = new Point(x, y),
                Size = new Size(150, 40),
                FlatStyle = FlatStyle.Flat,
                BackColor = color,
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Font = Theme.BoldFont
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        #endregion
    }
}