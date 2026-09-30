using DokanNet;

namespace FolderLocker
{
    public partial class FormCarpetas : Form
    {
        #region 1. VARIABLES DE ESTADO

        // Control de unidades montadas (Ruta Física -> Letra Unidad)
        private Dictionary<string, string> montajesActivos = new Dictionary<string, string>();
        private Dictionary<string, DokanInstance> instanciasDokan = new Dictionary<string, DokanInstance>();

        // Bandera para distinguir entre minimizar al tray y cerrar la app real
        private bool cierreReal = false;

        // Control de concurrencia y bloqueo de operaciones en curso
        private bool _estaProcesando = false;
        private string? _rutaEnProceso = null;

        // Referencia a la ventana de progreso actual
        private DarkProgress? progresoActivo = null;

        #endregion

        #region 2. CONSTRUCTOR E INICIO

        public FormCarpetas()
        {
            this.SetStyle(ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer |
                          ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            this.UpdateStyles();
            this.AllowDrop = true;

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
        }

        private void OnLoginSuccessful(UserProfile? user)
        {
            if (user == null) return;

            _loginView.Visible = false;
            if (PanelOpciones != null) PanelOpciones.Visible = true;
            if (panel1 != null) panel1.Visible = true;

            ActualizarTextosIdioma();
            ModoNormal();
            DarkDialogs.ShowInfo(string.Format(Localization.Get("login_welcome"), user.Username), "Bienvenido", this);
        }

        #endregion

        #region 3. LÓGICA DE BLOQUEO (ENCRIPTACIÓN TRANSACCIONAL Y VALIDACIONES BLINDADAS)

        private async void EjecutarBloqueo(string ruta, string contrasena)
        {
            // 1. Validación de concurrencia: si ya se está procesando algo
            if (_estaProcesando)
            {
                DarkDialogs.ShowInfo("Ya hay una operación de cifrado en curso. Por favor espera a que finalice.", "Operación en Curso", this);
                return;
            }

            if (string.IsNullOrEmpty(ruta)) { DarkDialogs.ShowInfo(Localization.Get("msg_select_dir"), "Info", this); return; }
            if (EsRutaProhibida(ruta, out string errorSeguridad)) { DarkDialogs.ShowInfo(errorSeguridad, Localization.Get("err_security_title"), this); return; }

            string rNorm = Path.GetFullPath(ruta).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            // 2. Validación de integridad: No encriptar una carpeta que esté montada como disco Dokan
            if (montajesActivos.ContainsKey(rNorm) || montajesActivos.Keys.Any(k => string.Equals(Path.GetFullPath(k).TrimEnd('\\'), rNorm, StringComparison.OrdinalIgnoreCase)))
            {
                string letra = montajesActivos.FirstOrDefault(k => string.Equals(Path.GetFullPath(k.Key).TrimEnd('\\'), rNorm, StringComparison.OrdinalIgnoreCase)).Value ?? "activa";
                DarkDialogs.ShowInfo($"La carpeta '{Path.GetFileName(ruta)}' está actualmente montada como unidad virtual ({letra}).\n\nDebes desmontar la unidad antes de poder encriptar la carpeta.", "Carpeta Montada en Uso", this);
                return;
            }

            if (!Directory.Exists(ruta))
            {
                try { Directory.CreateDirectory(ruta); }
                catch { DarkDialogs.ShowInfo("No se pudo encontrar ni crear la carpeta.", "Error", this); return; }
            }

            // 3. Validación de contraseña
            if (!UserManager.Login(UserManager.CurrentUser.Username, contrasena))
            {
                DarkDialogs.ShowInfo(Localization.Get("msg_pass_wrong"), Localization.Get("title_error"), this);
                return;
            }

            // 4. Validación de permisos de escritura en la carpeta destino
            try
            {
                string testPath = Path.Combine(ruta, ".folderlocker_perm_test.tmp");
                File.WriteAllText(testPath, "test");
                File.Delete(testPath);
            }
            catch (UnauthorizedAccessException)
            {
                DarkDialogs.ShowInfo("No tienes permisos suficientes de escritura en esta carpeta.\n\nEjecuta FolderLocker como Administrador o cambia los permisos de la carpeta.", "Permiso Denegado", this);
                return;
            }
            catch (Exception ex)
            {
                DarkDialogs.ShowInfo("No se pudo verificar el acceso a la carpeta: " + ex.Message, "Error de Acceso", this);
                return;
            }

            // 5. Escaneo rápido y resumen previo de la carpeta
            int totalArchivos = 0;
            long totalBytes = 0;

            try
            {
                foreach (var f in Directory.EnumerateFiles(ruta, "*.*", SearchOption.AllDirectories))
                {
                    string n = Path.GetFileName(f).ToLowerInvariant();
                    if (n != "locker.id" && n != "dir.idx" && !n.EndsWith(".tmp"))
                    {
                        try
                        {
                            totalBytes += new FileInfo(f).Length;
                            totalArchivos++;
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                DarkDialogs.ShowInfo("Error al escanear la carpeta: " + ex.Message, "Error", this);
                return;
            }

            if (totalArchivos == 0)
            {
                DarkDialogs.ShowInfo("La carpeta está vacía. No se puede proteger.", "Carpeta vacía", this);
                return;
            }

            // 6. Validación de espacio en disco disponible
            try
            {
                string driveRoot = Path.GetPathRoot(Path.GetFullPath(ruta))!;
                var driveInfo = new DriveInfo(driveRoot);
                long espacioRequerido = totalBytes + (50L * 1024 * 1024); // Margen de 50 MB
                if (driveInfo.AvailableFreeSpace < espacioRequerido)
                {
                    DarkDialogs.ShowInfo($"Espacio insuficiente en disco ({driveRoot}).\n\nSe requieren al menos {FormatearTamano(espacioRequerido)} de espacio libre para completar el cifrado seguro, pero el disco solo tiene {FormatearTamano(driveInfo.AvailableFreeSpace)} disponibles.", "Espacio Insuficiente", this);
                    return;
                }
            }
            catch { }

            // 7. Estimación de tiempo y formato de datos
            string tamanoTexto = FormatearTamano(totalBytes);
            string tiempoEstimado = EstimarTiempo(totalArchivos, totalBytes);
            bool esVolumenGrande = totalArchivos >= 500 || totalBytes >= 500L * 1024 * 1024; // >500 archivos o >500 MB

            // 8. Diálogo visual profesional con KPIs, tarjeta de ruta y callout
            if (DarkDialogs.ShowPreScanSummary(this, ruta, totalArchivos, tamanoTexto, tiempoEstimado, esVolumenGrande) != DialogResult.Yes)
            {
                return;
            }

            bool esNuevaProteccion = !UserManager.CurrentUser.LockedFolders.Contains(ruta);

            if (esNuevaProteccion)
            {
                UserManager.CurrentUser.LockedFolders.Add(ruta);
                UserManager.SaveDatabase();
            }

            try
            {
                CrearMarcador(ruta);
                new DirectoryInfo(ruta).Attributes = FileAttributes.Hidden | FileAttributes.System;
            }
            catch { }

            // 🔒 BLOQUEO TOTAL DE LA UI TRASERA: Nadie puede hacer clic en botones, cambiar de pestaña o arrastrar
            BloquearUIProcesando(true, ruta);

            try
            {
                InicializarBarraProgreso();

                var progressHandler = new Progress<Tuple<int, string>>(data =>
                {
                    if (progresoActivo != null && !progresoActivo.IsDisposed)
                        progresoActivo.Actualizar(data.Item1, data.Item2);
                });

                if (EsCarpetaYaProtegidaFisicamente(ruta) && !EsElPropietario(ruta))
                {
                    CerrarBarraProgreso();
                    DarkDialogs.ShowInfo(Localization.Get("err_not_owner"), Localization.Get("title_security"), this);
                    return;
                }

                await ProcesarArchivosAsync(ruta, contrasena, true, progressHandler);

                CerrarBarraProgreso();

                trayIcon.ShowBalloonTip(3000, "FolderLocker", Localization.Get("msg_lock_success"), ToolTipIcon.None);

                if (!this.Visible)
                {
                    RestaurarVentana();
                }
                else
                {
                    ForzarPrimerPlano();
                    DarkDialogs.ShowInfo(Localization.Get("msg_lock_success"), Localization.Get("title_success"), this);
                }

                _protegerView.LimpiarCampos();

                if (_montarView.Visible) ActualizarYMostrarPanelMontar();
                if (_restaurarView.Visible) ActualizarYMostrarPanelRestaurar();
            }
            catch (Exception ex)
            {
                CerrarBarraProgreso();
                DarkDialogs.ShowInfo("Hubo una interrupción: " + ex.Message + "\n\nLa carpeta se ha guardado en tu lista para que puedas intentar Restaurarla o Protegerla nuevamente.", "Aviso", this);
            }
            finally
            {
                CerrarBarraProgreso();
                BloquearUIProcesando(false);
            }
        }

        #endregion

        #region 4. LÓGICA DE DESBLOQUEO (RESTAURACIÓN Y VALIDACIONES)

        private async void EjecutarRestauracion(string rutaSeleccionada)
        {
            if (_estaProcesando)
            {
                DarkDialogs.ShowInfo("Ya hay una operación en curso. Por favor espera a que finalice.", "Operación en Curso", this);
                return;
            }

            if (string.IsNullOrEmpty(rutaSeleccionada)) { DarkDialogs.ShowInfo(Localization.Get("msg_select_restore"), "Info", this); return; }

            if (!Directory.Exists(rutaSeleccionada))
            {
                DarkDialogs.ShowInfo($"La carpeta '{rutaSeleccionada}' ya no existe físicamente en el disco.", "Carpeta no encontrada", this);
                return;
            }

            if (DarkDialogs.ShowConfirm(string.Format(Localization.Get("msg_confirm_decrypt"), rutaSeleccionada), Localization.Get("title_confirm"), this) == DialogResult.Yes)
            {
                string pass = DarkDialogs.ShowInput(Localization.Get("lbl_pass"), Localization.Get("title_security"), true);

                if (!UserManager.Login(UserManager.CurrentUser.Username, pass))
                {
                    DarkDialogs.ShowInfo(Localization.Get("msg_pass_wrong"), Localization.Get("title_error"), this);
                    return;
                }

                string rNorm = Path.GetFullPath(rutaSeleccionada).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (montajesActivos.ContainsKey(rNorm) || montajesActivos.Keys.Any(k => string.Equals(Path.GetFullPath(k).TrimEnd('\\'), rNorm, StringComparison.OrdinalIgnoreCase)))
                {
                    string letraAsociada = montajesActivos.FirstOrDefault(k => string.Equals(Path.GetFullPath(k.Key).TrimEnd('\\'), rNorm, StringComparison.OrdinalIgnoreCase)).Value;
                    if (!string.IsNullOrEmpty(letraAsociada))
                    {
                        DesmontarSilencioso(letraAsociada, rutaSeleccionada);
                        montajesActivos.Remove(rutaSeleccionada);
                    }
                }

                // 🔒 BLOQUEO TOTAL DE LA UI TRASERA
                BloquearUIProcesando(true, rutaSeleccionada);

                try
                {
                    InicializarBarraProgreso();

                    var progressHandler = new Progress<Tuple<int, string>>(data =>
                    {
                        if (progresoActivo != null && !progresoActivo.IsDisposed)
                            progresoActivo.Actualizar(data.Item1, data.Item2);
                    });

                    if (Directory.Exists(rutaSeleccionada))
                    {
                        new DirectoryInfo(rutaSeleccionada).Attributes = FileAttributes.Normal;

                        await ProcesarArchivosAsync(rutaSeleccionada, pass, false, progressHandler);

                        string idFile = Path.Combine(rutaSeleccionada, "locker.id");
                        if (File.Exists(idFile))
                        {
                            File.SetAttributes(idFile, FileAttributes.Normal);
                            File.Delete(idFile);
                        }
                    }

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
                catch (Exception ex)
                {
                    CerrarBarraProgreso();
                    DarkDialogs.ShowInfo("Error al restaurar: " + ex.Message, "Error", this);
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

            // Bloquear o desbloquear navegación del sidebar
            if (btnCarpetas != null) btnCarpetas.Enabled = !bloqueado;
            if (btnMenuAbrir != null) btnMenuAbrir.Enabled = !bloqueado;
            if (btnExplorador != null) btnExplorador.Enabled = !bloqueado;
            if (btnManual != null) btnManual.Enabled = !bloqueado;
            if (btnSetup != null) btnSetup.Enabled = !bloqueado;
            if (btnSalir != null) btnSalir.Enabled = !bloqueado;

            // Bloquear o desbloquear pestañas superiores
            if (btnProteger != null) btnProteger.Enabled = !bloqueado;
            if (btnDejarDeProteger != null) btnDejarDeProteger.Enabled = !bloqueado;

            // Bloquear arrastrar y soltar
            this.AllowDrop = !bloqueado;

            // Notificar a las vistas activas
            if (_protegerView != null) _protegerView.ConfigurarProcesando(bloqueado);
            if (_restaurarView != null) _restaurarView.ConfigurarProcesando(bloqueado);

            this.Cursor = bloqueado ? Cursors.WaitCursor : Cursors.Default;
        }

        private void InicializarBarraProgreso()
        {
            progresoActivo = new DarkProgress();
            progresoActivo.OnMinimizarAlTray += (s, args) =>
            {
                this.Hide();
                trayIcon.ShowBalloonTip(3000, Localization.Get("tray_working"), Localization.Get("tray_working_desc"), ToolTipIcon.None);
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
        }

        #endregion

        #region 6. VIRTUALIZACIÓN (DOKAN Y VALIDACIONES)

        private void EjecutarMontaje(string rutaSeleccionada, string letraDeseada, string password)
        {
            if (string.IsNullOrEmpty(rutaSeleccionada)) { DarkDialogs.ShowInfo(Localization.Get("msg_mount_select"), "Info", this); return; }

            string rNorm = Path.GetFullPath(rutaSeleccionada).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            // Validación de concurrencia: si la carpeta está encriptándose o desencriptándose
            if (_estaProcesando)
            {
                if (_rutaEnProceso != null && string.Equals(Path.GetFullPath(_rutaEnProceso).TrimEnd('\\'), rNorm, StringComparison.OrdinalIgnoreCase))
                {
                    DarkDialogs.ShowInfo("Esta carpeta se está cifrando o descifrando en este momento.\n\nNo se puede montar como disco virtual hasta que la operación termine por completo.", "Carpeta en Proceso", this);
                    return;
                }
                DarkDialogs.ShowInfo("Hay una operación de cifrado en curso en el sistema. Por seguridad, espera a que termine antes de montar unidades virtuales.", "Sistema Ocupado", this);
                return;
            }

            if (string.IsNullOrEmpty(letraDeseada))
            {
                DarkDialogs.ShowInfo(Localization.Get("msg_mount_select_drive") ?? "Selecciona una letra de unidad.", Localization.Get("title_warning"), this);
                return;
            }

            if (montajesActivos.ContainsKey(rutaSeleccionada))
            {
                DarkDialogs.ShowInfo(string.Format(Localization.Get("msg_mount_active"), montajesActivos[rutaSeleccionada]), Localization.Get("title_warning"), this);
                return;
            }

            if (montajesActivos.ContainsValue(letraDeseada) || Directory.Exists(letraDeseada))
            {
                DarkDialogs.ShowInfo(string.Format(Localization.Get("msg_drive_busy"), letraDeseada), Localization.Get("title_warning"), this);
                return;
            }

            if (!UserManager.Login(UserManager.CurrentUser.Username, password))
            {
                DarkDialogs.ShowInfo(Localization.Get("msg_pass_wrong"), Localization.Get("title_error"), this);
                return;
            }

            Properties.Settings.Default.LetraGuardada = letraDeseada;
            Properties.Settings.Default.Save();

            MontarMotorDokan(rutaSeleccionada, letraDeseada, password);
        }

        private bool VerificarDokanDisponible(out string error)
        {
            try
            {
                var dokan = new DokanNet.Dokan(null);
                int version = dokan.Version;
                int driverVersion = dokan.DriverVersion;
                if (driverVersion == 0)
                {
                    error = "El controlador de disco virtual (Dokan) no está activo en el sistema.\n\nSi acabas de instalar el programa, por favor reinicia tu computadora para que Windows inicie el servicio del controlador.";
                    return false;
                }
                error = string.Empty;
                return true;
            }
            catch (DllNotFoundException)
            {
                error = "No se encontró la librería 'dokan2.dll' o faltan componentes de Microsoft Visual C++ Redistributable (x64) en esta computadora.\n\nPor favor reinstala la aplicación usando el instalador oficial de FolderLocker.";
                return false;
            }
            catch (DokanException ex)
            {
                error = "El servicio del controlador Dokan no está en ejecución (" + ex.Message + ").\n\nPor favor reinicia tu computadora o ejecuta la aplicación como Administrador.";
                return false;
            }
            catch (Exception ex)
            {
                error = "No fue posible verificar el servicio de disco virtual: " + ex.Message;
                return false;
            }
        }

        private void MontarMotorDokan(string ruta, string letra, string password)
        {
            if (!VerificarDokanDisponible(out string errorDokan))
            {
                DarkDialogs.ShowInfo(errorDokan, "Controlador Requerido", this);
                return;
            }

            try
            {
                var dokan = new DokanNet.Dokan(null);
                try { dokan.RemoveMountPoint(letra); } catch { }

                var espejo = new Mirror(ruta, password);
                var builder = new DokanInstanceBuilder(dokan)
                    .ConfigureOptions(o =>
                    {
                        o.Options = DokanOptions.RemovableDrive | DokanOptions.MountManager;
                        o.MountPoint = letra;
                    });

                var instance = builder.Build(espejo);
                instanciasDokan[ruta] = instance;
                montajesActivos[ruta] = letra;

                Thread t = new Thread(() =>
                {
                    try
                    {
                        using (instance)
                        {
                            instance.WaitForFileSystemClosed(uint.MaxValue);
                        }
                    }
                    catch { }
                    finally
                    {
                        if (!this.IsDisposed && this.IsHandleCreated)
                        {
                            try
                            {
                                this.BeginInvoke((MethodInvoker)delegate
                                {
                                    instanciasDokan.Remove(ruta);
                                    montajesActivos.Remove(ruta);
                                });
                            }
                            catch { }
                        }
                    }
                });
                t.IsBackground = true;
                t.Start();

                DarkDialogs.ShowInfo(
                    string.Format(Localization.Get("msg_mount_success"), letra),
                    Localization.Get("title_success"),
                    this
                );

                _montarView.LimpiarPassword();
            }
            catch (Exception ex)
            {
                DarkDialogs.ShowInfo("Error al montar la unidad virtual: " + ex.Message, "Error Dokan", this);
            }
        }

        private void EjecutarDesmontaje(string rutaSeleccionada)
        {
            if (_estaProcesando)
            {
                DarkDialogs.ShowInfo("No puedes desmontar unidades mientras hay una operación de cifrado en curso.", "Sistema Ocupado", this);
                return;
            }

            if (string.IsNullOrEmpty(rutaSeleccionada))
            {
                DarkDialogs.ShowInfo(Localization.Get("msg_unmount_select"), Localization.Get("title_warning"), this);
                return;
            }

            if (montajesActivos.ContainsKey(rutaSeleccionada))
            {
                string letraAsociada = montajesActivos[rutaSeleccionada];
                if (DesmontarSilencioso(letraAsociada, rutaSeleccionada))
                {
                    montajesActivos.Remove(rutaSeleccionada);
                    DarkDialogs.ShowInfo(string.Format(Localization.Get("msg_unmount_success"), letraAsociada), Localization.Get("title_success"), this);
                }
                else
                {
                    DarkDialogs.ShowInfo(Localization.Get("msg_unmount_error"), Localization.Get("title_error"), this);
                }
            }
            else
            {
                DarkDialogs.ShowInfo(Localization.Get("msg_no_vault"), Localization.Get("title_warning"), this);
            }
        }

        private bool DesmontarSilencioso(string letra, string? ruta = null)
        {
            try
            {
                if (!string.IsNullOrEmpty(ruta) && instanciasDokan.TryGetValue(ruta, out var instance))
                {
                    try { instance.Dispose(); } catch { }
                    instanciasDokan.Remove(ruta);
                }
                new DokanNet.Dokan(null).RemoveMountPoint(letra);
                return true;
            }
            catch { return false; }
        }

        #endregion

        #region 7. MOTOR DE PROCESAMIENTO (ATOMIC SAVE & TWO-PHASE COMMIT)

        private async System.Threading.Tasks.Task ProcesarArchivosAsync(string rutaBase, string password, bool esEncriptar, IProgress<Tuple<int, string>> progreso)
        {
            await System.Threading.Tasks.Task.Run(() =>
            {
                var motorCifrado = new CryptoService(password);
                var mapa = new DirectoryMap(rutaBase, motorCifrado, autoSave: false);

                if (!esEncriptar && File.Exists(Path.Combine(rutaBase, "dir.idx")) && mapa.GetAll().Count == 0)
                {
                    throw new Exception("¡Contraseña Incorrecta! El índice no se puede leer.");
                }

                var archivos = Directory.GetFiles(rutaBase, "*.*", SearchOption.AllDirectories);

                long totalBytes = 0;
                foreach (var f in archivos)
                {
                    string n = Path.GetFileName(f).ToLowerInvariant();
                    if (n != "locker.id" && n != "dir.idx" && !n.EndsWith(".tmp"))
                    {
                        try { totalBytes += new FileInfo(f).Length; } catch { }
                    }
                }
                if (totalBytes == 0) totalBytes = 1;

                long bytesProcesadosTotal = 0;
                var archivosOriginalesABorrar = new List<string>();
                int archivosEnLote = 0;
                const int LotePuntoDeControl = 500;

                const int BufferSize = 128 * 1024;
                byte[] buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(BufferSize);

                var cronometroUI = System.Diagnostics.Stopwatch.StartNew();
                long ultimoReporteMs = 0;

                try
                {
                    foreach (var archivoPath in archivos)
                    {
                        string nombreArchivoFisico = Path.GetFileName(archivoPath);
                        string nombreLow = nombreArchivoFisico.ToLowerInvariant();

                        if (nombreLow.EndsWith(".tmp"))
                        {
                            try { File.Delete(archivoPath); } catch { }
                            continue;
                        }
                        if (nombreLow == "locker.id" || nombreLow == "dir.idx") continue;

                        string rutaRelativa = Path.GetRelativePath(rutaBase, archivoPath);

                        try
                        {
                            FileEntry? entry = null;

                            if (esEncriptar)
                            {
                                entry = mapa.GetByPhysicalName(nombreArchivoFisico);
                                if (entry != null) { bytesProcesadosTotal += new FileInfo(archivoPath).Length; continue; }

                                entry = mapa.GetByRelativePath(rutaRelativa);
                                if (entry == null) entry = mapa.AddEntry(nombreArchivoFisico, false, rutaRelativa);
                            }
                            else
                            {
                                entry = mapa.GetByPhysicalName(nombreArchivoFisico);
                                if (entry == null && nombreArchivoFisico.EndsWith(".restored"))
                                    entry = mapa.GetByPhysicalName(nombreArchivoFisico.Replace(".restored", ""));

                                bool pareceEncriptado = nombreArchivoFisico.EndsWith(".lock");
                                if (entry == null && !pareceEncriptado)
                                {
                                    bytesProcesadosTotal += new FileInfo(archivoPath).Length;
                                    continue;
                                }
                            }

                            string rutaTemp = archivoPath + ".tmp";

                            using (var fsOrigen = new FileStream(archivoPath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read))
                            {
                                using (var fsDestino = new FileStream(rutaTemp, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.None))
                                {
                                    int bytesLeidos;
                                    long offsetGlobal = 0;

                                    while ((bytesLeidos = fsOrigen.Read(buffer, 0, BufferSize)) > 0)
                                    {
                                        motorCifrado.TransformarDatos(buffer, offsetGlobal, bytesLeidos);
                                        fsDestino.Write(buffer, 0, bytesLeidos);

                                        offsetGlobal += bytesLeidos;
                                        bytesProcesadosTotal += bytesLeidos;

                                        long actualMs = cronometroUI.ElapsedMilliseconds;
                                        if (actualMs - ultimoReporteMs >= 100)
                                        {
                                            ultimoReporteMs = actualMs;
                                            int p = (int)((bytesProcesadosTotal * 100) / totalBytes);
                                            string estado = esEncriptar ? $"Protegiendo: {nombreArchivoFisico}" : $"Restaurando: {nombreArchivoFisico}";
                                            progreso.Report(Tuple.Create(Math.Min(p, 100), estado));
                                        }
                                    }
                                }
                            }

                            string rutaFinal = archivoPath;

                            if (esEncriptar && entry != null)
                            {
                                rutaFinal = Path.Combine(Path.GetDirectoryName(archivoPath)!, entry.PhysicalName);
                            }
                            else if (!esEncriptar && entry != null)
                            {
                                rutaFinal = Path.Combine(Path.GetDirectoryName(archivoPath)!, entry.RealName);
                            }

                            if (rutaFinal != archivoPath && File.Exists(rutaFinal))
                            {
                                if (!esEncriptar && entry != null)
                                {
                                    rutaFinal = Path.Combine(Path.GetDirectoryName(archivoPath)!, "Restored_" + entry.RealName);
                                }
                                if (File.Exists(rutaFinal)) File.Delete(rutaFinal);
                            }

                            File.Move(rutaTemp, rutaFinal);

                            if (rutaFinal != archivoPath)
                            {
                                archivosOriginalesABorrar.Add(archivoPath);
                            }

                            if (!esEncriptar && entry != null)
                            {
                                mapa.RemoveEntryByPhysical(entry.PhysicalName);
                            }

                            archivosEnLote++;

                            if (archivosEnLote >= LotePuntoDeControl)
                            {
                                mapa.GuardarIndice();

                                foreach (var f in archivosOriginalesABorrar)
                                {
                                    try { if (File.Exists(f)) File.Delete(f); } catch { }
                                }
                                archivosOriginalesABorrar.Clear();
                                archivosEnLote = 0;
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine("Error crítico archivo: " + ex.Message);
                            try { if (File.Exists(archivoPath + ".tmp")) File.Delete(archivoPath + ".tmp"); } catch { }
                        }
                    }

                    mapa.GuardarIndice();

                    foreach (var f in archivosOriginalesABorrar)
                    {
                        try { if (File.Exists(f)) File.Delete(f); } catch { }
                    }
                    archivosOriginalesABorrar.Clear();

                    if (!esEncriptar && mapa.GetAll().Count == 0)
                    {
                        EliminarArchivoSeguro(Path.Combine(rutaBase, "dir.idx"));
                        EliminarArchivoSeguro(Path.Combine(rutaBase, "locker.id"));
                    }

                    progreso.Report(Tuple.Create(100, Localization.Get("status_done")));
                }
                finally
                {
                    System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
                }
            });
        }

        private void EliminarArchivoSeguro(string path)
        {
            try
            {
                if (File.Exists(path)) { File.SetAttributes(path, FileAttributes.Normal); File.Delete(path); }
            }
            catch { }
        }

        #endregion

        #region 8. GESTIÓN DE SISTEMA Y NAVEGACIÓN

        private void IntentarAbrirExplorador()
        {
            if (montajesActivos.Count > 0)
            {
                string letraA_Abrir = "";
                if (_montarView.Visible && !string.IsNullOrEmpty(_montarView.CarpetaSeleccionada) && montajesActivos.ContainsKey(_montarView.CarpetaSeleccionada))
                {
                    letraA_Abrir = montajesActivos[_montarView.CarpetaSeleccionada];
                }
                else
                {
                    foreach (var val in montajesActivos.Values) { letraA_Abrir = val; break; }
                }

                if (!string.IsNullOrEmpty(letraA_Abrir))
                {
                    try { System.Diagnostics.Process.Start("explorer.exe", letraA_Abrir); this.WindowState = FormWindowState.Minimized; } catch { }
                }
            }
            else
            {
                ActualizarYMostrarPanelMontar();
                DarkDialogs.ShowInfo(Localization.Get("msg_no_vault"), "Info", this);
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
                DarkDialogs.ShowInfo("Hay una operación de cifrado o descifrado en curso.\n\nPor seguridad para evitar la corrupción de archivos, espera a que finalice.", "Operación en Curso", this);
                return;
            }
            cierreReal = true;
            Application.Exit();
        }

        private void CerrarSesion()
        {
            if (_estaProcesando)
            {
                DarkDialogs.ShowInfo("No puedes cerrar sesión mientras se estén procesando archivos.", "Operación en Curso", this);
                return;
            }

            if (DarkDialogs.ShowConfirm(Localization.Get("msg_logout_confirm"), Localization.Get("title_confirm"), this) == DialogResult.Yes)
            {
                foreach (var kvp in montajesActivos) { DesmontarSilencioso(kvp.Value); }
                montajesActivos.Clear();
                MostrarPanelLogin();
                ActualizarTextosIdioma();
            }
        }

        private void BtnFactoryReset_Click()
        {
            if (_estaProcesando)
            {
                DarkDialogs.ShowInfo("No puedes restablecer datos mientras se estén procesando archivos.", "Operación en Curso", this);
                return;
            }

            if (DarkDialogs.ShowConfirm(Localization.Get("cfg_msg_reset"), Localization.Get("title_warning"), this) == DialogResult.Yes)
            {
                foreach (var kvp in montajesActivos) DesmontarSilencioso(kvp.Value);
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
                DarkDialogs.ShowResultWithCopy(Localization.Get("rec_success_msg"), rec, this);
                _protegerView.Contrasena = rec;
            }
            else
            {
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
                DarkDialogs.ShowInfo("Hay una operación de cifrado o descifrado en curso.\n\nPor seguridad para evitar daños o pérdida de datos, no puedes cerrar la aplicación hasta que termine.", "Operación en Curso", this);
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
                foreach (var kvp in montajesActivos) DesmontarSilencioso(kvp.Value, kvp.Key);
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
                DarkDialogs.ShowInfo("No puedes agregar otra carpeta mientras hay una operación de cifrado en curso.", "Sistema Ocupado", this);
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

        // --- Helpers de Seguridad y Estimación ---

        private string FormatearTamano(long bytes)
        {
            string[] sufijos = { "B", "KB", "MB", "GB", "TB" };
            int i = 0;
            double dBytes = bytes;
            while (dBytes >= 1024 && i < sufijos.Length - 1)
            {
                dBytes /= 1024;
                i++;
            }
            return $"{dBytes:0.##} {sufijos[i]}";
        }

        private string EstimarTiempo(int numArchivos, long bytes)
        {
            double segPorBytes = (double)bytes / (30.0 * 1024 * 1024);
            double segPorArchivos = (double)numArchivos / 600.0;
            double segundosTotales = Math.Max(segPorBytes, segPorArchivos);

            if (segundosTotales < 5)
                return Localization.CurrentLang == "EN" ? "Less than 5 seconds" : "Menos de 5 segundos";
            if (segundosTotales < 60)
                return Localization.CurrentLang == "EN" ? $"Approx. {(int)Math.Ceiling(segundosTotales)} seconds" : $"Aprox. {(int)Math.Ceiling(segundosTotales)} segundos";

            int minutos = (int)Math.Ceiling(segundosTotales / 60.0);
            if (minutos < 60)
                return Localization.CurrentLang == "EN" ? $"Approx. {minutos} minute(s)" : $"Aprox. {minutos} {(minutos == 1 ? "minuto" : "minutos")}";

            int horas = minutos / 60;
            int minsRestantes = minutos % 60;
            return Localization.CurrentLang == "EN" ? $"Approx. {horas}h {minsRestantes}m" : $"Aprox. {horas}h {minsRestantes}m";
        }

        private bool EsElPropietario(string rutaCarpeta)
        {
            try
            {
                string rutaId = Path.Combine(rutaCarpeta, "locker.id");
                if (!File.Exists(rutaId)) return true;
                string contenido = File.ReadAllText(rutaId);
                if (contenido.StartsWith("OWNER:"))
                {
                    string owner = contenido.Substring(6).Trim();
                    return string.Equals(owner, UserManager.CurrentUser.Username, StringComparison.OrdinalIgnoreCase);
                }
                return false;
            }
            catch { return false; }
        }

        private void CrearMarcador(string rutaCarpeta)
        {
            try
            {
                string f = Path.Combine(rutaCarpeta, "locker.id");
                File.WriteAllText(f, "OWNER:" + UserManager.CurrentUser.Username);
                File.SetAttributes(f, FileAttributes.Hidden | FileAttributes.System);
            }
            catch { }
        }

        private bool EsCarpetaYaProtegidaFisicamente(string r) => File.Exists(Path.Combine(r, "locker.id"));

        private bool EsRutaProhibida(string ruta, out string msg)
        {
            msg = "";
            string rNorm = Path.GetFullPath(ruta).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            foreach (string d in Directory.GetLogicalDrives())
                if (rNorm.Equals(Path.GetFullPath(d).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase)) { msg = string.Format(Localization.Get("err_root"), d); return true; }

            if (rNorm.Equals(Path.GetFullPath(Application.StartupPath).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase)) { msg = Localization.Get("err_self"); return true; }

            if (ruta.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.Windows), StringComparison.OrdinalIgnoreCase)) { msg = Localization.Get("err_sys"); return true; }

            return false;
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