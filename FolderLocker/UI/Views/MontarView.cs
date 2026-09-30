using FolderLocker.UI.Common;

namespace FolderLocker.UI.Views
{
    public class MontarView : UserControl
    {
        private Label lblTituloMontar = null!;
        private Label lblSubtituloMontar = null!;
        private Panel card = null!;
        private Label lblListaMontar = null!;
        private Label lblCountBovedas = null!;
        private Panel pnlSearchGroup = null!;
        private Label lblSearchIcon = null!;
        private TextBox txtBuscarBoveda = null!;
        private Button btnClearSearch = null!;
        private ListBox lstCarpetasParaMontar = null!;
        private Label lblNoResults = null!;
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
        private bool _searchFocused = false;
        private readonly List<string> _todasLasCarpetas = new();

        public string? CarpetaSeleccionada => lstCarpetasParaMontar.SelectedItem?.ToString();
        public string LetraSeleccionada => _letraSeleccionada;
        public string Password => txtPassMontar.Text;
        public Panel CardPanel => card;
        public TextBox SearchBox => txtBuscarBoveda;
        public int TotalCarpetasCount => _todasLasCarpetas.Count;
        public int CarpetasFiltradasCount => lstCarpetasParaMontar.Items.Count;

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

            // Tarjeta principal (680 x 445)
            card = new Panel
            {
                Size = new Size(680, 445),
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
            int badgeY = 16;
            var badge1 = CrearBadge("💾 DOKAN VIRTUAL DISK", Color.FromArgb(50, 22, 22), Color.FromArgb(252, 165, 165), 40, badgeY);
            var badge2 = CrearBadge("⚡ ON-THE-FLY ACCESS", Color.FromArgb(36, 33, 33), Color.FromArgb(209, 213, 219), 215, badgeY);
            var badge3 = CrearBadge("🔒 AES-256 CTR", Color.FromArgb(36, 33, 33), Color.FromArgb(209, 213, 219), 375, badgeY);
            card.Controls.AddRange(new Control[] { badge1, badge2, badge3 });

            // 1. Selector de Bóvedas con Buscador en Tiempo Real
            lblListaMontar = new Label
            {
                Text = Localization.Get("lbl_vaults").ToUpper(),
                Location = new Point(40, 52),
                ForeColor = UITheme.cTextSecondary,
                AutoSize = true,
                Font = new Font("Segoe UI", 8, FontStyle.Bold)
            };
            card.Controls.Add(lblListaMontar);

            lblCountBovedas = new Label
            {
                Location = new Point(400, 52),
                Size = new Size(240, 16),
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = UITheme.cTextSecondary,
                Font = new Font("Segoe UI", 8, FontStyle.Bold)
            };
            card.Controls.Add(lblCountBovedas);

            // Barra de Búsqueda
            pnlSearchGroup = new Panel
            {
                Location = new Point(40, 74),
                Size = new Size(600, 32),
                BackColor = UITheme.cInputBackground
            };
            pnlSearchGroup.Paint += (s, e) =>
            {
                Color borderColor = _searchFocused ? UITheme.cAccentRed : Color.FromArgb(52, 47, 47);
                ControlPaint.DrawBorder(e.Graphics, pnlSearchGroup.ClientRectangle, borderColor, ButtonBorderStyle.Solid);
            };

            lblSearchIcon = new Label
            {
                Text = "🔍",
                Location = new Point(8, 5),
                Size = new Size(22, 20),
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(160, 150, 150),
                BackColor = Color.Transparent
            };
            pnlSearchGroup.Controls.Add(lblSearchIcon);

            txtBuscarBoveda = new TextBox
            {
                Location = new Point(34, 6),
                Size = new Size(534, 20),
                BorderStyle = BorderStyle.None,
                BackColor = UITheme.cInputBackground,
                ForeColor = UITheme.cTextPrimary,
                Font = new Font("Segoe UI", 9.5f),
                PlaceholderText = Localization.Get("placeholder_search_vault") ?? "Buscar bóveda por nombre o ruta..."
            };
            txtBuscarBoveda.GotFocus += (s, e) => { _searchFocused = true; pnlSearchGroup.Invalidate(); };
            txtBuscarBoveda.LostFocus += (s, e) => { _searchFocused = false; pnlSearchGroup.Invalidate(); };
            txtBuscarBoveda.TextChanged += (s, e) =>
            {
                btnClearSearch.Visible = !string.IsNullOrEmpty(txtBuscarBoveda.Text);
                FiltrarCarpetas();
            };
            txtBuscarBoveda.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Down && lstCarpetasParaMontar.Items.Count > 0)
                {
                    lstCarpetasParaMontar.Focus();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Escape && !string.IsNullOrEmpty(txtBuscarBoveda.Text))
                {
                    txtBuscarBoveda.Clear();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Enter)
                {
                    txtPassMontar.Focus();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            };
            pnlSearchGroup.Controls.Add(txtBuscarBoveda);

            btnClearSearch = new Button
            {
                Text = "✕",
                Location = new Point(572, 4),
                Size = new Size(24, 24),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(160, 150, 150),
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Visible = false
            };
            btnClearSearch.FlatAppearance.BorderSize = 0;
            btnClearSearch.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btnClearSearch.MouseEnter += (s, e) => btnClearSearch.ForeColor = UITheme.cAccentRed;
            btnClearSearch.MouseLeave += (s, e) => btnClearSearch.ForeColor = Color.FromArgb(160, 150, 150);
            btnClearSearch.Click += (s, e) =>
            {
                txtBuscarBoveda.Clear();
                txtBuscarBoveda.Focus();
            };
            pnlSearchGroup.Controls.Add(btnClearSearch);
            card.Controls.Add(pnlSearchGroup);

            // Lista de Bóvedas
            lstCarpetasParaMontar = new ListBox
            {
                Location = new Point(40, 112),
                Size = new Size(600, 92),
                BackColor = UITheme.cInputBackground,
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10),
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 30
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
                    e.Graphics.DrawString(itemText, isSelected ? new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold) : new Font("Segoe UI", 9.5f), tb, e.Bounds.X + 8, e.Bounds.Y + 5);
                }
            };
            lstCarpetasParaMontar.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Up && lstCarpetasParaMontar.SelectedIndex == 0)
                {
                    txtBuscarBoveda.Focus();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Enter)
                {
                    txtPassMontar.Focus();
                    e.Handled = true;
                }
            };
            card.Controls.Add(lstCarpetasParaMontar);

            // Mensaje de estado vacío / sin resultados
            lblNoResults = new Label
            {
                Location = new Point(42, 114),
                Size = new Size(596, 88),
                BackColor = UITheme.cInputBackground,
                ForeColor = Color.FromArgb(160, 150, 150),
                Font = new Font("Segoe UI", 9f, FontStyle.Italic),
                TextAlign = ContentAlignment.MiddleCenter,
                Visible = false
            };
            card.Controls.Add(lblNoResults);

            // 2. Chips para Letra de Unidad Virtual
            lblLetraMontar = new Label
            {
                Text = Localization.Get("lbl_drive").ToUpper(),
                Location = new Point(40, 214),
                ForeColor = UITheme.cTextSecondary,
                AutoSize = true,
                Font = new Font("Segoe UI", 8, FontStyle.Bold)
            };
            card.Controls.Add(lblLetraMontar);

            pnlLetrasChips = new FlowLayoutPanel
            {
                Location = new Point(40, 234),
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
                Location = new Point(40, 280),
                ForeColor = UITheme.cTextSecondary,
                AutoSize = true,
                Font = new Font("Segoe UI", 8, FontStyle.Bold)
            };
            card.Controls.Add(lblPassMontar);

            pnlPassGroup = new Panel
            {
                Location = new Point(40, 300),
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
            txtPassMontar.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    btnAccionMontar.PerformClick();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            };
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
                Size = new Size(390, 46),
                Location = new Point(40, 352),
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
                Size = new Size(200, 46),
                Location = new Point(440, 352),
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
                Location = new Point(40, 410),
                Size = new Size(600, 18),
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
            _todasLasCarpetas.Clear();
            if (carpetas != null)
            {
                _todasLasCarpetas.AddRange(carpetas);
            }

            // Si se pasa una carpeta específica a seleccionar que no coincida con el filtro actual, limpiamos el filtro
            if (!string.IsNullOrEmpty(seleccionar) && !string.IsNullOrEmpty(txtBuscarBoveda.Text))
            {
                if (!_todasLasCarpetas.Any(c => c.Equals(seleccionar, StringComparison.OrdinalIgnoreCase) && CoincideFiltro(c, txtBuscarBoveda.Text)))
                {
                    txtBuscarBoveda.Text = string.Empty;
                }
            }

            FiltrarCarpetas(seleccionar);
        }

        private bool CoincideFiltro(string ruta, string filtro)
        {
            if (string.IsNullOrWhiteSpace(filtro)) return true;
            var terminos = filtro.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string folderName = Path.GetFileName(ruta.TrimEnd('\\', '/'));
            return terminos.All(t =>
                ruta.Contains(t, StringComparison.OrdinalIgnoreCase) ||
                folderName.Contains(t, StringComparison.OrdinalIgnoreCase));
        }

        private void FiltrarCarpetas(string? seleccionar = null)
        {
            string filtro = txtBuscarBoveda?.Text?.Trim() ?? string.Empty;
            string? prevSeleccionada = seleccionar ?? lstCarpetasParaMontar.SelectedItem?.ToString();

            var filtradas = _todasLasCarpetas
                .Where(c => CoincideFiltro(c, filtro))
                .ToList();

            lstCarpetasParaMontar.BeginUpdate();
            lstCarpetasParaMontar.Items.Clear();

            foreach (var c in filtradas)
            {
                lstCarpetasParaMontar.Items.Add(c);
            }

            if (!string.IsNullOrEmpty(prevSeleccionada) && lstCarpetasParaMontar.Items.Contains(prevSeleccionada))
            {
                lstCarpetasParaMontar.SelectedItem = prevSeleccionada;
            }
            else if (lstCarpetasParaMontar.Items.Count > 0)
            {
                lstCarpetasParaMontar.SelectedIndex = 0;
            }

            lstCarpetasParaMontar.EndUpdate();

            // Actualizar etiquetas de estado y contador
            if (_todasLasCarpetas.Count == 0)
            {
                lblCountBovedas.Text = Localization.CurrentLang == "EN" ? "0 VAULTS" : "0 BÓVEDAS";
                lblCountBovedas.ForeColor = UITheme.cTextSecondary;
                lblNoResults.Text = Localization.Get("lbl_no_vaults_available");
                lblNoResults.Visible = true;
                lblNoResults.BringToFront();
            }
            else if (string.IsNullOrEmpty(filtro))
            {
                lblCountBovedas.Text = Localization.CurrentLang == "EN"
                    ? $"{_todasLasCarpetas.Count} VAULT{(_todasLasCarpetas.Count != 1 ? "S" : "")}"
                    : $"{_todasLasCarpetas.Count} BÓVEDA{(_todasLasCarpetas.Count != 1 ? "S" : "")}";
                lblCountBovedas.ForeColor = UITheme.cTextSecondary;
                lblNoResults.Visible = false;
            }
            else
            {
                int encontrados = lstCarpetasParaMontar.Items.Count;
                lblCountBovedas.Text = Localization.CurrentLang == "EN"
                    ? $"{encontrados}/{_todasLasCarpetas.Count} FOUND"
                    : $"{encontrados}/{_todasLasCarpetas.Count} ENCONTRADAS";
                lblCountBovedas.ForeColor = encontrados > 0 ? Color.FromArgb(74, 222, 128) : UITheme.cAccentRed;

                if (encontrados == 0)
                {
                    lblNoResults.Text = Localization.Get("lbl_no_vaults_found");
                    lblNoResults.Visible = true;
                    lblNoResults.BringToFront();
                }
                else
                {
                    lblNoResults.Visible = false;
                }
            }
        }

        public void LimpiarPassword()
        {
            txtPassMontar.Text = "";
        }

        public void LimpiarFiltro()
        {
            txtBuscarBoveda.Text = "";
        }

        public void ActualizarIdioma()
        {
            lblTituloMontar.Text = Localization.Get("title_virtual");
            lblSubtituloMontar.Text = Localization.CurrentLang == "EN"
                ? "Mount your protected vaults as virtual hard drives on-the-fly without decrypting."
                : "Monta tus bóvedas como discos duros virtuales en tiempo real sin necesidad de desencriptar.";
            lblListaMontar.Text = Localization.Get("lbl_vaults").ToUpper();
            txtBuscarBoveda.PlaceholderText = Localization.Get("placeholder_search_vault") ?? "Buscar bóveda por nombre o ruta...";
            lblLetraMontar.Text = Localization.Get("lbl_drive").ToUpper();
            lblPassMontar.Text = Localization.Get("lbl_mount_pass").ToUpper();
            btnAccionMontar.Text = "💾 " + (Localization.Get("btn_mount") ?? "MONTAR COMO DISCO VIRTUAL");
            btnAccionDesmontar.Text = "⏏️ " + (Localization.Get("btn_unmount") ?? "DESMONTAR");
            lblTip.Text = Localization.CurrentLang == "EN"
                ? "💡 The virtual drive appears in Windows Explorer and encrypts seamlessly in real-time."
                : "💡 La unidad virtual aparecerá en Este Equipo y se cifra en tiempo real sin alterar el disco físico.";

            FiltrarCarpetas(lstCarpetasParaMontar.SelectedItem?.ToString());
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
