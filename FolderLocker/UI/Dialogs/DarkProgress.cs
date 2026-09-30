using System.Runtime.InteropServices;

namespace FolderLocker
{
    public class DarkProgress : Form
    {
        [DllImport("user32.dll", EntryPoint = "ReleaseCapture")]
        private static extern void ReleaseCapture();
        [DllImport("user32.dll", EntryPoint = "SendMessage")]
        private static extern void SendMessage(IntPtr hwnd, int wmsg, int wparam, int lparam);

        private static void MoverVentana(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                if (sender is Control c && c.TopLevelControl is Form f)
                    SendMessage(f.Handle, 0xA1, 0x2, 0);
            }
        }

        private ProgressBar barra;
        private Label lblEstado;
        private Label lblPorcentaje;

        public event EventHandler? OnMinimizarAlTray;

        public DarkProgress()
        {
            this.Size = new Size(500, 180);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(20, 18, 18);
            this.TopMost = false; // No se encimar sobre otras aplicaciones externas (como Brave)
            this.ShowInTaskbar = false;

            var pnlBorde = new Panel { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
            pnlBorde.MouseDown += MoverVentana;
            this.Controls.Add(pnlBorde);

            var lblTitulo = new Label
            {
                Text = Localization.Get("prog_title"),
                Location = new Point(20, 15),
                AutoSize = true,
                ForeColor = Color.FromArgb(198, 40, 40),
                Font = new Font("Segoe UI", 12, FontStyle.Bold)
            };
            lblTitulo.MouseDown += MoverVentana;
            pnlBorde.Controls.Add(lblTitulo);

            lblEstado = new Label
            {
                Text = Localization.Get("prog_init"),
                Location = new Point(20, 45),
                AutoSize = true,
                ForeColor = Color.WhiteSmoke,
                Font = new Font("Segoe UI", 9)
            };
            pnlBorde.Controls.Add(lblEstado);

            lblPorcentaje = new Label
            {
                Text = "0%",
                Location = new Point(440, 45),
                AutoSize = true,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            pnlBorde.Controls.Add(lblPorcentaje);

            barra = new ProgressBar
            {
                Location = new Point(20, 70),
                Size = new Size(460, 20),
                Style = ProgressBarStyle.Continuous,
                Maximum = 100,
                Value = 0
            };
            pnlBorde.Controls.Add(barra);

            var btnHide = new Button
            {
                Text = Localization.Get("prog_hide_btn"),
                Location = new Point(125, 110),
                Size = new Size(250, 35),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.DarkGray,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9)
            };
            btnHide.FlatAppearance.BorderColor = Color.FromArgb(60, 60, 60);
            btnHide.Click += (s, e) => { this.Hide(); OnMinimizarAlTray?.Invoke(this, EventArgs.Empty); };
            pnlBorde.Controls.Add(btnHide);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (this.Owner != null)
            {
                int x = this.Owner.Location.X + (this.Owner.Width - this.Width) / 2;
                int y = this.Owner.Location.Y + (this.Owner.Height - this.Height) / 2;
                this.Location = new Point(Math.Max(0, x), Math.Max(0, y));
            }
        }

        public void Actualizar(int porcentaje, string texto)
        {
            if (porcentaje > 100) porcentaje = 100;
            if (barra.Value != porcentaje) barra.Value = porcentaje;
            lblEstado.Text = texto;
            lblPorcentaje.Text = porcentaje + "%";
            Application.DoEvents();
        }
    }
}