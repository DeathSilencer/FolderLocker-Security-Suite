using FolderLocker.UI.Common;

namespace FolderLocker.UI.Views
{
    public class MontarView : UserControl
    {
        private Label lblTituloMontar = null!;
        private Panel card = null!;
        private Label lblListaMontar = null!;
        private ListBox lstCarpetasParaMontar = null!;
        private Label lblLetraMontar = null!;
        private ComboBox cmbLetraMontar = null!;
        private Label lblPassMontar = null!;
        private TextBox txtPassMontar = null!;
        private Button btnAccionMontar = null!;
        private Button btnAccionDesmontar = null!;

        public string? CarpetaSeleccionada => lstCarpetasParaMontar.SelectedItem?.ToString();
        public string LetraSeleccionada => cmbLetraMontar.Text;
        public string Password => txtPassMontar.Text;

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
                ForeColor = UITheme.cAccentRed,
                Font = new Font("Segoe UI Black", 20, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(50, 30)
            };
            this.Controls.Add(lblTituloMontar);

            card = UITheme.CrearTarjetaBase(550, 420);
            this.Controls.Add(card);

            lblListaMontar = UITheme.CrearEtiqueta(card, Localization.Get("lbl_vaults"), 40, 30);

            lstCarpetasParaMontar = new ListBox
            {
                Location = new Point(40, 55),
                Size = new Size(470, 100),
                BackColor = UITheme.cInputBackground,
                ForeColor = UITheme.cTextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10)
            };
            card.Controls.Add(lstCarpetasParaMontar);

            lblLetraMontar = UITheme.CrearEtiqueta(card, Localization.Get("lbl_drive"), 40, 170);

            cmbLetraMontar = new ComboBox
            {
                Location = new Point(40, 195),
                Size = new Size(100, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = UITheme.cInputBackground,
                ForeColor = UITheme.cTextPrimary,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                DropDownStyle = ComboBoxStyle.DropDownList,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 34
            };
            cmbLetraMontar.Items.AddRange(new[] { "M:\\", "Z:\\", "X:\\", "W:\\", "L:\\", "K:\\", "J:\\" });
            cmbLetraMontar.DrawItem += (s, e) =>
            {
                if (e.Index < 0 || s is not ComboBox cb) return;
                bool sel = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
                using (var b = new SolidBrush(sel ? UITheme.cAccentRed : UITheme.cInputBackground)) e.Graphics.FillRectangle(b, e.Bounds);
                using (var tb = new SolidBrush(UITheme.cTextPrimary))
                {
                    string txt = cb.Items[e.Index]?.ToString() ?? "";
                    SizeF sz = e.Graphics.MeasureString(txt, cb.Font);
                    e.Graphics.DrawString(txt, cb.Font, tb, new PointF(e.Bounds.X + (e.Bounds.Width - sz.Width) / 2, e.Bounds.Y + (e.Bounds.Height - cb.Font.Height) / 2));
                }
            };
            string savedLetter = Properties.Settings.Default.LetraGuardada;
            cmbLetraMontar.Text = string.IsNullOrEmpty(savedLetter) ? "M:\\" : savedLetter;
            card.Controls.Add(cmbLetraMontar);

            lblPassMontar = UITheme.CrearEtiqueta(card, Localization.Get("lbl_mount_pass"), 160, 170);
            txtPassMontar = UITheme.CrearInputPassword(card, 160, 195, 350);

            btnAccionMontar = new Button
            {
                Text = Localization.Get("btn_mount"),
                Size = new Size(470, 45),
                Location = new Point(40, 260)
            };
            UITheme.EstilarBotonAccion(btnAccionMontar);
            btnAccionMontar.Click += (s, e) =>
            {
                if (lstCarpetasParaMontar.SelectedItem == null)
                {
                    DarkDialogs.ShowInfo(Localization.Get("msg_mount_select"));
                    return;
                }
                MontarRequested?.Invoke(lstCarpetasParaMontar.SelectedItem.ToString()!, cmbLetraMontar.Text, txtPassMontar.Text);
            };
            card.Controls.Add(btnAccionMontar);

            btnAccionDesmontar = new Button
            {
                Text = Localization.Get("btn_unmount"),
                Size = new Size(470, 40),
                Location = new Point(40, 315)
            };
            UITheme.EstilarBotonSecundario(btnAccionDesmontar);
            btnAccionDesmontar.ForeColor = Color.IndianRed;
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

            Recentrar();
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
            lblListaMontar.Text = Localization.Get("lbl_vaults").ToUpper();
            lblLetraMontar.Text = Localization.Get("lbl_drive").ToUpper();
            lblPassMontar.Text = Localization.Get("lbl_mount_pass").ToUpper();
            btnAccionMontar.Text = Localization.Get("btn_mount");
            btnAccionDesmontar.Text = Localization.Get("btn_unmount");
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

            if (lblTituloMontar != null)
            {
                lblTituloMontar.Location = new Point(x, Math.Max(10, y - 45));
            }
        }
    }
}
