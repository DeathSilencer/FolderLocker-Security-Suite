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
            btnCarpetas.Click += (s, e) => MostrarPanelProteger();
            btnMenuAbrir.Click += (s, e) => ActualizarYMostrarPanelMontar();
            btnDejarDeProteger.Click += (s, e) => ActualizarYMostrarPanelRestaurar();
            btnProteger.Click += (s, e) => MostrarPanelProteger();
            btnManual.Click += (s, e) => MostrarPanelManual();
            btnSetup.Click += (s, e) => MostrarPanelConfiguracion();
            btnExplorador.Click += (s, e) => IntentarAbrirExplorador();
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
                DarkDialogs.ShowInfo(Localization.Get("cfg_done"), Localization.Get("title_success"));
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
            DarkDialogs.ShowInfo(string.Format(Localization.Get("login_welcome"), user.Username));
        }

        #endregion

        #region 3. LÓGICA DE BLOQUEO (ENCRIPTACIÓN TRANSACCIONAL)

        private async void EjecutarBloqueo(string ruta, string contrasena)
        {
            if (string.IsNullOrEmpty(ruta)) { DarkDialogs.ShowInfo(Localization.Get("msg_select_dir")); return; }
            if (EsRutaProhibida(ruta, out string errorSeguridad)) { DarkDialogs.ShowInfo(errorSeguridad, Localization.Get("err_security_title")); return; }

            if (!Directory.Exists(ruta))
            {
                try { Directory.CreateDirectory(ruta); }
                catch { DarkDialogs.ShowInfo("No se pudo encontrar ni crear la carpeta.", "Error"); return; }
            }

            bool tieneArchivos = Directory.EnumerateFiles(ruta, "*.*", SearchOption.AllDirectories).Any();
            if (!tieneArchivos)
            {
                DarkDialogs.ShowInfo("La carpeta está vacía. No se puede proteger.", "Carpeta vacía");
                return;
            }

            if (!UserManager.Login(UserManager.CurrentUser.Username, contrasena))
            {
                DarkDialogs.ShowInfo(Localization.Get("msg_pass_wrong"), Localization.Get("title_error"), this);
                return;
            }

            // --- ESCANEO RÁPIDO Y RESUMEN PREVIO DE LA CARPETA ---
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

            // Estimación y formato
            string tamanoTexto = FormatearTamano(totalBytes);
            string tiempoEstimado = EstimarTiempo(totalArchivos, totalBytes);
            bool esVolumenGrande = totalArchivos >= 500 || totalBytes >= 500L * 1024 * 1024; // >500 archivos o >500 MB

            string tituloConfirm = esVolumenGrande
                ? (Localization.CurrentLang == "EN" ? "⚠️ Warning: Large File Volume" : "⚠️ Advertencia: Gran Volumen de Archivos")
                : (Localization.CurrentLang == "EN" ? "Confirm Protection" : "Confirmar Protección");

            string advertenciaExtra = esVolumenGrande
                ? (Localization.CurrentLang == "EN"
                    ? "\n⚠️ NOTICE: This folder contains a significant number of files or large size. Please verify this is the exact folder you want to protect.\n"
                    : "\n⚠️ AVISO: Esta carpeta contiene una gran cantidad de archivos o peso. Verifica que sea la carpeta correcta antes de continuar.\n")
                : "";

            string mensajeResumen;
            if (Localization.CurrentLang == "EN")
            {
                mensajeResumen = $"Folder Protection Summary:\n\n" +
                                 $"📁 Folder: {Path.GetFileName(ruta)}\n" +
                                 $"📍 Path: {ruta}\n" +
                                 $"📄 Total Files: {totalArchivos:N0}\n" +
                                 $"💾 Total Size: {tamanoTexto}\n" +
                                 $"⏱️ Estimated Time: {tiempoEstimado}\n" +
                                 advertenciaExtra + "\n" +
                                 "Do you want to proceed with encryption?";
            }
            else
            {
                mensajeResumen = $"Resumen de la carpeta a proteger:\n\n" +
                                 $"📁 Carpeta: {Path.GetFileName(ruta)}\n" +
                                 $"📍 Ruta: {ruta}\n" +
                                 $"📄 Total de archivos: {totalArchivos:N0}\n" +
                                 $"💾 Tamaño total: {tamanoTexto}\n" +
                                 $"⏱️ Tiempo estimado: {tiempoEstimado}\n" +
                                 advertenciaExtra + "\n" +
                                 "¿Deseas iniciar la encriptación ahora?";
            }

            if (DarkDialogs.ShowConfirm(mensajeResumen, tituloConfirm, this, ancho: 480, alinearIzquierda: true) != DialogResult.Yes)
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

            _protegerView.ConfigurarProcesando(true);

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
                    DarkDialogs.ShowInfo(Localization.Get("err_not_owner"), Localization.Get("title_security"));
                    return;
                }

                await ProcesarArchivosAsync(ruta, contrasena, true, progressHandler);

                CerrarBarraProgreso();

                trayIcon.ShowBalloonTip(3000, "FolderLocker", Localization.Get("msg_lock_success"), ToolTipIcon.Info);

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
                DarkDialogs.ShowInfo("Hubo una interrupción: " + ex.Message + "\n\nLa carpeta se ha guardado en tu lista para que puedas intentar Restaurarla o Protegerla nuevamente.");
            }
            finally
            {
                CerrarBarraProgreso();
                _protegerView.ConfigurarProcesando(false);
            }
        }

        #endregion

        #region 4. LÓGICA DE DESBLOQUEO (RESTAURACIÓN)

        private async void EjecutarRestauracion(string rutaSeleccionada)
        {
            if (string.IsNullOrEmpty(rutaSeleccionada)) { DarkDialogs.ShowInfo(Localization.Get("msg_select_restore")); return; }

            if (DarkDialogs.ShowConfirm(string.Format(Localization.Get("msg_confirm_decrypt"), rutaSeleccionada), Localization.Get("title_confirm")) == DialogResult.Yes)
            {
                string pass = DarkDialogs.ShowInput(Localization.Get("lbl_pass"), Localization.Get("title_security"), true);

                if (!UserManager.Login(UserManager.CurrentUser.Username, pass))
                {
                    DarkDialogs.ShowInfo(Localization.Get("msg_pass_wrong"), Localization.Get("title_error"));
                    return;
                }

                if (montajesActivos.ContainsKey(rutaSeleccionada))
                {
                    DesmontarSilencioso(montajesActivos[rutaSeleccionada]);
                    montajesActivos.Remove(rutaSeleccionada);
                }

                _restaurarView.ConfigurarProcesando(true);

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

                    trayIcon.ShowBalloonTip(5000, Localization.Get("tray_done"), Localization.Get("tray_done_decrypt"), ToolTipIcon.Info);
                    RestaurarVentana();
                    Application.DoEvents();

                    DarkDialogs.ShowResultWithCopy(Localization.Get("msg_decrypt_success"), rutaSeleccionada, this);
                    MostrarPanelProteger();
                }
                catch (Exception ex)
                {
                    CerrarBarraProgreso();
                    DarkDialogs.ShowInfo("Error al restaurar: " + ex.Message);
                }
                finally
                {
                    CerrarBarraProgreso();
                    _restaurarView.ConfigurarProcesando(false);
                }
            }
        }

        #endregion

        #region 5. HELPERS DE PROGRESO

        private void InicializarBarraProgreso()
        {
            progresoActivo = new DarkProgress();
            progresoActivo.OnMinimizarAlTray += (s, args) =>
            {
                this.Hide();
                trayIcon.ShowBalloonTip(3000, Localization.Get("tray_working"), Localization.Get("tray_working_desc"), ToolTipIcon.Info);
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

        #region 6. VIRTUALIZACIÓN (DOKAN)

        private void EjecutarMontaje(string rutaSeleccionada, string letraDeseada, string password)
        {
            if (string.IsNullOrEmpty(rutaSeleccionada)) { DarkDialogs.ShowInfo(Localization.Get("msg_mount_select")); return; }

            if (string.IsNullOrEmpty(letraDeseada))
            {
                DarkDialogs.ShowInfo(Localization.Get("msg_mount_select_drive") ?? "Selecciona una letra de unidad.", Localization.Get("title_warning"));
                return;
            }

            if (montajesActivos.ContainsKey(rutaSeleccionada))
            {
                DarkDialogs.ShowInfo(string.Format(Localization.Get("msg_mount_active"), montajesActivos[rutaSeleccionada]), Localization.Get("title_warning"));
                return;
            }

            if (montajesActivos.ContainsValue(letraDeseada) || Directory.Exists(letraDeseada))
            {
                DarkDialogs.ShowInfo(string.Format(Localization.Get("msg_drive_busy"), letraDeseada), Localization.Get("title_warning"));
                return;
            }

            if (!UserManager.Login(UserManager.CurrentUser.Username, password))
            {
                DarkDialogs.ShowInfo(Localization.Get("msg_pass_wrong"), Localization.Get("title_error"));
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
                DarkDialogs.ShowInfo(Localization.Get("msg_no_vault"));
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
            cierreReal = true;
            Application.Exit();
        }

        private void CerrarSesion()
        {
            if (DarkDialogs.ShowConfirm(Localization.Get("msg_logout_confirm"), Localization.Get("title_confirm")) == DialogResult.Yes)
            {
                foreach (var kvp in montajesActivos) { DesmontarSilencioso(kvp.Value); }
                montajesActivos.Clear();
                MostrarPanelLogin();
                ActualizarTextosIdioma();
            }
        }

        private void BtnFactoryReset_Click()
        {
            if (DarkDialogs.ShowConfirm(Localization.Get("cfg_msg_reset"), Localization.Get("title_warning")) == DialogResult.Yes)
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
                DarkDialogs.ShowResultWithCopy(Localization.Get("rec_success_msg"), rec);
                _protegerView.Contrasena = rec;
            }
            else
            {
                DarkDialogs.ShowInfo(Localization.Get("rec_fail_msg"), Localization.Get("title_error"));
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
            if (!cierreReal)
            {
                e.Cancel = true;
                this.WindowState = FormWindowState.Minimized;
                this.Hide();
                trayIcon.ShowBalloonTip(2000, Localization.Get("tray_minimized_title"), Localization.Get("tray_minimized_msg"), ToolTipIcon.Info);
            }
            else
            {
                foreach (var kvp in montajesActivos) DesmontarSilencioso(kvp.Value, kvp.Key);
            }
            base.OnFormClosing(e);
        }

        private void FormCarpetas_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy; else e.Effect = DragDropEffects.None;
        }

        private void FormCarpetas_DragDrop(object? sender, DragEventArgs e)
        {
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
                    DarkDialogs.ShowInfo(Localization.Get("err_drag_folder"), Localization.Get("err_drag_folder_title"));
                }
            }
        }

        // --- Helpers de Seguridad ---

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