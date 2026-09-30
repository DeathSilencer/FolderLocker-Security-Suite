using FolderLocker.UI.Common;

namespace FolderLocker.UI.Views
{
    public class SetupView : UserControl
    {
        private Label lblTituloSetup = null!;
        private Panel card = null!;
        private Label lblIdiomaSetup = null!;
        private ComboBox cmbLangSetup = null!;
        private Label lblSubSetup = null!;
        private Label lblCreateSetup = null!;
        private TextBox txtSetupPass = null!;
        private Label lblConfirmSetup = null!;
        private TextBox txtSetupConfirm = null!;
        private Label lblNotaSetup = null!;
        private Button btnFinalizarSetup = null!;

        public event Action<string>? SetupCompleted;
        public event Action? IdiomaChanged;

        public SetupView()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = UITheme.cBackground;
            InicializarComponentes();
        }

        private void InicializarComponentes()
        {
            lblTituloSetup = new Label
            {
                Text = Localization.Get("setup_title"),
                ForeColor = UITheme.cAccentRed,
                Font = new Font("Segoe UI Black", 24, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(40, 40)
            };
            this.Controls.Add(lblTituloSetup);

            card = UITheme.CrearTarjetaBase(550, 480);
            this.Controls.Add(card);

            lblIdiomaSetup = UITheme.CrearEtiqueta(card, Localization.Get("config_lbl_lang").ToUpper(), 45, 30);

            cmbLangSetup = new ComboBox
            {
                Location = new Point(45, 55),
                Size = new Size(460, 45),
                FlatStyle = FlatStyle.Flat,
                BackColor = UITheme.cInputBackground,
                ForeColor = UITheme.cTextPrimary,
                Font = new Font("Segoe UI", 11),
                DropDownStyle = ComboBoxStyle.DropDownList,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 40
            };
            cmbLangSetup.Items.AddRange(new object[] { "Español (ES)", "English (EN)", "Português (PT)", "Русский (RU)", "中文 (CN)" });
            cmbLangSetup.DrawItem += UITheme.DibujarComboConBanderas;
            cmbLangSetup.SelectedIndexChanged += (s, e) =>
            {
                string lang = "ES";
                if (cmbLangSetup.SelectedIndex == 1) lang = "EN";
                else if (cmbLangSetup.SelectedIndex == 2) lang = "PT";
                else if (cmbLangSetup.SelectedIndex == 3) lang = "RU";
                else if (cmbLangSetup.SelectedIndex == 4) lang = "CN";

                if (Localization.CurrentLang != lang)
                {
                    Localization.CurrentLang = lang;
                    Properties.Settings.Default.Idioma = lang;
                    Properties.Settings.Default.Save();
                    ActualizarIdioma();
                    IdiomaChanged?.Invoke();
                }
            };
            card.Controls.Add(cmbLangSetup);

            UITheme.CrearSeparador(card, 110);

            lblSubSetup = new Label
            {
                Text = Localization.Get("setup_sub"),
                ForeColor = UITheme.cTextSecondary,
                Font = new Font("Segoe UI", 10),
                AutoSize = true,
                Location = new Point(45, 125)
            };
            card.Controls.Add(lblSubSetup);

            lblCreateSetup = UITheme.CrearEtiqueta(card, Localization.Get("setup_lbl_create"), 45, 160);
            txtSetupPass = UITheme.CrearInputPassword(card, 45, 185, 460);
            UITheme.AdjuntarMedidorFortaleza(card, txtSetupPass, 45, 220, 460);

            lblConfirmSetup = UITheme.CrearEtiqueta(card, Localization.Get("setup_lbl_confirm"), 45, 230);
            txtSetupConfirm = UITheme.CrearInputPassword(card, 45, 255, 460);

            lblNotaSetup = new Label
            {
                Text = Localization.Get("setup_note"),
                ForeColor = Color.Orange,
                Font = new Font("Segoe UI", 8),
                AutoSize = true,
                Location = new Point(45, 300)
            };
            card.Controls.Add(lblNotaSetup);

            btnFinalizarSetup = new Button
            {
                Text = Localization.Get("setup_btn"),
                Size = new Size(460, 50),
                Location = new Point(45, 350)
            };
            UITheme.EstilarBotonAccion(btnFinalizarSetup);
            btnFinalizarSetup.Click += (s, e) =>
            {
                if (string.IsNullOrEmpty(txtSetupPass.Text))
                {
                    DarkDialogs.ShowInfo(Localization.Get("setup_err_empty"));
                    return;
                }
                if (txtSetupPass.Text != txtSetupConfirm.Text)
                {
                    DarkDialogs.ShowInfo(Localization.Get("setup_err_match"));
                    return;
                }

                SetupCompleted?.Invoke(txtSetupPass.Text);
            };
            card.Controls.Add(btnFinalizarSetup);

            SincronizarIdiomaCombo();
            Recentrar();
        }

        private void SincronizarIdiomaCombo()
        {
            switch (Localization.CurrentLang)
            {
                case "EN": cmbLangSetup.SelectedIndex = 1; break;
                case "PT": cmbLangSetup.SelectedIndex = 2; break;
                case "RU": cmbLangSetup.SelectedIndex = 3; break;
                case "CN": cmbLangSetup.SelectedIndex = 4; break;
                default: cmbLangSetup.SelectedIndex = 0; break;
            }
        }

        public void ActualizarIdioma()
        {
            lblTituloSetup.Text = Localization.Get("setup_title");
            lblIdiomaSetup.Text = Localization.Get("config_lbl_lang").ToUpper();
            lblSubSetup.Text = Localization.Get("setup_sub");
            lblCreateSetup.Text = Localization.Get("setup_lbl_create").ToUpper();
            lblConfirmSetup.Text = Localization.Get("setup_lbl_confirm").ToUpper();
            lblNotaSetup.Text = Localization.Get("setup_note");
            btnFinalizarSetup.Text = Localization.Get("setup_btn");
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

            if (lblTituloSetup != null)
            {
                lblTituloSetup.Location = new Point(x, Math.Max(10, y - 45));
            }
        }
    }
}
