namespace FolderLocker.UI.Common
{
    public static class UITheme
    {
        #region PALETA DE COLORES PROFESIONAL

        public static readonly Color cBackground = Color.FromArgb(18, 16, 16);
        public static readonly Color cSurface = Color.FromArgb(28, 26, 26);
        public static readonly Color cInputBackground = Color.FromArgb(38, 35, 35);
        public static readonly Color cAccentRed = Color.FromArgb(185, 28, 28);
        public static readonly Color cAccentRedHover = Color.FromArgb(220, 38, 38);
        public static readonly Color cTextPrimary = Color.FromArgb(240, 240, 240);
        public static readonly Color cTextSecondary = Color.FromArgb(160, 150, 150);
        public static readonly Color cBorder = Color.FromArgb(45, 40, 40);

        #endregion

        #region FACTORIES Y ESTILOS DE CONTROLES

        public static Panel CrearTarjetaBase(int w, int h)
        {
            var p = new Panel { Size = new Size(w, h), BackColor = cSurface };
            p.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, p.ClientRectangle, cBorder, ButtonBorderStyle.Solid);
            return p;
        }

        public static Label CrearEtiqueta(Control p, string texto, int x, int y)
        {
            var lbl = new Label
            {
                Text = texto.ToUpper(),
                Location = new Point(x, y),
                ForeColor = cTextSecondary,
                AutoSize = true,
                Font = new Font("Segoe UI", 8, FontStyle.Bold)
            };
            p.Controls.Add(lbl);
            return lbl;
        }

        public static Label CrearHeaderSeccion(Control p, string texto, int x, int y)
        {
            var lbl = new Label
            {
                Text = texto,
                Location = new Point(x, y),
                AutoSize = true,
                ForeColor = cAccentRed,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            p.Controls.Add(lbl);
            return lbl;
        }

        public static TextBox CrearInput(Control p, int x, int y, int w)
        {
            var t = new TextBox
            {
                Location = new Point(x, y),
                Width = w,
                BackColor = cInputBackground,
                ForeColor = cTextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 11),
                AutoSize = false,
                Height = 30
            };
            p.Controls.Add(t);
            return t;
        }

        public static TextBox CrearInputPassword(Control p, int x, int y, int w)
        {
            var t = new TextBox
            {
                Location = new Point(x, y),
                Width = w - 40,
                BackColor = cInputBackground,
                ForeColor = cTextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 11),
                AutoSize = false,
                Height = 30,
                UseSystemPasswordChar = true
            };
            p.Controls.Add(t);

            var btnEye = new Button
            {
                Text = "👁️",
                Location = new Point(x + w - 35, y),
                Size = new Size(35, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = cInputBackground,
                ForeColor = cTextPrimary,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10)
            };
            btnEye.FlatAppearance.BorderSize = 0;
            btnEye.MouseDown += (s, e) => { t.UseSystemPasswordChar = false; btnEye.ForeColor = cAccentRed; };
            btnEye.MouseUp += (s, e) => { t.UseSystemPasswordChar = true; btnEye.ForeColor = cTextPrimary; };
            p.Controls.Add(btnEye);

            return t;
        }

        public static void CrearSeparador(Control p, int y)
        {
            var sep = new Panel
            {
                Size = new Size(p.Width - 80, 1),
                BackColor = Color.FromArgb(45, 45, 45),
                Location = new Point(40, y)
            };
            p.Controls.Add(sep);
        }

        public static void EstilarBotonAccion(Button b)
        {
            if (b == null) return;
            b.BackColor = cAccentRed;
            b.ForeColor = Color.White;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.Font = new Font("Segoe UI Black", 11, FontStyle.Bold);
            b.Cursor = Cursors.Hand;
        }

        public static void EstilarBotonSecundario(Button b)
        {
            if (b == null) return;
            b.FlatStyle = FlatStyle.Flat;
            b.ForeColor = cTextPrimary;
            b.BackColor = cInputBackground;
            b.FlatAppearance.BorderColor = cSurface;
            b.Cursor = Cursors.Hand;
        }

        public static void DibujarComboConBanderas(object? s, DrawItemEventArgs e)
        {
            if (e.Index < 0 || s is not ComboBox combo) return;
            string text = combo.Items[e.Index]?.ToString() ?? "";
            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

            using (var brush = new SolidBrush(isSelected ? cAccentRed : cInputBackground))
                e.Graphics.FillRectangle(brush, e.Bounds);

            Image? flag = null;
            if (text.Contains("Español") || text.Contains("ES")) flag = Properties.Resources.flag_es;
            else if (text.Contains("English") || text.Contains("EN")) flag = Properties.Resources.flag_en;
            else if (text.Contains("Português") || text.Contains("PT")) flag = Properties.Resources.flag_pt;
            else if (text.Contains("Русский") || text.Contains("RU")) flag = Properties.Resources.flag_ru;
            else if (text.Contains("中文") || text.Contains("CN")) flag = Properties.Resources.flag_ch;

            if (flag != null)
                e.Graphics.DrawImage(flag, new Rectangle(e.Bounds.X + 10, e.Bounds.Y + (e.Bounds.Height - 20) / 2, 20, 20));

            using (var textBrush = new SolidBrush(cTextPrimary))
                e.Graphics.DrawString(text, combo.Font ?? SystemFonts.DefaultFont, textBrush, new Point(40, e.Bounds.Y + 5));
        }

        public static void AdjuntarMedidorFortaleza(Control parent, TextBox txtPassword, int x, int y, int w)
        {
            var pnlBack = new Panel { Location = new Point(x, y), Size = new Size(w, 4), BackColor = Color.FromArgb(40, 40, 40) };
            var pnlFill = new Panel { Location = new Point(0, 0), Size = new Size(0, 4), BackColor = cAccentRed };
            pnlBack.Controls.Add(pnlFill);
            parent.Controls.Add(pnlBack);

            var lblStatus = new Label
            {
                Text = "",
                Location = new Point(x, y + 8),
                AutoSize = true,
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Color.DimGray
            };
            parent.Controls.Add(lblStatus);

            txtPassword.TextChanged += (s, e) =>
            {
                int score = AuthService.CalcularFortaleza(txtPassword.Text);
                Color c = Color.DimGray;
                int pct = 0;
                switch (score)
                {
                    case 0: pct = 5; c = Color.FromArgb(100, 30, 30); break;
                    case 1: pct = 20; c = Color.IndianRed; break;
                    case 2: pct = 40; c = Color.Orange; break;
                    case 3: pct = 60; c = Color.Gold; break;
                    case 4: pct = 80; c = Color.YellowGreen; break;
                    case 5: pct = 100; c = Color.LimeGreen; break;
                }
                pnlFill.Width = (w * pct) / 100;
                pnlFill.BackColor = c;
                lblStatus.Text = Localization.Get("str_" + score);
                lblStatus.ForeColor = c;
            };
        }

        #endregion

        public class CircularPictureBox : PictureBox
        {
            protected override void OnPaint(PaintEventArgs pe)
            {
                using var gp = new System.Drawing.Drawing2D.GraphicsPath();
                gp.AddEllipse(0, 0, ClientSize.Width, ClientSize.Height);
                this.Region = new Region(gp);
                base.OnPaint(pe);
            }
        }
    }
}
