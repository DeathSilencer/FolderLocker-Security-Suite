using FolderLocker.UI.Common;

namespace FolderLocker.UI.Views
{
    public class ManualView : UserControl
    {
        private Label lblManualTitulo = null!;
        private Label lblManualSubtitulo = null!;
        private Panel card = null!;
        private Panel pnlScroll = null!;

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
                Text = Localization.Get("manual_title") ?? "GUÍA RÁPIDA DE USUARIO",
                ForeColor = UITheme.cTextPrimary,
                Font = new Font("Segoe UI Semibold", 20, FontStyle.Bold),
                AutoSize = true
            };
            this.Controls.Add(lblManualTitulo);

            lblManualSubtitulo = new Label
            {
                Text = Localization.Get("manual_sub"),
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
            var badge1 = CrearBadge(Localization.Get("manual_badge_guide"), Color.FromArgb(50, 22, 22), Color.FromArgb(252, 165, 165), 40, badgeY);
            var badge2 = CrearBadge(Localization.Get("manual_badge_practices"), Color.FromArgb(36, 33, 33), Color.FromArgb(209, 213, 219), 180, badgeY);
            card.Controls.AddRange(new Control[] { badge1, badge2 });

            // Contenedor scrollable moderno
            pnlScroll = new Panel
            {
                Location = new Point(35, 54),
                Size = new Size(610, 368),
                AutoScroll = true,
                BackColor = Color.Transparent
            };
            card.Controls.Add(pnlScroll);

            CargarTarjetasManual();
            Recentrar();
        }

        private void CargarTarjetasManual()
        {
            pnlScroll.SuspendLayout();
            pnlScroll.Controls.Clear();

            int cardWidth = 582;
            int currentY = 0;

            // 1. Proteger
            var card1 = CrearGuiaCard(
                "🔒",
                Localization.Get("man_card1_title"),
                Color.FromArgb(252, 165, 165),
                new[]
                {
                    Localization.Get("man_card1_step1"),
                    Localization.Get("man_card1_step2"),
                    Localization.Get("man_card1_step3")
                },
                Color.FromArgb(30, 27, 27),
                Color.FromArgb(48, 42, 42),
                currentY,
                cardWidth
            );
            pnlScroll.Controls.Add(card1);
            currentY += card1.Height + 12;

            // 2. Abrir Bóveda
            var card2 = CrearGuiaCard(
                "💾",
                Localization.Get("man_card2_title"),
                Color.FromArgb(56, 189, 248),
                new[]
                {
                    Localization.Get("man_card2_step1"),
                    Localization.Get("man_card2_step2"),
                    Localization.Get("man_card2_step3")
                },
                Color.FromArgb(30, 27, 27),
                Color.FromArgb(48, 42, 42),
                currentY,
                cardWidth
            );
            pnlScroll.Controls.Add(card2);
            currentY += card2.Height + 12;

            // 3. Restaurar
            var card3 = CrearGuiaCard(
                "🔓",
                Localization.Get("man_card3_title"),
                Color.FromArgb(134, 239, 172),
                new[]
                {
                    Localization.Get("man_card3_step1"),
                    Localization.Get("man_card3_step2"),
                    Localization.Get("man_card3_step3")
                },
                Color.FromArgb(30, 27, 27),
                Color.FromArgb(48, 42, 42),
                currentY,
                cardWidth
            );
            pnlScroll.Controls.Add(card3);
            currentY += card3.Height + 12;

            // 4. Advertencias Críticas
            var card4 = CrearGuiaCard(
                "⚠️",
                Localization.Get("man_card4_title"),
                Color.FromArgb(248, 113, 113),
                new[]
                {
                    Localization.Get("man_card4_step1"),
                    Localization.Get("man_card4_step2"),
                    Localization.Get("man_card4_step3"),
                    Localization.Get("man_card4_step4"),
                    Localization.Get("man_card4_step5")
                },
                Color.FromArgb(40, 20, 20),
                Color.FromArgb(74, 30, 30),
                currentY,
                cardWidth,
                UITheme.cAccentRed
            );
            pnlScroll.Controls.Add(card4);

            pnlScroll.ResumeLayout(true);
        }

        private Panel CrearGuiaCard(string icon, string titulo, Color colorTitulo, string[] pasos, Color fondo, Color borde, int yPos, int w, Color? acentoIzquierdo = null)
        {
            var pnl = new Panel
            {
                Location = new Point(0, yPos),
                Width = w,
                BackColor = fondo
            };

            pnl.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, pnl.ClientRectangle, borde, ButtonBorderStyle.Solid);
                if (acentoIzquierdo.HasValue)
                {
                    using var b = new SolidBrush(acentoIzquierdo.Value);
                    e.Graphics.FillRectangle(b, 0, 0, 4, pnl.Height);
                }
            };

            var lblHeader = new Label
            {
                Text = $"{icon}  {titulo}",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = colorTitulo,
                Location = new Point(14, 10),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            pnl.Controls.Add(lblHeader);

            int curY = 34;
            foreach (var paso in pasos)
            {
                var lblPaso = new Label
                {
                    Text = paso,
                    Font = new Font("Segoe UI", 8.8f),
                    ForeColor = Color.FromArgb(220, 210, 210),
                    Location = new Point(18, curY),
                    Width = w - 36,
                    AutoSize = true,
                    MaximumSize = new Size(w - 36, 0),
                    BackColor = Color.Transparent
                };
                pnl.Controls.Add(lblPaso);
                curY += lblPaso.PreferredHeight + 6;
            }

            pnl.Height = curY + 8;
            return pnl;
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
            lblManualTitulo.Text = Localization.Get("manual_title") ?? "GUÍA RÁPIDA DE USUARIO";
            lblManualSubtitulo.Text = Localization.Get("manual_sub");
            CargarTarjetasManual();
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
