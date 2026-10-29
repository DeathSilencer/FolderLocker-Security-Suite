using System.Text.Json;
using FolderLocker;
using FolderLocker.Services.Audit;
using FolderLocker.Services.VirtualDisk;

namespace FolderLocker.Services.Protection
{
    public class FolderProtectionService : IFolderProtectionService
    {
        private const string LockStateFileName = ".fl_lock_state";

        public InterruptedProtectionInfo DetectInterruptedProtection(string path)
        {
            var info = new InterruptedProtectionInfo { Path = path };
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                return info;
            }

            try
            {
                string statePath = Path.Combine(path, LockStateFileName);
                string journalPath = Path.Combine(path, "dir.journal");
                string idxPath = Path.Combine(path, "dir.idx");
                string markerPath = Path.Combine(path, "locker.id");

                bool hasState = File.Exists(statePath);
                bool hasJournal = File.Exists(journalPath);
                bool hasIdx = File.Exists(idxPath);
                bool hasMarker = File.Exists(markerPath);

                info.HasJournal = hasJournal;

                if (hasState)
                {
                    try
                    {
                        string json = File.ReadAllText(statePath);
                        using var doc = JsonDocument.Parse(json);
                        if (doc.RootElement.TryGetProperty("Username", out var userProp))
                        {
                            info.LastUser = userProp.GetString();
                        }
                        if (doc.RootElement.TryGetProperty("StartTime", out var timeProp) &&
                            timeProp.TryGetDateTime(out var dt))
                        {
                            info.InterruptedAt = dt;
                        }
                    }
                    catch { }
                }

                int encryptedCount = 0;
                long encryptedBytes = 0;
                int pendingCount = 0;
                long pendingBytes = 0;

                var allFiles = Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories);
                foreach (var f in allFiles)
                {
                    if (JunkFileFilter.IsJunkOrTemporaryFile(f)) continue;

                    string name = Path.GetFileName(f);
                    if (name.Equals("locker.id", StringComparison.OrdinalIgnoreCase) ||
                        name.Equals("dir.idx", StringComparison.OrdinalIgnoreCase) ||
                        name.Equals("dir.journal", StringComparison.OrdinalIgnoreCase) ||
                        name.Equals(LockStateFileName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    long len = 0;
                    try { len = new FileInfo(f).Length; } catch { }

                    if (name.EndsWith(".lock", StringComparison.OrdinalIgnoreCase))
                    {
                        encryptedCount++;
                        encryptedBytes += len;
                    }
                    else
                    {
                        pendingCount++;
                        pendingBytes += len;
                    }
                }

                info.EncryptedFilesCount = encryptedCount;
                info.EncryptedBytes = encryptedBytes;
                info.PendingFilesCount = pendingCount;
                info.PendingBytes = pendingBytes;

                // Se considera interrumpida si:
                // 1. Existe el archivo de estado de bloqueo en progreso .fl_lock_state, O
                // 2. Existe dir.journal sin terminar, O
                // 3. No tiene locker.id (no finalizó), pero ya contiene archivos .lock o dir.idx
                if (!hasMarker && (hasState || hasJournal || (encryptedCount > 0 && (pendingCount > 0 || hasIdx))))
                {
                    info.IsInterrupted = true;
                }
                else if (hasState && !hasMarker)
                {
                    info.IsInterrupted = true;
                }
            }
            catch (Exception ex)
            {
                SecurityAuditLogger.LogError("INTERRUPTED_CHECK_ERROR", $"Error al verificar interrupción en '{path}': {ex.Message}", ex, path);
            }

            return info;
        }

        public FolderScanResult ScanFolder(string path)
        {
            var result = new FolderScanResult { Path = path };

            if (!Directory.Exists(path))
            {
                return result;
            }

            try
            {
                var interrupted = DetectInterruptedProtection(path);
                result.HasInterruptedProtection = interrupted.IsInterrupted;
                result.AlreadyEncryptedFiles = interrupted.EncryptedFilesCount;
                result.AlreadyEncryptedBytes = interrupted.EncryptedBytes;
                result.PendingFiles = interrupted.PendingFilesCount;
                result.PendingBytes = interrupted.PendingBytes;

                var files = Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories);
                int count = 0;
                long bytes = 0;
                int junkCount = 0;

                foreach (var f in files)
                {
                    if (JunkFileFilter.IsJunkOrTemporaryFile(f))
                    {
                        junkCount++;
                        continue;
                    }

                    string name = Path.GetFileName(f);
                    if (name.Equals("locker.id", StringComparison.OrdinalIgnoreCase) ||
                        name.Equals("dir.idx", StringComparison.OrdinalIgnoreCase) ||
                        name.Equals("dir.journal", StringComparison.OrdinalIgnoreCase) ||
                        name.Equals(LockStateFileName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    count++;
                    try
                    {
                        bytes += new FileInfo(f).Length;
                    }
                    catch { }
                }

                result.TotalFiles = count;
                result.TotalBytes = bytes;
                result.IgnoredJunkFiles = junkCount;
                result.FormattedSize = FormatearTamano(bytes);

                // El tiempo estimado se calcula con los bytes pendientes reales
                long bytesParaEstimacion = interrupted.IsInterrupted ? interrupted.PendingBytes : bytes;
                int archivosParaEstimacion = interrupted.IsInterrupted ? interrupted.PendingFilesCount : count;
                result.EstimatedTime = EstimarTiempo(archivosParaEstimacion, bytesParaEstimacion);
                result.IsLargeVolume = count >= 500 || bytes >= 500L * 1024 * 1024;
            }
            catch (Exception ex)
            {
                SecurityAuditLogger.LogError("FOLDER_SCAN_ERROR", $"Error al escanear carpeta '{path}': {ex.Message}", ex, path);
            }

            return result;
        }

        public ProtectionValidationResult ValidateCanProtect(string path, IVaultService vaultService)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                return ProtectionValidationResult.Fail(Localization.Get("err_dir_missing") ?? "La carpeta especificada no existe.");
            }

            string rNorm = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            // Validar rutas prohibidas (raíz, app, windows)
            if (EsRutaProhibida(rNorm, out string msgProhibida))
            {
                SecurityAuditLogger.LogSecurity("PROTECT_VALIDATION", "Validación de Ruta Prohibida", false, msgProhibida, rNorm);
                return ProtectionValidationResult.Fail(msgProhibida, isCritical: true);
            }

            // Validar que no esté montada como unidad virtual en Dokan
            if (vaultService.IsMounted(rNorm))
            {
                string msgDokan = Localization.Get("err_folder_mounted_dokan");
                SecurityAuditLogger.LogSecurity("PROTECT_VALIDATION", "Conflicto con Dokan", false, msgDokan, rNorm);
                return ProtectionValidationResult.Fail(msgDokan, isCritical: true);
            }

            // Validar permisos reales de escritura en el directorio
            try
            {
                string testFile = Path.Combine(rNorm, $".fl_write_test_{Guid.NewGuid():N}.tmp");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
            }
            catch (Exception ex)
            {
                string msgPermiso = string.Format(Localization.Get("err_insufficient_permissions"), ex.Message);
                SecurityAuditLogger.LogSecurity("PROTECT_VALIDATION", "Prueba de Escritura Fallida", false, msgPermiso, rNorm);
                return ProtectionValidationResult.Fail(msgPermiso, isCritical: true);
            }

            // Validar espacio libre en disco (tamaño de los archivos pendientes + 50 MB de margen de seguridad)
            try
            {
                var scan = ScanFolder(rNorm);
                string root = Path.GetPathRoot(rNorm) ?? "";
                if (!string.IsNullOrEmpty(root))
                {
                    var drive = new DriveInfo(root);
                    long bytesCalculo = scan.HasInterruptedProtection ? scan.PendingBytes : scan.TotalBytes;
                    long espacioRequerido = bytesCalculo + (50L * 1024 * 1024);
                    if (drive.AvailableFreeSpace < espacioRequerido)
                    {
                        string msgEspacio = string.Format(Localization.Get("err_insufficient_disk_space"), FormatearTamano(espacioRequerido), FormatearTamano(drive.AvailableFreeSpace), root);
                        SecurityAuditLogger.LogSecurity("PROTECT_VALIDATION", "Espacio en Disco Insuficiente", false, msgEspacio, rNorm);
                        return ProtectionValidationResult.Fail(msgEspacio, isCritical: true);
                    }
                }
            }
            catch { }

            return ProtectionValidationResult.Ok();
        }

        public ProtectionValidationResult ValidateCanRestore(string path, IVaultService vaultService)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                return ProtectionValidationResult.Fail(Localization.Get("err_dir_missing") ?? "La carpeta protegida no existe o fue eliminada/desconectada.");
            }

            string rNorm = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            // Si está montada, desmontar automáticamente
            if (vaultService.IsMounted(rNorm))
            {
                vaultService.Unmount(rNorm, out _);
            }

            return ProtectionValidationResult.Ok();
        }

        public async System.Threading.Tasks.Task ProtectFolderAsync(string path, string password, IProgress<Tuple<int, string>> progress, System.Threading.ManualResetEventSlim? pauseEvent = null, CancellationToken ct = default)
        {
            var interrupcion = DetectInterruptedProtection(path);
            if (interrupcion.IsInterrupted)
            {
                SecurityAuditLogger.LogSecurity("ENCRYPT_RESUME", "Reanudación de Cifrado Interrumpido", true,
                    $"Reanudando cifrado tras interrupción forzada. Archivos ya protegidos: {interrupcion.EncryptedFilesCount}, Pendientes: {interrupcion.PendingFilesCount}", path);
            }
            else
            {
                SecurityAuditLogger.LogSecurity("ENCRYPT_START", "Inicio de Cifrado Atómico", true, "Iniciando cifrado con Two-Phase Commit", path);
            }

            // Registrar estado de operación en progreso para detección de caídas/cortes
            CrearEstadoOperacion(path);

            // Saneamiento preventivo de archivos basura o temporales no bloqueados
            int saneados = JunkFileFilter.CleanTemporaryFiles(path);
            if (saneados > 0)
            {
                SecurityAuditLogger.LogInfo("ENCRYPT_PRE_CLEANUP", $"Se limpiaron {saneados} archivos temporales/basura antes del cifrado.", path);
            }

            try
            {
                await ProcesarArchivosMotorAsync(path, password, esEncriptar: true, progress, pauseEvent, ct);
                CrearMarcador(path);
                EliminarEstadoOperacion(path);
                SecurityAuditLogger.LogSecurity("ENCRYPT_SUCCESS", "Cifrado Completado", true, "Carpeta protegida exitosamente con Two-Phase Commit", path);
            }
            catch
            {
                // En caso de excepción/cancelación se conserva el estado para poder reanudar
                throw;
            }
        }

        public async System.Threading.Tasks.Task RestoreFolderAsync(string path, string password, IProgress<Tuple<int, string>> progress, System.Threading.ManualResetEventSlim? pauseEvent = null, CancellationToken ct = default)
        {
            SecurityAuditLogger.LogSecurity("DECRYPT_START", "Inicio de Descifrado", true, "Iniciando descifrado y restauración atómica", path);
            await ProcesarArchivosMotorAsync(path, password, esEncriptar: false, progress, pauseEvent, ct);
            QuitarMarcador(path);
            SecurityAuditLogger.LogSecurity("DECRYPT_SUCCESS", "Descifrado Completado", true, "Carpeta restaurada exitosamente a su estado original", path);
        }

        public void CrearMarcador(string path)
        {
            try
            {
                string f = Path.Combine(path, "locker.id");
                string user = UserManager.CurrentUser?.Username ?? "DEFAULT";
                File.WriteAllText(f, "OWNER:" + user);
                File.SetAttributes(f, FileAttributes.Hidden | FileAttributes.System);

                // Ocultar directorio
                var di = new DirectoryInfo(path);
                di.Attributes |= FileAttributes.Hidden | FileAttributes.System;
            }
            catch (Exception ex)
            {
                SecurityAuditLogger.LogWarning("MARKER_CREATE_WARN", $"No se pudo aplicar atributos de ocultación: {ex.Message}", path);
            }
        }

        public void QuitarMarcador(string path)
        {
            try
            {
                EliminarArchivoSeguro(Path.Combine(path, "locker.id"));
                EliminarArchivoSeguro(Path.Combine(path, "dir.idx"));
                EliminarArchivoSeguro(Path.Combine(path, "dir.journal"));
                EliminarArchivoSeguro(Path.Combine(path, LockStateFileName));

                var di = new DirectoryInfo(path);
                di.Attributes &= ~FileAttributes.Hidden;
                di.Attributes &= ~FileAttributes.System;
            }
            catch (Exception ex)
            {
                SecurityAuditLogger.LogWarning("MARKER_REMOVE_WARN", $"No se pudo quitar atributos de ocultación: {ex.Message}", path);
            }
        }

        private static void CrearEstadoOperacion(string path)
        {
            try
            {
                string statePath = Path.Combine(path, LockStateFileName);
                string user = UserManager.CurrentUser?.Username ?? "DEFAULT";
                var stateObj = new
                {
                    Version = "5.2",
                    Status = "Encrypting",
                    Username = user,
                    StartTime = DateTime.Now,
                    TargetPath = path
                };
                string json = JsonSerializer.Serialize(stateObj);
                File.WriteAllText(statePath, json);
                File.SetAttributes(statePath, FileAttributes.Hidden | FileAttributes.System);
            }
            catch { }
        }

        private static void EliminarEstadoOperacion(string path)
        {
            try
            {
                EliminarArchivoSeguro(Path.Combine(path, LockStateFileName));
            }
            catch { }
        }

        public bool EsCarpetaProtegida(string path)
        {
            try
            {
                return File.Exists(Path.Combine(path, "locker.id"));
            }
            catch
            {
                return false;
            }
        }

        private async System.Threading.Tasks.Task ProcesarArchivosMotorAsync(string rutaBase, string password, bool esEncriptar, IProgress<Tuple<int, string>> progreso, System.Threading.ManualResetEventSlim? pauseEvent, CancellationToken ct)
        {
            await System.Threading.Tasks.Task.Run(() =>
            {
                var motorCifrado = new CryptoService(password);
                var mapa = new DirectoryMap(rutaBase, motorCifrado, autoSave: false);

                if (!esEncriptar && File.Exists(Path.Combine(rutaBase, "dir.idx")) && mapa.GetAll().Count == 0)
                {
                    throw new InvalidOperationException("¡Contraseña Incorrecta! El catálogo criptográfico no se puede descifrar.");
                }

                var archivos = Directory.GetFiles(rutaBase, "*.*", SearchOption.AllDirectories);

                long totalBytes = 0;
                foreach (var f in archivos)
                {
                    if (JunkFileFilter.IsJunkOrTemporaryFile(f)) continue;
                    string name = Path.GetFileName(f);
                    if (name.Equals("locker.id", StringComparison.OrdinalIgnoreCase) ||
                        name.Equals("dir.idx", StringComparison.OrdinalIgnoreCase) ||
                        name.Equals("dir.journal", StringComparison.OrdinalIgnoreCase) ||
                        name.Equals(LockStateFileName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    try { totalBytes += new FileInfo(f).Length; } catch { }
                }
                if (totalBytes == 0) totalBytes = 1;

                long bytesProcesadosTotal = 0;
                int archivosEnLote = 0;
                const int LotePuntoDeControl = 20;

                const int BufferSize = 128 * 1024;
                byte[] buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(BufferSize);

                var cronometroUI = System.Diagnostics.Stopwatch.StartNew();
                long ultimoReporteMs = 0;

                try
                {
                    foreach (var archivoPath in archivos)
                    {
                        // 1. Control de Pausa (0% CPU mientras esté en pausa)
                        if (pauseEvent != null && !pauseEvent.IsSet)
                        {
                            progreso.Report(Tuple.Create(-1, Localization.Get("prog_paused") ?? "⏸️ Operación en pausa..."));
                            SecurityAuditLogger.LogInfo("CRYPTO_PAUSE", "Operación pausada por el usuario", rutaBase);
                            pauseEvent.Wait(ct);
                            SecurityAuditLogger.LogInfo("CRYPTO_RESUME", "Operación reanudada por el usuario", rutaBase);
                        }

                        // 2. Control de Cancelación
                        ct.ThrowIfCancellationRequested();

                        string nombreArchivoFisico = Path.GetFileName(archivoPath);
                        string nombreLow = nombreArchivoFisico.ToLowerInvariant();

                        // Omitir archivos de metadatos internos
                        if (nombreLow.Equals("locker.id") ||
                            nombreLow.Equals("dir.idx") ||
                            nombreLow.Equals("dir.journal") ||
                            nombreLow.Equals(LockStateFileName.ToLowerInvariant()))
                        {
                            continue;
                        }

                        // Omitir o limpiar archivos basura o temporales
                        if (JunkFileFilter.IsJunkOrTemporaryFile(archivoPath))
                        {
                            if (esEncriptar && (nombreLow.EndsWith(".tmp") || nombreLow.StartsWith(".fl_tmp_")))
                            {
                                try { File.Delete(archivoPath); } catch { }
                            }
                            continue;
                        }

                        string rutaRelativa = Path.GetRelativePath(rutaBase, archivoPath);

                        try
                        {
                            FileEntry? entry = null;

                            if (esEncriptar)
                            {
                                // A. ¿El archivo ya es un archivo encriptado (.lock)?
                                if (nombreArchivoFisico.EndsWith(".lock", StringComparison.OrdinalIgnoreCase))
                                {
                                    entry = mapa.GetByPhysicalName(nombreArchivoFisico);
                                    if (entry != null)
                                    {
                                        bytesProcesadosTotal += new FileInfo(archivoPath).Length;
                                        continue;
                                    }
                                    else
                                    {
                                        // Archivo con extensión .lock no indexado previamente (ej. interrupción legacy).
                                        // ¡NUNCA RE-ENCRIPTAR! Se omite para garantizar que no se destruyan datos.
                                        SecurityAuditLogger.LogWarning("ENCRYPT_SKIP_LOCK", $"Se omitió '{nombreArchivoFisico}' para prevenir doble cifrado y corrupción de datos.", rutaBase);
                                        bytesProcesadosTotal += new FileInfo(archivoPath).Length;
                                        continue;
                                    }
                                }

                                // B. Archivo en texto plano: verificar si ya existe en catálogo
                                entry = mapa.GetByRelativePath(rutaRelativa);
                                if (entry != null)
                                {
                                    string rutaLockExistente = Path.Combine(Path.GetDirectoryName(archivoPath)!, entry.PhysicalName);
                                    if (File.Exists(rutaLockExistente) && new FileInfo(rutaLockExistente).Length > 0)
                                    {
                                        // Este archivo ya fue cifrado con éxito en una corrida previa interrumpida.
                                        // El archivo original en texto plano quedó pendiente de borrado al ocurrir el cierre.
                                        try
                                        {
                                            File.Delete(archivoPath);
                                            SecurityAuditLogger.LogInfo("ENCRYPT_CLEANUP_ORIGINAL", $"Limpiado original redundante tras interrupción: '{rutaRelativa}' ya que '{entry.PhysicalName}' existe y está protegido.", rutaBase);
                                        }
                                        catch { }

                                        bytesProcesadosTotal += new FileInfo(rutaLockExistente).Length;
                                        continue;
                                    }
                                }

                                // C. Si no existía en el mapa, registrar nueva entrada
                                if (entry == null)
                                {
                                    entry = mapa.AddEntry(nombreArchivoFisico, false, rutaRelativa);
                                }
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

                            using (var fsOrigen = new FileStream(archivoPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                            {
                                using (var fsDestino = new FileStream(rutaTemp, FileMode.Create, FileAccess.Write, FileShare.None))
                                {
                                    int bytesLeidos;
                                    long offsetGlobal = 0;

                                    while ((bytesLeidos = fsOrigen.Read(buffer, 0, BufferSize)) > 0)
                                    {
                                        ct.ThrowIfCancellationRequested();

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

                            if (esEncriptar && entry != null)
                            {
                                // Resguardo atómico inmediato en WAL Journal
                                mapa.AppendJournal(entry);

                                // Limpieza inmediata del archivo original para evitar duplicados en disco
                                if (rutaFinal != archivoPath && File.Exists(archivoPath))
                                {
                                    try { File.Delete(archivoPath); } catch { }
                                }
                            }
                            else if (!esEncriptar && entry != null)
                            {
                                mapa.RemoveEntryByPhysical(entry.PhysicalName);
                                if (rutaFinal != archivoPath && File.Exists(archivoPath))
                                {
                                    try { File.Delete(archivoPath); } catch { }
                                }
                            }

                            archivosEnLote++;

                            if (archivosEnLote >= LotePuntoDeControl)
                            {
                                mapa.GuardarIndice();
                                archivosEnLote = 0;
                            }
                        }
                        catch (Exception ex)
                        {
                            SecurityAuditLogger.LogError("FILE_PROCESS_ERROR", $"Fallo en archivo '{archivoPath}': {ex.Message}", ex, archivoPath);
                            try { if (File.Exists(archivoPath + ".tmp")) File.Delete(archivoPath + ".tmp"); } catch { }
                        }
                    }

                    mapa.GuardarIndice();

                    if (!esEncriptar && mapa.GetAll().Count == 0)
                    {
                        EliminarArchivoSeguro(Path.Combine(rutaBase, "dir.idx"));
                        EliminarArchivoSeguro(Path.Combine(rutaBase, "dir.journal"));
                        EliminarArchivoSeguro(Path.Combine(rutaBase, "locker.id"));
                        EliminarArchivoSeguro(Path.Combine(rutaBase, LockStateFileName));
                    }

                    progreso.Report(Tuple.Create(100, Localization.Get("status_done") ?? "Completado"));
                }
                catch (OperationCanceledException)
                {
                    try { mapa.GuardarIndice(); } catch { }
                    JunkFileFilter.CleanTemporaryFiles(rutaBase);
                    SecurityAuditLogger.LogWarning("CRYPTO_CANCELLED", "Operación cancelada por el usuario. Índice resguardado en estado consistente.", rutaBase);
                    throw;
                }
                finally
                {
                    System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
                }
            }, ct);
        }

        private static void EliminarArchivoSeguro(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.SetAttributes(path, FileAttributes.Normal);
                    File.Delete(path);
                }
            }
            catch { }
        }

        private static bool EsRutaProhibida(string ruta, out string msg)
        {
            msg = "";
            string rNorm = Path.GetFullPath(ruta).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            foreach (string d in Directory.GetLogicalDrives())
            {
                if (rNorm.Equals(Path.GetFullPath(d).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
                {
                    msg = string.Format(Localization.Get("err_root") ?? "No se puede proteger una unidad raíz ({0})", d);
                    return true;
                }
            }

            if (rNorm.Equals(Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
            {
                msg = Localization.Get("err_self") ?? "No puedes proteger la carpeta de la propia aplicación.";
                return true;
            }

            if (ruta.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.Windows), StringComparison.OrdinalIgnoreCase))
            {
                msg = Localization.Get("err_sys") ?? "No puedes proteger carpetas del sistema Windows.";
                return true;
            }

            return false;
        }

        private static string FormatearTamano(long bytes)
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

        private static string EstimarTiempo(int numArchivos, long bytes)
        {
            double segPorBytes = (double)bytes / (30.0 * 1024 * 1024);
            double segPorArchivos = (double)numArchivos / 600.0;
            double segundosTotales = Math.Max(segPorBytes, segPorArchivos);

            if (segundosTotales < 5)
                return Localization.Get("time_less_5s");
            if (segundosTotales < 60)
                return string.Format(Localization.Get("time_approx_seconds"), (int)Math.Ceiling(segundosTotales));

            int minutos = (int)Math.Ceiling(segundosTotales / 60.0);
            if (minutos < 60)
            {
                if (minutos == 1)
                    return Localization.Get("time_approx_minute");
                return string.Format(Localization.Get("time_approx_minutes"), minutos);
            }

            int horas = minutos / 60;
            int minsRestantes = minutos % 60;
            return string.Format(Localization.Get("time_approx_hours_mins"), horas, minsRestantes);
        }
    }
}
