using FolderLocker;
using FolderLocker.Services.Audit;
using FolderLocker.Services.VirtualDisk;

namespace FolderLocker.Services.Protection
{
    public class FolderProtectionService : IFolderProtectionService
    {
        public FolderScanResult ScanFolder(string path)
        {
            var result = new FolderScanResult { Path = path };

            if (!Directory.Exists(path))
            {
                return result;
            }

            try
            {
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
                result.EstimatedTime = EstimarTiempo(count, bytes);
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
                string msgDokan = "Esta carpeta está actualmente montada como unidad virtual.\n\nPor favor desmonta la unidad virtual antes de intentar protegerla o modificar su cifrado.";
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
                string msgPermiso = $"No tienes permisos suficientes de escritura en esta carpeta:\n{ex.Message}\n\nAsegúrate de tener permisos de administrador o de que los archivos no estén bloqueados.";
                SecurityAuditLogger.LogSecurity("PROTECT_VALIDATION", "Prueba de Escritura Fallida", false, msgPermiso, rNorm);
                return ProtectionValidationResult.Fail(msgPermiso, isCritical: true);
            }

            // Validar espacio libre en disco (tamaño de la carpeta + 50 MB de margen de seguridad)
            try
            {
                var scan = ScanFolder(rNorm);
                string root = Path.GetPathRoot(rNorm) ?? "";
                if (!string.IsNullOrEmpty(root))
                {
                    var drive = new DriveInfo(root);
                    long espacioRequerido = scan.TotalBytes + (50L * 1024 * 1024);
                    if (drive.AvailableFreeSpace < espacioRequerido)
                    {
                        string msgEspacio = $"Espacio insuficiente en disco.\n\nSe requieren aproximadamente {FormatearTamano(espacioRequerido)} libres para un cifrado atómico seguro con Two-Phase Commit, pero solo hay {FormatearTamano(drive.AvailableFreeSpace)} disponibles en la unidad {root}.";
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
            SecurityAuditLogger.LogSecurity("ENCRYPT_START", "Inicio de Cifrado Atómico", true, "Iniciando cifrado con Two-Phase Commit", path);

            // Saneamiento preventivo de archivos basura o temporales no bloqueados
            int saneados = JunkFileFilter.CleanTemporaryFiles(path);
            if (saneados > 0)
            {
                SecurityAuditLogger.LogInfo("ENCRYPT_PRE_CLEANUP", $"Se limpiaron {saneados} archivos temporales/basura antes del cifrado.", path);
            }

            await ProcesarArchivosMotorAsync(path, password, esEncriptar: true, progress, pauseEvent, ct);
            CrearMarcador(path);
            SecurityAuditLogger.LogSecurity("ENCRYPT_SUCCESS", "Cifrado Completado", true, "Carpeta protegida exitosamente con Two-Phase Commit", path);
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

                var di = new DirectoryInfo(path);
                di.Attributes &= ~FileAttributes.Hidden;
                di.Attributes &= ~FileAttributes.System;
            }
            catch (Exception ex)
            {
                SecurityAuditLogger.LogWarning("MARKER_REMOVE_WARN", $"No se pudo quitar atributos de ocultación: {ex.Message}", path);
            }
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
                    try { totalBytes += new FileInfo(f).Length; } catch { }
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
                            SecurityAuditLogger.LogError("FILE_PROCESS_ERROR", $"Fallo en archivo '{archivoPath}': {ex.Message}", ex, archivoPath);
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
    }
}
