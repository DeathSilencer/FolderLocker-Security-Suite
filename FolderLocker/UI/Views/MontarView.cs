using FolderLocker.UI.Common;

namespace FolderLocker.UI.Views
{
    public class MontarView : UserControl
    {
        private Label lblTituloMontar = null!;
        private Label lblSubtituloMontar = null!;
        private Panel card = null!;
        private Label lblListaMontar = null!;
        private ListBox lstCarpetasParaMontar = null!;
        private Label lblLetraMontar = null!;
        private FlowLayoutPanel pnlLetrasChips = null!;
        private Label lblPassMontar = null!;
        private Panel pnlPassGroup = null!;
        private TextBox txtPassMontar = null!;
        private Button btnEye = null!;
        private Button btnAccionMontar = null!;
        private Button btnAccionDesmontar = null!;
        private Label lblTip = null!;

        private string _letraSeleccionada = "M:\\";
        private readonly List<Button> _chipButtons = new();
        private bool _passFocused = false;

        public string? CarpetaSeleccionada => lstCarpetasParaMontar.SelectedItem?.ToString();
        public string LetraSeleccionada => _letraSeleccionada;
        public string Password => txtPassMontar.Text;
        public Panel CardPanel => card;

        public event Action<string, string, string>? MontarRequested;
        public event Action<string>? DesmontarRequested;

        public MontarView()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = UITheme.cBackground;
            InicializarComponentes();
        }

        private void InicializarComponentes()
        {
            lblTituloMontar = new Label
            {
                Text = Localization.Get("title_virtual"),
                ForeColor = UITheme.cTextPrimary,
                Font = new Font("Segoe UI Semibold", 20, FontStyle.Bold),
                AutoSize = true
            };
            this.Controls.Add(lblTituloMontar);

            lblSubtituloMontar = new Label
            {
                Text = Localization.CurrentLang == "EN"
                    ? "Mount your protected vaults as virtual hard drives on-the-fly without decrypting."
                    : "Monta tus bóvedas como discos duros virtuales en tiempo real sin necesidad de desencriptar.",
                ForeColor = Color.FromArgb(160, 150, 150),
                Font = new Font("Segoe UI", 9.5f),
                AutoSize = true
            };
            this.Controls.Add(lblSubtituloMontar);

            // Tarjeta principal (680 x 430)
            card = new Panel
            {
                Size = new Size(680, 430),
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
            var badge1 = CrearBadge("💾 DOKAN VIRTUAL DISK", Color.FromArgb(50, 22, 22), Color.FromArgb(252, 165, 165), 40, badgeY);
            var badge2 = CrearBadge("⚡ ON-THE-FLY ACCESS", Color.FromArgb(36, 33, 33), Color.FromArgb(209, 213, 219), 215, badgeY);
            var badge3 = CrearBadge("🔒 AES-256 CTR", Color.FromArgb(36, 33, 33), Color.FromArgb(209, 213, 219), 375, badgeY);
            card.Controls.AddRange(new Control[] { badge1, badge2, badge3 });

            // 1. Selector de Bóvedas
            lblListaMontar = new Label
            {
                Text = Localization.Get("lbl_vaults").ToUpper(),
                Location = new Point(40, 56),
                ForeColor = UITheme.cTextSecondary,
                AutoSize = true,
                Font = new Font("Segoe UI", 8, FontStyle.Bold)
            };
            card.Controls.Add(lblListaMontar);

            lstCarpetasParaMontar = new ListBox
            {
                Location = new Point(40, 80),
                Size = new Size(600, 100),
                BackColor = UITheme.cInputBackground,
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10),
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 32
            };
            lstCarpetasParaMontar.DrawItem += (s, e) =>
            {
                if (e.Index < 0 || e.Index >= lstCarpetasParaMontar.Items.Count) return;
                bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
                Color bg = isSelected ? Color.FromArgb(52, 24, 24) : UITheme.cInputBackground;
                Color fg = isSelected ? Color.White : Color.FromArgb(210, 210, 210);

                using (var b = new SolidBrush(bg)) e.Graphics.FillRectangle(b, e.Bounds);
                if (isSelected)
                {
                    using var ab = new SolidBrush(UITheme.cAccentRed);
                    e.Graphics.FillRectangle(ab, e.Bounds.X, e.Bounds.Y, 3, e.Bounds.Height);
                }

                string itemText = "🔒 " + (lstCarpetasParaMontar.Items[e.Index]?.ToString() ?? "");
                using (var tb = new SolidBrush(fg))
                {
                    e.Graphics.DrawString(itemText, isSelected ? new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold) : new Font("Segoe UI", 9.5f), tb, e.Bounds.X + 8, e.Bounds.Y + 6);
                }
            };
            card.Controls.Add(lstCarpetasParaMontar);

            // 2. Chips para Letra de Unidad Virtual
            lblLetraMontar = new Label
            {
                Text = Localization.Get("lbl_drive").ToUpper(),
                Location = new Point(40, 192),
                ForeColor = UITheme.cTextSecondary,
                AutoSize = true,
                Font = new Font("Segoe UI", 8, FontStyle.Bold)
            };
            card.Controls.Add(lblLetraMontar);

            pnlLetrasChips = new FlowLayoutPanel
            {
                Location = new Point(40, 214),
                Size = new Size(600, 36),
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

            string savedLetter = Properties.Settings.Default.LetraGuardada;
            _letraSeleccionada = string.IsNullOrEmpty(savedLetter) ? "M:\\" : savedLetter;
            string[] letras = { "M:\\", "Z:\\", "X:\\", "W:\\", "L:\\", "K:\\", "J:\\" };

            foreach (var l in letras)
            {
                var chip = new Button
                {
                    Text = l,
                    Size = new Size(54, 34),
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand,
                    Tag = l,
                    Margin = new Padding(0, 0, 8, 0)
                };
                chip.FlatAppearance.BorderSize = 0;
                chip.Click += (s, e) =>
                {
                    _letraSeleccionada = l;
                    Properties.Settings.Default.LetraGuardada = l;
                    Properties.Settings.Default.Save();
                    ActualizarEstiloChips();
                };
                _chipButtons.Add(chip);
                pnlLetrasChips.Controls.Add(chip);
            }
            ActualizarEstiloChips();
            card.Controls.Add(pnlLetrasChips);

            // 3. Contraseña de Montaje
            lblPassMontar = new Label
            {
                Text = Localization.Get("lbl_mount_pass").ToUpper(),
                Location = new Point(40, 260),
                ForeColor = UITheme.cTextSecondary,
                AutoSize = true,
                Font = new Font("Segoe UI", 8, FontStyle.Bold)
            };
            card.Controls.Add(lblPassMontar);

            pnlPassGroup = new Panel
            {
                Location = new Point(40, 280),
                Size = new Size(600, 42),
                BackColor = UITheme.cInputBackground
            };
            pnlPassGroup.Paint += (s, e) =>
            {
                Color borderColor = _passFocused ? UITheme.cAccentRed : Color.FromArgb(52, 47, 47);
                ControlPaint.DrawBorder(e.Graphics, pnlPassGroup.ClientRectangle, borderColor, ButtonBorderStyle.Solid);
            };

            var lblPassIcon = new Label
            {
                Text = "🔑",
                Location = new Point(10, 10),
                Size = new Size(26, 24),
                Font = new Font("Segoe UI", 12),
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };
            pnlPassGroup.Controls.Add(lblPassIcon);

            txtPassMontar = new TextBox
            {
                Location = new Point(40, 10),
                Size = new Size(512, 24),
                BorderStyle = BorderStyle.None,
                BackColor = UITheme.cInputBackground,
                ForeColor = UITheme.cTextPrimary,
                Font = new Font("Segoe UI", 10.5f),
                UseSystemPasswordChar = true
            };
            txtPassMontar.GotFocus += (s, e) => { _passFocused = true; pnlPassGroup.Invalidate(); };
            txtPassMontar.LostFocus += (s, e) => { _passFocused = false; pnlPassGroup.Invalidate(); };
            pnlPassGroup.Controls.Add(txtPassMontar);

            btnEye = new Button
            {
                Text = "👁",
                Location = new Point(558, 4),
                Size = new Size(38, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(160, 150, 150),
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 11)
            };
            btnEye.FlatAppearance.BorderSize = 0;
            btnEye.MouseDown += (s, e) => { txtPassMontar.UseSystemPasswordChar = false; btnEye.ForeColor = UITheme.cAccentRed; };
            btnEye.MouseUp += (s, e) => { txtPassMontar.UseSystemPasswordChar = true; btnEye.ForeColor = Color.FromArgb(160, 150, 150); };
            pnlPassGroup.Controls.Add(btnEye);
            card.Controls.Add(pnlPassGroup);

            // 4. Botones de Acción
            btnAccionMontar = new Button
            {
                Text = "💾 " + (Localization.Get("btn_mount") ?? "MONTAR COMO DISCO VIRTUAL"),
                Size = new Size(390, 48),
                Location = new Point(40, 336),
                BackColor = UITheme.cAccentRed,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAccionMontar.FlatAppearance.BorderSize = 0;
            btnAccionMontar.FlatAppearance.MouseOverBackColor = UITheme.cAccentRedHover;
            btnAccionMontar.Click += (s, e) =>
            {
                if (lstCarpetasParaMontar.SelectedItem == null)
                {
                    DarkDialogs.ShowInfo(Localization.Get("msg_mount_select"));
                    return;
                }
                MontarRequested?.Invoke(lstCarpetasParaMontar.SelectedItem.ToString()!, _letraSeleccionada, txtPassMontar.Text);
            };
            card.Controls.Add(btnAccionMontar);

            btnAccionDesmontar = new Button
            {
                Text = "⏏️ " + (Localization.Get("btn_unmount") ?? "DESMONTAR"),
                Size = new Size(200, 48),
                Location = new Point(440, 336),
                BackColor = Color.FromArgb(46, 36, 36),
                ForeColor = Color.FromArgb(248, 113, 113),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAccionDesmontar.FlatAppearance.BorderSize = 0;
            btnAccionDesmontar.FlatAppearance.MouseOverBackColor = Color.FromArgb(64, 46, 46);
            btnAccionDesmontar.Click += (s, e) =>
            {
                if (lstCarpetasParaMontar.SelectedItem == null)
                {
                    DarkDialogs.ShowInfo(Localization.Get("msg_mount_select"));
                    return;
                }
                DesmontarRequested?.Invoke(lstCarpetasParaMontar.SelectedItem.ToString()!);
            };
            card.Controls.Add(btnAccionDesmontar);

            // 5. Tip de ayuda al pie
            lblTip = new Label
            {
                Text = Localization.CurrentLang == "EN"
                    ? "💡 The virtual drive appears in Windows Explorer and encrypts seamlessly in real-time."
                    : "💡 La unidad virtual aparecerá en Este Equipo y se cifra en tiempo real sin alterar el disco físico.",
                Location = new Point(40, 396),
                Size = new Size(600, 20),
                ForeColor = Color.FromArgb(120, 110, 110),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 7.8f, FontStyle.Regular)
            };
            card.Controls.Add(lblTip);

            Recentrar();
        }

        private void ActualizarEstiloChips()
        {
            foreach (var btn in _chipButtons)
            {
                bool sel = string.Equals(btn.Tag?.ToString(), _letraSeleccionada, StringComparison.OrdinalIgnoreCase);
                btn.BackColor = sel ? UITheme.cAccentRed : Color.FromArgb(42, 38, 38);
                btn.ForeColor = sel ? Color.White : Color.FromArgb(200, 190, 190);
                btn.Font = new Font("Segoe UI", 9.5f, sel ? FontStyle.Bold : FontStyle.Regular);
            }
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

        public void CargarCarpetas(IEnumerable<string> carpetas, string seleccionar = "")
        {
            lstCarpetasParaMontar.Items.Clear();
            foreach (var c in carpetas)
            {
                lstCarpetasParaMontar.Items.Add(c);
            }

            if (!string.IsNullOrEmpty(seleccionar) && lstCarpetasParaMontar.Items.Contains(seleccionar))
            {
                lstCarpetasParaMontar.SelectedItem = seleccionar;
            }
            else if (lstCarpetasParaMontar.Items.Count > 0)
            {
                lstCarpetasParaMontar.SelectedIndex = 0;
            }
        }

        public void LimpiarPassword()
        {
            txtPassMontar.Text = "";
        }

        public void ActualizarIdioma()
        {
            lblTituloMontar.Text = Localization.Get("title_virtual");
            lblSubtituloMontar.Text = Localization.CurrentLang == "EN"
                ? "Mount your protected vaults as virtual hard drives on-the-fly without decrypting."
                : "Monta tus bóvedas como discos duros virtuales en tiempo real sin necesidad de desencriptar.";
            lblListaMontar.Text = Localization.Get("lbl_vaults").ToUpper();
            lblLetraMontar.Text = Localization.Get("lbl_drive").ToUpper();
            lblPassMontar.Text = Localization.Get("lbl_mount_pass").ToUpper();
            btnAccionMontar.Text = "💾 " + (Localization.Get("btn_mount") ?? "MONTAR COMO DISCO VIRTUAL");
            btnAccionDesmontar.Text = "⏏️ " + (Localization.Get("btn_unmount") ?? "DESMONTAR");
            lblTip.Text = Localization.CurrentLang == "EN"
                ? "💡 The virtual drive appears in Windows Explorer and encrypts seamlessly in real-time."
                : "💡 La unidad virtual aparecerá en Este Equipo y se cifra en tiempo real sin alterar el disco físico.";
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

            if (lblTituloMontar != null)
            {
                lblTituloMontar.Location = new Point(x, startY);
            }
            if (lblSubtituloMontar != null)
            {
                lblSubtituloMontar.Location = new Point(x, startY + 34);
            }

            card.Location = new Point(x, startY + 68);
        }
    }
}
