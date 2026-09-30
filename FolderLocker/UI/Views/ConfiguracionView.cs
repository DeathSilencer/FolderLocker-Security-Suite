using FolderLocker.Services.Protection;
using FolderLocker.UI.Common;
using Microsoft.Win32;

namespace FolderLocker.UI.Views
{
    public class ConfiguracionView : UserControl
    {
        private Label lblTituloConfig = null!;
        private Label lblSubtituloConfig = null!;
        private Panel card = null!;

        private Label lblSecGeneral = null!;
        private ComboBox cmbIdioma = null!;

        private Label lblSecSystem = null!;
        private Panel pnlStartup = null!;
        private Label lblStartupTitle = null!;
        private Label lblStartupSub = null!;
        private CheckBox chkInicioWindows = null!;

        private Panel pnlJunkFilter = null!;
        private Label lblJunkTitle = null!;
        private Label lblJunkSub = null!;
        private CheckBox chkJunkFilter = null!;

        private Panel pnlAutoLock = null!;
        private Label lblAutoLockTitle = null!;
        private Label lblAutoLockSub = null!;
        private ComboBox cmbAutoLock = null!;

        private Label lblSecData = null!;
        private Panel pnlDanger = null!;
        private Label lblDangerTitle = null!;
        private Label lblDangerSub = null!;
        private Button btnReset = null!;

        private Label lblSecDev = null!;
        private Panel pnlDevInfo = null!;
        private Label lblDevTitle = null!;
        private Label lblDevSub = null!;
        private Button btnCreditos = null!;

        private Label lblFooter = null!;

        public event Action? IdiomaChanged;
        public event Action? VerCreditosRequested;
        public event Action? FactoryResetRequested;
        public event Action<int>? AutoLockChanged;

        public Panel CardPanel => card;

        public ConfiguracionView()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = UITheme.cBackground;
            InicializarComponentes();
        }

        private void InicializarComponentes()
        {
            lblTituloConfig = new Label
            {
                Text = Localization.Get("menu_config") ?? "CONFIGURACIÓN",
                ForeColor = UITheme.cTextPrimary,
                Font = new Font("Segoe UI Semibold", 20, FontStyle.Bold),
                AutoSize = true
            };
            this.Controls.Add(lblTituloConfig);

            lblSubtituloConfig = new Label
            {
                Text = Localization.CurrentLang == "EN"
                    ? "Manage language settings, system startup, and local cryptographic database."
                    : "Administra el idioma de la aplicación, el arranque del sistema y los datos criptográficos locales.",
                ForeColor = Color.FromArgb(160, 150, 150),
                Font = new Font("Segoe UI", 9.5f),
                AutoSize = true
            };
            this.Controls.Add(lblSubtituloConfig);

            // Tarjeta principal (680 x 510)
            card = new Panel
            {
                Size = new Size(680, 510),
                BackColor = UITheme.cSurface
            };
            card.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, card.ClientRectangle, UITheme.cBorder, ButtonBorderStyle.Solid);
                using var b = new SolidBrush(UITheme.cAccentRed);
                e.Graphics.FillRectangle(b, 0, 0, card.Width, 3);
            };
            this.Controls.Add(card);

            // Badges superiores
            int badgeY = 18;
            var badge1 = CrearBadge("⚙ PREFERENCIAS DEL SISTEMA", Color.FromArgb(50, 22, 22), Color.FromArgb(252, 165, 165), 40, badgeY);
            var badge2 = CrearBadge("🔒 SEGURIDAD LOCAL", Color.FromArgb(36, 33, 33), Color.FromArgb(209, 213, 219), 245, badgeY);
            card.Controls.AddRange(new Control[] { badge1, badge2 });

            // 1. Sección: Idioma
            lblSecGeneral = new Label
            {
                Text = (Localization.Get("cfg_sec_general") ?? "IDIOMA Y REGIÓN").ToUpper(),
                Location = new Point(40, 54),
                ForeColor = UITheme.cTextSecondary,
                AutoSize = true,
                Font = new Font("Segoe UI", 8, FontStyle.Bold)
            };
            card.Controls.Add(lblSecGeneral);

            cmbIdioma = new ComboBox
            {
                Location = new Point(40, 76),
                Size = new Size(600, 40),
                FlatStyle = FlatStyle.Flat,
                BackColor = UITheme.cInputBackground,
                ForeColor = UITheme.cTextPrimary,
                Font = new Font("Segoe UI", 10.5f),
                DropDownStyle = ComboBoxStyle.DropDownList,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 36
            };
            cmbIdioma.Items.AddRange(new object[] { "Español (MX)", "English (EN)", "Português (PT)", "Русский (RU)", "汉语 (CN)" });
            cmbIdioma.DrawItem += UITheme.DibujarComboConBanderas;
            cmbIdioma.SelectedIndexChanged += (s, e) =>
            {
                string lang = "ES";
                if (cmbIdioma.SelectedIndex == 1) lang = "EN";
                else if (cmbIdioma.SelectedIndex == 2) lang = "PT";
                else if (cmbIdioma.SelectedIndex == 3) lang = "RU";
                else if (cmbIdioma.SelectedIndex == 4) lang = "CN";

                if (Localization.CurrentLang != lang)
                {
                    Localization.CurrentLang = lang;
                    Properties.Settings.Default.Idioma = lang;
                    Properties.Settings.Default.Save();
                    IdiomaChanged?.Invoke();
                }
            };
            card.Controls.Add(cmbIdioma);

            // 2. Sección: Sistema y Arranque
            lblSecSystem = new Label
            {
                Text = (Localization.Get("cfg_sec_system") ?? "SISTEMA Y ARRANQUE").ToUpper(),
                Location = new Point(40, 130),
                ForeColor = UITheme.cTextSecondary,
                AutoSize = true,
                Font = new Font("Segoe UI", 8, FontStyle.Bold)
            };
            card.Controls.Add(lblSecSystem);

            pnlStartup = new Panel
            {
                Location = new Point(40, 148),
                Size = new Size(600, 48),
                BackColor = UITheme.cInputBackground
            };
            pnlStartup.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, pnlStartup.ClientRectangle, Color.FromArgb(50, 44, 44), ButtonBorderStyle.Solid);

            var lblStartupIcon = new Label
            {
                Text = "🚀",
                Location = new Point(12, 12),
                Size = new Size(26, 24),
                Font = new Font("Segoe UI", 12),
                BackColor = Color.Transparent
            };
            pnlStartup.Controls.Add(lblStartupIcon);

            lblStartupTitle = new Label
            {
                Text = Localization.CurrentLang == "EN" ? "Start with Windows" : "Iniciar con Windows",
                Location = new Point(46, 6),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            pnlStartup.Controls.Add(lblStartupTitle);

            lblStartupSub = new Label
            {
                Text = Localization.CurrentLang == "EN"
                    ? "Launch FolderLocker minimized in the background on startup."
                    : "Ejecutar FolderLocker minimizado en la bandeja al encender el equipo.",
                Location = new Point(46, 26),
                ForeColor = Color.FromArgb(150, 140, 140),
                Font = new Font("Segoe UI", 8),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            pnlStartup.Controls.Add(lblStartupSub);

            chkInicioWindows = new CheckBox
            {
                Location = new Point(560, 12),
                Size = new Size(24, 24),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            try
            {
                using RegistryKey? rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", false);
                if (rk != null) chkInicioWindows.Checked = rk.GetValue("FolderLocker") != null;
            }
            catch { }
            chkInicioWindows.CheckedChanged += (s, e) => SetStartup(chkInicioWindows.Checked);
            pnlStartup.Controls.Add(chkInicioWindows);
            card.Controls.Add(pnlStartup);

            // Filtro de Archivos Innecesarios / Temporales
            pnlJunkFilter = new Panel
            {
                Location = new Point(40, 202),
                Size = new Size(600, 48),
                BackColor = UITheme.cInputBackground
            };
            pnlJunkFilter.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, pnlJunkFilter.ClientRectangle, Color.FromArgb(50, 44, 44), ButtonBorderStyle.Solid);

            var lblJunkIcon = new Label
            {
                Text = "🧹",
                Location = new Point(12, 12),
                Size = new Size(26, 24),
                Font = new Font("Segoe UI", 12),
                BackColor = Color.Transparent
            };
            pnlJunkFilter.Controls.Add(lblJunkIcon);

            lblJunkTitle = new Label
            {
                Text = Localization.CurrentLang == "EN" ? "Junk / Temp File Filter" : "Filtro de Archivos Innecesarios",
                Location = new Point(46, 6),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            pnlJunkFilter.Controls.Add(lblJunkTitle);

            lblJunkSub = new Label
            {
                Text = Localization.CurrentLang == "EN"
                    ? "Skip and clean thumbs.db, desktop.ini, .tmp and temporary locks."
                    : "Omitir y sanear archivos thumbs.db, desktop.ini, .tmp y bloqueos temporales.",
                Location = new Point(46, 26),
                ForeColor = Color.FromArgb(150, 140, 140),
                Font = new Font("Segoe UI", 8),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            pnlJunkFilter.Controls.Add(lblJunkSub);

            chkJunkFilter = new CheckBox
            {
                Location = new Point(560, 12),
                Size = new Size(24, 24),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                Checked = JunkFileFilter.IsFilterEnabled
            };
            chkJunkFilter.CheckedChanged += (s, e) => JunkFileFilter.IsFilterEnabled = chkJunkFilter.Checked;
            pnlJunkFilter.Controls.Add(chkJunkFilter);
            card.Controls.Add(pnlJunkFilter);

            // Auto-Bloqueo por Inactividad
            pnlAutoLock = new Panel
            {
                Location = new Point(40, 256),
                Size = new Size(600, 48),
                BackColor = UITheme.cInputBackground
            };
            pnlAutoLock.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, pnlAutoLock.ClientRectangle, Color.FromArgb(50, 44, 44), ButtonBorderStyle.Solid);

            var lblAutoLockIcon = new Label
            {
                Text = "⏱️",
                Location = new Point(12, 12),
                Size = new Size(26, 24),
                Font = new Font("Segoe UI", 12),
                BackColor = Color.Transparent
            };
            pnlAutoLock.Controls.Add(lblAutoLockIcon);

            lblAutoLockTitle = new Label
            {
                Text = Localization.Get("cfg_autolock_title") ?? (Localization.CurrentLang == "EN" ? "Inactivity Auto-Lock" : "Auto-Bloqueo por Inactividad"),
                Location = new Point(46, 6),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            pnlAutoLock.Controls.Add(lblAutoLockTitle);

            lblAutoLockSub = new Label
            {
                Text = Localization.Get("cfg_autolock_sub") ?? (Localization.CurrentLang == "EN"
                    ? "Lock session and unmount virtual drives after idle time."
                    : "Bloquear sesión y cerrar unidades tras tiempo inactivo."),
                Location = new Point(46, 26),
                ForeColor = Color.FromArgb(150, 140, 140),
                Font = new Font("Segoe UI", 8),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            pnlAutoLock.Controls.Add(lblAutoLockSub);

            cmbAutoLock = new ComboBox
            {
                Location = new Point(415, 9),
                Size = new Size(171, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(48, 42, 42),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8.8f),
                Cursor = Cursors.Hand
            };
            PoblarComboAutoLock();
            cmbAutoLock.SelectedIndexChanged += (s, e) =>
            {
                int[] minutesMap = { 0, 1, 3, 5, 10, 15, 30 };
                if (cmbAutoLock.SelectedIndex >= 0 && cmbAutoLock.SelectedIndex < minutesMap.Length)
                {
                    int min = minutesMap[cmbAutoLock.SelectedIndex];
                    Properties.Settings.Default.AutoLockMinutos = min;
                    Properties.Settings.Default.Save();
                    AutoLockChanged?.Invoke(min);
                }
            };
            pnlAutoLock.Controls.Add(cmbAutoLock);
            card.Controls.Add(pnlAutoLock);

            // 3. Sección: Datos y Zona de Peligro
            lblSecData = new Label
            {
                Text = (Localization.Get("cfg_sec_data") ?? "ZONA DE DATOS LOCALES").ToUpper(),
                Location = new Point(40, 312),
                ForeColor = UITheme.cTextSecondary,
                AutoSize = true,
                Font = new Font("Segoe UI", 8, FontStyle.Bold)
            };
            card.Controls.Add(lblSecData);

            pnlDanger = new Panel
            {
                Location = new Point(40, 332),
                Size = new Size(600, 52),
                BackColor = Color.FromArgb(36, 20, 20)
            };
            pnlDanger.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, pnlDanger.ClientRectangle, Color.FromArgb(64, 28, 28), ButtonBorderStyle.Solid);
                using var b = new SolidBrush(UITheme.cAccentRed);
                e.Graphics.FillRectangle(b, 0, 0, 3, pnlDanger.Height);
            };

            var lblDangerIcon = new Label
            {
                Text = "⚠️",
                Location = new Point(12, 13),
                Size = new Size(26, 24),
                Font = new Font("Segoe UI", 12),
                BackColor = Color.Transparent
            };
            pnlDanger.Controls.Add(lblDangerIcon);

            lblDangerTitle = new Label
            {
                Text = Localization.CurrentLang == "EN" ? "Factory Reset" : "Restablecimiento de Fábrica",
                Location = new Point(46, 7),
                ForeColor = Color.FromArgb(254, 202, 202),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            pnlDanger.Controls.Add(lblDangerTitle);

            lblDangerSub = new Label
            {
                Text = Localization.CurrentLang == "EN"
                    ? "Deletes local user database and restores the suite to initial state."
                    : "Elimina la base de datos de usuarios local y restablece la suite.",
                Location = new Point(46, 27),
                ForeColor = Color.FromArgb(200, 180, 180),
                Font = new Font("Segoe UI", 8),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            pnlDanger.Controls.Add(lblDangerSub);

            btnReset = new Button
            {
                Text = Localization.CurrentLang == "EN" ? "RESET" : "RESTABLECER",
                Location = new Point(450, 8),
                Size = new Size(136, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = UITheme.cAccentRed,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnReset.FlatAppearance.BorderSize = 0;
            btnReset.FlatAppearance.MouseOverBackColor = UITheme.cAccentRedHover;
            btnReset.Click += (s, e) => FactoryResetRequested?.Invoke();
            pnlDanger.Controls.Add(btnReset);
            card.Controls.Add(pnlDanger);

            // 4. Sección: Acerca de y Créditos
            lblSecDev = new Label
            {
                Text = "ACERCA DE Y CRÉDITOS",
                Location = new Point(40, 392),
                ForeColor = UITheme.cTextSecondary,
                AutoSize = true,
                Font = new Font("Segoe UI", 8, FontStyle.Bold)
            };
            card.Controls.Add(lblSecDev);

            pnlDevInfo = new Panel
            {
                Location = new Point(40, 412),
                Size = new Size(600, 52),
                BackColor = UITheme.cInputBackground
            };
            pnlDevInfo.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, pnlDevInfo.ClientRectangle, Color.FromArgb(50, 44, 44), ButtonBorderStyle.Solid);

            var lblDevIcon = new Label
            {
                Text = "👨‍💻",
                Location = new Point(12, 13),
                Size = new Size(26, 24),
                Font = new Font("Segoe UI", 12),
                BackColor = Color.Transparent
            };
            pnlDevInfo.Controls.Add(lblDevIcon);

            lblDevTitle = new Label
            {
                Text = "FolderLocker Security Suite",
                Location = new Point(46, 7),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            pnlDevInfo.Controls.Add(lblDevTitle);

            lblDevSub = new Label
            {
                Text = "Desarrollado con Dokan & AES-256 CTR • Arquitectura de Cifrado Atómico",
                Location = new Point(46, 27),
                ForeColor = Color.FromArgb(150, 140, 140),
                Font = new Font("Segoe UI", 8),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            pnlDevInfo.Controls.Add(lblDevSub);

            btnCreditos = new Button
            {
                Text = Localization.CurrentLang == "EN" ? "CREDITS" : "CRÉDITOS",
                Location = new Point(450, 9),
                Size = new Size(136, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(52, 46, 46),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCreditos.FlatAppearance.BorderSize = 0;
            btnCreditos.FlatAppearance.MouseOverBackColor = Color.FromArgb(70, 62, 62);
            btnCreditos.Click += (s, e) => VerCreditosRequested?.Invoke();
            pnlDevInfo.Controls.Add(btnCreditos);
            card.Controls.Add(pnlDevInfo);

            // Pie
            lblFooter = new Label
            {
                Text = "FolderLocker Security Suite • Build 2026.1 • Suite Criptográfica Segura",
                Location = new Point(40, 474),
                Size = new Size(600, 20),
                ForeColor = Color.FromArgb(110, 100, 100),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 7.8f, FontStyle.Regular)
            };
            card.Controls.Add(lblFooter);

            SincronizarIdiomaCombo();
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

        private void SincronizarIdiomaCombo()
        {
            switch (Localization.CurrentLang)
            {
                case "EN": cmbIdioma.SelectedIndex = 1; break;
                case "PT": cmbIdioma.SelectedIndex = 2; break;
                case "RU": cmbIdioma.SelectedIndex = 3; break;
                case "CN": cmbIdioma.SelectedIndex = 4; break;
                default: cmbIdioma.SelectedIndex = 0; break;
            }
        }

        private void SetStartup(bool start)
        {
            try
            {
                using RegistryKey? rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                if (rk != null)
                {
                    if (start) rk.SetValue("FolderLocker", Application.ExecutablePath);
                    else rk.DeleteValue("FolderLocker", false);
                }
            }
            catch { }
        }

        private void PoblarComboAutoLock()
        {
            if (cmbAutoLock == null) return;
            int currentMin = Properties.Settings.Default.AutoLockMinutos;
            cmbAutoLock.Items.Clear();
            if (Localization.CurrentLang == "EN")
            {
                cmbAutoLock.Items.AddRange(new object[]
                {
                    "Disabled",
                    "1 minute (Test)",
                    "3 minutes",
                    "5 minutes (Recommended)",
                    "10 minutes",
                    "15 minutes",
                    "30 minutes"
                });
            }
            else
            {
                cmbAutoLock.Items.AddRange(new object[]
                {
                    "Desactivado",
                    "1 minuto (Prueba)",
                    "3 minutos",
                    "5 minutos (Recomendado)",
                    "10 minutos",
                    "15 minutos",
                    "30 minutos"
                });
            }

            int[] minutesMap = { 0, 1, 3, 5, 10, 15, 30 };
            int sel = Array.IndexOf(minutesMap, currentMin);
            cmbAutoLock.SelectedIndex = sel >= 0 ? sel : 3;
        }

        public void ActualizarIdioma()
        {
            lblTituloConfig.Text = Localization.Get("menu_config") ?? "CONFIGURACIÓN";
            lblSubtituloConfig.Text = Localization.CurrentLang == "EN"
                ? "Manage language settings, system startup, and local cryptographic database."
                : "Administra el idioma de la aplicación, el arranque del sistema y los datos criptográficos locales.";
            lblSecGeneral.Text = (Localization.Get("cfg_sec_general") ?? "IDIOMA Y REGIÓN").ToUpper();
            lblSecSystem.Text = (Localization.Get("cfg_sec_system") ?? "SISTEMA Y ARRANQUE").ToUpper();
            lblSecData.Text = (Localization.Get("cfg_sec_data") ?? "ZONA DE DATOS LOCALES").ToUpper();
            lblStartupTitle.Text = Localization.CurrentLang == "EN" ? "Start with Windows" : "Iniciar con Windows";
            lblStartupSub.Text = Localization.CurrentLang == "EN"
                ? "Launch FolderLocker minimized in the background on startup."
                : "Ejecutar FolderLocker minimizado en la bandeja al encender el equipo.";
            lblJunkTitle.Text = Localization.CurrentLang == "EN" ? "Junk / Temp File Filter" : "Filtro de Archivos Innecesarios";
            lblJunkSub.Text = Localization.CurrentLang == "EN"
                ? "Skip and clean thumbs.db, desktop.ini, .tmp and temporary locks."
                : "Omitir y sanear archivos thumbs.db, desktop.ini, .tmp y bloqueos temporales.";
            if (lblAutoLockTitle != null)
            {
                lblAutoLockTitle.Text = Localization.Get("cfg_autolock_title") ?? (Localization.CurrentLang == "EN" ? "Inactivity Auto-Lock" : "Auto-Bloqueo por Inactividad");
                lblAutoLockSub.Text = Localization.Get("cfg_autolock_sub") ?? (Localization.CurrentLang == "EN"
                    ? "Lock session and unmount virtual drives after idle time."
                    : "Bloquear sesión y cerrar unidades tras tiempo inactivo.");
                PoblarComboAutoLock();
            }
            lblDangerTitle.Text = Localization.CurrentLang == "EN" ? "Factory Reset" : "Restablecimiento de Fábrica";
            lblDangerSub.Text = Localization.CurrentLang == "EN"
                ? "Deletes local user database and restores the suite to initial state."
                : "Elimina la base de datos de usuarios local y restablece la suite.";
            btnReset.Text = Localization.CurrentLang == "EN" ? "RESET" : "RESTABLECER";
            btnCreditos.Text = Localization.CurrentLang == "EN" ? "CREDITS" : "CRÉDITOS";
            SincronizarIdiomaCombo();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Recentrar();
        }

        private void Recentrar()
        {
            if (card == null) return;
            int totalH = 80 + card.Height;
            int startY = Math.Max(25, (this.ClientSize.Height - totalH) / 2);
            int x = Math.Max(20, (this.ClientSize.Width - card.Width) / 2);

            if (lblTituloConfig != null)
            {
                lblTituloConfig.Location = new Point(x, startY);
            }
            if (lblSubtituloConfig != null)
            {
                lblSubtituloConfig.Location = new Point(x, startY + 34);
            }

            card.Location = new Point(x, startY + 68);
        }
    }
}
