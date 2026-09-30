using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

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

        private readonly ProgressBar barra;
        private readonly Label lblEstado;
        private readonly Label lblPorcentaje;
        private readonly Button btnPausar;
        private readonly Button btnCancelar;
        private readonly Button btnHide;

        public event EventHandler? OnMinimizarAlTray;
        public event EventHandler? OnPausarRequested;
        public event EventHandler? OnReanudarRequested;
        public event EventHandler? OnCancelarRequested;

        public bool IsPaused { get; private set; }

        public DarkProgress()
        {
            this.Size = new Size(500, 168);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(20, 18, 18);
            this.TopMost = false; // No se encimar sobre otras aplicaciones externas
            this.ShowInTaskbar = false;

            var pnlBorde = new Panel { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
            pnlBorde.MouseDown += MoverVentana;
            this.Controls.Add(pnlBorde);

            var lblTitulo = new Label
            {
                Text = Localization.Get("prog_title") ?? "OPERACIÓN EN CURSO",
                Location = new Point(20, 15),
                AutoSize = true,
                ForeColor = Color.FromArgb(198, 40, 40),
                Font = new Font("Segoe UI", 12, FontStyle.Bold)
            };
            lblTitulo.MouseDown += MoverVentana;
            pnlBorde.Controls.Add(lblTitulo);

            lblEstado = new Label
            {
                Text = Localization.Get("prog_init") ?? "Iniciando motor criptográfico...",
                Location = new Point(20, 44),
                Size = new Size(410, 20),
                AutoEllipsis = true,
                ForeColor = Color.WhiteSmoke,
                Font = new Font("Segoe UI", 9)
            };
            lblEstado.MouseDown += MoverVentana;
            pnlBorde.Controls.Add(lblEstado);

            lblPorcentaje = new Label
            {
                Text = "0%",
                Location = new Point(440, 44),
                AutoSize = true,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            pnlBorde.Controls.Add(lblPorcentaje);

            barra = new ProgressBar
            {
                Location = new Point(20, 68),
                Size = new Size(460, 22),
                Style = ProgressBarStyle.Continuous,
                Maximum = 100,
                Value = 0
            };
            pnlBorde.Controls.Add(barra);

            // 1. Botón Pausar / Reanudar
            btnPausar = new Button
            {
                Text = Localization.Get("prog_pause") ?? "⏸️  PAUSAR",
                Location = new Point(20, 106),
                Size = new Size(135, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(42, 38, 38),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold)
            };
            btnPausar.FlatAppearance.BorderColor = Color.FromArgb(70, 60, 60);
            btnPausar.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, 52, 52);
            btnPausar.Click += (s, e) =>
            {
                if (!IsPaused)
                {
                    OnPausarRequested?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    OnReanudarRequested?.Invoke(this, EventArgs.Empty);
                }
            };
            pnlBorde.Controls.Add(btnPausar);

            // 2. Botón Cancelar
            btnCancelar = new Button
            {
                Text = Localization.Get("prog_cancel") ?? "🛑  CANCELAR",
                Location = new Point(165, 106),
                Size = new Size(135, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(48, 24, 24),
                ForeColor = Color.FromArgb(254, 202, 202),
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold)
            };
            btnCancelar.FlatAppearance.BorderColor = Color.FromArgb(100, 36, 36);
            btnCancelar.FlatAppearance.MouseOverBackColor = Color.FromArgb(85, 28, 28);
            btnCancelar.Click += (s, e) =>
            {
                OnCancelarRequested?.Invoke(this, EventArgs.Empty);
            };
            pnlBorde.Controls.Add(btnCancelar);

            // 3. Botón Minimizar a Bandeja (Segundo Plano)
            btnHide = new Button
            {
                Text = Localization.Get("prog_hide_btn") ?? "👁️  SEGUNDO PLANO",
                Location = new Point(310, 106),
                Size = new Size(170, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(32, 28, 28),
                ForeColor = Color.FromArgb(180, 170, 170),
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 8.2f)
            };
            btnHide.FlatAppearance.BorderColor = Color.FromArgb(55, 50, 50);
            btnHide.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, 44, 44);
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

        public void SetPaused(bool paused)
        {
            IsPaused = paused;
            if (paused)
            {
                btnPausar.Text = Localization.Get("prog_resume") ?? "▶️  REANUDAR";
                btnPausar.BackColor = Color.FromArgb(22, 60, 35);
                btnPausar.FlatAppearance.BorderColor = Color.FromArgb(34, 197, 94);
                btnPausar.ForeColor = Color.FromArgb(187, 247, 208);
                lblEstado.Text = Localization.Get("prog_paused") ?? "⏸️ Operación en pausa por el usuario";
            }
            else
            {
                btnPausar.Text = Localization.Get("prog_pause") ?? "⏸️  PAUSAR";
                btnPausar.BackColor = Color.FromArgb(42, 38, 38);
                btnPausar.FlatAppearance.BorderColor = Color.FromArgb(70, 60, 60);
                btnPausar.ForeColor = Color.White;
            }
            Application.DoEvents();
        }

        public void DisableActions()
        {
            btnPausar.Enabled = false;
            btnCancelar.Enabled = false;
        }

        public void Actualizar(int porcentaje, string texto)
        {
            if (porcentaje >= 0)
            {
                if (porcentaje > 100) porcentaje = 100;
                if (barra.Value != porcentaje) barra.Value = porcentaje;
                lblPorcentaje.Text = porcentaje + "%";

                if (porcentaje >= 100)
                {
                    DisableActions();
                }
            }

            lblEstado.Text = texto;
            Application.DoEvents();
        }
    }
}