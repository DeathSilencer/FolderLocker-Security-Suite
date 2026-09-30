using FolderLocker.UI.Common;
using FolderLocker.UI.Views;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace FolderLocker
{
    public class DarkRedMenuColors : ProfessionalColorTable
    {
        private Color cBack = Color.FromArgb(35, 28, 28);
        private Color cRed = Color.FromArgb(198, 40, 40);
        private Color cText = Color.FromArgb(245, 245, 245);

        public override Color ToolStripDropDownBackground => cBack;
        public override Color ImageMarginGradientBegin => cBack;
        public override Color ImageMarginGradientMiddle => cBack;
        public override Color ImageMarginGradientEnd => cBack;

        public override Color MenuBorder => cRed;
        public override Color MenuItemBorder => cRed;

        public override Color MenuItemSelected => cRed;
        public override Color MenuItemSelectedGradientBegin => cRed;
        public override Color MenuItemSelectedGradientEnd => cRed;

        public override Color MenuItemPressedGradientBegin => cRed;
        public override Color MenuItemPressedGradientEnd => cRed;

        public override Color SeparatorDark => Color.FromArgb(60, 60, 60);
        public override Color SeparatorLight => Color.FromArgb(60, 60, 60);
    }

    public partial class FormCarpetas
    {
        #region 1. CONSTANTES Y ESTILOS (The Theme)

        private readonly Color cBackground = UITheme.cBackground;
        private readonly Color cSurface = UITheme.cSurface;
        private readonly Color cInputBackground = UITheme.cInputBackground;
        private readonly Color cAccentRed = UITheme.cAccentRed;

        private readonly Color cTextPrimary = UITheme.cTextPrimary;
        private readonly Color cTextSecondary = UITheme.cTextSecondary;
        private readonly Color cActiveYellow = Color.FromArgb(230, 210, 30);

        private readonly Font iconFont = new Font("Segoe UI", 9, FontStyle.Regular);
        #endregion

        #region 2. VARIABLES Y VISTAS MODULARES

        // --- VISTAS MODULARES ---
        private LoginView _loginView = null!;
        private RegistroView _registroView = null!;
        private ProtegerView _protegerView = null!;
        private RestaurarView _restaurarView = null!;
        private MontarView _montarView = null!;
        private ConfiguracionView _configView = null!;
        private ManualView _manualView = null!;
        private CreditosView _creditosView = null!;
        private SetupView _setupView = null!;

        public LoginView LoginView => _loginView;
        public RegistroView RegistroView => _registroView;
        public ProtegerView ProtegerView => _protegerView;
        public RestaurarView RestaurarView => _restaurarView;
        public MontarView MontarView => _montarView;
        public ConfiguracionView ConfiguracionView => _configView;
        public ManualView ManualView => _manualView;
        public CreditosView CreditosView => _creditosView;
        public SetupView SetupView => _setupView;

        // --- SISTEMA Y VENTANA ---
        public NotifyIcon trayIcon = null!;
        private ContextMenuStrip trayMenu = null!;
        public ToolStripMenuItem itemAbrir = null!;
        public ToolStripMenuItem itemSalir = null!;
        private bool _yaSeNotificoTray = false;

        // Botones de control de ventana (Globales)
        private Button btnClose = null!;
        private Button btnMax = null!;
        private Button btnMin = null!;

        // --- ELEMENTOS DE UI DINÁMICOS ---
        // Sidebar & Navegación
        private Button btnExplorador = null!;
        private Button btnSetup = null!;
        private Button btnManual = null!;
        private Button btnSalir = null!;
        private Panel pnlIndicador = null!;
        private Label lblSubtitle = null!;

        // Header / Tabs
        private Panel separatorLine = null!;
        private ComboBox cmbGlobalLang = null!;

        #endregion

        #region 3. WIN32 API (Gestión de Ventana, Multimonitor y Arrastre)
        [DllImport("user32.dll", EntryPoint = "ReleaseCapture")]
        private extern static void ReleaseCapture();
        [DllImport("user32.dll", EntryPoint = "SendMessage")]
        private extern static void SendMessage(System.IntPtr hwnd, int wmsg, int wparam, int lparam);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr handle, uint flags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        private const uint MONITOR_DEFAULTTONEAREST = 0x00000002;
        private const int WM_GETMINMAXINFO = 0x0024;
        private const int WM_NCHITTEST = 0x0084;
        private const int HTCLIENT = 1;
        private const int HTLEFT = 10;
        private const int HTRIGHT = 11;
        private const int HTTOP = 12;
        private const int HTTOPLEFT = 13;
        private const int HTTOPRIGHT = 14;
        private const int HTBOTTOM = 15;
        private const int HTBOTTOMLEFT = 16;
        private const int HTBOTTOMRIGHT = 17;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public int dwFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.Style |= 0x00020000;
                cp.Style |= 0x00010000;
                return cp;
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_GETMINMAXINFO)
            {
                WmGetMinMaxInfo(m.HWnd, m.LParam);
                m.Result = IntPtr.Zero;
                return;
            }

            if (m.Msg == WM_NCHITTEST && this.WindowState == FormWindowState.Normal)
            {
                base.WndProc(ref m);
                if (m.Result == (IntPtr)HTCLIENT)
                {
                    Point screenPt = new Point(m.LParam.ToInt32());
                    Point clientPt = this.PointToClient(screenPt);
                    int borderSize = 6;

                    bool isLeft = clientPt.X <= borderSize;
                    bool isRight = clientPt.X >= this.ClientSize.Width - borderSize;
                    bool isTop = clientPt.Y <= borderSize;
                    bool isBottom = clientPt.Y >= this.ClientSize.Height - borderSize;

                    if (isTop && isLeft) { m.Result = (IntPtr)HTTOPLEFT; return; }
                    if (isTop && isRight) { m.Result = (IntPtr)HTTOPRIGHT; return; }
                    if (isBottom && isLeft) { m.Result = (IntPtr)HTBOTTOMLEFT; return; }
                    if (isBottom && isRight) { m.Result = (IntPtr)HTBOTTOMRIGHT; return; }
                    if (isLeft) { m.Result = (IntPtr)HTLEFT; return; }
                    if (isRight) { m.Result = (IntPtr)HTRIGHT; return; }
                    if (isTop) { m.Result = (IntPtr)HTTOP; return; }
                    if (isBottom) { m.Result = (IntPtr)HTBOTTOM; return; }
                }
                return;
            }

            base.WndProc(ref m);
        }

        private void WmGetMinMaxInfo(IntPtr hwnd, IntPtr lParam)
        {
            IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            if (monitor != IntPtr.Zero)
            {
                MONITORINFO monitorInfo = new MONITORINFO();
                monitorInfo.cbSize = Marshal.SizeOf(typeof(MONITORINFO));
                if (GetMonitorInfo(monitor, ref monitorInfo))
                {
                    MINMAXINFO mmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO));

                    mmi.ptMaxPosition.x = Math.Abs(monitorInfo.rcWork.Left - monitorInfo.rcMonitor.Left);
                    mmi.ptMaxPosition.y = Math.Abs(monitorInfo.rcWork.Top - monitorInfo.rcMonitor.Top);
                    mmi.ptMaxSize.x = Math.Abs(monitorInfo.rcWork.Right - monitorInfo.rcWork.Left);
                    mmi.ptMaxSize.y = Math.Abs(monitorInfo.rcWork.Bottom - monitorInfo.rcWork.Top);

                    mmi.ptMinTrackSize.x = Math.Max(this.MinimumSize.Width, 1000);
                    mmi.ptMinTrackSize.y = Math.Max(this.MinimumSize.Height, 680);

                    Marshal.StructureToPtr(mmi, lParam, true);
                }
            }
        }
        #endregion

        #region 4. INICIALIZACIÓN (Entry Point)

        public void InicializarEstiloProfesional()
        {
            // A. Configuración Base de la Ventana
            ConfigurarVentanaSinBordes();

            // B. Configurar Sidebar (PanelOpciones)
            PanelOpciones.Visible = true;
            PanelOpciones.Dock = DockStyle.Left;
            PanelOpciones.Width = 260;
            PanelOpciones.BackColor = cSurface;
            PanelOpciones.BringToFront();

            pnlIndicador = new Panel { Width = 5, Height = 45, BackColor = cAccentRed, Visible = false };
            PanelOpciones.Controls.Add(pnlIndicador);
            pnlIndicador.BringToFront();

            // C. Configurar Panel Principal (Contenido)
            panel1.Parent = this;
            panel1.Location = new Point(260, 0);
            panel1.Size = new Size(this.ClientSize.Width - 260, this.ClientSize.Height);
            panel1.BackColor = cBackground;
            panel1.Dock = DockStyle.None;
            panel1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            panel1.AutoScroll = true;

            // D. Construcción de UI (Sidebar y Header)
            EstilarLogo();
            ConstruirMenuLateral();
            ConstruirHeaderSinPanel();

            // E. Inicialización de Vistas Modulares del Dashboard
            _protegerView = new ProtegerView { Dock = DockStyle.Fill, Visible = false };
            _restaurarView = new RestaurarView { Dock = DockStyle.Fill, Visible = false };
            _montarView = new MontarView { Dock = DockStyle.Fill, Visible = false };
            _configView = new ConfiguracionView { Dock = DockStyle.Fill, Visible = false };
            _manualView = new ManualView { Dock = DockStyle.Fill, Visible = false };
            _creditosView = new CreditosView { Dock = DockStyle.Fill, Visible = false };
            _setupView = new SetupView { Dock = DockStyle.Fill, Visible = false };

            panel1.Controls.Add(_protegerView);
            panel1.Controls.Add(_restaurarView);
            panel1.Controls.Add(_montarView);
            panel1.Controls.Add(_configView);
            panel1.Controls.Add(_manualView);
            panel1.Controls.Add(_creditosView);
            panel1.Controls.Add(_setupView);

            // F. Vistas de Autenticación de Pantalla Completa
            _loginView = new LoginView { Dock = DockStyle.Fill, Visible = false };
            _registroView = new RegistroView { Dock = DockStyle.Fill, Visible = false };

            this.Controls.Add(_loginView);
            this.Controls.Add(_registroView);
            InicializarSelectorIdiomaGlobal();

            // G. Lógica de Interacción (Drag & Drop, Tray)
            HabilitarArrastreGlobal();
            ConfigurarSystemTray();

            // H. Finalización
            TraerControlesVentanaAlFrente();
            RecentrarPaneles();
        }

        private void ConstruirMenuLateral()
        {
            int btnWidth = 260;

            btnCarpetas.Text = Localization.Get("menu_protect");
            btnCarpetas.Size = new Size(btnWidth, 45);
            btnCarpetas.Location = new Point(0, 155);
            EstilarBotonSidebar(btnCarpetas);

            btnMenuAbrir.Text = Localization.Get("menu_unlock");
            btnMenuAbrir.Size = new Size(btnWidth, 45);
            btnMenuAbrir.Location = new Point(0, 205);
            EstilarBotonSidebar(btnMenuAbrir);

            if (btnExplorador == null) { btnExplorador = new Button(); PanelOpciones.Controls.Add(btnExplorador); }
            btnExplorador.Text = Localization.Get("menu_files");
            btnExplorador.Size = new Size(btnWidth, 45);
            btnExplorador.Location = new Point(0, 255);
            EstilarBotonSidebar(btnExplorador);

            if (btnManual == null) { btnManual = new Button(); PanelOpciones.Controls.Add(btnManual); }
            btnManual.Text = Localization.Get("menu_manual");
            btnManual.Size = new Size(btnWidth, 45);
            btnManual.Location = new Point(0, 305);
            EstilarBotonSidebar(btnManual);

            int yFondo = PanelOpciones.Height;

            if (btnSalir == null) { btnSalir = new Button(); PanelOpciones.Controls.Add(btnSalir); }
            btnSalir.Text = Localization.Get("menu_exit");
            btnSalir.Size = new Size(260, 45);
            EstilarBotonSidebar(btnSalir);
            btnSalir.Location = new Point(0, yFondo - 60);
            btnSalir.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

            if (btnSetup == null) { btnSetup = new Button(); PanelOpciones.Controls.Add(btnSetup); }
            btnSetup.Text = Localization.Get("menu_config");
            btnSetup.Size = new Size(260, 45);
            EstilarBotonSidebar(btnSetup);
            btnSetup.Location = new Point(0, yFondo - 110);
            btnSetup.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        }

        #endregion

        #region 5. NAVEGACIÓN Y GESTIÓN DE VISTAS

        private void OcultarTodosPaneles()
        {
            if (_configView != null) _configView.Visible = false;
            if (_montarView != null) _montarView.Visible = false;
            if (_protegerView != null) _protegerView.Visible = false;
            if (_restaurarView != null) _restaurarView.Visible = false;
            if (_setupView != null) _setupView.Visible = false;
            if (_manualView != null) _manualView.Visible = false;
            if (_creditosView != null) _creditosView.Visible = false;

            if (lblBienvenido != null) lblBienvenido.Visible = false;
            if (btnProteger != null) btnProteger.Visible = false;
            if (btnDejarDeProteger != null) btnDejarDeProteger.Visible = false;
            if (separatorLine != null) separatorLine.Visible = false;

            if (_loginView != null) _loginView.Visible = false;
            if (_registroView != null) _registroView.Visible = false;

            if (cmbGlobalLang != null) cmbGlobalLang.Visible = false;
        }

        public void MostrarPanelProteger()
        {
            OcultarTodosPaneles();
            if (_protegerView != null)
            {
                _protegerView.Visible = true;
                _protegerView.BringToFront();
            }

            if (lblBienvenido != null) { lblBienvenido.Visible = true; lblBienvenido.BringToFront(); }
            if (btnProteger != null) { btnProteger.Visible = true; btnProteger.BringToFront(); }
            if (btnDejarDeProteger != null) { btnDejarDeProteger.Visible = true; btnDejarDeProteger.BringToFront(); }
            if (separatorLine != null) { separatorLine.Visible = true; separatorLine.BringToFront(); }

            ResaltarBotonActivo(btnCarpetas);
            ActualizarEstiloTabs(true);
            RecentrarPaneles();
        }

        public void MostrarPanelRestaurar(string ruta = "")
        {
            OcultarTodosPaneles();
            if (_restaurarView != null)
            {
                _restaurarView.Visible = true;
                _restaurarView.BringToFront();
            }

            if (lblBienvenido != null) { lblBienvenido.Visible = true; lblBienvenido.BringToFront(); }
            if (btnProteger != null) { btnProteger.Visible = true; btnProteger.BringToFront(); }
            if (btnDejarDeProteger != null) { btnDejarDeProteger.Visible = true; btnDejarDeProteger.BringToFront(); }
            if (separatorLine != null) { separatorLine.Visible = true; separatorLine.BringToFront(); }

            ResaltarBotonActivo(btnCarpetas);
            ActualizarEstiloTabs(false);
            RecentrarPaneles();
        }

        public void MostrarPanelMontar(string ruta = "")
        {
            OcultarTodosPaneles();
            if (_montarView != null)
            {
                _montarView.Visible = true;
                _montarView.BringToFront();
            }
            ResaltarBotonActivo(btnMenuAbrir);
            RecentrarPaneles();
        }

        public void MostrarPanelConfiguracion()
        {
            this.SuspendLayout();
            OcultarTodosPaneles();

            if (_configView != null)
            {
                _configView.Visible = true;
                _configView.BringToFront();
                RecentrarPaneles();
            }

            ResaltarBotonActivo(btnSetup);
            this.ResumeLayout(true);
        }

        public void MostrarPanelCreditos()
        {
            OcultarTodosPaneles();

            if (PanelOpciones != null) PanelOpciones.Visible = false;

            if (_creditosView != null)
            {
                _creditosView.Visible = true;
                _creditosView.BringToFront();
            }

            DesactivarResaltadoSidebar();
            RecentrarPaneles();
        }

        public void MostrarPanelManual()
        {
            OcultarTodosPaneles();
            if (_manualView != null)
            {
                _manualView.Visible = true;
                _manualView.BringToFront();
            }
            ResaltarBotonActivo(btnManual);
            RecentrarPaneles();
        }

        public void MostrarPanelSetup()
        {
            OcultarTodosPaneles();
            if (_setupView != null)
            {
                _setupView.Visible = true;
                _setupView.BringToFront();
            }
            RecentrarPaneles();
        }

        public void MostrarPanelLogin()
        {
            if (PanelOpciones != null) PanelOpciones.Visible = false;
            if (panel1 != null) panel1.Visible = false;

            OcultarTodosPaneles();
            ActualizarTextosIdioma();

            if (_loginView != null)
            {
                _loginView.Visible = true;
                _loginView.BringToFront();
            }

            if (cmbGlobalLang != null)
            {
                cmbGlobalLang.Visible = true;
                cmbGlobalLang.BringToFront();
            }

            TraerControlesVentanaAlFrente();
            RecentrarPaneles();
        }

        public void MostrarPanelRegistro()
        {
            if (PanelOpciones != null) PanelOpciones.Visible = false;
            if (panel1 != null) panel1.Visible = false;

            OcultarTodosPaneles();
            ActualizarTextosIdioma();

            if (_registroView != null)
            {
                _registroView.Visible = true;
                _registroView.BringToFront();
            }

            if (cmbGlobalLang != null)
            {
                cmbGlobalLang.Visible = true;
                cmbGlobalLang.BringToFront();
            }

            TraerControlesVentanaAlFrente();
            RecentrarPaneles();
        }

        // --- LÓGICA DE CENTRADO RESPONSIVA ---
        public void RecentrarPaneles()
        {
            if (this.WindowState == FormWindowState.Minimized) return;

            this.SuspendLayout();

            if (_loginView != null && _loginView.Visible)
            {
                _loginView.Size = this.ClientSize;
                this.ResumeLayout();
                return;
            }

            if (_registroView != null && _registroView.Visible)
            {
                _registroView.Size = this.ClientSize;
                this.ResumeLayout();
                return;
            }

            if (panel1 != null)
            {
                int sidebarW = (PanelOpciones != null && PanelOpciones.Visible) ? PanelOpciones.Width : 0;
                panel1.Location = new Point(sidebarW, 0);
                panel1.Width = Math.Max(100, this.ClientSize.Width - sidebarW);
                panel1.Height = this.ClientSize.Height;

                int pW = panel1.Width;
                int pH = panel1.Height;

                if ((_protegerView != null && _protegerView.Visible) || (_restaurarView != null && _restaurarView.Visible))
                {
                    CentrarPanelConTabs(pW, pH);
                }
            }

            this.ResumeLayout(true);
        }

        private void CentrarPanelConTabs(int pW, int pH)
        {
            int gapHeader = 105;
            Panel? activeCard = null;
            if (_protegerView != null && _protegerView.Visible) activeCard = _protegerView.CardPanel;
            else if (_restaurarView != null && _restaurarView.Visible) activeCard = _restaurarView.CardPanel;

            if (activeCard != null)
            {
                int totalH = gapHeader + activeCard.Height;
                int startY = Math.Max(25, (pH - totalH) / 2);
                int xCard = Math.Max(20, (pW - activeCard.Width) / 2);

                activeCard.Location = new Point(xCard, startY + gapHeader);

                if (lblBienvenido != null) lblBienvenido.Location = new Point(xCard, startY);
                if (btnProteger != null) btnProteger.Location = new Point(xCard, startY + 48);
                if (btnProteger != null && btnDejarDeProteger != null) btnDejarDeProteger.Location = new Point(xCard + btnProteger.Width + 10, startY + 48);

                if (separatorLine != null && btnProteger != null && btnDejarDeProteger != null)
                {
                    int xSep = (_protegerView != null && _protegerView.Visible) ? btnProteger.Location.X : btnDejarDeProteger.Location.X;
                    separatorLine.Location = new Point(xSep, startY + 48 + btnProteger.Height);
                    separatorLine.Width = (_protegerView != null && _protegerView.Visible) ? btnProteger.Width : btnDejarDeProteger.Width;
                }
            }
        }

        #endregion

        #region 6. LÓGICA DE IDIOMAS

        public void ActualizarTextosIdioma()
        {
            // Sidebar
            if (btnCarpetas != null) btnCarpetas.Text = Localization.Get("menu_protect");
            if (btnMenuAbrir != null) btnMenuAbrir.Text = Localization.Get("menu_unlock");
            if (btnDejarDeProteger != null) btnDejarDeProteger.Text = Localization.Get("tab_restore");
            if (btnExplorador != null) btnExplorador.Text = Localization.Get("menu_files");
            if (btnSetup != null) btnSetup.Text = Localization.Get("menu_config");
            if (btnManual != null) btnManual.Text = Localization.Get("menu_manual");

            if (btnSalir != null)
            {
                string baseText = Localization.Get("menu_exit");
                if (FolderLocker.UserManager.CurrentUser != null)
                    btnSalir.Text = $"{baseText} ({FolderLocker.UserManager.CurrentUser.Username})";
                else
                    btnSalir.Text = baseText;
            }

            // Header Tabs
            if (lblBienvenido != null) lblBienvenido.Text = Localization.Get("title_main");
            if (btnProteger != null) btnProteger.Text = Localization.Get("menu_protect");

            // Vistas modulares
            _loginView?.ActualizarIdioma();
            _registroView?.ActualizarIdioma();
            _protegerView?.ActualizarIdioma();
            _restaurarView?.ActualizarIdioma();
            _montarView?.ActualizarIdioma();
            _configView?.ActualizarIdioma();
            _manualView?.ActualizarIdioma();
            _creditosView?.ActualizarIdioma();
            _setupView?.ActualizarIdioma();

            // System Tray
            if (itemAbrir != null) itemAbrir.Text = Localization.Get("tray_menu_open");
            if (itemSalir != null) itemSalir.Text = Localization.Get("tray_menu_exit");

            // Selector global
            SincronizarSeleccionIdioma(cmbGlobalLang);
        }

        private void SincronizarSeleccionIdioma(ComboBox cmb)
        {
            if (cmb == null) return;
            switch (Localization.CurrentLang)
            {
                case "EN": cmb.SelectedIndex = 1; break;
                case "PT": cmb.SelectedIndex = 2; break;
                case "RU": cmb.SelectedIndex = 3; break;
                case "CN": cmb.SelectedIndex = 4; break;
                default: cmb.SelectedIndex = 0; break;
            }
        }

        #endregion

        #region 7. CONSTRUCCIÓN DE UI (Sidebar y Header)

        private void EstilarLogo()
        {
            const int LogoSize = 82;
            const int LogoPaddingX = 15;
            const int TextSpacingX = 5;
            const int TextStartX = LogoPaddingX + LogoSize + TextSpacingX;
            const int LogoCenterY = 66;
            const int TextCenterOffset = 30;

            try
            {
                var logoBox = new PictureBox
                {
                    Image = Properties.Resources.LogoPanel,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Size = new Size(LogoSize, LogoSize),
                    Location = new Point(LogoPaddingX, 25),
                    BackColor = Color.Transparent,
                };
                PanelOpciones.Controls.Add(logoBox);
                logoBox.BringToFront();
                logoBox.MouseDown += MoverVentana;

                lblLogo.Text = "FOLDER\nLOCKER";
                lblLogo.ForeColor = cAccentRed;
                lblLogo.Font = new Font("Segoe UI Black", 16, FontStyle.Bold);
                lblLogo.Location = new Point(TextStartX, LogoCenterY - TextCenterOffset);
                lblLogo.AutoSize = true;
            }
            catch
            {
                lblLogo.Text = "🛡️ FOLDER\nLOCKER";
            }

            if (lblSubtitle == null) { lblSubtitle = new Label(); PanelOpciones.Controls.Add(lblSubtitle); }
            lblSubtitle.Text = Localization.Get("app_subtitle");
            lblSubtitle.ForeColor = cTextSecondary;
            lblSubtitle.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            lblSubtitle.Location = new Point(LogoPaddingX + 5, 110);
            lblSubtitle.AutoSize = true;

            var logoLine = new Panel { Height = 2, Width = 230, BackColor = cAccentRed, Location = new Point(LogoPaddingX, 130) };
            PanelOpciones.Controls.Add(logoLine);
        }

        private void ConstruirHeaderSinPanel()
        {
            lblBienvenido.Parent = panel1;
            lblBienvenido.ForeColor = cTextPrimary;
            lblBienvenido.Font = new Font("Segoe UI Semibold", 20, FontStyle.Bold);
            lblBienvenido.Text = Localization.Get("title_main");
            lblBienvenido.AutoSize = true;
            lblBienvenido.BringToFront();

            btnProteger.Parent = panel1;
            btnProteger.Size = new Size(160, 42);
            EstilarBotonTab(btnProteger, true);
            btnProteger.BringToFront();

            btnDejarDeProteger.Parent = panel1;
            btnDejarDeProteger.Size = new Size(190, 42);
            EstilarBotonTab(btnDejarDeProteger, false);
            btnDejarDeProteger.BringToFront();

            separatorLine = new Panel { Height = 3, BackColor = cAccentRed, Width = 160 };
            panel1.Controls.Add(separatorLine);
            separatorLine.BringToFront();
        }

        #endregion

        #region 8. SISTEMA Y VENTANA (Window Controls & Tray)

        private Point _dragStartPoint;
        private bool _isMouseDown = false;

        private void ConfigurarVentanaSinBordes()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            int btnW = 46, btnH = 32;

            btnClose = new Button { Text = "✕", Size = new Size(btnW, btnH), Anchor = AnchorStyles.Top | AnchorStyles.Right, FlatStyle = FlatStyle.Flat, BackColor = cBackground, ForeColor = Color.White, Font = iconFont };
            btnClose.Location = new Point(this.ClientSize.Width - btnW, 0);
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(232, 17, 35);
            btnClose.Click += (s, e) =>
            {
                this.WindowState = FormWindowState.Minimized;
                this.Hide();

                if (!_yaSeNotificoTray)
                {
                    trayIcon.ShowBalloonTip(
                        2000,
                        Localization.Get("tray_min_title") ?? "FolderLocker",
                        Localization.Get("tray_min_click") ?? "La aplicación sigue ejecutándose en segundo plano.",
                        ToolTipIcon.None
                    );
                    _yaSeNotificoTray = true;
                }
            };
            this.Controls.Add(btnClose);

            btnMax = new Button { Text = "🗖", Size = new Size(btnW, btnH), Anchor = AnchorStyles.Top | AnchorStyles.Right, FlatStyle = FlatStyle.Flat, BackColor = cBackground, ForeColor = Color.White, Font = new Font("Segoe UI Symbol", 11) };
            btnMax.Location = new Point(this.ClientSize.Width - (btnW * 2), 0);
            btnMax.FlatAppearance.BorderSize = 0;
            btnMax.Click += (s, e) => EjecutarMaximizar();
            this.Controls.Add(btnMax);

            btnMin = new Button { Text = "—", Size = new Size(btnW, btnH), Anchor = AnchorStyles.Top | AnchorStyles.Right, FlatStyle = FlatStyle.Flat, BackColor = cBackground, ForeColor = Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            btnMin.Location = new Point(this.ClientSize.Width - (btnW * 3), 0);
            btnMin.FlatAppearance.BorderSize = 0;
            btnMin.Click += (s, e) =>
            {
                this.WindowState = FormWindowState.Minimized;
                trayIcon.ShowBalloonTip(2000, Localization.Get("tray_min_title"), Localization.Get("tray_min_click"), ToolTipIcon.None);
            };
            this.Controls.Add(btnMin);
        }

        private void EjecutarMaximizar()
        {
            try
            {
                if (this.WindowState == FormWindowState.Normal)
                {
                    this.WindowState = FormWindowState.Maximized;
                    if (btnMax != null) btnMax.Text = "🗗";
                }
                else
                {
                    this.WindowState = FormWindowState.Normal;
                    if (btnMax != null) btnMax.Text = "🗖";
                }

                RecentrarPaneles();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al maximizar: " + ex.Message);
            }
        }

        private void HabilitarArrastreGlobal()
        {
            SuscribirEventos(this);
            SuscribirEventos(PanelOpciones);
            SuscribirEventos(panel1);
            SuscribirEventos(lblBienvenido);

            if (panel1 != null)
            {
                foreach (Control hijo in panel1.Controls)
                {
                    if (hijo is Panel || hijo is Label) SuscribirEventos(hijo);
                }
            }
        }

        private void SuscribirEventos(Control c)
        {
            if (c == null) return;
            c.MouseDown -= OnSmartMouseDown;
            c.MouseMove -= OnSmartMouseMove;
            c.MouseUp -= OnSmartMouseUp;
            c.MouseDoubleClick -= OnSmartDoubleClick;

            c.MouseDown += OnSmartMouseDown;
            c.MouseMove += OnSmartMouseMove;
            c.MouseUp += OnSmartMouseUp;
            c.MouseDoubleClick += OnSmartDoubleClick;
        }

        private void IniciarArrastreVentana(MouseEventArgs? e = null)
        {
            if (this.WindowState == FormWindowState.Maximized)
            {
                Point cursorPos = Cursor.Position;
                Screen screen = Screen.FromPoint(cursorPos);

                double ratioX = (double)(cursorPos.X - screen.WorkingArea.Left) / Math.Max(1, screen.WorkingArea.Width);
                ratioX = Math.Clamp(ratioX, 0.05, 0.95);

                this.WindowState = FormWindowState.Normal;
                if (btnMax != null) btnMax.Text = "🗖";

                int clickY = (e != null) ? Math.Min(e.Y, 25) : 15;
                int newX = cursorPos.X - (int)(this.Width * ratioX);
                int newY = cursorPos.Y - clickY;

                newX = Math.Max(screen.WorkingArea.Left, Math.Min(newX, screen.WorkingArea.Right - this.Width));
                newY = Math.Max(screen.WorkingArea.Top, newY);

                this.Location = new Point(newX, newY);
            }

            ReleaseCapture();
            SendMessage(this.Handle, 0xA1, 0x2, 0);
        }

        public void MoverVentana(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            if (e.Clicks == 2)
            {
                EjecutarMaximizar();
                return;
            }

            Point ptOnForm = this.PointToClient(Cursor.Position);
            if (ptOnForm.Y <= 50)
            {
                IniciarArrastreVentana(e);
            }
        }

        private void OnSmartMouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            Point ptOnForm = this.PointToClient(Cursor.Position);
            if (ptOnForm.Y <= 50)
            {
                _isMouseDown = true;
                _dragStartPoint = e.Location;
            }
        }

        private void OnSmartMouseMove(object? sender, MouseEventArgs e)
        {
            if (_isMouseDown)
            {
                int deltaX = Math.Abs(e.X - _dragStartPoint.X);
                int deltaY = Math.Abs(e.Y - _dragStartPoint.Y);

                if (deltaX > 5 || deltaY > 5)
                {
                    _isMouseDown = false;
                    IniciarArrastreVentana(e);
                }
            }
        }

        private void OnSmartMouseUp(object? sender, MouseEventArgs e)
        {
            _isMouseDown = false;
        }

        private void OnSmartDoubleClick(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            Point ptOnForm = this.PointToClient(Cursor.Position);
            if (ptOnForm.Y <= 45)
            {
                EjecutarMaximizar();
            }
        }

        private void ConfigurarSystemTray()
        {
            trayMenu = new ContextMenuStrip();
            trayMenu.Renderer = new ToolStripProfessionalRenderer(new DarkRedMenuColors());
            trayMenu.BackColor = Color.FromArgb(35, 28, 28);
            trayMenu.ForeColor = Color.FromArgb(245, 245, 245);
            trayMenu.ShowImageMargin = false;

            itemAbrir = new ToolStripMenuItem(Localization.Get("tray_menu_open"));
            itemSalir = new ToolStripMenuItem(Localization.Get("tray_menu_exit"));
            trayMenu.Items.AddRange(new ToolStripItem[] { itemAbrir, new ToolStripSeparator(), itemSalir });

            if (trayIcon == null)
            {
                trayIcon = new NotifyIcon
                {
                    Text = "FolderLocker",
                    Icon = this.Icon,
                    Visible = true
                };
            }
            trayIcon.ContextMenuStrip = trayMenu;

            trayIcon.MouseDoubleClick -= TrayIcon_MouseDoubleClick;
            trayIcon.MouseDoubleClick += TrayIcon_MouseDoubleClick;
            trayIcon.BalloonTipClicked -= TrayIcon_BalloonTipClicked;
            trayIcon.BalloonTipClicked += TrayIcon_BalloonTipClicked;

            itemAbrir.Click += (s, e) => RestaurarVentana();
        }

        private void TrayIcon_MouseDoubleClick(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) RestaurarVentana();
        }

        private void TrayIcon_BalloonTipClicked(object? sender, EventArgs e)
        {
            RestaurarVentana();
        }

        private void RestaurarVentana()
        {
            if (!this.Visible) this.Show();
            if (this.WindowState == FormWindowState.Minimized) this.WindowState = FormWindowState.Normal;

            this.BringToFront();
            this.Activate();

            if (progresoActivo != null && !progresoActivo.IsDisposed)
            {
                progresoActivo.Show();
                progresoActivo.BringToFront();
            }

            this.Refresh();
        }

        private void ForzarPrimerPlano()
        {
            if (!this.Visible) this.Show();
            if (this.WindowState == FormWindowState.Minimized) this.WindowState = FormWindowState.Normal;

            this.TopMost = true;
            this.TopMost = false;

            this.Activate();
            this.BringToFront();

            this.Refresh();
            Application.DoEvents();
        }

        private void TraerControlesVentanaAlFrente()
        {
            if (btnClose != null) btnClose.BringToFront();
            if (btnMax != null) btnMax.BringToFront();
            if (btnMin != null) btnMin.BringToFront();
            if (cmbGlobalLang != null && cmbGlobalLang.Visible) cmbGlobalLang.BringToFront();
        }

        #endregion

        #region 9. UI FACTORIES & HELPERS (Estilos Reutilizables)

        private void DesactivarResaltadoSidebar()
        {
            foreach (Control c in PanelOpciones.Controls)
            {
                if (c is Button btn)
                {
                    btn.BackColor = cBackground;
                    btn.ForeColor = cTextPrimary;
                }
            }
        }

        private void ResaltarBotonActivo(Button btnActivo)
        {
            Button[] menuButtons = { btnSetup, btnCarpetas, btnMenuAbrir, btnExplorador, btnManual };
            foreach (var btn in menuButtons)
            {
                if (btn == null) continue;
                if (btn == btnActivo)
                {
                    btn.BackColor = cInputBackground;
                    btn.ForeColor = cAccentRed;
                    btn.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                    if (pnlIndicador != null)
                    {
                        pnlIndicador.Visible = true;
                        pnlIndicador.Height = btn.Height;
                        pnlIndicador.Top = btn.Top;
                        pnlIndicador.Left = PanelOpciones.Width - pnlIndicador.Width;
                        pnlIndicador.BringToFront();
                    }
                }
                else
                {
                    btn.BackColor = Color.Transparent;
                    btn.ForeColor = cTextPrimary;
                    btn.Font = new Font("Segoe UI Semibold", 10, FontStyle.Bold);
                }
            }
        }

        private void ActualizarEstiloTabs(bool protegerActivo)
        {
            if (btnProteger != null)
            {
                btnProteger.BackColor = protegerActivo ? Color.FromArgb(52, 24, 24) : Color.FromArgb(28, 26, 26);
                btnProteger.ForeColor = protegerActivo ? Color.White : cTextSecondary;
            }
            if (btnDejarDeProteger != null)
            {
                btnDejarDeProteger.BackColor = !protegerActivo ? Color.FromArgb(52, 24, 24) : Color.FromArgb(28, 26, 26);
                btnDejarDeProteger.ForeColor = !protegerActivo ? Color.White : cTextSecondary;
            }
            if (separatorLine != null && btnProteger != null && btnDejarDeProteger != null)
            {
                separatorLine.Location = new Point(protegerActivo ? btnProteger.Location.X : btnDejarDeProteger.Location.X, btnProteger.Location.Y + btnProteger.Height);
                separatorLine.Width = protegerActivo ? btnProteger.Width : btnDejarDeProteger.Width;
            }
        }

        private void EstilarBotonSidebar(Button b)
        {
            if (b == null) return;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = cInputBackground;
            b.FlatAppearance.MouseDownBackColor = cBackground;
            b.BackColor = Color.Transparent;
            b.ForeColor = cTextPrimary;
            b.Font = new Font("Segoe UI Semibold", 10, FontStyle.Bold);
            b.TextAlign = ContentAlignment.MiddleLeft;
            b.Padding = new Padding(20, 0, 0, 0);
            b.Cursor = Cursors.Hand;
        }

        private void EstilarBotonTab(Button b, bool esPrincipal)
        {
            if (b == null) return;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.BackColor = esPrincipal ? Color.FromArgb(52, 24, 24) : Color.FromArgb(28, 26, 26);
            b.ForeColor = esPrincipal ? Color.White : cTextSecondary;
            b.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            b.Cursor = Cursors.Hand;
        }

        private void InicializarSelectorIdiomaGlobal()
        {
            if (cmbGlobalLang != null) return;

            cmbGlobalLang = new ComboBox
            {
                Size = new Size(160, 35),
                Location = new Point(this.ClientSize.Width - 180, 55),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = cInputBackground,
                ForeColor = cTextPrimary,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                DropDownStyle = ComboBoxStyle.DropDownList,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 28,
                Visible = false
            };

            cmbGlobalLang.Items.AddRange(new object[] { "Español", "English", "Português", "Русский", "中文" });
            cmbGlobalLang.DrawItem += UITheme.DibujarComboConBanderas;

            cmbGlobalLang.SelectedIndexChanged += (s, e) =>
            {
                string lang = "ES";
                if (cmbGlobalLang.SelectedIndex == 1) lang = "EN";
                else if (cmbGlobalLang.SelectedIndex == 2) lang = "PT";
                else if (cmbGlobalLang.SelectedIndex == 3) lang = "RU";
                else if (cmbGlobalLang.SelectedIndex == 4) lang = "CN";

                if (Localization.CurrentLang != lang)
                {
                    Localization.CurrentLang = lang;
                    Properties.Settings.Default.Idioma = lang;
                    Properties.Settings.Default.Save();
                    ActualizarTextosIdioma();
                }
            };

            switch (Localization.CurrentLang)
            {
                case "EN": cmbGlobalLang.SelectedIndex = 1; break;
                case "PT": cmbGlobalLang.SelectedIndex = 2; break;
                case "RU": cmbGlobalLang.SelectedIndex = 3; break;
                case "CN": cmbGlobalLang.SelectedIndex = 4; break;
                default: cmbGlobalLang.SelectedIndex = 0; break;
            }

            this.Controls.Add(cmbGlobalLang);
            cmbGlobalLang.BringToFront();
        }

        #endregion
    }
}