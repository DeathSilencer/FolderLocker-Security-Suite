using FolderLocker.UI.Common;

namespace FolderLocker.UI.Views
{
    public class RestaurarView : UserControl
    {
        private Panel card = null!;
        private Label lblTituloRestaurar = null!;
        private ListBox lstCarpetasRestaurar = null!;
        private Button btnAccionRestaurar = null!;
        private Label lblGarantia = null!;

        public string? CarpetaSeleccionada => lstCarpetasRestaurar.SelectedItem?.ToString();
        public Panel CardPanel => card;
        public event Action<string>? RestaurarRequested;

        public RestaurarView()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = UITheme.cBackground;
            InicializarComponentes();
        }

        private void InicializarComponentes()
        {
            card = new Panel
            {
                Size = new Size(680, 380),
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
            var badge1 = CrearBadge("🔓 DESENCRIPTACIÓN SEGURA", Color.FromArgb(50, 22, 22), Color.FromArgb(252, 165, 165), 40, badgeY);
            var badge2 = CrearBadge("🛡 RESTAURACIÓN ATÓMICA", Color.FromArgb(36, 33, 33), Color.FromArgb(209, 213, 219), 245, badgeY);
            card.Controls.AddRange(new Control[] { badge1, badge2 });

            lblTituloRestaurar = new Label
            {
                Text = Localization.Get("lbl_path_protected").ToUpper(),
                Location = new Point(40, 56),
                ForeColor = UITheme.cTextSecondary,
                AutoSize = true,
                Font = new Font("Segoe UI", 8, FontStyle.Bold)
            };
            card.Controls.Add(lblTituloRestaurar);

            lstCarpetasRestaurar = new ListBox
            {
                Location = new Point(40, 80),
                Size = new Size(600, 160),
                BackColor = UITheme.cInputBackground,
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10)
            };
            card.Controls.Add(lstCarpetasRestaurar);

            btnAccionRestaurar = new Button
            {
                Text = "🔓 " + (Localization.Get("btn_decrypt") ?? "RESTAURAR Y DESENCRIPTAR"),
                Size = new Size(600, 52),
                Location = new Point(40, 260),
                BackColor = UITheme.cAccentRed,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAccionRestaurar.FlatAppearance.BorderSize = 0;
            btnAccionRestaurar.FlatAppearance.MouseOverBackColor = UITheme.cAccentRedHover;
            btnAccionRestaurar.Click += (s, e) =>
            {
                if (lstCarpetasRestaurar.SelectedItem != null)
                {
                    RestaurarRequested?.Invoke(lstCarpetasRestaurar.SelectedItem.ToString()!);
                }
                else
                {
                    DarkDialogs.ShowInfo(Localization.Get("msg_select_restore"));
                }
            };
            card.Controls.Add(btnAccionRestaurar);

            lblGarantia = new Label
            {
                Text = Localization.Get("lbl_guarantee_decrypt"),
                Location = new Point(40, 324),
                Size = new Size(600, 20),
                ForeColor = Color.FromArgb(120, 110, 110),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 7.8f, FontStyle.Regular)
            };
            card.Controls.Add(lblGarantia);

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

        public void CargarCarpetas(IEnumerable<string> carpetas, string seleccionarRuta = "")
        {
            lstCarpetasRestaurar.Items.Clear();
            foreach (var c in carpetas)
            {
                lstCarpetasRestaurar.Items.Add(c);
            }

            if (!string.IsNullOrEmpty(seleccionarRuta) && lstCarpetasRestaurar.Items.Contains(seleccionarRuta))
            {
                lstCarpetasRestaurar.SelectedItem = seleccionarRuta;
            }
            else if (lstCarpetasRestaurar.Items.Count > 0)
            {
                lstCarpetasRestaurar.SelectedIndex = 0;
            }
        }

        public void ConfigurarProcesando(bool procesando)
        {
            btnAccionRestaurar.Enabled = !procesando;
            btnAccionRestaurar.Text = procesando ? Localization.Get("status_decrypting") : "🔓 " + (Localization.Get("btn_decrypt") ?? "RESTAURAR Y DESENCRIPTAR");
            this.Cursor = procesando ? Cursors.WaitCursor : Cursors.Default;
        }

        public void ActualizarIdioma()
        {
            lblTituloRestaurar.Text = Localization.Get("lbl_path_protected").ToUpper();
            btnAccionRestaurar.Text = "🔓 " + (Localization.Get("btn_decrypt") ?? "RESTAURAR Y DESENCRIPTAR");
            lblGarantia.Text = Localization.Get("lbl_guarantee_decrypt");
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Recentrar();
        }

        private void Recentrar()
        {
            if (card == null) return;
            int totalH = 100 + card.Height;
            int startY = Math.Max(25, (this.ClientSize.Height - totalH) / 2);
            int x = Math.Max(20, (this.ClientSize.Width - card.Width) / 2);
            card.Location = new Point(x, startY + 100);
        }
    }
}
