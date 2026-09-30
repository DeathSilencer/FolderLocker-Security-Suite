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

        // Referencia a la ventana de progreso actual (para poder cancelarla o minimizarla)
        private DarkProgress progresoActivo = null;

        #endregion

        #region 2. CONSTRUCTOR E INICIO

        public FormCarpetas()
        {
            // A. Optimización de Renderizado (Evita parpadeos/glitch)
            this.SetStyle(ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer |
                          ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            this.UpdateStyles();
            this.AllowDrop = true;

            InitializeComponent();

            // Llamamos al método de diseño que está en FormCarpetas.UI.cs
            InicializarEstiloProfesional();
            SuscribirEventos();
        }

        private void SuscribirEventos()
        {
            // Navegación
            btnCarpetas.Click += (s, e) => MostrarPanelProteger();
            btnMenuAbrir.Click += (s, e) => ActualizarYMostrarPanelMontar();
            btnDejarDeProteger.Click += (s, e) => ActualizarYMostrarPanelRestaurar();
            btnProteger.Click += (s, e) => MostrarPanelProteger();
            btnManual.Click += (s, e) => MostrarPanelManual();
            btnSetup.Click += (s, e) => MostrarPanelConfiguracion();
            btnExplorador.Click += (s, e) => IntentarAbrirExplorador();

            // Acciones de Usuario
            btnOlvide.Click += BtnOlvide_Click;
            btnSalir.Click += (s, e) => CerrarSesion();
            btnBuscarRuta.Click += BtnBuscarRuta_Click;

            // Acciones Críticas
            btnAccionGuardar.Click += BtnAccionBloquear_Click;
            btnAccionRestaurar.Click += BtnAccionRestaurar_Click;
            btnAccionMontar.Click += BtnAccionMontar_Click;
            btnAccionDesmontar.Click += BtnAccionDesmontar_Click;
            btnFinalizarSetup.Click += BtnFinalizarSetup_Click;

            // Drag & Drop
            this.DragEnter += FormCarpetas_DragEnter;
            this.DragDrop += FormCarpetas_DragDrop;

            // System Tray y Ventana
            this.Resize += FormCarpetas_Resize;
            this.Load += FormCarpetas_Load;

            // Nota: ItemAbrir y ItemSalir se configuran en ConfigurarSystemTray (UI.cs)
            // pero si necesitas lógica extra, añádela aquí.
            if (itemAbrir != null) itemAbrir.Click += (s, e) => RestaurarVentana();
            if (itemSalir != null) itemSalir.Click += (s, e) => SalirAplicacion();
        }

        private void FormCarpetas_Load(object sender, EventArgs e)
        {
            // Configuración de Pantalla
            this.MinimumSize = new Size(1000, 680);
            this.WindowState = FormWindowState.Maximized;

            // 1. Cargar Idioma
            string lang = Properties.Settings.Default.Idioma;
            Localization.CurrentLang = string.IsNullOrEmpty(lang) ? "ES" : lang;
            ActualizarTextosIdioma();

            // 2. Inicializar Datos
            cmbLetraMontar.Items.AddRange(new[] { "M:\\", "Z:\\", "X:\\", "W:\\", "L:\\", "K:\\", "J:\\" });
            UserManager.LoadDatabase();

            // 3. Inicio Seguro
            MostrarPanelLogin();
            RecentrarPaneles();
        }

        #endregion

        #region 3. LÓGICA DE BLOQUEO (ENCRIPTACIÓN TRANSACCIONAL)

        private async void BtnAccionBloquear_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtRuta.Text)) { DarkDialogs.ShowInfo(Localization.Get("msg_select_dir")); return; }
            if (EsRutaProhibida(txtRuta.Text, out string errorSeguridad)) { DarkDialogs.ShowInfo(errorSeguridad, Localization.Get("err_security_title")); return; }

            if (!Directory.Exists(txtRuta.Text))
            {
                try { Directory.CreateDirectory(txtRuta.Text); }
                catch { DarkDialogs.ShowInfo("No se pudo encontrar ni crear la carpeta.", "Error"); return; }
            }

            bool tieneArchivos = Directory.EnumerateFiles(txtRuta.Text, "*.*", SearchOption.AllDirectories).Any();
            if (!tieneArchivos)
            {
                DarkDialogs.ShowInfo("La carpeta está vacía. No se puede proteger.", "Carpeta vacía");
                return;
            }

            if (!UserManager.Login(UserManager.CurrentUser.Username, txtContrasena.Text))
            {
                DarkDialogs.ShowInfo(Localization.Get("msg_pass_wrong"), Localization.Get("title_error"));
                return;
            }

            bool esNuevaProteccion = !UserManager.CurrentUser.LockedFolders.Contains(txtRuta.Text);

            if (esNuevaProteccion)
            {
                UserManager.CurrentUser.LockedFolders.Add(txtRuta.Text);
                UserManager.SaveDatabase();
            }

            try
            {
                CrearMarcador(txtRuta.Text);
                new DirectoryInfo(txtRuta.Text).Attributes = FileAttributes.Hidden | FileAttributes.System;
            }
            catch { }

            ConfigurarUIProcesando(true);

            try
            {
                InicializarBarraProgreso();

                var progressHandler = new Progress<Tuple<int, string>>(data =>
                {
                    if (progresoActivo != null && !progresoActivo.IsDisposed)
                        progresoActivo.Actualizar(data.Item1, data.Item2);
                });

                if (EsCarpetaYaProtegidaFisicamente(txtRuta.Text) && !EsElPropietario(txtRuta.Text))
                {
                    CerrarBarraProgreso();
                    DarkDialogs.ShowInfo(Localization.Get("err_not_owner"), Localization.Get("title_security"));
                    return;
                }

                await ProcesarArchivosAsync(txtRuta.Text, txtContrasena.Text, true, progressHandler);

                CerrarBarraProgreso();

                // 1. Notificación de Windows (Usando texto del diccionario)
                trayIcon.ShowBalloonTip(3000, "FolderLocker", Localization.Get("msg_lock_success"), ToolTipIcon.Info);

                if (!this.Visible)
                {
                    RestaurarVentana();
                }
                else
                {
                    ForzarPrimerPlano();
                    // 2. Ventana emergente (Usando texto del diccionario)
                    // Título: "Éxito" (title_success), Mensaje: "Carpeta encriptada..." (msg_lock_success)
                    DarkDialogs.ShowInfo(Localization.Get("msg_lock_success"), Localization.Get("title_success"), this);
                }

                txtContrasena.Text = "";
                txtRuta.Text = "";

                if (panelMontar.Visible) ActualizarYMostrarPanelMontar();
                if (panelRestaurar.Visible) ActualizarYMostrarPanelRestaurar();
            }
            catch (Exception ex)
            {
                CerrarBarraProgreso();
                DarkDialogs.ShowInfo("Hubo una interrupción: " + ex.Message + "\n\nLa carpeta se ha guardado en tu lista para que puedas intentar Restaurarla o Protegerla nuevamente.");
            }
            finally
            {
                CerrarBarraProgreso();
                ConfigurarUIProcesando(false);
            }
        }


        #endregion

        #region 4. LÓGICA DE DESBLOQUEO (RESTAURACIÓN)

        private async void BtnAccionRestaurar_Click(object sender, EventArgs e)
        {
            if (lstCarpetasRestaurar.SelectedItem == null) { DarkDialogs.ShowInfo(Localization.Get("msg_select_restore")); return; }

            string rutaSeleccionada = lstCarpetasRestaurar.SelectedItem.ToString();

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

                // UI Bloqueada
                btnAccionRestaurar.Enabled = false;
                btnAccionRestaurar.Text = Localization.Get("status_decrypting");

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

                    // Actualizar DB
                    if (UserManager.CurrentUser != null)
                    {
                        UserManager.CurrentUser.LockedFolders.Remove(rutaSeleccionada);
                        UserManager.SaveDatabase();
                    }

                    CerrarBarraProgreso(); // Cerrar antes de mostrar éxito

                    // --- CAMBIO: FLUJO UNIFICADO DE NOTIFICACIÓN ---

                    // 1. Siempre lanzamos la notificación al Tray (Confirmación visual externa)
                    trayIcon.ShowBalloonTip(5000, Localization.Get("tray_done"), Localization.Get("tray_done_decrypt"), ToolTipIcon.Info);

                    // 2. Siempre aseguramos que la ventana esté visible y al frente
                    RestaurarVentana();

                    // Pequeña pausa de seguridad para que la UI termine de pintarse antes de lanzar el popup
                    Application.DoEvents();

                    // 3. Mostramos el resultado "Pegado" a la ventana principal (pasamos 'this')
                    // Al pasar 'this', DarkDialogs usará CenterParent.
                    DarkDialogs.ShowResultWithCopy(Localization.Get("msg_decrypt_success"), rutaSeleccionada, this);

                    // 4. Regresamos al panel principal
                    MostrarPanelProteger();
                }

                catch (Exception ex)
                {
                    // --- CORRECCIÓN CRÍTICA ---
                    // Si hay un error (ej: índice vacío), cerramos la barra INMEDIATAMENTE
                    // antes de mostrar el mensaje de error. Así no se bloquea la UI.
                    CerrarBarraProgreso();

                    DarkDialogs.ShowInfo("Error al restaurar: " + ex.Message);
                }
                finally
                {
                    CerrarBarraProgreso(); // Asegura limpieza final
                    btnAccionRestaurar.Enabled = true;
                    btnAccionRestaurar.Text = Localization.Get("btn_decrypt");
                    Cursor = Cursors.Default;
                }
            }
        }
        #endregion

        #region 5. HELPERS UI

        private void ConfigurarUIProcesando(bool procesando)
        {
            btnAccionGuardar.Enabled = !procesando;
            btnAccionGuardar.Text = procesando ? Localization.Get("status_processing") : Localization.Get("btn_lock");
            Cursor = procesando ? Cursors.WaitCursor : Cursors.Default;
        }

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

        private void BtnAccionMontar_Click(object sender, EventArgs e)
        {
            if (lstCarpetasParaMontar.SelectedItem == null) { DarkDialogs.ShowInfo(Localization.Get("msg_mount_select")); return; }

            // Validación de Unidad
            if (cmbLetraMontar.SelectedItem == null && string.IsNullOrEmpty(cmbLetraMontar.Text))
            {
                DarkDialogs.ShowInfo(Localization.Get("msg_mount_select_drive") ?? "Selecciona una letra de unidad.", Localization.Get("title_warning"));
                return;
            }

            string rutaSeleccionada = lstCarpetasParaMontar.SelectedItem.ToString();
            string letraDeseada = cmbLetraMontar.Text;

            // Validaciones de Estado
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

            // Validación de Contraseña
            if (!UserManager.Login(UserManager.CurrentUser.Username, txtPassMontar.Text))
            {
                DarkDialogs.ShowInfo(Localization.Get("msg_pass_wrong"), Localization.Get("title_error"));
                return;
            }

            // Guardar preferencia
            Properties.Settings.Default.LetraGuardada = letraDeseada;
            Properties.Settings.Default.Save();

            MontarMotorDokan(rutaSeleccionada, letraDeseada, txtPassMontar.Text);
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

                txtPassMontar.Text = "";
            }
            catch (Exception ex)
            {
                DarkDialogs.ShowInfo("Error al montar la unidad virtual: " + ex.Message, "Error Dokan", this);
            }
        }

        private void BtnAccionDesmontar_Click(object sender, EventArgs e)
        {
            if (lstCarpetasParaMontar.SelectedItem == null) { DarkDialogs.ShowInfo(Localization.Get("msg_unmount_select"), Localization.Get("title_warning"), this); return; }

            string rutaSeleccionada = lstCarpetasParaMontar.SelectedItem.ToString();

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

        private bool DesmontarSilencioso(string letra, string ruta = null)
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

        #region 7. MOTOR DE PROCESAMIENTO (ACTUALIZADO: ATOMIC SAVE)

        private async System.Threading.Tasks.Task ProcesarArchivosAsync(string rutaBase, string password, bool esEncriptar, IProgress<Tuple<int, string>> progreso)
        {
            await System.Threading.Tasks.Task.Run(() =>
            {
                var motorCifrado = new CryptoService(password);
                // autoSave: false para permitir procesamiento ultrarrápido en lote
                var mapa = new DirectoryMap(rutaBase, motorCifrado, autoSave: false);

                if (!esEncriptar && File.Exists(Path.Combine(rutaBase, "dir.idx")) && mapa.GetAll().Count == 0)
                {
                    throw new Exception("¡Contraseña Incorrecta! El índice no se puede leer.");
                }

                var archivos = Directory.GetFiles(rutaBase, "*.*", SearchOption.AllDirectories);

                // Cálculo eficiente de bytes totales en una sola pasada
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

                // Two-Phase Commit: Lista de archivos originales pendientes de eliminar tras confirmar el índice
                var archivosOriginalesABorrar = new List<string>();
                int archivosEnLote = 0;
                const int LotePuntoDeControl = 500; // Guarda el índice cada 500 archivos para blindaje ante apagones

                // Buffer reutilizable de 128 KB (óptimo para SSD y previene saturación de memoria/GC)
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

                        // Limpieza: Si encontramos un .tmp de un apagón anterior, se elimina
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

                            // --- FASE 1: IDENTIFICACIÓN O(1) ---
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

                                // Filtro de Inocencia
                                bool pareceEncriptado = nombreArchivoFisico.EndsWith(".lock");
                                if (entry == null && !pareceEncriptado)
                                {
                                    bytesProcesadosTotal += new FileInfo(archivoPath).Length;
                                    continue;
                                }
                            }

                            // --- FASE 2: CRIPTOGRAFÍA SEGURA (ATOMIC SWAP) ---
                            string rutaTemp = archivoPath + ".tmp"; // Archivo de trabajo seguro

                            using (var fsOrigen = new FileStream(archivoPath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read))
                            {
                                using (var fsDestino = new FileStream(rutaTemp, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.None))
                                {
                                    int bytesLeidos;
                                    long offsetGlobal = 0;

                                    while ((bytesLeidos = fsOrigen.Read(buffer, 0, BufferSize)) > 0)
                                    {
                                        motorCifrado.TransformarDatos(buffer, offsetGlobal, bytesLeidos);

                                        // Escribimos en el archivo TEMPORAL
                                        fsDestino.Write(buffer, 0, bytesLeidos);

                                        offsetGlobal += bytesLeidos;
                                        bytesProcesadosTotal += bytesLeidos;

                                        // Regulación de UI: Actualizar máximo cada 100 ms para no saturar Windows Forms
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

                            // --- FASE 3: EL CAMBIAZO (SWAP BLINDADO) ---
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

                            // Mover temporal al destino final
                            File.Move(rutaTemp, rutaFinal);

                            // El original NO se borra aún: se añade a la lista de confirmación
                            if (rutaFinal != archivoPath)
                            {
                                archivosOriginalesABorrar.Add(archivoPath);
                            }

                            if (!esEncriptar && entry != null)
                            {
                                mapa.RemoveEntryByPhysical(entry.PhysicalName);
                            }

                            archivosEnLote++;

                            // --- PUNTO DE CONTROL (CHECKPOINT) CADA 500 ARCHIVOS ---
                            if (archivosEnLote >= LotePuntoDeControl)
                            {
                                // 1. Confirmar y grabar índice en disco de forma atómica
                                mapa.GuardarIndice();

                                // 2. Ahora que el índice está 100% grabado, borramos los archivos originales de este lote
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

                    // --- FASE 4: FINALIZACIÓN Y CONFIRMACIÓN DEL ÚLTIMO LOTE ---
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

        #region 8. GESTIÓN DE SISTEMA Y UTILIDADES

        private void IntentarAbrirExplorador()
        {
            if (montajesActivos.Count > 0)
            {
                string letraA_Abrir = "";
                // Intentar abrir la seleccionada, si no, la primera que encuentre
                if (lstCarpetasParaMontar.Visible && lstCarpetasParaMontar.SelectedItem != null && montajesActivos.ContainsKey(lstCarpetasParaMontar.SelectedItem.ToString()))
                {
                    letraA_Abrir = montajesActivos[lstCarpetasParaMontar.SelectedItem.ToString()];
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

        private void ActualizarYMostrarPanelRestaurar()
        {
            MostrarPanelRestaurar("");
            LlenarListaDesdeSettings(lstCarpetasRestaurar);
        }

        private void ActualizarYMostrarPanelMontar()
        {
            MostrarPanelMontar("");
            LlenarListaDesdeSettings(lstCarpetasParaMontar);
        }

        private void LlenarListaDesdeSettings(ListBox lb)
        {
            lb.Items.Clear();
            if (UserManager.CurrentUser != null && UserManager.CurrentUser.LockedFolders != null)
            {
                foreach (string ruta in UserManager.CurrentUser.LockedFolders) lb.Items.Add(ruta);
            }
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

        private void BtnFactoryReset_Click(object sender, EventArgs e)
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

        private void BtnFinalizarSetup_Click(object sender, EventArgs e)
        {
            string p1 = txtSetupPass.Text;
            if (p1.Length < 4) { DarkDialogs.ShowInfo(Localization.Get("val_min_chars"), Localization.Get("title_error")); return; }
            if (p1 != txtSetupConfirm.Text) { DarkDialogs.ShowInfo(Localization.Get("val_no_match"), Localization.Get("title_error")); return; }
            UserManager.SetMasterPassword(p1);
            DarkDialogs.ShowInfo(Localization.Get("cfg_done"), Localization.Get("title_success"));
            ModoNormal();
        }

        private void BtnOlvide_Click(object sender, EventArgs e)
        {
            if (UserManager.CurrentUser == null) return;
            string codigo = DarkDialogs.ShowInput(Localization.Get("rec_prompt_msg"), Localization.Get("rec_prompt_title"), false);
            if (string.IsNullOrEmpty(codigo)) return;

            string rec = UserManager.RecoverLoginPassword(UserManager.CurrentUser.Username, codigo.Trim());
            if (rec != null) { DarkDialogs.ShowResultWithCopy(Localization.Get("rec_success_msg"), rec); txtContrasena.Text = rec; }
            else { DarkDialogs.ShowInfo(Localization.Get("rec_fail_msg"), Localization.Get("title_error")); }
        }

        private void BtnBuscarRuta_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == DialogResult.OK) txtRuta.Text = fbd.SelectedPath;
            }
        }

        // --- Eventos de Ventana y Drag&Drop ---

        private void FormCarpetas_Resize(object sender, EventArgs e)
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

        private void FormCarpetas_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy; else e.Effect = DragDropEffects.None;
        }

        private void FormCarpetas_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0 && Directory.Exists(files[0]))
            {
                if (!panelFormulario.Visible) MostrarPanelProteger();
                txtRuta.Text = files[0];
            }
            else
            {
                DarkDialogs.ShowInfo(Localization.Get("err_drag_folder"), Localization.Get("err_drag_folder_title"));
            }
        }

        // --- Helpers de Seguridad ---

        private bool EsElPropietario(string rutaCarpeta)
        {
            try
            {
                string rutaId = Path.Combine(rutaCarpeta, "locker.id");
                if (!File.Exists(rutaId)) return true; // Sin ID = Libre
                string contenido = File.ReadAllText(rutaId);
                if (contenido.StartsWith("OWNER:"))
                {
                    string owner = contenido.Substring(6).Trim();
                    return string.Equals(owner, UserManager.CurrentUser.Username, StringComparison.OrdinalIgnoreCase);
                }
                return false; // Archivo antiguo o corrupto = Bloquear
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

            // Raíz de disco
            foreach (string d in Directory.GetLogicalDrives())
                if (rNorm.Equals(Path.GetFullPath(d).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase)) { msg = string.Format(Localization.Get("err_root"), d); return true; }

            // Carpeta de la App
            if (rNorm.Equals(Path.GetFullPath(Application.StartupPath).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase)) { msg = Localization.Get("err_self"); return true; }

            // Windows
            if (ruta.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.Windows), StringComparison.OrdinalIgnoreCase)) { msg = Localization.Get("err_sys"); return true; }

            return false;
        }

        private long ObtenerTamanoDirectorio(string ruta)
        {
            long t = 0;
            try
            {
                foreach (var f in Directory.GetFiles(ruta, "*.*", SearchOption.AllDirectories))
                {
                    string n = Path.GetFileName(f).ToLower();
                    if (n != "locker.id" && n != "dir.idx") t += new FileInfo(f).Length;
                }
            }
            catch { }
            return t;
        }

        // Modos de Vista
        private void ModoConfiguracionInicial()
        {
            btnCarpetas.Visible = false; btnMenuAbrir.Visible = false; btnExplorador.Visible = false; btnSetup.Visible = false; btnManual.Visible = false;
            panelSetup.Visible = true; panelSetup.BringToFront(); panelFormulario.Visible = false;
            RecentrarPaneles();
        }

        private void ModoNormal()
        {
            // Mostrar sidebar y ocultar setup
            btnCarpetas.Visible = true;
            btnMenuAbrir.Visible = true;
            btnExplorador.Visible = true;
            btnSetup.Visible = true;
            if (btnManual != null) btnManual.Visible = true;

            panelSetup.Visible = false;

            // Mostrar el panel por defecto
            MostrarPanelProteger();

            this.PerformLayout();
            RecentrarPaneles();
        }

        #endregion
    }
}