using FolderLocker.UI.Common;

namespace FolderLocker.UI.Views
{
    public class ManualView : UserControl
    {
        private Label lblManualTitulo = null!;
        private Label lblManualSubtitulo = null!;
        private Panel card = null!;
        private TextBox txtManualContenido = null!;

        public Panel CardPanel => card;

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
                Text = Localization.Get("manual_title") ?? "MANUAL DE USO",
                ForeColor = UITheme.cTextPrimary,
                Font = new Font("Segoe UI Semibold", 20, FontStyle.Bold),
                AutoSize = true
            };
            this.Controls.Add(lblManualTitulo);

            lblManualSubtitulo = new Label
            {
                Text = Localization.CurrentLang == "EN"
                    ? "Learn how to encrypt, mount virtual drives, and protect your private data."
                    : "Aprende a cifrar carpetas, montar discos virtuales y proteger tu privacidad.",
                ForeColor = Color.FromArgb(160, 150, 150),
                Font = new Font("Segoe UI", 9.5f),
                AutoSize = true
            };
            this.Controls.Add(lblManualSubtitulo);

            // Tarjeta principal (680 x 440)
            card = new Panel
            {
                Size = new Size(680, 440),
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
            var badge1 = CrearBadge("📖 GUÍA DE USUARIO", Color.FromArgb(50, 22, 22), Color.FromArgb(252, 165, 165), 40, badgeY);
            var badge2 = CrearBadge("🛡 MEJORES PRÁCTICAS", Color.FromArgb(36, 33, 33), Color.FromArgb(209, 213, 219), 180, badgeY);
            card.Controls.AddRange(new Control[] { badge1, badge2 });

            txtManualContenido = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = UITheme.cInputBackground,
                ForeColor = Color.FromArgb(230, 230, 230),
                Font = new Font("Segoe UI", 10),
                BorderStyle = BorderStyle.None,
                Location = new Point(40, 56),
                Size = new Size(600, 360),
                Text = Localization.Get("manual_text")
            };
            card.Controls.Add(txtManualContenido);

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

        public void ActualizarIdioma()
        {
            lblManualTitulo.Text = Localization.Get("manual_title") ?? "MANUAL DE USO";
            lblManualSubtitulo.Text = Localization.CurrentLang == "EN"
                ? "Learn how to encrypt, mount virtual drives, and protect your private data."
                : "Aprende a cifrar carpetas, montar discos virtuales y proteger tu privacidad.";
            txtManualContenido.Text = Localization.Get("manual_text");
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

            if (lblManualTitulo != null)
            {
                lblManualTitulo.Location = new Point(x, startY);
            }
            if (lblManualSubtitulo != null)
            {
                lblManualSubtitulo.Location = new Point(x, startY + 34);
            }

            card.Location = new Point(x, startY + 68);
        }
    }
}
