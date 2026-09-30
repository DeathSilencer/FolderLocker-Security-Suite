using FolderLocker.UI.Common;

namespace FolderLocker.UI.Views
{
    public class RegistroView : UserControl
    {
        private Panel cardReg = null!;
        private Label lblRegTitle = null!;
        private Label lblRegSub = null!;
        private Label lblRegUser = null!;
        private Label lblRegPass = null!;
        private TextBox txtUser = null!;
        private TextBox txtPass = null!;
        private Button btnRegCreate = null!;
        private Button btnRegBack = null!;

        public event Action? RegistrationCompleted;
        public event Action? BackToLoginRequested;
        public event MouseEventHandler? WindowDragMouseDown;

        public RegistroView()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = UITheme.cBackground;
            InicializarComponentes();
        }

        private void InicializarComponentes()
        {
            this.MouseDown += (s, e) => WindowDragMouseDown?.Invoke(s, e);

            cardReg = UITheme.CrearTarjetaBase(450, 550);
            this.Controls.Add(cardReg);

            lblRegTitle = new Label
            {
                Text = Localization.Get("reg_title"),
                ForeColor = UITheme.cAccentRed,
                Font = new Font("Segoe UI Black", 24, FontStyle.Bold),
                AutoSize = false,
                Size = new Size(cardReg.Width, 45),
                Location = new Point(0, 40),
                TextAlign = ContentAlignment.MiddleCenter
            };
            cardReg.Controls.Add(lblRegTitle);

            lblRegSub = new Label
            {
                Text = Localization.Get("reg_subtitle"),
                ForeColor = UITheme.cTextSecondary,
                Font = new Font("Segoe UI", 10),
                AutoSize = false,
                Size = new Size(cardReg.Width, 25),
                Location = new Point(0, 85),
                TextAlign = ContentAlignment.MiddleCenter
            };
            cardReg.Controls.Add(lblRegSub);

            lblRegUser = UITheme.CrearEtiqueta(cardReg, Localization.Get("reg_lbl_user"), 50, 130);
            txtUser = UITheme.CrearInput(cardReg, 50, 155, 350);

            lblRegPass = UITheme.CrearEtiqueta(cardReg, Localization.Get("reg_lbl_pass"), 50, 210);
            txtPass = UITheme.CrearInputPassword(cardReg, 50, 235, 350);

            KeyEventHandler regEnterHandler = (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    btnRegCreate.PerformClick();
                }
            };
            txtUser.KeyDown += regEnterHandler;
            txtPass.KeyDown += regEnterHandler;

            UITheme.AdjuntarMedidorFortaleza(cardReg, txtPass, 50, 270, 350);

            btnRegCreate = new Button
            {
                Text = Localization.Get("reg_btn_create"),
                Size = new Size(350, 50),
                Location = new Point(50, 320)
            };
            UITheme.EstilarBotonAccion(btnRegCreate);
            btnRegCreate.Click += (s, e) =>
            {
                string u = txtUser.Text.Trim();
                string p = txtPass.Text;

                if (u.Length < 3 || p.Length < 4)
                {
                    DarkDialogs.ShowInfo(Localization.Get("reg_err_len"), Localization.Get("title_error"));
                    return;
                }

                string recCode = "REC-" + Guid.NewGuid().ToString().Substring(0, 4).ToUpper();
                if (UserManager.Register(u, p, recCode))
                {
                    DarkDialogs.ShowResultWithCopy(Localization.Get("reg_msg_code") + "\n\n" + Localization.Get("setup_note"), recCode);
                    DarkDialogs.ShowInfo(Localization.Get("reg_msg_login"));

                    txtUser.Text = "";
                    txtPass.Text = "";
                    RegistrationCompleted?.Invoke();
                }
                else
                {
                    DarkDialogs.ShowInfo(Localization.Get("reg_err_exists"), Localization.Get("title_error"));
                }
            };
            cardReg.Controls.Add(btnRegCreate);

            UITheme.CrearSeparador(cardReg, 400);

            btnRegBack = new Button
            {
                Text = Localization.Get("reg_btn_back"),
                FlatStyle = FlatStyle.Flat,
                ForeColor = UITheme.cTextSecondary,
                Size = new Size(350, 30),
                Location = new Point(50, 420),
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9)
            };
            btnRegBack.FlatAppearance.BorderSize = 0;
            btnRegBack.FlatAppearance.MouseOverBackColor = UITheme.cInputBackground;
            btnRegBack.FlatAppearance.MouseDownBackColor = UITheme.cBackground;
            btnRegBack.Click += (s, e) => BackToLoginRequested?.Invoke();
            cardReg.Controls.Add(btnRegBack);

            Recentrar();
        }

        public void ActualizarIdioma()
        {
            lblRegTitle.Text = Localization.Get("reg_title");
            lblRegSub.Text = Localization.Get("reg_subtitle");
            lblRegUser.Text = Localization.Get("reg_lbl_user").ToUpper();
            lblRegPass.Text = Localization.Get("reg_lbl_pass").ToUpper();
            btnRegCreate.Text = Localization.Get("reg_btn_create");
            btnRegBack.Text = Localization.Get("reg_btn_back");
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Recentrar();
        }

        private void Recentrar()
        {
            if (cardReg == null) return;
            int x = Math.Max(10, (this.ClientSize.Width - cardReg.Width) / 2);
            int y = Math.Max(10, (this.ClientSize.Height - cardReg.Height) / 2);
            cardReg.Location = new Point(x, y);
        }
    }
}
