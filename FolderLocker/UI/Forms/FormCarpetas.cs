using System.Diagnostics;
using FolderLocker.Services.Audit;
using FolderLocker.Services.Protection;
using FolderLocker.Services.Security;
using FolderLocker.Services.VirtualDisk;

namespace FolderLocker
{
    public partial class FormCarpetas : Form
    {
        #region 1. VARIABLES DE ESTADO Y SERVICIOS DESACOPLADOS

        // Capa de Servicios Desacoplada
        private readonly IVaultService _vaultService = new VaultService();
        private readonly IFolderProtectionService _protectionService = new FolderProtectionService();
        private readonly IInactivityService _inactivityService;

        // Bandera para distinguir entre minimizar al tray y cerrar la app real
        private bool cierreReal = false;

        // Control de concurrencia y bloqueo de operaciones en curso
        private bool _estaProcesando = false;
        private string? _rutaEnProceso = null;

        // Control de cancelación y pausa en operaciones de cifrado/descifrado
        private CancellationTokenSource? _operacionCts = null;
        private ManualResetEventSlim? _operacionPauseEvent = null;

        // Referencia a la ventana de progreso actual
        private DarkProgress? progresoActivo = null;

        public IInactivityService InactivityService => _inactivityService;

        #endregion

        #region 2. CONSTRUCTOR E INICIO

        public FormCarpetas()
        {
            this.SetStyle(ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer |
                          ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            this.UpdateStyles();
            this.AllowDrop = true;

            int autoLockMin = Properties.Settings.Default.AutoLockMinutos;
            _inactivityService = new InactivityService(autoLockMin);

            InitializeComponent();

            InicializarEstiloProfesional();
            SuscribirEventos();
        }

        private void SuscribirEventos()
        {
            // Navegación Sidebar y Tabs
            btnCarpetas.Click += (s, e) => { if (!_estaProcesando) MostrarPanelProteger(); };
            btnMenuAbrir.Click += (s, e) => { if (!_estaProcesando) ActualizarYMostrarPanelMontar(); };
            btnDejarDeProteger.Click += (s, e) => { if (!_estaProcesando) ActualizarYMostrarPanelRestaurar(); };
            btnProteger.Click += (s, e) => { if (!_estaProcesando) MostrarPanelProteger(); };
            btnManual.Click += (s, e) => { if (!_estaProcesando) MostrarPanelManual(); };
            btnSetup.Click += (s, e) => { if (!_estaProcesando) MostrarPanelConfiguracion(); };
            btnExplorador.Click += (s, e) => { if (!_estaProcesando) IntentarAbrirExplorador(); };
            btnSalir.Click += (s, e) => CerrarSesion();

            // Eventos de Vistas Modulares
            _loginView.LoginSuccessful += OnLoginSuccessful;
            _loginView.GoToRegisterRequested += () => MostrarPanelRegistro();
            _loginView.WindowDragMouseDown += (s, e) => MoverVentana(s, e);

            _registroView.RegistrationCompleted += () => MostrarPanelLogin();
            _registroView.BackToLoginRequested += () => MostrarPanelLogin();
            _registroView.WindowDragMouseDown += (s, e) => MoverVentana(s, e);

            _protegerView.BloquearRequested += (ruta, pass) => EjecutarBloqueo(ruta, pass);
            _protegerView.ForgotPasswordRequested += BtnOlvide_Click;

            _restaurarView.RestaurarRequested += (ruta) => EjecutarRestauracion(ruta);

            _montarView.MontarRequested += (ruta, letra, pass) => EjecutarMontaje(ruta, letra, pass);
            _montarView.DesmontarRequested += (ruta) => EjecutarDesmontaje(ruta);

            _configView.IdiomaChanged += () => ActualizarTextosIdioma();
            _configView.VerCreditosRequested += () => MostrarPanelCreditos();
            _configView.FactoryResetRequested += BtnFactoryReset_Click;
            _configView.AutoLockChanged += (minutos) =>
            {
                _inactivityService.TimeoutMinutes = minutos;
                SecurityAuditLogger.LogInfo("AUTOLOCK_CONFIG", $"Tiempo de auto-bloqueo configurado a {minutos} minutos.");
            };

            _inactivityService.InactivityTimeoutElapsed += OnInactivityTimeout;
            _inactivityService.Start();

            _creditosView.VolverRequested += () =>
            {
                if (PanelOpciones != null) PanelOpciones.Visible = true;
                MostrarPanelConfiguracion();
            };

            _setupView.SetupCompleted += (masterPass) =>
            {
                UserManager.SetMasterPassword(masterPass);
                DarkDialogs.ShowInfo(Localization.Get("cfg_done"), Localization.Get("title_success"), this);
                ModoNormal();
            };
            _setupView.IdiomaChanged += () => ActualizarTextosIdioma();

            // Eventos de servicio
            _vaultService.VaultUnmounted += (ruta) =>
            {
                if (!this.IsDisposed && this.IsHandleCreated)
                {
                    try
                    {
                        this.BeginInvoke((MethodInvoker)delegate
                        {
                            if (_montarView.Visible) ActualizarYMostrarPanelMontar();
                        });
                    }
                    catch { }
                }
            };

            // Drag & Drop
            this.DragEnter += FormCarpetas_DragEnter;
            this.DragDrop += FormCarpetas_DragDrop;

            // System Tray y Ventana
            this.Resize += FormCarpetas_Resize;
            this.Load += FormCarpetas_Load;

            if (itemAbrir != null) itemAbrir.Click += (s, e) => RestaurarVentana();
            if (itemSalir != null) itemSalir.Click += (s, e) => SalirAplicacion();
        }

        private void FormCarpetas_Load(object? sender, EventArgs e)
        {
            this.MinimumSize = new Size(1000, 680);
            this.WindowState = FormWindowState.Maximized;

            string lang = Properties.Settings.Default.Idioma;
            Localization.CurrentLang = string.IsNullOrEmpty(lang) ? "ES" : lang;
            ActualizarTextosIdioma();

            UserManager.LoadDatabase();

            MostrarPanelLogin();
            RecentrarPaneles();

            SecurityAuditLogger.LogInfo("APP_START", "FolderLocker Security Suite inicializada.");
        }

        private void OnLoginSuccessful(UserProfile? user)
        {
            if (user == null) return;

            _loginView.Visible = false;
            _loginView.OcultarNoticiaAutoLock();
            if (PanelOpciones != null) PanelOpciones.Visible = true;
            if (panel1 != null) panel1.Visible = true;

            ActualizarTextosIdioma();
            ModoNormal();

            _inactivityService.ResetTimer();
            SecurityAuditLogger.LogSecurity("USER_LOGIN", "Inicio de Sesión", true, $"Usuario '{user.Username}' autenticado correctamente.");
            DarkDialogs.ShowInfo(string.Format(Localization.Get("login_welcome"), user.Username), Localization.Get("title_welcome"), this);
        }

        #endregion

        #region 3. LÓGICA DE BLOQUEO (SERVICIO DESACOPLADO)

        private async void EjecutarBloqueo(string ruta, string contrasena)
        {
            if (_estaProcesando)
            {
                DarkDialogs.ShowInfo(Localization.Get("msg_busy_operation"), Localization.Get("title_busy"), this);
                return;
            }

            if (string.IsNullOrWhiteSpace(ruta))
            {
                DarkDialogs.ShowInfo(Localization.Get("msg_select_dir"), Localization.Get("title_info"), this);
                return;
            }

            // 1. Validación de contraseña maestra
            if (!UserManager.Login(UserManager.CurrentUser.Username, contrasena))
            {
                SecurityAuditLogger.LogSecurity("AUTH_FAIL", "Intento de Bloqueo", false, "Contraseña maestra incorrecta", ruta);
                DarkDialogs.ShowInfo(Localization.Get("msg_pass_wrong"), Localization.Get("title_error"), this);
                return;
            }

            // 2. Escaneo de la carpeta
            var scan = _protectionService.ScanFolder(ruta);
            if (scan.TotalFiles == 0)
            {
                DarkDialogs.ShowInfo(Localization.Get("msg_empty_folder"), Localization.Get("title_empty_folder"), this);
                return;
            }

            // 3. Validaciones de integridad del servicio
            var validacion = _protectionService.ValidateCanProtect(ruta, _vaultService);
            if (!validacion.IsValid)
            {
                DarkDialogs.ShowInfo(validacion.ErrorMessage, Localization.Get("title_security_validation"), this);
                return;
            }

            // 4. Confirmación visual con KPIs
            if (DarkDialogs.ShowPreScanSummary(this, ruta, scan.TotalFiles, scan.FormattedSize, scan.EstimatedTime, scan.IsLargeVolume, scan.IgnoredJunkFiles) != DialogResult.Yes)
            {
                return;
            }

            bool esNuevaProteccion = !UserManager.CurrentUser.LockedFolders.Contains(ruta);
            if (esNuevaProteccion)
            {
                UserManager.CurrentUser.LockedFolders.Add(ruta);
                UserManager.SaveDatabase();
            }

            BloquearUIProcesando(true, ruta);
            InicializarBarraProgreso();

            try
            {
                var progress = new Progress<Tuple<int, string>>(data =>
                {
                    if (progresoActivo != null && !progresoActivo.IsDisposed)
                    {
                        progresoActivo.Actualizar(data.Item1, data.Item2);
                    }
                });

                await _protectionService.ProtectFolderAsync(ruta, contrasena, progress, _operacionPauseEvent, _operacionCts?.Token ?? CancellationToken.None);

                CerrarBarraProgreso();
                trayIcon.ShowBalloonTip(3000, "FolderLocker", Localization.Get("msg_lock_success"), ToolTipIcon.None);

                RestaurarVentana();
                Application.DoEvents();

                _protegerView.LimpiarCampos();
                ActualizarYMostrarPanelMontar(ruta);
            }
            catch (OperationCanceledException)
            {
                CerrarBarraProgreso();
                SecurityAuditLogger.LogWarning("PROTECT_CANCELLED", "Protección de carpeta cancelada por el usuario", ruta);
                RestaurarVentana();
                DarkDialogs.ShowInfo(Localization.Get("msg_op_cancelled") ?? "La operación de cifrado fue cancelada por el usuario. Los archivos pendientes no fueron modificados.", Localization.Get("title_confirm_cancel"), this);
            }
            catch (Exception ex)
            {
                CerrarBarraProgreso();
                SecurityAuditLogger.LogError("PROTECT_ERROR", "Error inesperado al proteger carpeta", ex, ruta);
                DarkDialogs.ShowInfo(Localization.Get("err_protect_failed") + ": " + ex.Message, Localization.Get("title_critical_error"), this);
            }
            finally
            {
                CerrarBarraProgreso();
                BloquearUIProcesando(false);
            }
        }

        #endregion

        #region 4. LÓGICA DE DESBLOQUEO / RESTAURACIÓN (SERVICIO DESACOPLADO)

        private async void EjecutarRestauracion(string rutaSeleccionada)
        {
            if (_estaProcesando)
            {
                DarkDialogs.ShowInfo(Localization.Get("msg_busy_operation"), Localization.Get("title_busy"), this);
                return;
            }

            if (string.IsNullOrWhiteSpace(rutaSeleccionada))
            {
                DarkDialogs.ShowInfo(Localization.Get("msg_select_restore"), Localization.Get("title_info"), this);
                return;
            }

            var validacion = _protectionService.ValidateCanRestore(rutaSeleccionada, _vaultService);
            if (!validacion.IsValid)
            {
                DarkDialogs.ShowInfo(validacion.ErrorMessage, Localization.Get("title_restore_validation"), this);
                return;
            }

            if (DarkDialogs.ShowConfirm(string.Format(Localization.Get("msg_confirm_decrypt"), rutaSeleccionada), Localization.Get("title_confirm"), this) == DialogResult.Yes)
            {
                string pass = DarkDialogs.ShowInput(Localization.Get("lbl_pass"), Localization.Get("title_security"), true);

                if (!UserManager.Login(UserManager.CurrentUser.Username, pass))
                {
                    SecurityAuditLogger.LogSecurity("AUTH_FAIL", "Intento de Restauración", false, "Contraseña maestra incorrecta", rutaSeleccionada);
                    DarkDialogs.ShowInfo(Localization.Get("msg_pass_wrong"), Localization.Get("title_error"), this);
                    return;
                }

                BloquearUIProcesando(true, rutaSeleccionada);
                InicializarBarraProgreso();

                try
                {
                    var progress = new Progress<Tuple<int, string>>(data =>
                    {
                        if (progresoActivo != null && !progresoActivo.IsDisposed)
                        {
                            progresoActivo.Actualizar(data.Item1, data.Item2);
                        }
                    });

                    await _protectionService.RestoreFolderAsync(rutaSeleccionada, pass, progress, _operacionPauseEvent, _operacionCts?.Token ?? CancellationToken.None);

                    if (UserManager.CurrentUser != null)
                    {
                        UserManager.CurrentUser.LockedFolders.Remove(rutaSeleccionada);
                        UserManager.SaveDatabase();
                    }

                    CerrarBarraProgreso();
                    trayIcon.ShowBalloonTip(5000, Localization.Get("tray_done"), Localization.Get("tray_done_decrypt"), ToolTipIcon.None);

                    RestaurarVentana();
                    Application.DoEvents();

                    DarkDialogs.ShowResultWithCopy(Localization.Get("msg_decrypt_success"), rutaSeleccionada, this);
                    MostrarPanelProteger();
                }
                catch (OperationCanceledException)
                {
                    CerrarBarraProgreso();
                    SecurityAuditLogger.LogWarning("RESTORE_CANCELLED", "Restauración cancelada por el usuario", rutaSeleccionada);
                    RestaurarVentana();
                    DarkDialogs.ShowInfo(Localization.Get("msg_op_cancelled") ?? "La operación de restauración fue cancelada por el usuario.", Localization.Get("title_confirm_cancel"), this);
                }
                catch (Exception ex)
                {
                    CerrarBarraProgreso();
                    SecurityAuditLogger.LogError("RESTORE_ERROR", "Error inesperado al restaurar carpeta", ex, rutaSeleccionada);
                    DarkDialogs.ShowInfo(Localization.Get("err_restore_failed") + ": " + ex.Message, Localization.Get("title_critical_error"), this);
                }
                finally
                {
                    CerrarBarraProgreso();
                    BloquearUIProcesando(false);
                }
            }
        }

        #endregion

        #region 5. HELPERS DE PROGRESO Y BLOQUEO DE UI

        private void BloquearUIProcesando(bool bloqueado, string? ruta = null)
        {
            _estaProcesando = bloqueado;
            _rutaEnProceso = bloqueado ? ruta : null;

            if (btnCarpetas != null) btnCarpetas.Enabled = !bloqueado;
            if (btnMenuAbrir != null) btnMenuAbrir.Enabled = !bloqueado;
            if (btnExplorador != null) btnExplorador.Enabled = !bloqueado;
            if (btnManual != null) btnManual.Enabled = !bloqueado;
            if (btnSetup != null) btnSetup.Enabled = !bloqueado;
            if (btnSalir != null) btnSalir.Enabled = !bloqueado;

            if (btnProteger != null) btnProteger.Enabled = !bloqueado;
            if (btnDejarDeProteger != null) btnDejarDeProteger.Enabled = !bloqueado;

            this.AllowDrop = !bloqueado;

            if (_protegerView != null) _protegerView.ConfigurarProcesando(bloqueado);
            if (_restaurarView != null) _restaurarView.ConfigurarProcesando(bloqueado);

            this.Cursor = bloqueado ? Cursors.WaitCursor : Cursors.Default;
        }

        private void InicializarBarraProgreso()
        {
            _operacionCts = new CancellationTokenSource();
            _operacionPauseEvent = new ManualResetEventSlim(true);

            progresoActivo = new DarkProgress();
            progresoActivo.OnMinimizarAlTray += (s, args) =>
            {
                this.Hide();
                trayIcon.ShowBalloonTip(3000, Localization.Get("tray_working"), Localization.Get("tray_working_desc"), ToolTipIcon.None);
            };

            progresoActivo.OnPausarRequested += (s, args) =>
            {
                _operacionPauseEvent.Reset();
                progresoActivo.SetPaused(true);
            };

            progresoActivo.OnReanudarRequested += (s, args) =>
            {
                _operacionPauseEvent.Set();
                progresoActivo.SetPaused(false);
            };

            progresoActivo.OnCancelarRequested += (s, args) =>
            {
                string msg = Localization.Get("prog_confirm_cancel") ?? "¿Deseas cancelar la operación en curso?\n\nLos archivos procesados hasta ahora se mantendrán protegidos.";
                if (DarkDialogs.ShowConfirm(msg, Localization.Get("title_confirm_cancel"), progresoActivo) == DialogResult.Yes)
                {
                    _operacionPauseEvent.Set(); // Desbloquear si estaba pausado
                    _operacionCts?.Cancel();
                }
            };

            progresoActivo.Show(this);
        }

        private void CerrarBarraProgreso()
        {
            if (progresoActivo != null)
            {
                if (!progresoActivo.IsDisposed) progresoActivo.Dispose();
                progresoActivo = null;
            }

            try { _operacionPauseEvent?.Dispose(); } catch { }
            _operacionPauseEvent = null;

            try { _operacionCts?.Dispose(); } catch { }
            _operacionCts = null;
        }

        #endregion

        #region 6. VIRTUALIZACIÓN (DOKAN Y SERVICIO DESACOPLADO)

        private void EjecutarMontaje(string rutaSeleccionada, string letraDeseada, string password)
        {
            if (string.IsNullOrEmpty(rutaSeleccionada))
            {
                DarkDialogs.ShowInfo(Localization.Get("msg_mount_select"), Localization.Get("title_info"), this);
                return;
            }

            if (_estaProcesando)
            {
                DarkDialogs.ShowInfo(Localization.Get("msg_cant_mount_busy"), Localization.Get("title_system_busy"), this);
                return;
            }

            if (string.IsNullOrEmpty(letraDeseada))
            {
                DarkDialogs.ShowInfo(Localization.Get("msg_mount_select_drive") ?? "Selecciona una letra de unidad.", Localization.Get("title_warning"), this);
                return;
            }

            if (!UserManager.Login(UserManager.CurrentUser.Username, password))
            {
                SecurityAuditLogger.LogSecurity("AUTH_FAIL", "Intento de Montaje de Disco", false, "Contraseña incorrecta", rutaSeleccionada);
                DarkDialogs.ShowInfo(Localization.Get("msg_pass_wrong"), Localization.Get("title_error"), this);
                return;
            }

            Properties.Settings.Default.LetraGuardada = letraDeseada;
            Properties.Settings.Default.Save();

            if (!_vaultService.Mount(rutaSeleccionada, letraDeseada, password, out string errorMessage))
            {
                DarkDialogs.ShowInfo(errorMessage, Localization.Get("title_mount_error"), this);
                return;
            }

            DarkDialogs.ShowInfo(
                string.Format(Localization.Get("msg_mount_success"), letraDeseada),
                Localization.Get("title_success"),
                this
            );

            _montarView.LimpiarPassword();
        }

        private void EjecutarDesmontaje(string rutaSeleccionada)
        {
            if (string.IsNullOrEmpty(rutaSeleccionada))
            {
                DarkDialogs.ShowInfo(Localization.Get("msg_mount_select"), Localization.Get("title_info"), this);
                return;
            }

            if (!_vaultService.Unmount(rutaSeleccionada, out string errorMessage))
            {
                DarkDialogs.ShowInfo(errorMessage, Localization.Get("title_warning"), this);
                return;
            }

            DarkDialogs.ShowInfo(Localization.Get("msg_unmount_success"), Localization.Get("title_success"), this);
        }

        #endregion

        #region 7. GESTIÓN DE SISTEMA Y NAVEGACIÓN

        private void IntentarAbrirExplorador()
        {
            var activeMounts = _vaultService.GetActiveMounts();
            if (activeMounts.Count > 0)
            {
                string letraA_Abrir = "";
                if (_montarView.Visible && !string.IsNullOrEmpty(_montarView.CarpetaSeleccionada) && activeMounts.ContainsKey(_montarView.CarpetaSeleccionada))
                {
                    letraA_Abrir = activeMounts[_montarView.CarpetaSeleccionada];
                }
                else
                {
                    letraA_Abrir = activeMounts.Values.FirstOrDefault() ?? "";
                }

                if (!string.IsNullOrEmpty(letraA_Abrir))
                {
                    try { Process.Start("explorer.exe", letraA_Abrir); return; } catch { }
                }
            }
            else
            {
                ActualizarYMostrarPanelMontar();
                DarkDialogs.ShowInfo(Localization.Get("msg_no_vault"), Localization.Get("title_info"), this);
            }
        }

        private void ActualizarYMostrarPanelRestaurar(string ruta = "")
        {
            MostrarPanelRestaurar(ruta);
            _restaurarView.CargarCarpetas(UserManager.CurrentUser?.LockedFolders ?? Enumerable.Empty<string>(), ruta);
        }

        private void ActualizarYMostrarPanelMontar(string ruta = "")
        {
            MostrarPanelMontar(ruta);
            _montarView.CargarCarpetas(UserManager.CurrentUser?.LockedFolders ?? Enumerable.Empty<string>(), ruta);
        }

        private void SalirAplicacion()
        {
            if (_estaProcesando)
            {
                DarkDialogs.ShowInfo(Localization.Get("msg_cant_action_busy"), Localization.Get("title_busy"), this);
                return;
            }

            _vaultService.UnmountAll();
            SecurityAuditLogger.LogInfo("APP_EXIT", "FolderLocker cerrado por el usuario.");
            cierreReal = true;
            Application.Exit();
        }

        private void CerrarSesion()
        {
            if (_estaProcesando)
            {
                DarkDialogs.ShowInfo(Localization.Get("msg_cant_logout_busy"), Localization.Get("title_busy"), this);
                return;
            }

            if (DarkDialogs.ShowConfirm(Localization.Get("msg_logout_confirm"), Localization.Get("title_confirm"), this) == DialogResult.Yes)
            {
                _vaultService.UnmountAll();
                SecurityAuditLogger.LogSecurity("USER_LOGOUT", "Cierre de Sesión", true, "Sesión cerrada por el usuario.");
                MostrarPanelLogin();
                ActualizarTextosIdioma();
            }
        }

        private void OnInactivityTimeout()
        {
            // Si la aplicación no tiene sesión activa o ya está en login/registro, ignorar
            if (UserManager.CurrentUser == null || _loginView.Visible || _registroView.Visible)
            {
                return;
            }

            // Si hay un proceso de cifrado o descifrado en curso, posponer por seguridad
            if (_estaProcesando)
            {
                _inactivityService.ResetTimer();
                SecurityAuditLogger.LogInfo("AUTO_LOCK_DEFERRED", "Auto-bloqueo pospuesto debido a operación de cifrado activa.");
                return;
            }

            // Desmontar todas las unidades virtuales Dokan para asegurar la privacidad física
            _vaultService.UnmountAll();
            SecurityAuditLogger.LogSecurity("AUTO_LOCK", "Bloqueo por Inactividad", true,
                $"Sesión bloqueada automáticamente tras {_inactivityService.TimeoutMinutes} minutos sin interacción.");

            // Limpieza de campos sensibles
            _protegerView.LimpiarCampos();
            _montarView.LimpiarPassword();
            _loginView.LimpiarPassword();

            MostrarPanelLogin();
            _loginView.MostrarNoticiaAutoLock();
        }

        private void BtnFactoryReset_Click()
        {
            if (_estaProcesando)
            {
                DarkDialogs.ShowInfo(Localization.Get("msg_cant_reset_busy"), Localization.Get("title_busy"), this);
                return;
            }

            if (DarkDialogs.ShowConfirm(Localization.Get("cfg_msg_reset"), Localization.Get("title_warning"), this) == DialogResult.Yes)
            {
                _vaultService.UnmountAll();
                SecurityAuditLogger.LogSecurity("FACTORY_RESET", "Restablecimiento de Fábrica", true, "Base de datos y perfiles eliminados.");
                UserManager.DeleteCurrentUser();
                cierreReal = true;
                Application.Restart();
                Environment.Exit(0);
            }
        }

        private void BtnOlvide_Click()
        {
            if (UserManager.CurrentUser == null) return;
            string codigo = DarkDialogs.ShowInput(Localization.Get("rec_prompt_msg"), Localization.Get("rec_prompt_title"), false);
            if (string.IsNullOrEmpty(codigo)) return;

            string? rec = UserManager.RecoverLoginPassword(UserManager.CurrentUser.Username, codigo.Trim());
            if (rec != null)
            {
                SecurityAuditLogger.LogSecurity("PASSWORD_RECOVERY", "Recuperación de Contraseña", true, "Contraseña recuperada exitosamente con código REC.");
                DarkDialogs.ShowResultWithCopy(Localization.Get("rec_success_msg"), rec, this);
                _protegerView.Contrasena = rec;
            }
            else
            {
                SecurityAuditLogger.LogSecurity("PASSWORD_RECOVERY", "Recuperación de Contraseña", false, "Código REC inválido.");
                DarkDialogs.ShowInfo(Localization.Get("rec_fail_msg"), Localization.Get("title_error"), this);
            }
        }

        // --- Eventos de Ventana y Drag&Drop ---

        private void FormCarpetas_Resize(object? sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();
            }
            else
            {
                if (btnMax != null)
                {
                    btnMax.Text = (this.WindowState == FormWindowState.Maximized) ? "🗗" : "🗖";
                }
                RecentrarPaneles();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Seguridad: Bloquear cierre si hay proceso activo
            if (_estaProcesando)
            {
                DarkDialogs.ShowInfo(Localization.Get("msg_cant_close_busy"), Localization.Get("title_busy"), this);
                e.Cancel = true;
                return;
            }

            if (!cierreReal)
            {
                e.Cancel = true;
                this.WindowState = FormWindowState.Minimized;
                this.Hide();
                trayIcon.ShowBalloonTip(2000, Localization.Get("tray_minimized_title"), Localization.Get("tray_minimized_msg"), ToolTipIcon.None);
            }
            else
            {
                _inactivityService.Dispose();
                _vaultService.UnmountAll();
            }
            base.OnFormClosing(e);
        }

        private void FormCarpetas_DragEnter(object? sender, DragEventArgs e)
        {
            if (_estaProcesando)
            {
                e.Effect = DragDropEffects.None;
                return;
            }
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy; else e.Effect = DragDropEffects.None;
        }

        private void FormCarpetas_DragDrop(object? sender, DragEventArgs e)
        {
            if (_estaProcesando)
            {
                DarkDialogs.ShowInfo(Localization.Get("msg_cant_add_busy"), Localization.Get("title_system_busy"), this);
                return;
            }

            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[]? files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0 && Directory.Exists(files[0]))
                {
                    if (!_protegerView.Visible) MostrarPanelProteger();
                    _protegerView.Ruta = files[0];
                }
                else
                {
                    DarkDialogs.ShowInfo(Localization.Get("err_drag_folder"), Localization.Get("err_drag_folder_title"), this);
                }
            }
        }

        private void ModoConfiguracionInicial()
        {
            btnCarpetas.Visible = false; btnMenuAbrir.Visible = false; btnExplorador.Visible = false; btnSetup.Visible = false; btnManual.Visible = false;
            MostrarPanelSetup();
        }

        private void ModoNormal()
        {
            btnCarpetas.Visible = true;
            btnMenuAbrir.Visible = true;
            btnExplorador.Visible = true;
            btnSetup.Visible = true;
            if (btnManual != null) btnManual.Visible = true;

            MostrarPanelProteger();
            this.PerformLayout();
            RecentrarPaneles();
        }

        #endregion
    }
}