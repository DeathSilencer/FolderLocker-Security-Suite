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
        public static void ShowInfo(string mensaje, string titulo = "Info", FormCarpetas formCarpetas = null)
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

            if (formCarpetas != null)
            {
                form.StartPosition = FormStartPosition.CenterParent;
                form.ShowDialog(formCarpetas);
            }
            else
            {
                form.ShowDialog();
            }
        }

        // 2. CONFIRMACIÓN (Enter = Sí, Esc = No)
        public static DialogResult ShowConfirm(string mensaje, string titulo = "Confirm", IWin32Window? owner = null, int ancho = 400, bool alinearIzquierda = false)
        {
            var lbl = CrearLabel(mensaje, 20, 55, ancho - 40);
            if (alinearIzquierda) lbl.TextAlign = ContentAlignment.TopLeft;

            int altura = Math.Max(220, 130 + lbl.PreferredHeight);

            using var form = CrearBase(titulo, ancho, altura);
            form.Controls.Add(lbl);

            int yBotones = altura - 60;
            int espaciado = Math.Max(20, (ancho - 320) / 3);

            var btnSi = CrearBoton(Localization.Get("btn_yes"), Theme.AccentRed, espaciado, yBotones);
            btnSi.DialogResult = DialogResult.Yes;

            var btnNo = CrearBoton(Localization.Get("btn_cancel"), Theme.NeutralButton, espaciado * 2 + 150, yBotones);
            btnNo.DialogResult = DialogResult.No;

            form.Controls.AddRange(new Control[] { btnSi, btnNo });

            // --- KEYBOARD SUPPORT ---
            form.AcceptButton = btnSi; // Enter confirma
            form.CancelButton = btnNo; // Esc cancela

            // Por seguridad, el foco inicial va al "No", pero Enter sigue activando el "Sí"
            // (Windows prefiere que el foco coincida con AcceptButton, pero esto es más seguro para datos)
            form.ActiveControl = btnNo;

            if (owner != null)
            {
                form.StartPosition = FormStartPosition.CenterParent;
                return form.ShowDialog(owner);
            }

            return form.ShowDialog();
        }

        // 2.1. RESUMEN VISUAL DE PRE-ESCANEO (KPIs, Path Card y Callout)
        public static DialogResult ShowPreScanSummary(
            IWin32Window? owner,
            string ruta,
            int totalArchivos,
            string tamanoTexto,
            string tiempoEstimado,
            bool esVolumenGrande)
        {
            int w = 530;
            int h = esVolumenGrande ? 465 : 445;
            string titulo = esVolumenGrande
                ? (Localization.CurrentLang == "EN" ? "⚠️ Warning: Large File Volume" : "⚠️ Advertencia: Gran Volumen")
                : (Localization.CurrentLang == "EN" ? "🛡️ Security Summary" : "🛡️ Resumen de Seguridad");

            using var form = CrearBase(titulo, w, h);

            // Botón cerrar en el header
            var btnClose = new Button
            {
                Text = "✕",
                Size = new Size(36, 30),
                Location = new Point(w - 42, 5),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(180, 170, 170),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(198, 40, 40);
            btnClose.Click += (s, e) => { form.DialogResult = DialogResult.No; form.Close(); };
            if (form.Controls.Count > 0 && form.Controls[0] is Panel hdr)
            {
                hdr.Controls.Add(btnClose);
                btnClose.BringToFront();
            }

            // 1. Tarjeta de Ruta
            var pnlPath = new Panel
            {
                Location = new Point(25, 52),
                Size = new Size(480, 56),
                BackColor = Color.FromArgb(28, 25, 25)
            };
            pnlPath.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, pnlPath.ClientRectangle, Color.FromArgb(50, 44, 44), ButtonBorderStyle.Solid);

            var lblFolderIcon = new Label
            {
                Text = "📁",
                Font = new Font("Segoe UI", 14),
                Location = new Point(12, 12),
                Size = new Size(30, 30),
                BackColor = Color.Transparent
            };
            pnlPath.Controls.Add(lblFolderIcon);

            var lblFolderName = new Label
            {
                Text = Path.GetFileName(ruta),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(48, 8),
                Size = new Size(420, 20),
                AutoEllipsis = true,
                BackColor = Color.Transparent
            };
            pnlPath.Controls.Add(lblFolderName);

            var lblFullPath = new Label
            {
                Text = ruta,
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(160, 150, 150),
                Location = new Point(48, 30),
                Size = new Size(420, 18),
                AutoEllipsis = true,
                BackColor = Color.Transparent
            };
            pnlPath.Controls.Add(lblFullPath);
            form.Controls.Add(pnlPath);

            // 2. Tres KPI Cards
            int cardW = 153;
            int cardH = 72;
            int cardY = 118;

            // KPI 1: Archivos
            var pnlKpi1 = new Panel { Location = new Point(25, cardY), Size = new Size(cardW, cardH), BackColor = Color.FromArgb(28, 25, 25) };
            pnlKpi1.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, pnlKpi1.ClientRectangle, Color.FromArgb(50, 44, 44), ButtonBorderStyle.Solid);
            var lblKpi1Tag = new Label
            {
                Text = "📄 " + (Localization.CurrentLang == "EN" ? "FILES" : "ARCHIVOS"),
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(160, 150, 150),
                Location = new Point(10, 8),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            var lblKpi1Val = new Label
            {
                Text = $"{totalArchivos:N0}",
                Font = new Font("Segoe UI Black", 13, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(10, 28),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            pnlKpi1.Controls.AddRange(new Control[] { lblKpi1Tag, lblKpi1Val });
            form.Controls.Add(pnlKpi1);

            // KPI 2: Tamaño
            var pnlKpi2 = new Panel { Location = new Point(188, cardY), Size = new Size(cardW, cardH), BackColor = Color.FromArgb(28, 25, 25) };
            pnlKpi2.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, pnlKpi2.ClientRectangle, Color.FromArgb(50, 44, 44), ButtonBorderStyle.Solid);
            var lblKpi2Tag = new Label
            {
                Text = "💾 " + (Localization.CurrentLang == "EN" ? "TOTAL SIZE" : "TAMAÑO TOTAL"),
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(160, 150, 150),
                Location = new Point(10, 8),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            var lblKpi2Val = new Label
            {
                Text = tamanoTexto,
                Font = new Font("Segoe UI Black", 13, FontStyle.Bold),
                ForeColor = Theme.AccentRed,
                Location = new Point(10, 28),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            pnlKpi2.Controls.AddRange(new Control[] { lblKpi2Tag, lblKpi2Val });
            form.Controls.Add(pnlKpi2);

            // KPI 3: Tiempo
            var pnlKpi3 = new Panel { Location = new Point(352, cardY), Size = new Size(cardW, cardH), BackColor = Color.FromArgb(28, 25, 25) };
            pnlKpi3.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, pnlKpi3.ClientRectangle, Color.FromArgb(50, 44, 44), ButtonBorderStyle.Solid);
            var lblKpi3Tag = new Label
            {
                Text = "⏱️ " + (Localization.CurrentLang == "EN" ? "EST. TIME" : "TIEMPO EST."),
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(160, 150, 150),
                Location = new Point(10, 8),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            var lblKpi3Val = new Label
            {
                Text = tiempoEstimado,
                Font = new Font("Segoe UI Black", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(56, 189, 248),
                Location = new Point(10, 30),
                Size = new Size(135, 34),
                AutoEllipsis = true,
                BackColor = Color.Transparent
            };
            pnlKpi3.Controls.AddRange(new Control[] { lblKpi3Tag, lblKpi3Val });
            form.Controls.Add(pnlKpi3);

            // 3. Banner de Seguridad o Advertencia
            int bannerY = 200;
            int bannerH = esVolumenGrande ? 76 : 62;
            var pnlBanner = new Panel
            {
                Location = new Point(25, bannerY),
                Size = new Size(480, bannerH),
                BackColor = esVolumenGrande ? Color.FromArgb(42, 20, 20) : Color.FromArgb(20, 34, 25)
            };
            Color borderAccent = esVolumenGrande ? Theme.AccentRed : Color.FromArgb(16, 185, 129);
            pnlBanner.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, pnlBanner.ClientRectangle, Color.FromArgb(50, 44, 44), ButtonBorderStyle.Solid);
                using var b = new SolidBrush(borderAccent);
                e.Graphics.FillRectangle(b, 0, 0, 4, pnlBanner.Height);
            };

            var lblBannerText = new Label
            {
                Text = esVolumenGrande
                    ? (Localization.CurrentLang == "EN"
                        ? "⚠️ Notice: Large folder detected. The encryption process runs in transactional batches (Two-Phase Commit) to guarantee zero file loss."
                        : "⚠️ Aviso: Gran volumen detectado. El cifrado se realizará en lotes transaccionales (Two-Phase Commit) para garantizar cero pérdida de datos.")
                    : (Localization.CurrentLang == "EN"
                        ? "🛡️ Maximum Security: Folder will be protected with military AES-256 CTR encryption and authenticated integrity."
                        : "🛡️ Máxima Seguridad: La carpeta será protegida con cifrado militar AES-256 CTR e integridad autenticada."),
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = esVolumenGrande ? Color.FromArgb(254, 202, 202) : Color.FromArgb(167, 243, 208),
                Location = new Point(16, 10),
                Size = new Size(450, bannerH - 20),
                BackColor = Color.Transparent
            };
            pnlBanner.Controls.Add(lblBannerText);
            form.Controls.Add(pnlBanner);

            // 4. Pregunta
            int promptY = bannerY + bannerH + 12;
            var lblPrompt = new Label
            {
                Text = Localization.CurrentLang == "EN" ? "Do you want to proceed with encryption?" : "¿Deseas iniciar la encriptación ahora?",
                Font = new Font("Segoe UI Semibold", 9.5f),
                ForeColor = Color.FromArgb(240, 240, 240),
                Location = new Point(25, promptY),
                Size = new Size(480, 22),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            form.Controls.Add(lblPrompt);

            // 5. Botones de acción
            int btnY = promptY + 30;
            var btnSi = new Button
            {
                Text = Localization.CurrentLang == "EN" ? "🔒 PROCEED ENCRYPTION" : "🔒 INICIAR CIFRADO",
                Location = new Point(25, btnY),
                Size = new Size(250, 45),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.AccentRed,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand,
                DialogResult = DialogResult.Yes
            };
            btnSi.FlatAppearance.BorderSize = 0;
            btnSi.FlatAppearance.MouseOverBackColor = Color.FromArgb(220, 38, 38);

            var btnNo = new Button
            {
                Text = Localization.CurrentLang == "EN" ? "CANCEL" : "CANCELAR",
                Location = new Point(285, btnY),
                Size = new Size(220, 45),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(48, 42, 42),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                DialogResult = DialogResult.No
            };
            btnNo.FlatAppearance.BorderSize = 0;
            btnNo.FlatAppearance.MouseOverBackColor = Color.FromArgb(64, 56, 56);

            form.Controls.AddRange(new Control[] { btnSi, btnNo });

            // Keyboard support
            form.AcceptButton = btnSi;
            form.CancelButton = btnNo;
            form.ActiveControl = btnNo;

            // Centrado sobre el propietario
            if (owner != null)
            {
                form.StartPosition = FormStartPosition.CenterParent;
                return form.ShowDialog(owner);
            }

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