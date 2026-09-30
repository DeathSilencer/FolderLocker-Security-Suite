using FolderLocker.UI.Common;
using Microsoft.Win32;

namespace FolderLocker.UI.Views
{
    public class ConfiguracionView : UserControl
    {
        private Label lblTituloConfig = null!;
        private Panel card = null!;
        private Label lblSecGeneral = null!;
        private Label lblLangConfig = null!;
        private ComboBox cmbIdioma = null!;
        private Label lblSecSystem = null!;
        private CheckBox chkInicioWindows = null!;
        private Label lblSecData = null!;
        private Button btnReset = null!;
        private Label lblSecDev = null!;
        private Button btnCreditos = null!;

        public event Action? IdiomaChanged;
        public event Action? VerCreditosRequested;
        public event Action? FactoryResetRequested;

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
                Text = "CONFIG",
                ForeColor = UITheme.cAccentRed,
                Font = new Font("Segoe UI Black", 20, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(30, 30)
            };
            this.Controls.Add(lblTituloConfig);

            card = UITheme.CrearTarjetaBase(550, 480);
            this.Controls.Add(card);

            // Sección 1: General
            lblSecGeneral = UITheme.CrearHeaderSeccion(card, Localization.Get("cfg_sec_general"), 40, 30);
            lblLangConfig = UITheme.CrearEtiqueta(card, " IDIOMA", 40, 65);

            cmbIdioma = new ComboBox
            {
                Location = new Point(40, 90),
                Size = new Size(470, 45),
                FlatStyle = FlatStyle.Flat,
                BackColor = UITheme.cInputBackground,
                ForeColor = UITheme.cTextPrimary,
                Font = new Font("Segoe UI", 11),
                DropDownStyle = ComboBoxStyle.DropDownList,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 40
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

            // Sección 2: Sistema
            UITheme.CrearSeparador(card, 150);
            lblSecSystem = UITheme.CrearHeaderSeccion(card, Localization.Get("cfg_sec_system"), 40, 170);

            chkInicioWindows = new CheckBox
            {
                Text = "Start with Windows",
                Location = new Point(40, 205),
                AutoSize = true,
                ForeColor = UITheme.cTextPrimary,
                Font = new Font("Segoe UI", 11),
                Cursor = Cursors.Hand
            };
            try
            {
                using RegistryKey? rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", false);
                if (rk != null) chkInicioWindows.Checked = rk.GetValue("FolderLocker") != null;
            }
            catch { }
            chkInicioWindows.CheckedChanged += (s, e) => SetStartup(chkInicioWindows.Checked);
            card.Controls.Add(chkInicioWindows);

            // Sección 3: Datos
            UITheme.CrearSeparador(card, 250);
            lblSecData = UITheme.CrearHeaderSeccion(card, Localization.Get("cfg_sec_data"), 40, 270);

            btnReset = new Button
            {
                Text = Localization.Get("cfg_btn_reset"),
                Size = new Size(470, 45),
                Location = new Point(40, 300)
            };
            UITheme.EstilarBotonSecundario(btnReset);
            btnReset.ForeColor = Color.IndianRed;
            btnReset.Click += (s, e) => FactoryResetRequested?.Invoke();
            card.Controls.Add(btnReset);

            // Sección 4: Información
            UITheme.CrearSeparador(card, 360);
            lblSecDev = UITheme.CrearHeaderSeccion(card, "INFO DEL DESARROLLADOR", 40, 380);

            btnCreditos = new Button
            {
                Text = "Ver Créditos",
                Size = new Size(470, 45),
                Location = new Point(40, 410)
            };
            UITheme.EstilarBotonSecundario(btnCreditos);
            btnCreditos.Click += (s, e) => VerCreditosRequested?.Invoke();
            card.Controls.Add(btnCreditos);

            SincronizarIdiomaCombo();
            Recentrar();
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

        public void ActualizarIdioma()
        {
            lblSecGeneral.Text = Localization.Get("cfg_sec_general");
            lblSecSystem.Text = Localization.Get("cfg_sec_system");
            lblSecData.Text = Localization.Get("cfg_sec_data");
            btnReset.Text = Localization.Get("cfg_btn_reset");
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
            int x = Math.Max(20, (this.ClientSize.Width - card.Width) / 2);
            int y = Math.Max(50, (this.ClientSize.Height - card.Height) / 2);
            card.Location = new Point(x, y);

            if (lblTituloConfig != null)
            {
                lblTituloConfig.Location = new Point(x, Math.Max(10, y - 45));
            }
        }
    }
}
