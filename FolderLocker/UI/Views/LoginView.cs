using FolderLocker.UI.Common;

namespace FolderLocker.UI.Views
{
    public class LoginView : UserControl
    {
        private Panel pnlLoginLeft = null!;
        private Panel pnlLoginRight = null!;
        private Panel cardLogin = null!;
        private Panel pnlBranding = null!;

        private Label lblLoginTitle = null!;
        private Label lblLoginSub = null!;
        private Panel pnlAutoLockNotice = null!;
        private Label lblAutoLockNotice = null!;
        private Label lblLoginUser = null!;
        private Label lblLoginPass = null!;
        private TextBox txtUser = null!;
        private TextBox txtPass = null!;
        private CheckBox chkLoginRemember = null!;
        private Button btnLoginForgot = null!;
        private Button btnLoginEnter = null!;
        private Button btnLoginReg = null!;

        private readonly string _lastUserFile = Path.Combine(Application.StartupPath, "last_user.dat");

        public event Action<UserProfile>? LoginSuccessful;
        public event Action? GoToRegisterRequested;
        public event MouseEventHandler? WindowDragMouseDown;

        public LoginView()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = UITheme.cBackground;
            InicializarComponentes();
        }

        private void InicializarComponentes()
        {
            // Split screen
            pnlLoginLeft = new Panel
            {
                Dock = DockStyle.Left,
                Width = Math.Max(460, this.ClientSize.Width / 2),
                BackColor = UITheme.cBackground
            };
            pnlLoginRight = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(15, 15, 15)
            };

            this.Controls.Add(pnlLoginLeft);
            this.Controls.Add(pnlLoginRight);
            pnlLoginRight.BringToFront();

            // Tarjeta de Login
            cardLogin = UITheme.CrearTarjetaBase(450, 560);
            pnlLoginLeft.Controls.Add(cardLogin);

            lblLoginTitle = new Label
            {
                Text = Localization.Get("login_title"),
                ForeColor = UITheme.cAccentRed,
                Font = new Font("Segoe UI Black", 24, FontStyle.Bold),
                AutoSize = false,
                Size = new Size(cardLogin.Width, 50),
                Location = new Point(0, 40),
                TextAlign = ContentAlignment.MiddleCenter
            };
            cardLogin.Controls.Add(lblLoginTitle);

            lblLoginSub = new Label
            {
                Text = Localization.Get("app_subtitle"),
                ForeColor = UITheme.cTextSecondary,
                Font = new Font("Segoe UI", 10),
                AutoSize = false,
                Size = new Size(cardLogin.Width, 25),
                Location = new Point(0, 90),
                TextAlign = ContentAlignment.MiddleCenter
            };
            cardLogin.Controls.Add(lblLoginSub);

            pnlAutoLockNotice = new Panel
            {
                Location = new Point(50, 115),
                Size = new Size(350, 22),
                BackColor = Color.FromArgb(50, 22, 22),
                Visible = false
            };
            pnlAutoLockNotice.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, pnlAutoLockNotice.ClientRectangle, UITheme.cAccentRed, ButtonBorderStyle.Solid);
            };

            lblAutoLockNotice = new Label
            {
                Dock = DockStyle.Fill,
                Text = Localization.Get("msg_autolock_triggered") ?? "🔒 Sesión bloqueada por inactividad",
                ForeColor = Color.FromArgb(254, 202, 202),
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlAutoLockNotice.Controls.Add(lblAutoLockNotice);
            cardLogin.Controls.Add(pnlAutoLockNotice);

            lblLoginUser = UITheme.CrearEtiqueta(cardLogin, Localization.Get("login_lbl_user"), 50, 140);
            txtUser = UITheme.CrearInput(cardLogin, 50, 165, 350);
            txtUser.TextChanged += (s, e) => OcultarNoticiaAutoLock();

            lblLoginPass = UITheme.CrearEtiqueta(cardLogin, Localization.Get("login_lbl_pass"), 50, 210);
            txtPass = UITheme.CrearInputPassword(cardLogin, 50, 235, 350);
            txtPass.TextChanged += (s, e) => OcultarNoticiaAutoLock();

            KeyEventHandler enterHandler = (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    btnLoginEnter.PerformClick();
                }
            };
            txtUser.KeyDown += enterHandler;
            txtPass.KeyDown += enterHandler;

            chkLoginRemember = new CheckBox
            {
                Text = Localization.Get("login_chk_remember"),
                ForeColor = UITheme.cTextSecondary,
                Font = new Font("Segoe UI", 9),
                AutoSize = true,
                Location = new Point(50, 275),
                Cursor = Cursors.Hand
            };
            if (File.Exists(_lastUserFile))
            {
                try
                {
                    string u = File.ReadAllText(_lastUserFile);
                    if (!string.IsNullOrEmpty(u))
                    {
                        txtUser.Text = u;
                        chkLoginRemember.Checked = true;
                    }
                }
                catch { }
            }
            cardLogin.Controls.Add(chkLoginRemember);

            btnLoginForgot = new Button
            {
                Text = Localization.Get("link_forgot"),
                AutoSize = true,
                Location = new Point(260, 273),
                FlatStyle = FlatStyle.Flat,
                ForeColor = UITheme.cAccentRed,
                Font = new Font("Segoe UI", 8, FontStyle.Regular),
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent
            };
            btnLoginForgot.FlatAppearance.BorderSize = 0;
            btnLoginForgot.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btnLoginForgot.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btnLoginForgot.Click += (s, e) =>
            {
                if (string.IsNullOrEmpty(txtUser.Text))
                {
                    DarkDialogs.ShowInfo("Escribe tu usuario primero.");
                    return;
                }
                string code = DarkDialogs.ShowInput(Localization.Get("rec_prompt_msg"), Localization.Get("rec_prompt_title"), false);
                if (!string.IsNullOrEmpty(code))
                {
                    string? recoveredPass = UserManager.RecoverLoginPassword(txtUser.Text, code);
                    if (recoveredPass != null)
                    {
                        DarkDialogs.ShowResultWithCopy(Localization.Get("rec_success_msg"), recoveredPass);
                        txtPass.Text = recoveredPass;
                    }
                    else
                    {
                        DarkDialogs.ShowInfo(Localization.Get("rec_fail_msg"), "Error");
                    }
                }
            };
            cardLogin.Controls.Add(btnLoginForgot);

            btnLoginEnter = new Button
            {
                Text = Localization.Get("login_btn_enter"),
                Size = new Size(350, 50),
                Location = new Point(50, 320)
            };
            UITheme.EstilarBotonAccion(btnLoginEnter);
            btnLoginEnter.Click += (s, e) =>
            {
                if (UserManager.Login(txtUser.Text, txtPass.Text))
                {
                    try
                    {
                        if (chkLoginRemember.Checked) File.WriteAllText(_lastUserFile, txtUser.Text);
                        else if (File.Exists(_lastUserFile)) File.Delete(_lastUserFile);
                    }
                    catch { }

                    string passTmp = txtPass.Text;
                    txtPass.Text = "";
                    LoginSuccessful?.Invoke(UserManager.CurrentUser);
                }
                else
                {
                    DarkDialogs.ShowInfo(Localization.Get("login_err_auth"), Localization.Get("title_error"));
                }
            };
            cardLogin.Controls.Add(btnLoginEnter);

            UITheme.CrearSeparador(cardLogin, 400);

            btnLoginReg = new Button
            {
                Text = Localization.Get("login_btn_reg"),
                FlatStyle = FlatStyle.Flat,
                ForeColor = UITheme.cTextSecondary,
                Size = new Size(350, 30),
                Location = new Point(50, 420),
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9)
            };
            btnLoginReg.FlatAppearance.BorderSize = 0;
            btnLoginReg.Click += (s, e) => GoToRegisterRequested?.Invoke();
            cardLogin.Controls.Add(btnLoginReg);

            // Branding
            ConstruirBrandingDerecho();

            // Dragging
            pnlLoginLeft.MouseDown += (s, e) => WindowDragMouseDown?.Invoke(s, e);
            pnlLoginRight.MouseDown += (s, e) => WindowDragMouseDown?.Invoke(s, e);

            Recentrar();
        }

        private void ConstruirBrandingDerecho()
        {
            pnlBranding = new Panel { Size = new Size(500, 600), BackColor = Color.Transparent };
            pnlLoginRight.Controls.Add(pnlBranding);
            pnlBranding.BringToFront();

            try
            {
                var imgCompuesta = new PictureBox
                {
                    Image = Properties.Resources.FolderLockerConTexto,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Size = new Size(340, 340),
                    Location = new Point((pnlBranding.Width - 340) / 2, 40),
                    BackColor = Color.Transparent
                };
                pnlBranding.Controls.Add(imgCompuesta);
                imgCompuesta.MouseDown += (s, e) => WindowDragMouseDown?.Invoke(s, e);

                var lblDesc = new Label
                {
                    Text = "Grado de Seguridad Militar AES-256 CTR\nAcceso transparente mediante Unidad Virtual Dokan\nProtección absoluta 'Zero-Knowledge'",
                    ForeColor = Color.FromArgb(140, 140, 140),
                    Font = new Font("Segoe UI", 10, FontStyle.Regular),
                    AutoSize = false,
                    Size = new Size(460, 80),
                    Location = new Point(20, 410),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                pnlBranding.Controls.Add(lblDesc);
            }
            catch { }
        }

        public void ActualizarIdioma()
        {
            lblLoginTitle.Text = Localization.Get("login_title");
            lblLoginSub.Text = Localization.Get("app_subtitle");
            lblLoginUser.Text = Localization.Get("login_lbl_user").ToUpper();
            lblLoginPass.Text = Localization.Get("login_lbl_pass").ToUpper();
            chkLoginRemember.Text = Localization.Get("login_chk_remember");
            btnLoginForgot.Text = Localization.Get("link_forgot");
            btnLoginEnter.Text = Localization.Get("login_btn_enter");
            btnLoginReg.Text = Localization.Get("login_btn_reg");
            if (lblAutoLockNotice != null)
            {
                lblAutoLockNotice.Text = Localization.Get("msg_autolock_triggered") ?? "🔒 Sesión bloqueada por inactividad";
            }
        }

        public void MostrarNoticiaAutoLock(string? mensaje = null)
        {
            if (lblAutoLockNotice != null)
            {
                lblAutoLockNotice.Text = mensaje ?? (Localization.Get("msg_autolock_triggered") ?? "🔒 Sesión bloqueada por inactividad");
            }
            if (pnlAutoLockNotice != null)
            {
                pnlAutoLockNotice.Visible = true;
            }
        }

        public void OcultarNoticiaAutoLock()
        {
            if (pnlAutoLockNotice != null)
            {
                pnlAutoLockNotice.Visible = false;
            }
        }

        public void LimpiarPassword()
        {
            txtPass.Text = "";
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Recentrar();
        }

        private void Recentrar()
        {
            if (pnlLoginLeft == null || cardLogin == null) return;
            pnlLoginLeft.Width = Math.Max(460, this.ClientSize.Width / 2);

            int cx = Math.Max(10, (pnlLoginLeft.Width - cardLogin.Width) / 2);
            int cy = Math.Max(10, (pnlLoginLeft.Height - cardLogin.Height) / 2);
            cardLogin.Location = new Point(cx, cy);

            if (pnlLoginRight != null && pnlBranding != null)
            {
                int bx = Math.Max(10, (pnlLoginRight.Width - pnlBranding.Width) / 2);
                int by = Math.Max(10, (pnlLoginRight.Height - pnlBranding.Height) / 2);
                pnlBranding.Location = new Point(bx, by);
            }
        }
    }
}
