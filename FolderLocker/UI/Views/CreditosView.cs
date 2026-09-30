using System.Diagnostics;
using FolderLocker.UI.Common;

namespace FolderLocker.UI.Views
{
    public class CreditosView : UserControl
    {
        private Panel card = null!;
        private Label lblTitulo = null!;
        private UITheme.CircularPictureBox pbFoto = null!;
        private Label lblDevInfo = null!;
        private Label lblVersion = null!;
        private Button btnGitHub = null!;
        private Button btnVolver = null!;

        public event Action? VolverRequested;

        public CreditosView()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = UITheme.cBackground;
            InicializarComponentes();
        }

        private void InicializarComponentes()
        {
            card = UITheme.CrearTarjetaBase(550, 400);
            this.Controls.Add(card);

            lblTitulo = new Label
            {
                Text = "CRÉDITOS Y DESARROLLADOR",
                ForeColor = UITheme.cAccentRed,
                Font = new Font("Segoe UI Black", 20, FontStyle.Bold),
                AutoSize = true
            };
            card.Controls.Add(lblTitulo);

            pbFoto = new UITheme.CircularPictureBox
            {
                Size = new Size(100, 100),
                BackColor = Color.FromArgb(40, 40, 40),
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = Properties.Resources.FotoPerfil2
            };
            card.Controls.Add(pbFoto);

            lblDevInfo = new Label
            {
                Text = "Desarrollado por: David Platas\nContacto: davarman10@gmail.com",
                Font = new Font("Segoe UI", 11),
                ForeColor = UITheme.cTextPrimary,
                AutoSize = true
            };
            card.Controls.Add(lblDevInfo);

            lblVersion = new Label
            {
                Text = "FolderLocker v5.0 (2025)",
                Font = new Font("Segoe UI", 9),
                ForeColor = UITheme.cTextSecondary,
                AutoSize = true
            };
            card.Controls.Add(lblVersion);

            btnGitHub = new Button
            {
                Text = "Ver en GitHub",
                Size = new Size(150, 40)
            };
            UITheme.EstilarBotonSecundario(btnGitHub);
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
                Text = "Volver",
                Size = new Size(150, 40)
            };
            UITheme.EstilarBotonAccion(btnVolver);
            btnVolver.Click += (s, e) => VolverRequested?.Invoke();
            card.Controls.Add(btnVolver);

            LayoutInterno();
            Recentrar();
        }

        private void LayoutInterno()
        {
            if (card == null) return;
            lblTitulo.Location = new Point((card.Width - lblTitulo.Width) / 2, 30);
            pbFoto.Location = new Point((card.Width - pbFoto.Width) / 2 - 160, 100);
            lblDevInfo.Location = new Point(pbFoto.Right + 20, pbFoto.Top + 30);
            lblVersion.Location = new Point((card.Width - lblVersion.Width) / 2, pbFoto.Bottom + 40);

            int totalWidth = btnGitHub.Width + 20 + btnVolver.Width;
            int startX = (card.Width - totalWidth) / 2;
            btnGitHub.Location = new Point(startX, lblVersion.Bottom + 30);
            btnVolver.Location = new Point(btnGitHub.Right + 20, lblVersion.Bottom + 30);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutInterno();
            Recentrar();
        }

        private void Recentrar()
        {
            if (card == null) return;
            int x = Math.Max(20, (this.ClientSize.Width - card.Width) / 2);
            int y = Math.Max(20, (this.ClientSize.Height - card.Height) / 2);
            card.Location = new Point(x, y);
        }
    }
}
