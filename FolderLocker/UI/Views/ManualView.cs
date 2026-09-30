using FolderLocker.UI.Common;

namespace FolderLocker.UI.Views
{
    public class ManualView : UserControl
    {
        private Label lblManualTitulo = null!;
        private Panel card = null!;
        private Label lblManualTexto = null!;

        public ManualView()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = UITheme.cBackground;
            InicializarComponentes();
        }

        private void InicializarComponentes()
        {
            lblManualTitulo = new Label
            {
                Text = Localization.Get("manual_title"),
                ForeColor = UITheme.cAccentRed,
                Font = new Font("Segoe UI Black", 20, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(50, 30)
            };
            this.Controls.Add(lblManualTitulo);

            card = UITheme.CrearTarjetaBase(550, 600);
            this.Controls.Add(card);

            lblManualTexto = new Label
            {
                Text = Localization.Get("manual_text"),
                ForeColor = Color.WhiteSmoke,
                Font = new Font("Segoe UI", 11),
                AutoSize = false,
                Size = new Size(510, 560),
                Location = new Point(20, 20),
                TextAlign = ContentAlignment.TopLeft
            };
            card.Controls.Add(lblManualTexto);

            Recentrar();
        }

        public void ActualizarIdioma()
        {
            lblManualTitulo.Text = Localization.Get("manual_title");
            lblManualTexto.Text = Localization.Get("manual_text");
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

            if (lblManualTitulo != null)
            {
                lblManualTitulo.Location = new Point(x, Math.Max(10, y - 45));
            }
        }
    }
}
