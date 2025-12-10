using DokanNet;

namespace FolderLocker
{
    public partial class FormCarpetas : Form
    {
        #region 1. VARIABLES DE ESTADO

        // Control de unidades montadas (Ruta Física -> Letra Unidad)
        private Dictionary<string, string> montajesActivos = new Dictionary<string, string>();

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
            this.MaximizedBounds = Screen.FromHandle(this.Handle).WorkingArea;
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
            // Validaciones Básicas (Igual que antes)
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

            // Validaciones de Usuario
            if (!UserManager.Login(UserManager.CurrentUser.Username, txtContrasena.Text))
            {
                DarkDialogs.ShowInfo(Localization.Get("msg_pass_wrong"), Localization.Get("title_error"));
                return;
            }

            // --- CAMBIO 1: REGISTRO PREVENTIVO (SAFETY FIRST) ---
            // Si ocurre un apagón, la carpeta YA estará en la lista del usuario.
            // Esto permite que al reiniciar, el usuario vea la carpeta y pueda darle a "Restaurar" 
            // para arreglar cualquier archivo a medio procesar.

            bool esNuevaProteccion = !UserManager.CurrentUser.LockedFolders.Contains(txtRuta.Text);

            if (esNuevaProteccion)
            {
                // Agregamos a la lista visualmente y en DB antes de tocar un solo byte
                UserManager.CurrentUser.LockedFolders.Add(txtRuta.Text);
                UserManager.SaveDatabase();
            }
            else
            {
                // Si ya estaba en la lista, avisamos pero permitimos continuar (Modo Reparación/Reintento)
                // Esto es vital para tu escenario: Si se fue la luz, el usuario vuelve a darle "Proteger" para terminar el trabajo.
            }

            // Ocultamos la carpeta INMEDIATAMENTE para que Windows no indexe mientras ciframos
            try
            {
                CrearMarcador(txtRuta.Text); // Crea el locker.id con el OWNER
                new DirectoryInfo(txtRuta.Text).Attributes = FileAttributes.Hidden | FileAttributes.System;
            }
            catch { }

            // UI Procesando
            ConfigurarUIProcesando(true);

            try
            {
                InicializarBarraProgreso();

                var progressHandler = new Progress<Tuple<int, string>>(data =>
                {
                    if (progresoActivo != null && !progresoActivo.IsDisposed)
                        progresoActivo.Actualizar(data.Item1, data.Item2);
                });

                // Verificamos propiedad si ya existe el marcador
                if (EsCarpetaYaProtegidaFisicamente(txtRuta.Text) && !EsElPropietario(txtRuta.Text))
                {
                    CerrarBarraProgreso();
                    DarkDialogs.ShowInfo(Localization.Get("err_not_owner"), Localization.Get("title_security"));
                    return;
                }

                // Llamamos al nuevo proceso blindado
                await ProcesarArchivosAsync(txtRuta.Text, txtContrasena.Text, true, progressHandler);

                CerrarBarraProgreso();

                if (!this.Visible)
                {
                    trayIcon.ShowBalloonTip(5000, Localization.Get("tray_done"), Localization.Get("tray_done_lock"), ToolTipIcon.Info);
                    RestaurarVentana();
                }
                else
                {
                    DarkDialogs.ShowInfo(Localization.Get("msg_lock_success"), Localization.Get("title_success"));
                }

                txtContrasena.Text = "";
                txtRuta.Text = "";

                // Refrescamos listas si están visibles
                if (panelMontar.Visible) ActualizarYMostrarPanelMontar();
                if (panelRestaurar.Visible) ActualizarYMostrarPanelRestaurar();
            }
            catch (Exception ex)
            {
                CerrarBarraProgreso();
                // Si falla algo crítico, NO quitamos la carpeta de la lista.
                // Es mejor que el usuario tenga acceso a "Restaurar" para intentar arreglarlo.
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
            progresoActivo.Show();
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

        private void MontarMotorDokan(string ruta, string letra, string password)
        {
            Thread t = new Thread(() =>
            {
                try
                {
                    // Instancia Dokan para manipular el punto de montaje
                    var dokan = new DokanNet.Dokan(null);

                    // Eliminar montajes anteriores con la misma letra (evita errores de caché)
                    try { dokan.RemoveMountPoint(letra); } catch { }

                    var espejo = new Mirror(ruta, password);

                    // --- CONFIGURACIÓN DEL MOTOR DOKAN ---
                    var builder = new DokanInstanceBuilder(dokan)
                    .ConfigureOptions(o =>
                    {
                        // 1. Asignamos opciones BASE
                        o.Options = DokanOptions.RemovableDrive;

                        // 2. IMPORTANTE: Usamos "|=" para AGREGAR, no "=" para reemplazar
                        o.Options |= DokanOptions.MountManager;
                        // o.Options |= DokanOptions.WriteProtection; // <--- OJO: Si activas esto, NO podrás guardar archivos ni crear carpetas. Solo actívalo si quieres "Solo Lectura".

                        o.MountPoint = letra;
                    });

                    using (var instance = builder.Build(espejo))
                    {
                        // Mantiene la instancia viva hasta desmontar la unidad
                        instance.WaitForFileSystemClosed(uint.MaxValue);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error Dokan: " + ex.Message, "Dokan Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });

            t.IsBackground = true;
            t.Start();

            montajesActivos[ruta] = letra;

            DarkDialogs.ShowInfo(
                string.Format(Localization.Get("msg_mount_success"), letra),
                Localization.Get("title_success")
            );

            txtPassMontar.Text = "";
        }


        private void BtnAccionDesmontar_Click(object sender, EventArgs e)
        {
            if (lstCarpetasParaMontar.SelectedItem == null) { DarkDialogs.ShowInfo(Localization.Get("msg_unmount_select")); return; }

            string rutaSeleccionada = lstCarpetasParaMontar.SelectedItem.ToString();

            if (montajesActivos.ContainsKey(rutaSeleccionada))
            {
                string letraAsociada = montajesActivos[rutaSeleccionada];
                if (DesmontarSilencioso(letraAsociada))
                {
                    montajesActivos.Remove(rutaSeleccionada);
                    DarkDialogs.ShowInfo(string.Format(Localization.Get("msg_unmount_success"), letraAsociada));
                }
                else
                {
                    DarkDialogs.ShowInfo(Localization.Get("msg_unmount_error"));
                }
            }
            else
            {
                DarkDialogs.ShowInfo(Localization.Get("msg_no_vault"));
            }
        }

        private bool DesmontarSilencioso(string letra)
        {
            try
            {
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
                var mapa = new DirectoryMap(rutaBase, motorCifrado, true);

                if (!esEncriptar && File.Exists(Path.Combine(rutaBase, "dir.idx")) && mapa.GetAll().Count == 0)
                {
                    throw new Exception("¡Contraseña Incorrecta! El índice no se puede leer.");
                }

                var archivos = Directory.GetFiles(rutaBase, "*.*", SearchOption.AllDirectories);
                long totalBytes = ObtenerTamanoDirectorio(rutaBase);
                long bytesProcesadosTotal = 0;
                if (totalBytes == 0) totalBytes = 1;

                foreach (var archivoPath in archivos)
                {
                    string nombreArchivoFisico = Path.GetFileName(archivoPath);
                    string nombreLow = nombreArchivoFisico.ToLower();

                    // Limpieza: Si encontramos un .tmp viejo de un apagón anterior, lo ignoramos o borramos
                    if (nombreLow.EndsWith(".tmp"))
                    {
                        try { File.Delete(archivoPath); } catch { }
                        continue;
                    }
                    if (nombreLow == "locker.id" || nombreLow == "dir.idx") continue;

                    try
                    {
                        FileEntry entry = null;

                        // --- FASE 1: IDENTIFICACIÓN ---
                        if (esEncriptar)
                        {
                            entry = mapa.GetByPhysicalName(nombreArchivoFisico);
                            if (entry != null) { bytesProcesadosTotal += new FileInfo(archivoPath).Length; continue; }

                            entry = mapa.GetByRealName(nombreArchivoFisico);
                            if (entry == null) entry = mapa.AddEntry(nombreArchivoFisico, false);
                        }
                        else
                        {
                            entry = mapa.GetByPhysicalName(nombreArchivoFisico);
                            if (entry == null && nombreArchivoFisico.EndsWith(".restored"))
                                entry = mapa.GetByPhysicalName(nombreArchivoFisico.Replace(".restored", ""));

                            // FILTRO DE INOCENCIA (Lo que agregamos antes)
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
                                int bufferSize = 1024 * 1024;
                                byte[] buffer = new byte[bufferSize];
                                int bytesLeidos;
                                long offsetGlobal = 0;

                                while ((bytesLeidos = fsOrigen.Read(buffer, 0, bufferSize)) > 0)
                                {
                                    motorCifrado.TransformarDatos(buffer, offsetGlobal, bytesLeidos);

                                    // Escribimos en el archivo TEMPORAL, no en el original
                                    fsDestino.Write(buffer, 0, bytesLeidos);

                                    offsetGlobal += bytesLeidos;
                                    bytesProcesadosTotal += bytesLeidos;

                                    int p = (int)((bytesProcesadosTotal * 100) / totalBytes);
                                    string estado = esEncriptar ? $"Protegiendo: {nombreArchivoFisico}" : $"Restaurando: {nombreArchivoFisico}";
                                    progreso.Report(Tuple.Create(Math.Min(p, 100), estado));
                                }
                            }
                        }

                        // --- FASE CRÍTICA: EL CAMBIAZO (SWAP BLINDADO) ---
                        // Lógica: "Crear Destino -> Borrar Origen"
                        // Así nunca hay un momento en que el archivo deje de existir.

                        string rutaFinal = archivoPath;

                        // 1. Calcular nombre final
                        if (esEncriptar && entry != null)
                        {
                            rutaFinal = Path.Combine(Path.GetDirectoryName(archivoPath), entry.PhysicalName);
                        }
                        else if (!esEncriptar && entry != null)
                        {
                            rutaFinal = Path.Combine(Path.GetDirectoryName(archivoPath), entry.RealName);
                        }

                        // 2. Limpieza preventiva del destino
                        // Si por un apagón anterior quedó un archivo a medias en el destino, lo quitamos para poder escribir el bueno.
                        if (rutaFinal != archivoPath && File.Exists(rutaFinal))
                        {
                            // Caso especial: Si estamos restaurando y el destino ya existe, usamos "Restored_" para no sobrescribir algo importante
                            if (!esEncriptar)
                            {
                                rutaFinal = Path.Combine(Path.GetDirectoryName(archivoPath), "Restored_" + entry.RealName);
                            }

                            // Si aún así existe (ej: Restored_Video.mp4 ya existe), borramos ese para poner el nuevo recién procesado
                            if (File.Exists(rutaFinal)) File.Delete(rutaFinal);
                        }

                        // 3. MOVEMOS EL TEMPORAL AL FINAL (Aquí nace el archivo seguro)
                        // En este momento exacto, existen TANTO el original (archivoPath) COMO el nuevo (rutaFinal).
                        File.Move(rutaTemp, rutaFinal);

                        // 4. BORRAMOS EL ORIGINAL (Solo si el paso 3 tuvo éxito)
                        // Si se va la luz aquí, tendrás el archivo duplicado (encriptado y desencriptado), pero no perdiste nada.
                        if (rutaFinal != archivoPath)
                        {
                            File.Delete(archivoPath);
                        }

                        // 5. Actualizamos Mapa
                        if (esEncriptar) mapa.GuardarIndice();
                        else if (!esEncriptar && entry != null) mapa.RemoveEntry(entry.RealName);

                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Error crítico archivo: " + ex.Message);
                        // Si falló, intentamos borrar el .tmp para no dejar basura
                        try { if (File.Exists(archivoPath + ".tmp")) File.Delete(archivoPath + ".tmp"); } catch { }
                    }
                }

                // --- FASE 4: FINALIZACIÓN ---
                mapa.GuardarIndice();

                if (!esEncriptar && mapa.GetAll().Count == 0)
                {
                    EliminarArchivoSeguro(Path.Combine(rutaBase, "dir.idx"));
                    EliminarArchivoSeguro(Path.Combine(rutaBase, "locker.id"));
                }

                progreso.Report(Tuple.Create(100, Localization.Get("status_done")));
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
            if (this.WindowState == FormWindowState.Minimized) this.Hide();
            else RecentrarPaneles();
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
                foreach (var kvp in montajesActivos) DesmontarSilencioso(kvp.Value);
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