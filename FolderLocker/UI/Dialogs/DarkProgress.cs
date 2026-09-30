namespace FolderLocker
{
    public class DarkProgress : Form
    {
        private ProgressBar barra;
        private Label lblEstado;
        private Label lblPorcentaje;

        public event EventHandler OnMinimizarAlTray;

        public DarkProgress()
        {
            this.Size = new Size(500, 180);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(20, 18, 18);
            this.TopMost = true;

            var pnlBorde = new Panel { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
            this.Controls.Add(pnlBorde);

            var lblTitulo = new Label
            {
                // TRADUCCIÓN APLICADA
                Text = Localization.Get("prog_title"),
                Location = new Point(20, 15),
                AutoSize = true,
                ForeColor = Color.FromArgb(198, 40, 40),
                Font = new Font("Segoe UI", 12, FontStyle.Bold)
            };
            pnlBorde.Controls.Add(lblTitulo);

            lblEstado = new Label
            {
                // TRADUCCIÓN APLICADA
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
                // TRADUCCIÓN APLICADA
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