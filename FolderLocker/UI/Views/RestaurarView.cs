using FolderLocker.UI.Common;

namespace FolderLocker.UI.Views
{
    public class RestaurarView : UserControl
    {
        private Panel card = null!;
        private Label lblTituloRestaurar = null!;
        private ListBox lstCarpetasRestaurar = null!;
        private Button btnAccionRestaurar = null!;

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
            card = UITheme.CrearTarjetaBase(550, 300);
            this.Controls.Add(card);

            lblTituloRestaurar = UITheme.CrearEtiqueta(card, Localization.Get("lbl_path_protected"), 40, 30);

            lstCarpetasRestaurar = new ListBox
            {
                Location = new Point(40, 55),
                Size = new Size(470, 120),
                BackColor = UITheme.cInputBackground,
                ForeColor = UITheme.cAccentRed,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10)
            };
            card.Controls.Add(lstCarpetasRestaurar);

            btnAccionRestaurar = new Button
            {
                Text = Localization.Get("btn_decrypt"),
                Size = new Size(470, 50),
                Location = new Point(40, 200)
            };
            UITheme.EstilarBotonAccion(btnAccionRestaurar);
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

            Recentrar();
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
            btnAccionRestaurar.Text = procesando ? Localization.Get("status_decrypting") : Localization.Get("btn_decrypt");
            this.Cursor = procesando ? Cursors.WaitCursor : Cursors.Default;
        }

        public void ActualizarIdioma()
        {
            lblTituloRestaurar.Text = Localization.Get("lbl_path_protected").ToUpper();
            btnAccionRestaurar.Text = Localization.Get("btn_decrypt");
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
            int startY = Math.Max(30, (this.ClientSize.Height - totalH) / 2);
            int x = Math.Max(20, (this.ClientSize.Width - card.Width) / 2);
            card.Location = new Point(x, startY + 100);
        }
    }
}
