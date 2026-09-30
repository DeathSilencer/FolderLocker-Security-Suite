using System.Diagnostics;
using FolderLocker;
using FolderLocker.UI.Common;

namespace FolderLocker.UI.Views
{
    public class CreditosView : UserControl
    {
        private Panel card = null!;
        private Label lblBadge1 = null!;
        private Label lblBadge2 = null!;
        private Label lblTitulo = null!;
        private UITheme.CircularPictureBox pbFoto = null!;
        private Label lblDevInfo = null!;
        private Label lblVersion = null!;
        private Button btnGitHub = null!;
        private Button btnVolver = null!;

        public event Action? VolverRequested;
        public Panel CardPanel => card;

        public CreditosView()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = UITheme.cBackground;
            InicializarComponentes();
            ActualizarIdioma();
        }

        private void InicializarComponentes()
        {
            // Tarjeta principal (680 x 400)
            card = new Panel
            {
                Size = new Size(680, 400),
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
            lblBadge1 = CrearBadge(Localization.Get("cred_badge_dev"), Color.FromArgb(50, 22, 22), Color.FromArgb(252, 165, 165), 40, badgeY);
            lblBadge2 = CrearBadge(Localization.Get("cred_badge_suite"), Color.FromArgb(36, 33, 33), Color.FromArgb(209, 213, 219), 180, badgeY);
            card.Controls.AddRange(new Control[] { lblBadge1, lblBadge2 });

            lblTitulo = new Label
            {
                Text = Localization.Get("cred_title"),
                ForeColor = UITheme.cTextPrimary,
                Font = new Font("Segoe UI Semibold", 18, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(40, 56)
            };
            card.Controls.Add(lblTitulo);

            pbFoto = new UITheme.CircularPictureBox
            {
                Size = new Size(110, 110),
                BackColor = Color.FromArgb(40, 36, 36),
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = Properties.Resources.FotoPerfil2,
                Location = new Point(50, 110)
            };
            card.Controls.Add(pbFoto);

            lblDevInfo = new Label
            {
                Text = Localization.Get("cred_author_info"),
                Font = new Font("Segoe UI", 10.5f),
                ForeColor = UITheme.cTextPrimary,
                Location = new Point(180, 120),
                Size = new Size(450, 80),
                BackColor = Color.Transparent
            };
            card.Controls.Add(lblDevInfo);

            lblVersion = new Label
            {
                Text = Localization.Get("cred_desc"),
                Font = new Font("Segoe UI", 9),
                ForeColor = UITheme.cTextSecondary,
                Location = new Point(50, 240),
                Size = new Size(580, 40),
                BackColor = Color.Transparent
            };
            card.Controls.Add(lblVersion);

            btnGitHub = new Button
            {
                Text = Localization.Get("cred_btn_github"),
                Size = new Size(280, 48),
                Location = new Point(50, 305),
                BackColor = Color.FromArgb(46, 42, 42),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnGitHub.FlatAppearance.BorderSize = 0;
            btnGitHub.FlatAppearance.MouseOverBackColor = Color.FromArgb(64, 58, 58);
            btnGitHub.Click += (s, e) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo("https://github.com/DeathSilencer") { UseShellExecute = true });
                }
                catch { }
            };
            card.Controls.Add(btnGitHub);

            btnVolver = new Button
            {
                Text = Localization.Get("cred_btn_back"),
                Size = new Size(280, 48),
                Location = new Point(350, 305),
                BackColor = UITheme.cAccentRed,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnVolver.FlatAppearance.BorderSize = 0;
            btnVolver.FlatAppearance.MouseOverBackColor = UITheme.cAccentRedHover;
            btnVolver.Click += (s, e) => VolverRequested?.Invoke();
            card.Controls.Add(btnVolver);

            Recentrar();
        }

        public void ActualizarIdioma()
        {
            if (lblBadge1 != null) lblBadge1.Text = Localization.Get("cred_badge_dev");
            if (lblBadge2 != null) lblBadge2.Text = Localization.Get("cred_badge_suite");
            if (lblTitulo != null) lblTitulo.Text = Localization.Get("cred_title");
            if (lblDevInfo != null) lblDevInfo.Text = Localization.Get("cred_author_info");
            if (lblVersion != null) lblVersion.Text = Localization.Get("cred_desc");
            if (btnGitHub != null) btnGitHub.Text = Localization.Get("cred_btn_github");
            if (btnVolver != null) btnVolver.Text = Localization.Get("cred_btn_back");
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

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Recentrar();
        }

        private void Recentrar()
        {
            if (card == null) return;
            int x = Math.Max(20, (this.ClientSize.Width - card.Width) / 2);
            int y = Math.Max(25, (this.ClientSize.Height - card.Height) / 2);
            card.Location = new Point(x, y);
        }
    }
}
