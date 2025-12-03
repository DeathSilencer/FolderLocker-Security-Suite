using Microsoft.Win32;
using System.Diagnostics;
using System.Drawing.Drawing2D; // Necesario para bordes avanzados y banderas circulares
using System.Runtime.InteropServices;

namespace FolderLocker
{
    public class DarkRedMenuColors : ProfessionalColorTable
    {
        // Colores de la paleta de la bandeja de entrada
        private Color cBack = Color.FromArgb(35, 28, 28);      // Fondo Oscuro
        private Color cRed = Color.FromArgb(198, 40, 40);      // Rojo Accent
        private Color cText = Color.FromArgb(245, 245, 245);   // Texto Claro

        // Fondos
        public override Color ToolStripDropDownBackground => cBack;
        public override Color ImageMarginGradientBegin => cBack;
        public override Color ImageMarginGradientMiddle => cBack;
        public override Color ImageMarginGradientEnd => cBack;

        // Bordes y Selección
        public override Color MenuBorder => cRed;
        public override Color MenuItemBorder => cRed;

        // Hover (Cuando pasas el mouse)
        public override Color MenuItemSelected => cRed;
        public override Color MenuItemSelectedGradientBegin => cRed;
        public override Color MenuItemSelectedGradientEnd => cRed;

        // Clic
        public override Color MenuItemPressedGradientBegin => cRed;
        public override Color MenuItemPressedGradientEnd => cRed;

        // Separadores
        public override Color SeparatorDark => Color.FromArgb(60, 60, 60);
        public override Color SeparatorLight => Color.FromArgb(60, 60, 60);
    }

    public partial class FormCarpetas
    {
        #region 1. CONSTANTES Y ESTILOS (The Theme)

        // --- PALETA DE COLORES "RED SECURITY" ---
        private readonly Color cBackground = Color.FromArgb(20, 18, 18);
        private readonly Color cSurface = Color.FromArgb(35, 28, 28);
        private readonly Color cInputBackground = Color.FromArgb(50, 40, 40);
        private readonly Color cAccentRed = Color.FromArgb(198, 40, 40);

        private readonly Color cTextPrimary = Color.FromArgb(245, 245, 245);
        private readonly Color cTextSecondary = Color.FromArgb(180, 160, 160);
        private readonly Color cActiveYellow = Color.FromArgb(230, 210, 30);

        // Fuente estándar para iconos de ventana
        private readonly Font iconFont = new Font("Segoe UI", 9, FontStyle.Regular);
        #endregion

        #region 2. VARIABLES Y CONTROLES (State Management)

        // --- SISTEMA Y VENTANA ---
        public NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        public ToolStripMenuItem itemAbrir;
        public ToolStripMenuItem itemSalir;
        // --- NUEVO: Variable para controlar la notificación única ---
        private bool _yaSeNotificoTray = false;

        // Botones de control de ventana (Globales)
        private Button btnClose;
        private Button btnMax;
        private Button btnMin;

        // --- ELEMENTOS LOGIN ---
        private Label lblLoginTitle;
        private Label lblLoginSub;
        private Label lblLoginUser;
        private Label lblLoginPass;
        private CheckBox chkLoginRemember;
        private Button btnLoginForgot;
        private Button btnLoginEnter;
        private Button btnLoginReg;

        // --- ELEMENTOS REGISTRO ---
        private Label lblRegTitle;
        private Label lblRegSub;
        private Label lblRegUser;
        private Label lblRegPass;
        private Button btnRegCreate;
        private Button btnRegBack;

        // --- PANELES PRINCIPALES (Contenedores) ---
        private Panel panelFormulario;      // Proteger
        private Panel panelRestaurar;       // Desproteger
        private Panel panelMontar;          // Unidades Virtuales
        private Panel panelSetup;           // Bienvenida
        private Panel panelConfiguracion;   // Configuración
        private Panel panelManual;          // Manual de usuario
        private Panel panelLogin;           // Login
        private Panel panelRegistro;        // Registro 
        private Panel panelCreditos;        // Panel de Créditos

        // --- ELEMENTOS DE UI DINÁMICOS ---
        // Sidebar & Navegación
        private Button btnExplorador;
        private Button btnSetup;
        private Button btnManual;
        private Button btnSalir;
        private Panel pnlIndicador;         // Línea roja activa en sidebar
        private Label lblSubtitle;          // Subtítulo bajo el logo

        // Elementos Compartidos / Globales
        private Panel separatorLine;        // Línea roja en tabs superiores
        private ComboBox cmbGlobalLang;     // Selector de idioma flotante
        private Button btnReset;            // Botón reset factory

        // Elementos Internos: Proteger
        private Label lblTituloProteger, lblPassProteger;
        private TextBox txtRuta, txtContrasena;
        private Button btnBuscarRuta, btnOlvide, btnAccionGuardar;

        // Elementos Internos: Restaurar
        private Label lblTituloRestaurar;
        private ListBox lstCarpetasRestaurar;
        private Button btnAccionRestaurar;

        // Elementos Internos: Montar
        private Label lblTituloMontar, lblListaMontar, lblLetraMontar, lblPassMontar;
        private ListBox lstCarpetasParaMontar;
        private ComboBox cmbLetraMontar;
        private TextBox txtPassMontar;
        private Button btnAccionMontar, btnAccionDesmontar;

        // Elementos Internos: Setup
        private Label lblTituloSetup, lblSubSetup, lblCreateSetup, lblConfirmSetup, lblNotaSetup;
        private TextBox txtSetupPass, txtSetupConfirm;
        private Button btnFinalizarSetup;

        // Elementos Internos: Configuración
        private Label lblTituloConfig, lblLangConfig;
        private ComboBox cmbIdioma;
        private CheckBox chkInicioWindows;
        private Button btnCreditos;

        // Elementos Internos: Manual
        private Label lblManualTitulo, lblManualTexto;

        // Elementos Internos: Login & Branding
        private Panel pnlLoginLeft;
        private Panel pnlLoginRight;
        private Panel cardLogin;
        private Panel pnlBranding;

        #endregion

        #region 3. WIN32 API (Arrastre de Ventana)
        [DllImport("user32.dll", EntryPoint = "ReleaseCapture")]
        private extern static void ReleaseCapture();
        [DllImport("user32.dll", EntryPoint = "SendMessage")]
        private extern static void SendMessage(System.IntPtr hwnd, int wmsg, int wparam, int lparam);
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

            // Indicador visual de selección (Línea roja)
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

            // E. Construcción de Paneles Funcionales
            // IMPORTANTE: El orden importa para el Z-Index inicial
            CrearPanelFormulario();
            CrearPanelRestaurar();
            CrearPanelMontar();
            CrearPanelSetup();
            CrearPanelConfiguracion();
            CrearPanelManual();

            // F. Construcción de Paneles de Autenticación
            CrearPanelLogin();
            CrearPanelRegistro();
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

            // Botones Superiores (Navegación)
            btnCarpetas.Text = "🔒  PROTEGER";
            btnCarpetas.Size = new Size(btnWidth, 45);
            btnCarpetas.Location = new Point(0, 155);
            EstilarBotonSidebar(btnCarpetas);

            btnMenuAbrir.Text = "🔓  ABRIR BÓVEDA";
            btnMenuAbrir.Size = new Size(btnWidth, 45);
            btnMenuAbrir.Location = new Point(0, 205);
            EstilarBotonSidebar(btnMenuAbrir);

            if (btnExplorador == null) { btnExplorador = new Button(); PanelOpciones.Controls.Add(btnExplorador); }
            btnExplorador.Text = "📂  MIS ARCHIVOS";
            btnExplorador.Size = new Size(btnWidth, 45);
            btnExplorador.Location = new Point(0, 255);
            EstilarBotonSidebar(btnExplorador);

            if (btnManual == null) { btnManual = new Button(); PanelOpciones.Controls.Add(btnManual); }
            btnManual.Text = "📘  MANUAL";
            btnManual.Size = new Size(btnWidth, 45);
            btnManual.Location = new Point(0, 305);
            EstilarBotonSidebar(btnManual);

            // Botones Inferiores (Sistema)
            int yFondo = PanelOpciones.Height;

            if (btnSalir == null) { btnSalir = new Button(); PanelOpciones.Controls.Add(btnSalir); }
            btnSalir.Text = "🚪  SALIR";
            btnSalir.Size = new Size(260, 45);
            EstilarBotonSidebar(btnSalir);
            btnSalir.Location = new Point(0, yFondo - 60);
            btnSalir.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

            if (btnSetup == null) { btnSetup = new Button(); PanelOpciones.Controls.Add(btnSetup); }
            btnSetup.Text = "⚙️ CONFIGURACIÓN";
            btnSetup.Size = new Size(260, 45);
            EstilarBotonSidebar(btnSetup);
            btnSetup.Location = new Point(0, yFondo - 110);
            btnSetup.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        }

        #endregion

        #region 5. NAVEGACIÓN Y GESTIÓN DE PANELES

        private void OcultarTodosPaneles()
        {
            // Paneles de App
            if (panelConfiguracion != null) panelConfiguracion.Visible = false;
            if (panelMontar != null) panelMontar.Visible = false;
            if (panelFormulario != null) panelFormulario.Visible = false;
            if (panelRestaurar != null) panelRestaurar.Visible = false;
            if (panelSetup != null) panelSetup.Visible = false;
            if (panelManual != null) panelManual.Visible = false;

            // Elementos sueltos del Header
            if (lblBienvenido != null) lblBienvenido.Visible = false;
            if (btnProteger != null) btnProteger.Visible = false;
            if (btnDejarDeProteger != null) btnDejarDeProteger.Visible = false;
            if (separatorLine != null) separatorLine.Visible = false;

            // Paneles Full Screen
            if (panelLogin != null) panelLogin.Visible = false;
            if (panelRegistro != null) panelRegistro.Visible = false;

            // Selector Flotante
            if (cmbGlobalLang != null) cmbGlobalLang.Visible = false;
        }

        // --- MÉTODOS PÚBLICOS DE NAVEGACIÓN ---

        public void MostrarPanelProteger()
        {
            OcultarTodosPaneles();
            if (panelFormulario != null) panelFormulario.Visible = true;

            // Mostrar Header
            if (lblBienvenido != null) lblBienvenido.Visible = true;
            if (btnProteger != null) btnProteger.Visible = true;
            if (btnDejarDeProteger != null) btnDejarDeProteger.Visible = true;
            if (separatorLine != null) separatorLine.Visible = true;

            ResaltarBotonActivo(btnCarpetas);
            ActualizarEstiloTabs(true);

            // Forzar centrado inmediato
            RecentrarPaneles();
        }

        public void MostrarPanelRestaurar(string ruta = "")
        {
            OcultarTodosPaneles();
            if (panelRestaurar != null) panelRestaurar.Visible = true;

            // Mostrar Header
            if (lblBienvenido != null) lblBienvenido.Visible = true;
            if (btnProteger != null) btnProteger.Visible = true;
            if (btnDejarDeProteger != null) btnDejarDeProteger.Visible = true;
            if (separatorLine != null) separatorLine.Visible = true;

            ResaltarBotonActivo(btnCarpetas);
            ActualizarEstiloTabs(false);
            RecentrarPaneles();
        }

        public void MostrarPanelMontar(string ruta = "")
        {
            OcultarTodosPaneles();
            if (panelMontar != null)
            {
                panelMontar.Visible = true;
                panelMontar.BringToFront();
            }
            ResaltarBotonActivo(btnMenuAbrir);
            RecentrarPaneles();
        }

        public void MostrarPanelConfiguracion()
        {
            this.SuspendLayout(); // 1. Congelar pintado (evita parpadeo)

            OcultarTodosPaneles();

            if (panelConfiguracion != null)
            {
                // A. Primero lo hacemos visible para que RecentrarPaneles lo detecte
                panelConfiguracion.Visible = true;
                panelConfiguracion.BringToFront();

                // B. Ahora que es visible, calculamos su centro matemático
                RecentrarPaneles();
            }

            ResaltarBotonActivo(btnSetup);

            this.ResumeLayout(true); // 2. Descongelar y pintar todo en su lugar final
        }

        public void MostrarPanelCreditos()
        {
            OcultarTodosPaneles();

            // 🔒 Ocultar el Sidebar y encabezado global
            if (PanelOpciones != null) PanelOpciones.Visible = false;
            if (lblBienvenido != null) lblBienvenido.Visible = false;
            if (btnProteger != null) btnProteger.Visible = false;
            if (btnDejarDeProteger != null) btnDejarDeProteger.Visible = false;
            if (separatorLine != null) separatorLine.Visible = false;

            // Crear panel si no existe
            if (panelCreditos == null || panelCreditos.IsDisposed)
                CrearPanelCreditos();

            // Mostrar y traer al frente
            if (panelCreditos != null)
            {
                panelCreditos.Visible = true;
                panelCreditos.Dock = DockStyle.Fill; // Cubrir toda el área
                panelCreditos.BringToFront();
            }

            // Desactivar resaltado de sidebar (por si quedó algo activo)
            DesactivarResaltadoSidebar();

            RecentrarPaneles();
        }

        public void MostrarPanelManual()
        {
            OcultarTodosPaneles();
            if (panelManual != null)
            {
                panelManual.Visible = true;
                panelManual.BringToFront();
            }
            ResaltarBotonActivo(btnManual);
            RecentrarPaneles();
        }

        public void MostrarPanelLogin()
        {
            if (PanelOpciones != null) PanelOpciones.Visible = false;
            if (panel1 != null) panel1.Visible = false;

            if (panelLogin == null || panelLogin.IsDisposed) CrearPanelLogin();

            ActualizarTextosLoginYRegistro();

            panelLogin.Visible = true;
            panelLogin.BringToFront();
            RecentrarPaneles();

            if (panelRegistro != null) panelRegistro.Visible = false;

            if (cmbGlobalLang != null)
            {
                cmbGlobalLang.Visible = true;
                cmbGlobalLang.BringToFront();
            }
            TraerControlesVentanaAlFrente();
        }

        public void MostrarPanelRegistro()
        {
            if (PanelOpciones != null) PanelOpciones.Visible = false;
            if (panel1 != null) panel1.Visible = false;

            if (panelRegistro == null || panelRegistro.IsDisposed) CrearPanelRegistro();

            ActualizarTextosLoginYRegistro();

            panelRegistro.Visible = true;
            panelRegistro.BringToFront();
            RecentrarPaneles();

            if (panelLogin != null) panelLogin.Visible = false;

            if (cmbGlobalLang != null)
            {
                cmbGlobalLang.Visible = true;
                cmbGlobalLang.BringToFront();
            }
            TraerControlesVentanaAlFrente();
        }

        // --- LÓGICA DE CENTRADO CORREGIDA ---
        public void RecentrarPaneles()
        {
            if (this.WindowState == FormWindowState.Minimized) return;

            // Congelamos el pintado global del formulario para evitar parpadeos
            this.SuspendLayout();

            // 1. Login y Registro (Lógica existente)
            if (panelLogin != null && panelLogin.Visible)
            {
                int mitad = this.ClientSize.Width / 2;
                if (pnlLoginLeft != null)
                {
                    pnlLoginLeft.Width = mitad;
                    if (cardLogin != null) CenterControlInPanel(pnlLoginLeft, cardLogin);
                }
                if (pnlLoginRight != null && pnlBranding != null) CenterControlInPanel(pnlLoginRight, pnlBranding);
                this.ResumeLayout(); // Descongelar y salir
                return;
            }

            if (panelRegistro != null && panelRegistro.Visible)
            {
                panelRegistro.Size = this.ClientSize;
                if (panelRegistro.Controls.Count > 0) CenterControlInPanel(panelRegistro, panelRegistro.Controls[0]);
                this.ResumeLayout(); // Descongelar y salir
                return;
            }

            // =========================================================
            // 3. DASHBOARD
            // =========================================================
            if (panel1 != null)
            {
                // Forzar dimensiones
                int sidebarW = (PanelOpciones != null && PanelOpciones.Visible) ? PanelOpciones.Width : 0;
                panel1.Location = new Point(sidebarW, 0);
                panel1.Width = this.ClientSize.Width - sidebarW;
                panel1.Height = this.ClientSize.Height;

                int pW = panel1.Width;
                int pH = panel1.Height;

                // A. Paneles con Tabs
                if ((panelFormulario != null && panelFormulario.Visible) || (panelRestaurar != null && panelRestaurar.Visible))
                {
                    CentrarPanelConTabs(pW, pH);
                }
                // B. Paneles Simples
                else
                {
                    CentrarPanelSimpleAbsoluto(panelConfiguracion, pW, pH);
                    CentrarPanelSimpleAbsoluto(panelMontar, pW, pH);
                    CentrarPanelSimpleAbsoluto(panelManual, pW, pH);
                    CentrarPanelSimpleAbsoluto(panelSetup, pW, pH);
                }
            }

            // Finalmente, descongelamos y forzamos el pintado inmediato
            this.ResumeLayout(true);
        }

        // --- HELPERS DE CENTRADO ---

        // Este helper fuerza la posición al centro exacto (Matemática Pura)
        private void CentrarPanelSimpleAbsoluto(Panel contenedor, int pW, int pH)
        {
            if (contenedor == null || !contenedor.Visible) return;

            Control card = FindCard(contenedor);
            Control title = FindLabel(contenedor);

            if (card != null)
            {
                // 1. Calculamos el centro para la Tarjeta
                int cardX = (pW - card.Width) / 2;
                int cardY = (pH - card.Height) / 2;

                // Aplicamos margen mínimo superior (para que no se pegue al techo en pantallas chicas)
                if (cardY < 80) cardY = 80;

                // 2. Movemos la Tarjeta
                card.Location = new Point(cardX, cardY);

                // 3. Movemos el Título (pegado arriba a la izquierda de la tarjeta, o centrado)
                if (title != null)
                {
                    // Opción A: Título alineado a la izquierda de la tarjeta (Más ordenado)
                    // title.Location = new Point(cardX, cardY - 50);

                    // Opción B: Título centrado respecto a la tarjeta (Más estético)
                    int titleX = cardX + (card.Width - title.Width) / 2; // Centrado con la tarjeta
                                                                         // O si prefieres centrado con la pantalla: int titleX = (pW - title.Width) / 2;

                    title.Location = new Point(cardX, cardY - 50); // 50px arriba de la tarjeta
                }
            }
        }

        private void CentrarPanelConTabs(int pW, int pH)
        {
            int gapHeader = 110;
            Control activeCard = null;
            if (panelFormulario != null && panelFormulario.Visible) activeCard = FindCard(panelFormulario);
            else if (panelRestaurar != null && panelRestaurar.Visible) activeCard = FindCard(panelRestaurar);

            if (activeCard != null)
            {
                int totalH = gapHeader + activeCard.Height;
                int startY = (pH - totalH) / 2;
                if (startY < 40) startY = 40; // Margen de seguridad

                int xCard = (pW - activeCard.Width) / 2;

                // Mover tarjeta
                activeCard.Location = new Point(xCard, startY + gapHeader);

                // Mover Header Global
                if (lblBienvenido != null) lblBienvenido.Location = new Point(xCard, startY); // Alineado izquierda tarjeta

                if (btnProteger != null) btnProteger.Location = new Point(xCard, startY + 50);
                if (btnDejarDeProteger != null) btnDejarDeProteger.Location = new Point(xCard + 150, startY + 50);

                if (separatorLine != null && btnProteger != null && btnDejarDeProteger != null)
                {
                    int xSep = (panelFormulario != null && panelFormulario.Visible) ? btnProteger.Location.X : btnDejarDeProteger.Location.X;
                    separatorLine.Location = new Point(xSep, startY + 95);
                }
            }
        }

        private Control FindCard(Panel p)
        {
            if (p == null) return null;
            foreach (Control c in p.Controls) if (c is Panel && c != p) return c;
            return null;
        }

        private Control FindLabel(Panel p)
        {
            if (p == null) return null;
            foreach (Control c in p.Controls) if (c is Label) return c;
            return null;
        }

        private void CenterControlInPanel(Panel parent, Control child)
        {
            if (parent == null || child == null) return;
            child.Location = new Point((parent.Width - child.Width) / 2, (parent.Height - child.Height) / 2);
        }

        public class CircularPictureBox : PictureBox
        {
            protected override void OnPaint(PaintEventArgs pe)
            {
                GraphicsPath gp = new GraphicsPath();
                gp.AddEllipse(0, 0, ClientSize.Width, ClientSize.Height);
                this.Region = new Region(gp);
                base.OnPaint(pe);
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

            // Botón Salir con Usuario
            if (btnSalir != null)
            {
                string baseText = Localization.Get("menu_exit");
                if (FolderLocker.UserManager.CurrentUser != null)
                    btnSalir.Text = $"{baseText} ({FolderLocker.UserManager.CurrentUser.Username})";
                else
                    btnSalir.Text = baseText;
            }

            // Header
            if (lblBienvenido != null) lblBienvenido.Text = Localization.Get("title_main");
            // if (lblSubtitle != null) lblSubtitle.Text = Localization.Get("app_subtitle"); // Opcional
            if (btnProteger != null) btnProteger.Text = Localization.Get("menu_protect");

            // Panel Proteger
            if (lblTituloProteger != null) lblTituloProteger.Text = Localization.Get("lbl_target");
            if (lblPassProteger != null) lblPassProteger.Text = Localization.Get("lbl_pass");
            if (btnBuscarRuta != null) btnBuscarRuta.Text = Localization.Get("btn_browse");
            if (btnAccionGuardar != null) btnAccionGuardar.Text = Localization.Get("btn_lock");
            if (btnOlvide != null) btnOlvide.Text = Localization.Get("link_forgot");

            // Panel Restaurar
            if (lblTituloRestaurar != null) lblTituloRestaurar.Text = Localization.Get("lbl_path_protected");
            if (btnAccionRestaurar != null) btnAccionRestaurar.Text = Localization.Get("btn_decrypt");

            // Panel Montar
            if (lblTituloMontar != null) lblTituloMontar.Text = Localization.Get("title_virtual");
            if (lblListaMontar != null) lblListaMontar.Text = Localization.Get("lbl_vaults");
            if (lblLetraMontar != null) lblLetraMontar.Text = Localization.Get("lbl_drive");
            if (lblPassMontar != null) lblPassMontar.Text = Localization.Get("lbl_mount_pass");
            if (btnAccionMontar != null) btnAccionMontar.Text = Localization.Get("btn_mount");
            if (btnAccionDesmontar != null) btnAccionDesmontar.Text = Localization.Get("btn_unmount");

            // Panel Setup
            if (lblTituloSetup != null) lblTituloSetup.Text = Localization.Get("setup_title");
            if (lblSubSetup != null) lblSubSetup.Text = Localization.Get("setup_sub");
            if (lblCreateSetup != null) lblCreateSetup.Text = Localization.Get("setup_lbl_create");
            if (lblConfirmSetup != null) lblConfirmSetup.Text = Localization.Get("setup_lbl_confirm");
            if (lblNotaSetup != null) lblNotaSetup.Text = Localization.Get("setup_note");
            if (btnFinalizarSetup != null) btnFinalizarSetup.Text = Localization.Get("setup_btn");

            // Panel Config
            if (lblTituloConfig != null) lblTituloConfig.Text = Localization.Get("config_title");
            if (lblLangConfig != null) lblLangConfig.Text = Localization.Get("config_lbl_lang");
            if (chkInicioWindows != null) chkInicioWindows.Text = Localization.Get("config_chk_start");
            if (btnReset != null) btnReset.Text = Localization.Get("cfg_btn_reset");

            // Panel Manual
            if (lblManualTitulo != null) lblManualTitulo.Text = Localization.Get("manual_title");
            if (lblManualTexto != null) lblManualTexto.Text = Localization.Get("manual_text");

            // System Tray
            if (itemAbrir != null) itemAbrir.Text = Localization.Get("tray_menu_open");
            if (itemSalir != null) itemSalir.Text = Localization.Get("tray_menu_exit");


            // Sincronizar Combos
            SincronizarSeleccionIdioma(cmbIdioma);
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

        private void ActualizarTextosLoginYRegistro()
        {
            // 1. Evitar repintado visual mientras cambiamos textos
            this.SuspendLayout();

            // --- ACTUALIZAR LOGIN ---
            if (lblLoginTitle != null) lblLoginTitle.Text = Localization.Get("login_title");
            if (lblLoginSub != null) lblLoginSub.Text = Localization.Get("app_subtitle");
            if (lblLoginUser != null) lblLoginUser.Text = Localization.Get("login_lbl_user").ToUpper();
            if (lblLoginPass != null) lblLoginPass.Text = Localization.Get("login_lbl_pass").ToUpper();
            if (chkLoginRemember != null) chkLoginRemember.Text = Localization.Get("login_chk_remember");
            if (btnLoginForgot != null) btnLoginForgot.Text = Localization.Get("link_forgot");
            if (btnLoginEnter != null) btnLoginEnter.Text = Localization.Get("login_btn_enter");
            if (btnLoginReg != null) btnLoginReg.Text = Localization.Get("login_btn_reg");

            // --- ACTUALIZAR REGISTRO ---
            if (lblRegTitle != null) lblRegTitle.Text = Localization.Get("reg_title");
            if (lblRegSub != null) lblRegSub.Text = Localization.Get("reg_subtitle");
            if (lblRegUser != null) lblRegUser.Text = Localization.Get("reg_lbl_user").ToUpper();
            if (lblRegPass != null) lblRegPass.Text = Localization.Get("reg_lbl_pass").ToUpper();
            if (btnRegCreate != null) btnRegCreate.Text = Localization.Get("reg_btn_create");
            if (btnRegBack != null) btnRegBack.Text = Localization.Get("reg_btn_back");

            // 2. Liberar repintado (Cambio instantáneo)
            this.ResumeLayout();
        }
        #endregion
        #region 7. CONSTRUCCIÓN DE PANELES (UI Building)

        private void EstilarLogo()
        {
            const int LogoSize = 82;
            const int LogoPaddingX = 15;
            const int TextSpacingX = 5;
            const int TextStartX = LogoPaddingX + LogoSize + TextSpacingX;

            // Centro vertical del logo: 25 + (82/2) = 66
            const int LogoCenterY = 66;
            const int TextCenterOffset = 30; // Offset para el texto de 2 líneas (aprox. 44px altura)

            try
            {
                // 1. IMAGEN DEL LOGO (Izquierda)
                var logoBox = new PictureBox
                {
                    Image = Properties.Resources.LogoPanel,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Size = new Size(LogoSize, LogoSize),
                    Location = new Point(LogoPaddingX, 25), // Inicia en Y=25
                    BackColor = Color.Transparent,
                };
                PanelOpciones.Controls.Add(logoBox);
                logoBox.BringToFront();
                logoBox.MouseDown += MoverVentana;

                // 2. TEXTO PRINCIPAL (Alineado al centro del Logo)
                lblLogo.Text = "FOLDER\nLOCKER";
                lblLogo.ForeColor = cAccentRed;
                lblLogo.Font = new Font("Segoe UI Black", 16, FontStyle.Bold);

                // Posición Y: Alinear el centro del texto (Y=22px) con el centro del logo (Y=66px)
                lblLogo.Location = new Point(TextStartX, LogoCenterY - TextCenterOffset);
                lblLogo.AutoSize = true;

            }
            catch
            {
                lblLogo.Text = "🛡️ FOLDER\nLOCKER";
            }

            // 3. Subtítulo (Suite de Seguridad)
            if (lblSubtitle == null) { lblSubtitle = new Label(); PanelOpciones.Controls.Add(lblSubtitle); }
            lblSubtitle.Text = Localization.Get("app_subtitle");
            lblSubtitle.ForeColor = cTextSecondary;
            lblSubtitle.Font = new Font("Segoe UI", 9, FontStyle.Regular);

            // Lo colocamos debajo del logo/texto principal (debajo de Y=66)
            lblSubtitle.Location = new Point(LogoPaddingX + 5, 110);
            lblSubtitle.AutoSize = true;

            // 4. Línea Separadora
            // Aseguramos que la línea esté justo debajo del subtítulo, separándola de los botones.
            var logoLine = new Panel { Height = 2, Width = 230, BackColor = cAccentRed, Location = new Point(LogoPaddingX, 130) };
            PanelOpciones.Controls.Add(logoLine);
        }

        private void ConstruirHeaderSinPanel()
        {
            // Título Principal
            lblBienvenido.Parent = panel1;
            lblBienvenido.ForeColor = cTextPrimary;
            lblBienvenido.Font = new Font("Segoe UI Light", 22);
            lblBienvenido.Text = "Centro de Seguridad";
            lblBienvenido.AutoSize = true;
            lblBienvenido.BringToFront();

            // Tabs de Navegación (Proteger / Restaurar)
            btnProteger.Parent = panel1;
            btnProteger.Size = new Size(140, 50);
            EstilarBotonTab(btnProteger, true);
            btnProteger.BringToFront();

            btnDejarDeProteger.Parent = panel1;
            btnDejarDeProteger.Size = new Size(160, 50);
            EstilarBotonTab(btnDejarDeProteger, false);
            btnDejarDeProteger.BringToFront();

            // Línea separadora roja
            separatorLine = new Panel { Height = 3, BackColor = cAccentRed, Width = 140 };
            panel1.Controls.Add(separatorLine);
            separatorLine.BringToFront();
        }

        private void CrearPanelFormulario()
        {
            panelFormulario = new Panel { Dock = DockStyle.Fill, BackColor = cBackground, Padding = new Padding(50), Visible = false };
            panel1.Controls.Add(panelFormulario);

            // Tarjeta contenedora
            Panel card = CrearTarjetaBase(550, 350);
            panelFormulario.Controls.Add(card);

            // Contenido
            lblTituloProteger = CrearEtiqueta(card, Localization.Get("lbl_target"), 40, 30);
            txtRuta = CrearInput(card, 40, 55, 330);

            btnBuscarRuta = new Button { Text = Localization.Get("btn_browse"), Size = new Size(100, 30), Location = new Point(380, 55) };
            EstilarBotonSecundario(btnBuscarRuta);
            card.Controls.Add(btnBuscarRuta);

            CrearSeparador(card, 110);

            lblPassProteger = new Label { Text = Localization.Get("lbl_pass"), Location = new Point(40, 140), ForeColor = cTextSecondary, AutoSize = true, Font = new Font("Segoe UI", 8, FontStyle.Bold) };
            card.Controls.Add(lblPassProteger);

            txtContrasena = CrearInputPassword(card, 40, 165, 440);

            btnOlvide = new Button { Text = Localization.Get("link_forgot"), AutoSize = true, Location = new Point(350, 135), FlatStyle = FlatStyle.Flat, ForeColor = cAccentRed, Font = new Font("Segoe UI", 8, FontStyle.Regular), Cursor = Cursors.Hand, BackColor = Color.Transparent };
            btnOlvide.FlatAppearance.BorderSize = 0;
            btnOlvide.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btnOlvide.FlatAppearance.MouseOverBackColor = Color.Transparent;
            card.Controls.Add(btnOlvide);

            btnAccionGuardar = new Button { Text = Localization.Get("btn_lock"), Size = new Size(440, 50), Location = new Point(40, 250) };
            EstilarBotonAccion(btnAccionGuardar);
            card.Controls.Add(btnAccionGuardar);
        }

        private void CrearPanelRestaurar()
        {
            panelRestaurar = new Panel { Dock = DockStyle.Fill, BackColor = cBackground, Padding = new Padding(50), Visible = false };
            panel1.Controls.Add(panelRestaurar);

            Panel card = CrearTarjetaBase(550, 300);
            panelRestaurar.Controls.Add(card);

            lblTituloRestaurar = CrearEtiqueta(card, Localization.Get("lbl_path_protected"), 40, 30);

            lstCarpetasRestaurar = new ListBox { Location = new Point(40, 55), Size = new Size(470, 120), BackColor = cInputBackground, ForeColor = cAccentRed, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10) };
            card.Controls.Add(lstCarpetasRestaurar);

            btnAccionRestaurar = new Button { Text = Localization.Get("btn_decrypt"), Size = new Size(470, 50), Location = new Point(40, 200) };
            EstilarBotonAccion(btnAccionRestaurar);
            card.Controls.Add(btnAccionRestaurar);
        }

        private void CrearPanelMontar()
        {
            panelMontar = new Panel { Dock = DockStyle.Fill, BackColor = cBackground, Padding = new Padding(50), Visible = false };
            panel1.Controls.Add(panelMontar);

            lblTituloMontar = new Label { Text = Localization.Get("title_virtual"), ForeColor = cAccentRed, Font = new Font("Segoe UI Black", 20, FontStyle.Bold), AutoSize = true, Location = new Point(50, 30) };
            panelMontar.Controls.Add(lblTituloMontar);

            Panel card = CrearTarjetaBase(550, 420);
            panelMontar.Controls.Add(card);

            lblListaMontar = CrearEtiqueta(card, Localization.Get("lbl_vaults"), 40, 30);
            lstCarpetasParaMontar = new ListBox { Location = new Point(40, 55), Size = new Size(470, 100), BackColor = cInputBackground, ForeColor = cTextPrimary, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10) };
            card.Controls.Add(lstCarpetasParaMontar);

            lblLetraMontar = CrearEtiqueta(card, Localization.Get("lbl_drive"), 40, 170);

            // Combo Unidad (Letra)
            cmbLetraMontar = new ComboBox
            {
                Location = new Point(40, 195),
                Size = new Size(100, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = cInputBackground,
                ForeColor = cTextPrimary,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                DropDownStyle = ComboBoxStyle.DropDownList,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 34
            };
            // Lógica de pintado simple para centrar texto
            cmbLetraMontar.DrawItem += (s, e) =>
            {
                if (e.Index < 0) return;
                var cb = s as ComboBox;
                bool sel = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
                using (var b = new SolidBrush(sel ? cAccentRed : cInputBackground)) e.Graphics.FillRectangle(b, e.Bounds);
                using (var tb = new SolidBrush(cTextPrimary))
                {
                    string txt = cb.Items[e.Index].ToString();
                    SizeF sz = e.Graphics.MeasureString(txt, cb.Font);
                    e.Graphics.DrawString(txt, cb.Font, tb, new PointF(e.Bounds.X + (e.Bounds.Width - sz.Width) / 2, e.Bounds.Y + (e.Bounds.Height - cb.Font.Height) / 2));
                }
            };
            card.Controls.Add(cmbLetraMontar);

            lblPassMontar = CrearEtiqueta(card, Localization.Get("lbl_mount_pass"), 160, 170);
            txtPassMontar = CrearInputPassword(card, 160, 195, 350);

            btnAccionMontar = new Button { Text = Localization.Get("btn_mount"), Size = new Size(470, 45), Location = new Point(40, 260) };
            EstilarBotonAccion(btnAccionMontar);
            card.Controls.Add(btnAccionMontar);

            btnAccionDesmontar = new Button { Text = Localization.Get("btn_unmount"), Size = new Size(470, 40), Location = new Point(40, 315) };
            EstilarBotonSecundario(btnAccionDesmontar);
            btnAccionDesmontar.ForeColor = Color.IndianRed;
            card.Controls.Add(btnAccionDesmontar);
        }

        private void CrearPanelConfiguracion()
        {
            // Crear el panel principal (contenedor Dock.Fill)
            panelConfiguracion = new Panel { Dock = DockStyle.Fill, BackColor = cBackground, Padding = new Padding(50), Visible = false };
            panel1.Controls.Add(panelConfiguracion);

            // Título Global (Config)
            lblTituloConfig = new Label { Text = "CONFIG", ForeColor = cAccentRed, Font = new Font("Segoe UI Black", 20, FontStyle.Bold), AutoSize = true, Location = new Point(30, 30) };
            panelConfiguracion.Controls.Add(lblTituloConfig);

            // --- TARJETA CONTENEDORA ---
            Panel card = CrearTarjetaBase(550, 480);
            panelConfiguracion.Controls.Add(card);

            // --- SECCIÓN 1: GENERAL (IDIOMA) ---
            CrearHeaderSeccion(card, Localization.Get("cfg_sec_general"), 40, 30);
            lblLangConfig = CrearEtiqueta(card, " Idioma", 40, 65);

            cmbIdioma = new ComboBox { Location = new Point(40, 90), Size = new Size(470, 45), FlatStyle = FlatStyle.Flat, BackColor = cInputBackground, ForeColor = cTextPrimary, Font = new Font("Segoe UI", 11), DropDownStyle = ComboBoxStyle.DropDownList, DrawMode = DrawMode.OwnerDrawFixed, ItemHeight = 40 };
            cmbIdioma.Items.AddRange(new object[] { "Español (MX)", "English (EN)", "Português (PT)", "Русский (RU)", "汉语 (CN)" });

            // Evento Pintado de Banderas (Reutilizando lógica)
            cmbIdioma.DrawItem += DibujarComboConBanderas;

            cmbIdioma.SelectedIndexChanged += (s, e) =>
            {
                string lang = "ES";
                if (cmbIdioma.SelectedIndex == 1) lang = "EN";
                else if (cmbIdioma.SelectedIndex == 2) lang = "PT";
                else if (cmbIdioma.SelectedIndex == 3) lang = "RU";
                else if (cmbIdioma.SelectedIndex == 4) lang = "CN";

                Localization.CurrentLang = lang;
                Properties.Settings.Default.Idioma = lang;
                Properties.Settings.Default.Save();
                ActualizarTextosIdioma();
            };
            card.Controls.Add(cmbIdioma);

            // --- SECCIÓN 2: SISTEMA ---
            CrearSeparador(card, 150);
            CrearHeaderSeccion(card, Localization.Get("cfg_sec_system"), 40, 170);

            chkInicioWindows = new CheckBox { Text = "Start with Windows", Location = new Point(40, 205), AutoSize = true, ForeColor = cTextPrimary, Font = new Font("Segoe UI", 11), Cursor = Cursors.Hand };
            try
            {
                using (RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", false))
                    if (rk != null) chkInicioWindows.Checked = rk.GetValue("FolderLocker") != null;
            }
            catch { }
            chkInicioWindows.CheckedChanged += (s, e) => SetStartup(chkInicioWindows.Checked);
            card.Controls.Add(chkInicioWindows);

            // --- SECCIÓN 3: DATOS Y RESTAURACIÓN ---
            CrearSeparador(card, 250);
            CrearHeaderSeccion(card, Localization.Get("cfg_sec_data"), 40, 270);

            // Botón Reset
            btnReset = new Button { Text = Localization.Get("cfg_btn_reset"), Size = new Size(470, 45), Location = new Point(40, 300) };
            EstilarBotonSecundario(btnReset);
            btnReset.ForeColor = Color.IndianRed;
            btnReset.Click += BtnFactoryReset_Click;
            card.Controls.Add(btnReset);

            // --- SECCIÓN 4: INFORMACIÓN Y CRÉDITOS (NUEVO) ---
            CrearSeparador(card, 360); // Nuevo separador para mejor espacio
            CrearHeaderSeccion(card, "INFO DEL DESAROLLADOR", 40, 380); // Nuevo título de sección

            // Botón Créditos
            btnCreditos = new Button
            {
                Text = "Ver Créditos",
                Size = new Size(470, 45),
                Location = new Point(40, 410) // Posición debajo del nuevo separador
            };
            EstilarBotonSecundario(btnCreditos);
            btnCreditos.Click += (s, e) => MostrarPanelCreditos(); // Conexión al panel de Créditos
            card.Controls.Add(btnCreditos);
        }

        private void CrearPanelManual()
        {
            panelManual = new Panel { Dock = DockStyle.Fill, BackColor = cBackground, Padding = new Padding(50), Visible = false };
            panel1.Controls.Add(panelManual);

            lblManualTitulo = new Label { Text = Localization.Get("manual_title"), ForeColor = cAccentRed, Font = new Font("Segoe UI Black", 20, FontStyle.Bold), AutoSize = true, Location = new Point(50, 30) };
            panelManual.Controls.Add(lblManualTitulo);

            Panel card = CrearTarjetaBase(550, 600);
            panelManual.Controls.Add(card);

            lblManualTexto = new Label { Text = Localization.Get("manual_text"), ForeColor = Color.WhiteSmoke, Font = new Font("Segoe UI", 11), AutoSize = false, Size = new Size(510, 560), Location = new Point(20, 20), TextAlign = ContentAlignment.TopLeft };
            card.Controls.Add(lblManualTexto);
        }

        private void CrearPanelCreditos()
        {
            panelCreditos = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = cBackground,
                Padding = new Padding(50),
                Visible = false
            };
            panel1.Controls.Add(panelCreditos);

            // --- TARJETA PRINCIPAL CENTRADA ---
            Panel card = new Panel
            {
                Size = new Size(550, 400),
                BackColor = Color.FromArgb(28, 26, 26)
            };
            panelCreditos.Controls.Add(card);

            // --- CONTROLES ---
            Label lblTitulo = new Label
            {
                Text = "CRÉDITOS Y DESARROLLADOR",
                ForeColor = cAccentRed,
                Font = new Font("Segoe UI Black", 20, FontStyle.Bold),
                AutoSize = true
            };
            card.Controls.Add(lblTitulo);

            CircularPictureBox pbFoto = new CircularPictureBox
            {
                Size = new Size(100, 100),
                BackColor = Color.FromArgb(40, 40, 40),
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = Properties.Resources.FotoPerfil2
            };
            card.Controls.Add(pbFoto);

            Label lblDevInfo = new Label
            {
                Text = "Desarrollado por: David Platas\nContacto: davarman10@gmail.com",
                Font = new Font("Segoe UI", 11),
                ForeColor = cTextPrimary,
                AutoSize = true
            };
            card.Controls.Add(lblDevInfo);

            Label lblVersion = new Label
            {
                Text = "FolderLocker v5.0 (2025)",
                Font = new Font("Segoe UI", 9),
                ForeColor = cTextSecondary,
                AutoSize = true
            };
            card.Controls.Add(lblVersion);

            Button btnGitHub = new Button
            {
                Text = "Ver en GitHub",
                Size = new Size(150, 40)
            };
            EstilarBotonSecundario(btnGitHub);
            btnGitHub.Click += (s, e) =>
                Process.Start(new ProcessStartInfo("https://github.com/DeathSilencer") { UseShellExecute = true });
            card.Controls.Add(btnGitHub);

            Button btnVolver = new Button
            {
                Text = "Volver",
                Size = new Size(150, 40)
            };
            EstilarBotonAccion(btnVolver);
            btnVolver.Click += (s, e) =>
            {
                if (panelCreditos != null) panelCreditos.Visible = false;
                if (PanelOpciones != null) PanelOpciones.Visible = true;
                MostrarPanelConfiguracion();
            };
            card.Controls.Add(btnVolver);

            // --- LAYOUT INTERNO ---
            void LayoutInterno()
            {
                // Centrar título
                lblTitulo.Location = new Point((card.Width - lblTitulo.Width) / 2, 30);

                // Centrar imagen a la izquierda
                pbFoto.Location = new Point((card.Width - pbFoto.Width) / 2 - 160, 100);

                // Datos del desarrollador al lado derecho
                lblDevInfo.Location = new Point(pbFoto.Right + 20, pbFoto.Top + 30);

                // Centrar versión
                lblVersion.Location = new Point((card.Width - lblVersion.Width) / 2, pbFoto.Bottom + 40);

                // Alinear botones al centro
                int totalWidth = btnGitHub.Width + 20 + btnVolver.Width;
                int startX = (card.Width - totalWidth) / 2;
                btnGitHub.Location = new Point(startX, lblVersion.Bottom + 30);
                btnVolver.Location = new Point(btnGitHub.Right + 20, lblVersion.Bottom + 30);
            }

            // --- CENTRAR PANEL CARD ---
            void CenterCard()
            {
                int x = (panelCreditos.ClientSize.Width - card.Width) / 2;
                int y = (panelCreditos.ClientSize.Height - card.Height) / 2;
                if (y < 40) y = 40; // margen mínimo superior
                card.Location = new Point(x, y);
            }

            // Ejecutar inicialmente
            LayoutInterno();
            CenterCard();

            // 🔁 Recalcular dinámicamente al redimensionar
            panelCreditos.Resize += (s, e) =>
            {
                LayoutInterno();
                CenterCard();
            };

            // Borde visual del card
            card.Paint += (s, e) =>
                ControlPaint.DrawBorder(e.Graphics, card.ClientRectangle, Color.FromArgb(45, 40, 40), ButtonBorderStyle.Solid);
        }


        private void CrearPanelSetup()
        {
            panelSetup = new Panel { Dock = DockStyle.Fill, BackColor = cBackground, Padding = new Padding(40), Visible = false };
            panel1.Controls.Add(panelSetup);

            lblTituloSetup = new Label { Text = "WELCOME", ForeColor = cAccentRed, Font = new Font("Segoe UI Black", 24, FontStyle.Bold), AutoSize = true, Location = new Point(40, 40) };
            panelSetup.Controls.Add(lblTituloSetup);

            Panel card = CrearTarjetaBase(550, 480);
            panelSetup.Controls.Add(card);

            CrearEtiqueta(card, "IDIOMA", 45, 30);
            // Reutilizamos lógica de combo idioma (simplificado aquí)
            ComboBox cmbLangSetup = new ComboBox { Location = new Point(45, 55), Size = new Size(460, 45), FlatStyle = FlatStyle.Flat, BackColor = cInputBackground, ForeColor = cTextPrimary, Font = new Font("Segoe UI", 11), DropDownStyle = ComboBoxStyle.DropDownList, DrawMode = DrawMode.OwnerDrawFixed, ItemHeight = 40 };
            cmbLangSetup.Items.AddRange(new object[] { "Español (ES)", "English (EN)", "Português (PT)", "Русский (RU)", "中文 (CN)" });
            cmbLangSetup.DrawItem += DibujarComboConBanderas;

            // Lógica de cambio de idioma instantáneo en setup
            cmbLangSetup.SelectedIndexChanged += (s, e) =>
            {
                string lang = "ES";
                if (cmbLangSetup.SelectedIndex == 1) lang = "EN";
                else if (cmbLangSetup.SelectedIndex == 2) lang = "PT";
                else if (cmbLangSetup.SelectedIndex == 3) lang = "RU";
                else if (cmbLangSetup.SelectedIndex == 4) lang = "CN";
                Localization.CurrentLang = lang;
                Properties.Settings.Default.Idioma = lang;
                Properties.Settings.Default.Save();
                ActualizarTextosIdioma();
            };
            card.Controls.Add(cmbLangSetup);
            CrearSeparador(card, 110);

            lblSubSetup = new Label { Text = Localization.Get("setup_sub"), ForeColor = cTextSecondary, Font = new Font("Segoe UI", 10), AutoSize = true, Location = new Point(45, 125) };
            card.Controls.Add(lblSubSetup);

            lblCreateSetup = CrearEtiqueta(card, Localization.Get("setup_lbl_create"), 45, 160);
            txtSetupPass = CrearInputPassword(card, 45, 185, 460);
            AdjuntarMedidorFortaleza(card, txtSetupPass, 45, 220, 460);

            lblConfirmSetup = CrearEtiqueta(card, Localization.Get("setup_lbl_confirm"), 45, 230);
            txtSetupConfirm = CrearInputPassword(card, 45, 255, 460);

            lblNotaSetup = new Label { Text = Localization.Get("setup_note"), ForeColor = Color.Orange, Font = new Font("Segoe UI", 8), AutoSize = true, Location = new Point(45, 300) };
            card.Controls.Add(lblNotaSetup);

            btnFinalizarSetup = new Button { Text = Localization.Get("setup_btn"), Size = new Size(460, 50), Location = new Point(45, 350) };
            EstilarBotonAccion(btnFinalizarSetup);
            card.Controls.Add(btnFinalizarSetup);
        }

        // --- PANELES DE AUTENTICACIÓN (LOGIN/REGISTRO) ---

        private void CrearPanelLogin()
        {
            // 1. Configuración del Contenedor Principal
            panelLogin = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = cBackground,
                Visible = false
            };
            this.Controls.Add(panelLogin);

            // 2. Estructura Split Screen (Izquierda: Form, Derecha: Branding)
            pnlLoginLeft = new Panel { Dock = DockStyle.Left, Width = this.ClientSize.Width / 2, BackColor = cBackground };
            pnlLoginRight = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(15, 15, 15) };

            panelLogin.Controls.Add(pnlLoginLeft);
            panelLogin.Controls.Add(pnlLoginRight);
            pnlLoginRight.BringToFront();

            // 3. Tarjeta de Login (Card)
            cardLogin = CrearTarjetaBase(450, 560);
            cardLogin.Location = new Point(0, 0); // Se centra dinámicamente en RecentrarPaneles()
            pnlLoginLeft.Controls.Add(cardLogin);

            // --- ELEMENTOS DE TEXTO (Variables Globales para Hot-Swap) ---

            // Título
            lblLoginTitle = new Label
            {
                Text = Localization.Get("login_title"),
                ForeColor = cAccentRed,
                Font = new Font("Segoe UI Black", 24, FontStyle.Bold),
                AutoSize = false,
                Size = new Size(cardLogin.Width, 50),
                Location = new Point(0, 40),
                TextAlign = ContentAlignment.MiddleCenter
            };
            cardLogin.Controls.Add(lblLoginTitle);

            // Subtítulo
            lblLoginSub = new Label
            {
                Text = Localization.Get("app_subtitle"),
                ForeColor = cTextSecondary,
                Font = new Font("Segoe UI", 10),
                AutoSize = false,
                Size = new Size(cardLogin.Width, 25),
                Location = new Point(0, 90),
                TextAlign = ContentAlignment.MiddleCenter
            };
            cardLogin.Controls.Add(lblLoginSub);

            // Input Usuario
            lblLoginUser = CrearEtiqueta(cardLogin, Localization.Get("login_lbl_user"), 50, 140);
            var txtUser = CrearInput(cardLogin, 50, 165, 350);

            // Input Contraseña
            lblLoginPass = CrearEtiqueta(cardLogin, Localization.Get("login_lbl_pass"), 50, 210);
            var txtPass = CrearInputPassword(cardLogin, 50, 235, 350);

            // Agrega esto:
            KeyEventHandler enterHandler = (s, e) => {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true; // Quita el sonido
                    btnLoginEnter.PerformClick(); // Simula clic
                }
            };

            txtUser.KeyDown += enterHandler;
            txtPass.KeyDown += enterHandler;

            // Checkbox Recordar
            chkLoginRemember = new CheckBox
            {
                Text = Localization.Get("login_chk_remember"),
                ForeColor = cTextSecondary,
                Font = new Font("Segoe UI", 9),
                AutoSize = true,
                Location = new Point(50, 275),
                Cursor = Cursors.Hand
            };

            // Lógica: Cargar usuario guardado
            string lastUserFile = Path.Combine(Application.StartupPath, "last_user.dat");
            if (File.Exists(lastUserFile))
            {
                try
                {
                    string u = File.ReadAllText(lastUserFile);
                    if (!string.IsNullOrEmpty(u))
                    {
                        txtUser.Text = u;
                        chkLoginRemember.Checked = true;
                        this.ActiveControl = txtPass;
                    }
                }
                catch { }
            }
            cardLogin.Controls.Add(chkLoginRemember);

            // Botón "Olvidaste tu contraseña"
            btnLoginForgot = new Button
            {
                Text = Localization.Get("link_forgot"),
                AutoSize = true,
                Location = new Point(260, 273),
                FlatStyle = FlatStyle.Flat,
                ForeColor = cAccentRed,
                Font = new Font("Segoe UI", 8, FontStyle.Regular),
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent
            };
            btnLoginForgot.FlatAppearance.BorderSize = 0;
            btnLoginForgot.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btnLoginForgot.FlatAppearance.MouseOverBackColor = Color.Transparent;

            btnLoginForgot.Click += (s, e) =>
            {
                if (string.IsNullOrEmpty(txtUser.Text)) { DarkDialogs.ShowInfo("Escribe tu usuario primero."); return; }
                string code = DarkDialogs.ShowInput(Localization.Get("rec_prompt_msg"), Localization.Get("rec_prompt_title"), false);
                if (!string.IsNullOrEmpty(code))
                {
                    string recoveredPass = UserManager.RecoverLoginPassword(txtUser.Text, code);
                    if (recoveredPass != null) { DarkDialogs.ShowResultWithCopy(Localization.Get("rec_success_msg"), recoveredPass); txtPass.Text = recoveredPass; }
                    else { DarkDialogs.ShowInfo(Localization.Get("rec_fail_msg"), "Error"); }
                }
            };
            cardLogin.Controls.Add(btnLoginForgot);

            // Botón Entrar
            btnLoginEnter = new Button { Text = Localization.Get("login_btn_enter"), Size = new Size(350, 50), Location = new Point(50, 320) };
            EstilarBotonAccion(btnLoginEnter);

            btnLoginEnter.Click += (s, e) =>
            {
                if (UserManager.Login(txtUser.Text, txtPass.Text))
                {
                    try
                    {
                        if (chkLoginRemember.Checked) File.WriteAllText(lastUserFile, txtUser.Text);
                        else if (File.Exists(lastUserFile)) File.Delete(lastUserFile);
                    }
                    catch { }

                    // Transición exitosa
                    panelLogin.Visible = false;
                    if (PanelOpciones != null) PanelOpciones.Visible = true;
                    if (panel1 != null) panel1.Visible = true;
                    if (cmbGlobalLang != null) cmbGlobalLang.Visible = false;

                    ActualizarTextosIdioma();
                    ModoNormal();

                    DarkDialogs.ShowInfo(string.Format(Localization.Get("login_welcome"), UserManager.CurrentUser.Username));
                    txtPass.Text = "";
                }
                else
                {
                    DarkDialogs.ShowInfo(Localization.Get("login_err_auth"), Localization.Get("title_error"));
                }
            };
            cardLogin.Controls.Add(btnLoginEnter);
            this.AcceptButton = btnLoginEnter;

            // Separador visual
            CrearSeparador(cardLogin, 400);

            // Botón Registrarse
            btnLoginReg = new Button
            {
                Text = Localization.Get("login_btn_reg"),
                FlatStyle = FlatStyle.Flat,
                ForeColor = cTextSecondary,
                Size = new Size(350, 30),
                Location = new Point(50, 420),
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9)
            };
            btnLoginReg.FlatAppearance.BorderSize = 0;

            btnLoginReg.Click += (s, e) =>
            {
                panelLogin.Visible = false;
                MostrarPanelRegistro();
            };
            cardLogin.Controls.Add(btnLoginReg);

            // 4. Branding Derecho y Eventos de Ventana
            ConstruirBrandingDerecho();

            pnlLoginLeft.MouseDown += MoverVentana;
            pnlLoginRight.MouseDown += MoverVentana;
        }

        private void ConstruirBrandingDerecho()
        {
            pnlBranding = new Panel { Size = new Size(500, 600), BackColor = Color.Transparent };
            pnlLoginRight.Controls.Add(pnlBranding);
            pnlBranding.BringToFront();

            try
            {
                var imgCompuesta = new PictureBox
                {
                    // Usamos el recurso de la imagen compuesta que incluye el texto
                    Image = Properties.Resources.FolderLockerConTexto,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Size = new Size(400, 400),
                    Location = new Point(50, 100), // Centrar visualmente
                    BackColor = Color.Transparent
                };
                pnlBranding.Controls.Add(imgCompuesta);
            }
            catch
            { // Manejar error }

                pnlBranding.MouseDown += MoverVentana;
                pnlLoginRight.MouseDown += MoverVentana;
            }
        }

        private void CrearPanelRegistro()
        {
            // 1. Configuración Base del Panel
            panelRegistro = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = cBackground,
                Padding = new Padding(50),
                Visible = false
            };
            this.Controls.Add(panelRegistro);

            // 2. Tarjeta Central
            Panel card = CrearTarjetaBase(450, 550);
            // Centrado inicial (luego RecentrarPaneles se encarga del redimensionamiento)
            card.Location = new Point((this.ClientSize.Width - 450) / 2, (this.ClientSize.Height - 550) / 2);
            panelRegistro.Controls.Add(card);

            // 3. Título (Global para traducción dinámica)
            lblRegTitle = new Label
            {
                Text = Localization.Get("reg_title"),
                ForeColor = cAccentRed,
                Font = new Font("Segoe UI Black", 24, FontStyle.Bold),
                AutoSize = false,
                Size = new Size(card.Width, 45),
                Location = new Point(0, 40),
                TextAlign = ContentAlignment.MiddleCenter
            };
            card.Controls.Add(lblRegTitle);

            // 4. Subtítulo (Agregado para consistencia visual con Login)
            lblRegSub = new Label
            {
                Text = Localization.Get("reg_subtitle"),
                ForeColor = cTextSecondary,
                Font = new Font("Segoe UI", 10),
                AutoSize = false,
                Size = new Size(card.Width, 25),
                Location = new Point(0, 85),
                TextAlign = ContentAlignment.MiddleCenter
            };
            card.Controls.Add(lblRegSub);

            // 5. Inputs y Etiquetas (Capturamos las etiquetas en variables globales)
            lblRegUser = CrearEtiqueta(card, Localization.Get("reg_lbl_user"), 50, 130);
            var txtUser = CrearInput(card, 50, 155, 350);

            lblRegPass = CrearEtiqueta(card, Localization.Get("reg_lbl_pass"), 50, 210);
            var txtPass = CrearInputPassword(card, 50, 235, 350);

            KeyEventHandler regEnterHandler = (s, e) => {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    btnRegCreate.PerformClick();
                }
            };
            lblRegUser.KeyDown += regEnterHandler; // (o como se llamen tus variables locales)
            lblRegPass.KeyDown += regEnterHandler;

            // Medidor de fortaleza debajo del password
            AdjuntarMedidorFortaleza(card, txtPass, 50, 270, 350);

            // 6. Botón Crear Cuenta (Global)
            btnRegCreate = new Button { Text = Localization.Get("reg_btn_create"), Size = new Size(350, 50), Location = new Point(50, 320) };
            EstilarBotonAccion(btnRegCreate);

            btnRegCreate.Click += (s, e) =>
            {
                string u = txtUser.Text.Trim();
                string p = txtPass.Text;

                // Validaciones
                if (u.Length < 3 || p.Length < 4)
                {
                    DarkDialogs.ShowInfo(Localization.Get("reg_err_len"), Localization.Get("title_error"));
                    return;
                }

                // Lógica de Registro
                string recCode = "REC-" + Guid.NewGuid().ToString().Substring(0, 4).ToUpper();
                if (UserManager.Register(u, p, recCode))
                {
                    DarkDialogs.ShowResultWithCopy(Localization.Get("reg_msg_code") + "\n\n" + Localization.Get("setup_note"), recCode);

                    panelRegistro.Visible = false;
                    MostrarPanelLogin();
                    DarkDialogs.ShowInfo(Localization.Get("reg_msg_login"));

                    // Limpieza
                    txtUser.Text = "";
                    txtPass.Text = "";
                }
                else
                {
                    DarkDialogs.ShowInfo(Localization.Get("reg_err_exists"), Localization.Get("title_error"));
                }
            };
            card.Controls.Add(btnRegCreate);

            // 7. Separador
            CrearSeparador(card, 400);

            // 8. Botón Volver (Global)
            btnRegBack = new Button
            {
                Text = Localization.Get("reg_btn_back"),
                FlatStyle = FlatStyle.Flat,
                ForeColor = cTextSecondary,
                Size = new Size(350, 30),
                Location = new Point(50, 420),
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9)
            };
            btnRegBack.FlatAppearance.BorderSize = 0;
            // Estilos hover manuales para botones secundarios transparentes
            btnRegBack.FlatAppearance.MouseOverBackColor = cInputBackground;
            btnRegBack.FlatAppearance.MouseDownBackColor = cBackground;

            btnRegBack.Click += (s, e) =>
            {
                panelRegistro.Visible = false;
                MostrarPanelLogin();
            };
            card.Controls.Add(btnRegBack);

            // 9. Habilitar arrastre de ventana desde el fondo
            panelRegistro.MouseDown += MoverVentana;
        }

        #endregion

        #region 8. SISTEMA Y VENTANA (Window Controls)

        // Variables para gestionar el arrastre inteligente
        private Point _dragStartPoint;
        private bool _isMouseDown = false;

        private void ConfigurarVentanaSinBordes()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            int btnW = 46, btnH = 32;

            // Botón Cerrar
            btnClose = new Button { Text = "✕", Size = new Size(btnW, btnH), Anchor = AnchorStyles.Top | AnchorStyles.Right, FlatStyle = FlatStyle.Flat, BackColor = cBackground, ForeColor = Color.White, Font = iconFont };
            btnClose.Location = new Point(this.ClientSize.Width - btnW, 0);
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(232, 17, 35);
            btnClose.Click += (s, e) =>
            {
                this.WindowState = FormWindowState.Minimized;
                this.Hide();

                // --- LÓGICA DE NOTIFICACIÓN ÚNICA ---
                if (!_yaSeNotificoTray)
                {
                    trayIcon.ShowBalloonTip(
                        2000,
                        Localization.Get("tray_min_title") ?? "FolderLocker",
                        Localization.Get("tray_min_click") ?? "La aplicación sigue ejecutándose en segundo plano.",
                        ToolTipIcon.Info
                    );
                    _yaSeNotificoTray = true; // ¡Marcamos que ya avisamos!
                }
            };
            this.Controls.Add(btnClose);

            // Botón Maximizar
            btnMax = new Button { Text = "🗖", Size = new Size(btnW, btnH), Anchor = AnchorStyles.Top | AnchorStyles.Right, FlatStyle = FlatStyle.Flat, BackColor = cBackground, ForeColor = Color.White, Font = new Font("Segoe UI Symbol", 11) };
            btnMax.Location = new Point(this.ClientSize.Width - (btnW * 2), 0);
            btnMax.FlatAppearance.BorderSize = 0;
            btnMax.Click += (s, e) => EjecutarMaximizar();
            this.Controls.Add(btnMax);

            // Botón Minimizar
            btnMin = new Button { Text = "—", Size = new Size(btnW, btnH), Anchor = AnchorStyles.Top | AnchorStyles.Right, FlatStyle = FlatStyle.Flat, BackColor = cBackground, ForeColor = Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            btnMin.Location = new Point(this.ClientSize.Width - (btnW * 3), 0);
            btnMin.FlatAppearance.BorderSize = 0;
            btnMin.Click += (s, e) =>
            {
                this.WindowState = FormWindowState.Minimized;
                trayIcon.ShowBalloonTip(2000, Localization.Get("tray_min_title"), Localization.Get("tray_min_click"), ToolTipIcon.Info);
            };
            this.Controls.Add(btnMin);
        }

        // --- LÓGICA DE MAXIMIZAR ---
        private void EjecutarMaximizar()
        {
            try
            {
                // Detectar el monitor que contiene la mayor parte de la ventana
                Screen currentScreen = Screen.FromRectangle(this.Bounds);
                Rectangle workingArea = currentScreen.WorkingArea;
                this.MaximizedBounds = workingArea;

                // Establecer correctamente los límites máximos
                this.MaximizedBounds = new Rectangle(
                    workingArea.X,
                    workingArea.Y,
                    workingArea.Width,
                    workingArea.Height
                );

                // Cambiar el estado
                if (this.WindowState == FormWindowState.Normal)
                {
                    this.StartPosition = FormStartPosition.Manual;

                    // Reposicionar exactamente dentro de los límites del monitor actual
                    this.Location = new Point(workingArea.X, workingArea.Y);
                    this.Size = new Size(workingArea.Width, workingArea.Height);

                    this.WindowState = FormWindowState.Maximized;

                    if (btnMax != null)
                        btnMax.Text = "❐";
                }
                else
                {
                    this.WindowState = FormWindowState.Normal;
                    if (btnMax != null)
                        btnMax.Text = "🗖";

                    // Restaurar centrado en el monitor actual
                    this.StartPosition = FormStartPosition.Manual;
                    this.Location = new Point(
                        workingArea.X + (workingArea.Width - this.Width) / 2,
                        workingArea.Y + (workingArea.Height - this.Height) / 2
                    );
                }

                // Mantener los paneles correctamente centrados
                RecentrarPaneles();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al maximizar: " + ex.Message);
            }
        }

        // --- GESTIÓN INTELIGENTE DEL MOUSE (SOLUCIÓN DOBLE CLIC) ---

        private void HabilitarArrastreGlobal()
        {
            // Suscribir eventos a todos los contenedores posibles
            SuscribirEventos(this);
            SuscribirEventos(PanelOpciones);
            SuscribirEventos(panel1);
            SuscribirEventos(lblBienvenido);

            // Login y Registro
            SuscribirEventos(panelLogin);
            SuscribirEventos(pnlLoginLeft);
            SuscribirEventos(pnlLoginRight);
            SuscribirEventos(panelRegistro);

            // Hijos del panel principal
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
            // Limpieza preventiva
            c.MouseDown -= OnSmartMouseDown;
            c.MouseMove -= OnSmartMouseMove;
            c.MouseUp -= OnSmartMouseUp;
            c.MouseDoubleClick -= OnSmartDoubleClick;

            // Suscripción
            c.MouseDown += OnSmartMouseDown;
            c.MouseMove += OnSmartMouseMove;
            c.MouseUp += OnSmartMouseUp;
            c.MouseDoubleClick += OnSmartDoubleClick;
        }

        // Lógica de Arrastre (Mantiene la restricción de 40px)
        private void MoverVentana(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            // Filtro de Zona para Arrastre
            bool esZonaTitulo = (sender is Label) || (e.Y <= 40);

            if (esZonaTitulo)
            {
                ReleaseCapture();
                SendMessage(this.Handle, 0xA1, 0x2, 0);
            }
        }

        // 1. Mouse Down: Solo tomamos nota de la posición, NO movemos aún.
        private void OnSmartMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            // Validar Zona Segura (Top 40px)
            bool esZonaTitulo = (sender is Label) || (e.Y <= 40);

            if (esZonaTitulo)
            {
                _isMouseDown = true;
                _dragStartPoint = e.Location;
            }
        }

        // 2. Mouse Move: Solo iniciamos el arrastre si se mueve el mouse (Intención de arrastrar)
        private void OnSmartMouseMove(object sender, MouseEventArgs e)
        {
            if (_isMouseDown)
            {
                // Calcular distancia movida
                int deltaX = Math.Abs(e.X - _dragStartPoint.X);
                int deltaY = Math.Abs(e.Y - _dragStartPoint.Y);

                // Si se mueve más de 5 pixeles, asumimos que quiere arrastrar
                // Esto deja "espacio" para que el doble clic ocurra si no se mueve el mouse
                if (deltaX > 5 || deltaY > 5)
                {
                    _isMouseDown = false; // Ya no estamos esperando clic, estamos arrastrando
                    ReleaseCapture();
                    SendMessage(this.Handle, 0xA1, 0x2, 0);
                }
            }
        }

        // 3. Mouse Up: Limpiamos bandera
        private void OnSmartMouseUp(object sender, MouseEventArgs e)
        {
            _isMouseDown = false;
        }

        // 4. Doble Clic: Ahora sí llega libremente porque el MouseDown no bloqueó el hilo
        private void OnSmartDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            bool esZonaTitulo = (sender is Label) || (e.Y <= 40);
            if (esZonaTitulo)
            {
                EjecutarMaximizar();
            }
        }

        // --- SYSTEM TRAY ---
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

            // 3. Crear Icono
            if (trayIcon == null)
            {
                trayIcon = new NotifyIcon
                {
                    Text = "FolderLocker",
                    // CAMBIO CLAVE: Usa el icono de la ventana principal, que ya es el tipo Icon correcto.
                    Icon = this.Icon,
                    Visible = true
                };
            }
            trayIcon.ContextMenuStrip = trayMenu;

            // Eventos Tray
            trayIcon.MouseDoubleClick -= TrayIcon_MouseDoubleClick;
            trayIcon.MouseDoubleClick += TrayIcon_MouseDoubleClick;
            trayIcon.BalloonTipClicked -= TrayIcon_BalloonTipClicked;
            trayIcon.BalloonTipClicked += TrayIcon_BalloonTipClicked;

            itemAbrir.Click += (s, e) => RestaurarVentana();
        }

        private void TrayIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) RestaurarVentana();
        }

        private void TrayIcon_BalloonTipClicked(object sender, EventArgs e)
        {
            RestaurarVentana();
        }

        private void RestaurarVentana()
        {
            this.Show();
            this.WindowState = FormWindowState.Maximized;
            this.BringToFront();
            this.Activate();

            if (progresoActivo != null && !progresoActivo.IsDisposed)
            {
                progresoActivo.Show();
                progresoActivo.BringToFront();
            }
            RecentrarPaneles();
        }

        private void TraerControlesVentanaAlFrente()
        {
            if (btnClose != null) btnClose.BringToFront();
            if (btnMax != null) btnMax.BringToFront();
            if (btnMin != null) btnMin.BringToFront();
            if (cmbGlobalLang != null && cmbGlobalLang.Visible) cmbGlobalLang.BringToFront();
        }

        private void SetStartup(bool enable)
        {
            try
            {
                using (RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                    if (enable) rk.SetValue("FolderLocker", Application.ExecutablePath);
                    else rk.DeleteValue("FolderLocker", false);
            }
            catch { DarkDialogs.ShowInfo("Error: Admin required for startup config."); }
        }



        #endregion

        #region 9. UI FACTORIES & HELPERS (Estilos Reutilizables)

        private void DesactivarResaltadoSidebar()
        {
            foreach (Control c in PanelOpciones.Controls)
            {
                if (c is Button btn)
                {
                    btn.BackColor = cBackground; // color base del sidebar
                    btn.ForeColor = cTextPrimary;
                }
            }
        }

        private Panel CrearTarjetaBase(int w, int h)
        {
            Panel p = new Panel { Size = new Size(w, h), BackColor = Color.FromArgb(28, 26, 26) };
            p.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, p.ClientRectangle, Color.FromArgb(45, 40, 40), ButtonBorderStyle.Solid);
            return p;
        }

        private void ResaltarBotonActivo(Button btnActivo)
        {
            Button[] menuButtons = { btnSetup, btnCarpetas, btnMenuAbrir, btnExplorador, btnManual };
            foreach (var btn in menuButtons)
            {
                if (btn == null) continue;
                if (btn == btnActivo)
                {
                    btn.BackColor = cInputBackground; btn.ForeColor = cAccentRed; btn.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                    if (pnlIndicador != null) { pnlIndicador.Visible = true; pnlIndicador.Height = btn.Height; pnlIndicador.Top = btn.Top; pnlIndicador.Left = PanelOpciones.Width - pnlIndicador.Width; pnlIndicador.BringToFront(); }
                }
                else
                {
                    btn.BackColor = Color.Transparent; btn.ForeColor = cTextPrimary; btn.Font = new Font("Segoe UI Semibold", 10, FontStyle.Bold);
                }
            }
        }

        private void ActualizarEstiloTabs(bool protegerActivo)
        {
            if (btnProteger != null) { btnProteger.BackColor = protegerActivo ? cAccentRed : cSurface; btnProteger.ForeColor = protegerActivo ? Color.White : cTextSecondary; }
            if (btnDejarDeProteger != null) { btnDejarDeProteger.BackColor = !protegerActivo ? cAccentRed : cSurface; btnDejarDeProteger.ForeColor = !protegerActivo ? Color.White : cTextSecondary; }
            if (separatorLine != null && btnProteger != null && btnDejarDeProteger != null)
            {
                separatorLine.Location = new Point(protegerActivo ? btnProteger.Location.X : btnDejarDeProteger.Location.X, separatorLine.Location.Y);
                separatorLine.Width = protegerActivo ? btnProteger.Width : btnDejarDeProteger.Width;
            }
        }

        // --- Estilos de Botones ---
        private void EstilarBotonSidebar(Button b)
        {
            if (b == null) return;
            b.FlatStyle = FlatStyle.Flat; b.FlatAppearance.BorderSize = 0; b.FlatAppearance.MouseOverBackColor = cInputBackground; b.FlatAppearance.MouseDownBackColor = cBackground;
            b.BackColor = Color.Transparent; b.ForeColor = cTextPrimary; b.Font = new Font("Segoe UI Semibold", 10, FontStyle.Bold);
            b.TextAlign = ContentAlignment.MiddleLeft; b.Padding = new Padding(20, 0, 0, 0); b.Cursor = Cursors.Hand;
        }

        private void EstilarBotonTab(Button b, bool esPrincipal)
        {
            if (b == null) return;
            b.FlatStyle = FlatStyle.Flat; b.FlatAppearance.BorderSize = 0;
            b.BackColor = esPrincipal ? Color.FromArgb(60, 30, 30) : cSurface;
            b.ForeColor = cTextPrimary; b.Font = new Font("Segoe UI", 9, FontStyle.Bold); b.Cursor = Cursors.Hand;
        }

        private void EstilarBotonAccion(Button b)
        {
            b.BackColor = cAccentRed; b.ForeColor = Color.White; b.FlatStyle = FlatStyle.Flat; b.FlatAppearance.BorderSize = 0;
            b.Font = new Font("Segoe UI Black", 11, FontStyle.Bold); b.Cursor = Cursors.Hand;
        }

        private void EstilarBotonSecundario(Button b)
        {
            b.FlatStyle = FlatStyle.Flat; b.ForeColor = cTextPrimary; b.BackColor = cInputBackground; b.FlatAppearance.BorderColor = cSurface; b.Cursor = Cursors.Hand;
        }

        // --- Creadores de Inputs ---
        private Label CrearEtiqueta(Panel p, string texto, int x, int y)
        {
            var lbl = new Label { Text = texto.ToUpper(), Location = new Point(x, y), ForeColor = cTextSecondary, AutoSize = true, Font = new Font("Segoe UI", 8, FontStyle.Bold) };
            p.Controls.Add(lbl); return lbl;
        }

        private Label CrearHeaderSeccion(Panel p, string texto, int x, int y)
        {
            var lbl = new Label { Text = texto, Location = new Point(x, y), AutoSize = true, ForeColor = cAccentRed, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            p.Controls.Add(lbl); return lbl;
        }

        private TextBox CrearInput(Panel p, int x, int y, int w)
        {
            var t = new TextBox { Location = new Point(x, y), Width = w, BackColor = cInputBackground, ForeColor = cTextPrimary, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 11), AutoSize = false, Height = 30 };
            p.Controls.Add(t); return t;
        }

        private TextBox CrearInputPassword(Panel p, int x, int y, int w)
        {
            var t = new TextBox { Location = new Point(x, y), Width = w - 40, BackColor = cInputBackground, ForeColor = cTextPrimary, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 11), AutoSize = false, Height = 30, UseSystemPasswordChar = true };
            p.Controls.Add(t);
            var btnEye = new Button { Text = "👁️", Location = new Point(x + w - 35, y), Size = new Size(35, 30), FlatStyle = FlatStyle.Flat, BackColor = cInputBackground, ForeColor = cTextPrimary, Cursor = Cursors.Hand, Font = new Font("Segoe UI", 10) };
            btnEye.FlatAppearance.BorderSize = 0;
            btnEye.MouseDown += (s, e) => { t.UseSystemPasswordChar = false; btnEye.ForeColor = cAccentRed; };
            btnEye.MouseUp += (s, e) => { t.UseSystemPasswordChar = true; btnEye.ForeColor = cTextPrimary; };
            p.Controls.Add(btnEye); return t;
        }

        private void CrearSeparador(Panel p, int y)
        {
            var sep = new Panel { Size = new Size(p.Width - 80, 1), BackColor = Color.FromArgb(45, 45, 45), Location = new Point(40, y) };
            p.Controls.Add(sep);
        }

        // --- Helpers Avanzados ---

        private void InicializarSelectorIdiomaGlobal()
        {
            // Si ya existe, salimos para no duplicarlo
            if (cmbGlobalLang != null) return;

            // 1. Configuración Visual
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

            // 2. Pintado de Banderas
            cmbGlobalLang.DrawItem += DibujarComboConBanderas;

            // 3. LÓGICA DE CAMBIO DE IDIOMA (OPTIMIZADA SIN PARPADEO)
            cmbGlobalLang.SelectedIndexChanged += (s, e) =>
            {
                // 1. Determinar código de idioma
                string lang = "ES";
                if (cmbGlobalLang.SelectedIndex == 1) lang = "EN";
                else if (cmbGlobalLang.SelectedIndex == 2) lang = "PT";
                else if (cmbGlobalLang.SelectedIndex == 3) lang = "RU";
                else if (cmbGlobalLang.SelectedIndex == 4) lang = "CN";

                // 2. Solo actuar si es diferente al actual
                if (Localization.CurrentLang != lang)
                {
                    // Guardar configuración
                    Localization.CurrentLang = lang;
                    Properties.Settings.Default.Idioma = lang;
                    Properties.Settings.Default.Save();

                    // 3. ACTUALIZACIÓN EN CALIENTE (HOT-SWAP)
                    // Esto cambia las propiedades .Text de los controles existentes
                    // SIN destruir ni recrear los paneles. Cero parpadeo.

                    ActualizarTextosLoginYRegistro(); // Método nuevo que creamos antes
                    ActualizarTextosIdioma();         // Método general de la app
                }
            };

            // 4. Selección Inicial Correcta
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

        // Método único para dibujar combos con banderas (Flags)
        private void DibujarComboConBanderas(object s, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            var combo = s as ComboBox;
            string text = combo.Items[e.Index].ToString();
            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

            using (var brush = new SolidBrush(isSelected ? cAccentRed : cInputBackground)) e.Graphics.FillRectangle(brush, e.Bounds);

            Image flag = null;
            if (text.Contains("Español") || text.Contains("ES")) flag = Properties.Resources.flag_es;
            else if (text.Contains("English") || text.Contains("EN")) flag = Properties.Resources.flag_en;
            else if (text.Contains("Português") || text.Contains("PT")) flag = Properties.Resources.flag_pt;
            else if (text.Contains("Русский") || text.Contains("RU")) flag = Properties.Resources.flag_ru;
            else if (text.Contains("中文") || text.Contains("CN")) flag = Properties.Resources.flag_ch;

            if (flag != null) e.Graphics.DrawImage(flag, new Rectangle(e.Bounds.X + 10, e.Bounds.Y + (e.Bounds.Height - 20) / 2, 20, 20));
            using (var textBrush = new SolidBrush(cTextPrimary)) e.Graphics.DrawString(text, combo.Font, textBrush, new Point(40, e.Bounds.Y + 5));
        }

        private void AdjuntarMedidorFortaleza(Panel parent, TextBox txtPassword, int x, int y, int w)
        {
            var pnlBack = new Panel { Location = new Point(x, y), Size = new Size(w, 4), BackColor = Color.FromArgb(40, 40, 40) };
            var pnlFill = new Panel { Location = new Point(0, 0), Size = new Size(0, 4), BackColor = cAccentRed };
            pnlBack.Controls.Add(pnlFill); parent.Controls.Add(pnlBack);

            var lblStatus = new Label { Text = "", Location = new Point(x, y + 8), AutoSize = true, Font = new Font("Segoe UI", 8, FontStyle.Bold), ForeColor = Color.DimGray };
            parent.Controls.Add(lblStatus);

            txtPassword.TextChanged += (s, e) =>
            {
                int score = AuthService.CalcularFortaleza(txtPassword.Text);
                Color c = Color.DimGray; int pct = 0;
                switch (score)
                {
                    case 0: pct = 5; c = Color.FromArgb(100, 30, 30); break;
                    case 1: pct = 20; c = Color.IndianRed; break;
                    case 2: pct = 40; c = Color.Orange; break;
                    case 3: pct = 60; c = Color.Gold; break;
                    case 4: pct = 80; c = Color.YellowGreen; break;
                    case 5: pct = 100; c = Color.LimeGreen; break;
                }
                pnlFill.Width = (w * pct) / 100; pnlFill.BackColor = c;
                lblStatus.Text = Localization.Get("str_" + score); lblStatus.ForeColor = c;
            };
        }

        #endregion
    }
}