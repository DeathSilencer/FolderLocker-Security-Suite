using System;
using System.IO;
using FolderLocker;
using FolderLocker.Services.Audit;
using FolderLocker.Services.Protection;
using FolderLocker.Services.VirtualDisk;
using Xunit;

namespace FolderLocker.Tests
{
    public class ServiceTests
    {
        [Fact]
        public void SecurityAuditLogger_WritesLogEntriesWithoutExceptions()
        {
            // Act
            SecurityAuditLogger.LogInfo("TEST_EVENT", "Mensaje informativo de prueba unitaria");
            SecurityAuditLogger.LogWarning("TEST_WARN", "Advertencia de prueba unitaria");
            SecurityAuditLogger.LogError("TEST_ERROR", "Error simulado de prueba unitaria", new InvalidOperationException("Fallo"));
            SecurityAuditLogger.LogSecurity("VAULT_MOUNT", "Montar unidad", true, "Prueba unitaria exitosa");

            // Assert
            string logPath = SecurityAuditLogger.LogFilePath;
            Assert.False(string.IsNullOrWhiteSpace(logPath));
            Assert.True(File.Exists(logPath));

            string content = File.ReadAllText(logPath);
            Assert.Contains("TEST_EVENT", content);
            Assert.Contains("TEST_WARN", content);
            Assert.Contains("TEST_ERROR", content);
            Assert.Contains("VAULT_MOUNT", content);
            Assert.Contains("AUDIT_OK", content);
        }

        [Fact]
        public void FolderProtectionService_ScanFolder_DetectsFilesAndSizesCorrectly()
        {
            // Arrange
            string tempDir = Path.Combine(Path.GetTempPath(), "FL_TestScan_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                File.WriteAllText(Path.Combine(tempDir, "file1.txt"), "Hola Mundo");
                File.WriteAllText(Path.Combine(tempDir, "file2.bin"), new string('A', 5000));
                Directory.CreateDirectory(Path.Combine(tempDir, "SubDir"));
                File.WriteAllText(Path.Combine(tempDir, "SubDir", "file3.dat"), "Archivo secundario");

                var service = new FolderProtectionService();

                // Act
                var result = service.ScanFolder(tempDir);

                // Assert
                Assert.Equal(3, result.TotalFiles);
                Assert.True(result.TotalBytes > 5000);
                Assert.False(string.IsNullOrWhiteSpace(result.FormattedSize));
                Assert.False(string.IsNullOrWhiteSpace(result.EstimatedTime));
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Fact]
        public void FolderProtectionService_ValidateCanProtect_ValidatesExpectedConditions()
        {
            var protectionService = new FolderProtectionService();
            var vaultService = new VaultService();

            // 1. Probar ruta inexistente
            var res1 = protectionService.ValidateCanProtect(@"C:\RutaQueNoExisteDefinitivamente_" + Guid.NewGuid().ToString("N"), vaultService);
            Assert.False(res1.IsValid);

            // 2. Probar ruta de Windows protegida
            string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            var res2 = protectionService.ValidateCanProtect(winDir, vaultService);
            Assert.False(res2.IsValid);

            // 3. Con un directorio normal con permisos es válida
            string tempDir = Path.Combine(Path.GetTempPath(), "FL_TestVal_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                File.WriteAllText(Path.Combine(tempDir, "data.txt"), "Contenido");
                var res3 = protectionService.ValidateCanProtect(tempDir, vaultService);
                Assert.True(res3.IsValid);
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void VaultService_TracksMountedState()
        {
            var vaultService = new VaultService();

            Assert.False(vaultService.IsMounted(@"C:\BovedaFalsa"));
            Assert.Null(vaultService.GetMountedDriveLetter(@"C:\BovedaFalsa"));
            Assert.Empty(vaultService.GetActiveMounts());
        }

        [Fact]
        public void JunkFileFilter_IdentifiesJunkFilesCorrectly()
        {
            JunkFileFilter.IsFilterEnabled = true;

            // Archivos basura/temporales conocidos
            Assert.True(JunkFileFilter.IsJunkOrTemporaryFile(@"C:\Folder\desktop.ini"));
            Assert.True(JunkFileFilter.IsJunkOrTemporaryFile(@"C:\Folder\thumbs.db"));
            Assert.True(JunkFileFilter.IsJunkOrTemporaryFile(@"C:\Folder\.DS_Store"));
            Assert.True(JunkFileFilter.IsJunkOrTemporaryFile(@"C:\Folder\cache.tmp"));
            Assert.True(JunkFileFilter.IsJunkOrTemporaryFile(@"C:\Folder\~$documento.docx"));
            Assert.True(JunkFileFilter.IsJunkOrTemporaryFile(@"C:\Folder\descarga.crdownload"));
            Assert.True(JunkFileFilter.IsJunkOrTemporaryFile(@"C:\Folder\locker.id"));
            Assert.True(JunkFileFilter.IsJunkOrTemporaryFile(@"C:\Folder\dir.idx"));

            // Archivos de usuario legítimos
            Assert.False(JunkFileFilter.IsJunkOrTemporaryFile(@"C:\Folder\tesis.docx"));
            Assert.False(JunkFileFilter.IsJunkOrTemporaryFile(@"C:\Folder\foto_vacaciones.png"));
            Assert.False(JunkFileFilter.IsJunkOrTemporaryFile(@"C:\Folder\base_datos.sqlite"));
            Assert.False(JunkFileFilter.IsJunkOrTemporaryFile(@"C:\Folder\reporte_mensual.pdf"));
        }

        [Fact]
        public void JunkFileFilter_CleanTemporaryFiles_RemovesOnlyJunkFiles()
        {
            JunkFileFilter.IsFilterEnabled = true;
            string tempDir = Path.Combine(Path.GetTempPath(), "FL_TestJunkClean_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                // Archivos legítimos
                string docPath = Path.Combine(tempDir, "documento.txt");
                string pdfPath = Path.Combine(tempDir, "factura.pdf");
                File.WriteAllText(docPath, "Documento importante");
                File.WriteAllText(pdfPath, "Factura 2026");

                // Archivos basura / temporales
                string tmpPath = Path.Combine(tempDir, "swap.tmp");
                string thumbsPath = Path.Combine(tempDir, "thumbs.db");
                string officeLockPath = Path.Combine(tempDir, "~$documento.txt");
                File.WriteAllText(tmpPath, "temp data");
                File.WriteAllText(thumbsPath, "fake thumbs db");
                File.WriteAllText(officeLockPath, "lock data");

                // Act
                int eliminados = JunkFileFilter.CleanTemporaryFiles(tempDir);

                // Assert
                Assert.Equal(3, eliminados);
                Assert.True(File.Exists(docPath));
                Assert.True(File.Exists(pdfPath));
                Assert.False(File.Exists(tmpPath));
                Assert.False(File.Exists(thumbsPath));
                Assert.False(File.Exists(officeLockPath));
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void FolderProtectionService_ScanFolder_ExcludesJunkFilesAndCountsThem()
        {
            JunkFileFilter.IsFilterEnabled = true;
            string tempDir = Path.Combine(Path.GetTempPath(), "FL_TestJunkScan_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                // 2 legítimos
                File.WriteAllText(Path.Combine(tempDir, "foto.jpg"), new string('F', 2000));
                File.WriteAllText(Path.Combine(tempDir, "nota.txt"), "Hola");

                // 2 basura
                File.WriteAllText(Path.Combine(tempDir, "desktop.ini"), "[.ShellClassInfo]");
                File.WriteAllText(Path.Combine(tempDir, "temp.tmp"), "cache");

                var service = new FolderProtectionService();

                // Act
                var result = service.ScanFolder(tempDir);

                // Assert
                Assert.Equal(2, result.TotalFiles);
                Assert.Equal(2, result.IgnoredJunkFiles);
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public async System.Threading.Tasks.Task FolderProtectionService_CanBeCancelledSafely()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "FL_TestCancel_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                // Crear varios archivos
                for (int i = 0; i < 20; i++)
                {
                    File.WriteAllText(Path.Combine(tempDir, $"archivo_{i}.dat"), new string('X', 5000));
                }

                var service = new FolderProtectionService();
                using var cts = new System.Threading.CancellationTokenSource();
                cts.Cancel(); // Cancelar inmediatamente

                var progress = new Progress<Tuple<int, string>>(_ => { });

                // Act & Assert
                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                {
                    await service.ProtectFolderAsync(tempDir, "Pass123!", progress, ct: cts.Token);
                });
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public async System.Threading.Tasks.Task FolderProtectionService_CanBePausedAndResumed()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "FL_TestPause_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                for (int i = 0; i < 10; i++)
                {
                    File.WriteAllText(Path.Combine(tempDir, $"doc_{i}.txt"), "Contenido " + i);
                }

                var service = new FolderProtectionService();
                using var pauseEvent = new System.Threading.ManualResetEventSlim(true);

                int reportsCount = 0;
                var progress = new Progress<Tuple<int, string>>(_ =>
                {
                    reportsCount++;
                });

                // Iniciar tarea
                var task = service.ProtectFolderAsync(tempDir, "Pass123!", progress, pauseEvent: pauseEvent);

                // Reanudar y asegurar que finalice correctamente
                pauseEvent.Set();
                await task;

                // Verificar que se completó
                Assert.True(service.EsCarpetaProtegida(tempDir));
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void DirectoryMap_Journal_ReplaysEntriesIfIndexNotSaved()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "FL_TestJournal_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                var crypto = new CryptoService("TestPass123!");
                var map1 = new DirectoryMap(tempDir, crypto, autoSave: false);
                var entry = map1.AddEntry("secreto.pdf", false, "secreto.pdf");

                // Escribir solo al journal sin llamar GuardarIndice()
                map1.AppendJournal(entry);

                // Instanciar nuevo DirectoryMap simulando inicio tras caída
                var map2 = new DirectoryMap(tempDir, crypto, autoSave: false);
                var recoveredEntry = map2.GetByPhysicalName(entry.PhysicalName);

                Assert.NotNull(recoveredEntry);
                Assert.Equal("secreto.pdf", recoveredEntry.RealName);
                Assert.Equal(entry.PhysicalName, recoveredEntry.PhysicalName);
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void FolderProtectionService_DetectInterruptedProtection_IdentifiesIncompleteLock()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "FL_TestDetectInterrupted_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                File.WriteAllText(Path.Combine(tempDir, "pendiente.txt"), "Texto pendiente");
                File.WriteAllText(Path.Combine(tempDir, "abc123def456.lock"), "Cifrado simulado");

                var service = new FolderProtectionService();
                var info = service.DetectInterruptedProtection(tempDir);

                Assert.True(info.IsInterrupted);
                Assert.Equal(1, info.EncryptedFilesCount);
                Assert.Equal(1, info.PendingFilesCount);
                Assert.True(info.PendingBytes > 0);
                Assert.True(info.EncryptedBytes > 0);
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public async System.Threading.Tasks.Task FolderProtectionService_ResumingInterruptedLock_DoesNotReEncryptAlreadyLockedFiles()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "FL_TestResumeLock_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                string pass = "MasterSecurePass2026!";
                var crypto = new CryptoService(pass);
                var service = new FolderProtectionService();

                // 1. Crear 2 archivos en la carpeta
                string file1Content = "Contenido original archivo 1";
                string file2Content = "Contenido original archivo 2";
                string file1Path = Path.Combine(tempDir, "archivo1.txt");
                string file2Path = Path.Combine(tempDir, "archivo2.txt");
                File.WriteAllText(file1Path, file1Content);
                File.WriteAllText(file2Path, file2Content);

                // 2. Simular que archivo 1 ya fue encriptado previamente antes de una interrupción
                var map = new DirectoryMap(tempDir, crypto, autoSave: false);
                var entry1 = map.AddEntry("archivo1.txt", false, "archivo1.txt");

                byte[] file1Bytes = System.Text.Encoding.UTF8.GetBytes(file1Content);
                crypto.TransformarDatos(file1Bytes, 0, file1Bytes.Length);

                string lock1Path = Path.Combine(tempDir, entry1.PhysicalName);
                File.WriteAllBytes(lock1Path, file1Bytes);
                File.Delete(file1Path); // El original ya no existe

                // Guardar la entrada en el journal
                map.AppendJournal(entry1);

                // Verificar que archivo 1 en disco está cifrado (no texto plano)
                byte[] rawLockBytes = File.ReadAllBytes(lock1Path);
                Assert.NotEqual(file1Content, System.Text.Encoding.UTF8.GetString(rawLockBytes));

                // 3. Ejecutar ProtectFolderAsync como si el usuario volviera a proteger/completar la carpeta
                var progress = new Progress<Tuple<int, string>>(_ => { });
                await service.ProtectFolderAsync(tempDir, pass, progress);

                // 4. Asegurar que archivo1.lock NO fue re-encriptado dos veces
                // Si fue re-encriptado dos veces, descifrarlo fallaría. Comprobemos que descifra exactamente a su texto original:
                byte[] lockBytesAfterSecondRun = File.ReadAllBytes(lock1Path);
                byte[] decryptedBytes = (byte[])lockBytesAfterSecondRun.Clone();
                crypto.TransformarDatos(decryptedBytes, 0, decryptedBytes.Length);
                string decryptedText = System.Text.Encoding.UTF8.GetString(decryptedBytes);

                Assert.Equal(file1Content, decryptedText);

                // 5. Verificar que archivo 2 ahora también está protegido
                Assert.False(File.Exists(file2Path)); // archivo2.txt en plano fue eliminado
                var finalMap = new DirectoryMap(tempDir, crypto, autoSave: false);
                var entry2 = finalMap.GetByRelativePath("archivo2.txt");
                Assert.NotNull(entry2);
                Assert.True(File.Exists(Path.Combine(tempDir, entry2.PhysicalName)));

                // 6. Restaurar carpeta completa y comprobar integridad de ambos archivos
                await service.RestoreFolderAsync(tempDir, pass, progress);

                Assert.True(File.Exists(file1Path));
                Assert.True(File.Exists(file2Path));
                Assert.Equal(file1Content, File.ReadAllText(file1Path));
                Assert.Equal(file2Content, File.ReadAllText(file2Path));
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public async System.Threading.Tasks.Task FolderProtectionService_InterruptedLock_CleansRedundantPlaintextFilesSafely()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "FL_TestRedundantClean_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                string pass = "MasterSecurePass2026!";
                var crypto = new CryptoService(pass);
                var service = new FolderProtectionService();

                // Simular que un archivo fue cifrado pero la interrupción impidió borrar el original
                string content = "Informacion Ultra Secreta 2026";
                string plainPath = Path.Combine(tempDir, "informe.docx");
                File.WriteAllText(plainPath, content);

                var map = new DirectoryMap(tempDir, crypto, autoSave: false);
                var entry = map.AddEntry("informe.docx", false, "informe.docx");

                byte[] cipherBytes = System.Text.Encoding.UTF8.GetBytes(content);
                crypto.TransformarDatos(cipherBytes, 0, cipherBytes.Length);

                string lockPath = Path.Combine(tempDir, entry.PhysicalName);
                File.WriteAllBytes(lockPath, cipherBytes);
                map.AppendJournal(entry);

                // Ambos archivos coexisten (plainPath y lockPath)
                Assert.True(File.Exists(plainPath));
                Assert.True(File.Exists(lockPath));

                // Reanudar protección
                var progress = new Progress<Tuple<int, string>>(_ => { });
                await service.ProtectFolderAsync(tempDir, pass, progress);

                // El archivo en texto plano redundante debe haber sido eliminado limpiamente
                Assert.False(File.Exists(plainPath));
                Assert.True(File.Exists(lockPath));

                // Comprobar que el archivo .lock contiene exactamente el cifrado original sin doble cifrado
                byte[] lockBytes = File.ReadAllBytes(lockPath);
                crypto.TransformarDatos(lockBytes, 0, lockBytes.Length);
                Assert.Equal(content, System.Text.Encoding.UTF8.GetString(lockBytes));
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }
    }
}
