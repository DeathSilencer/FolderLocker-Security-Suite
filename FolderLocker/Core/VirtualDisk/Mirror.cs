using DokanNet;
using System.Security.AccessControl;
using System.Text;
// Alias para evitar conflictos con System.IO.FileAccess
using DokanFileAccess = DokanNet.FileAccess;

namespace FolderLocker
{
    public class Mirror : IDokanOperations
    {
        // --- CAMPOS ---
        private readonly string _path;
        private readonly CryptoService _crypto;
        private readonly DirectoryMap _map;

        // --- CONSTRUCTOR ---
        public Mirror(string path, string password)
        {
            if (!Directory.Exists(path)) throw new DirectoryNotFoundException(path);
            _path = path;
            _crypto = new CryptoService(password);
            _map = new DirectoryMap(path, _crypto, true);
        }

        // --- HELPER PARA GENERAR FECHAS CONSISTENTES (Anti-Caché) ---
        // Esto asegura que la fecha del archivo sea siempre la misma basada en su nombre físico
        private DateTime ObtenerFechaConsistente(string nombreFisico)
        {
            int seed = Math.Abs(nombreFisico.GetHashCode());
            // Año base 2020 + dias basados en el hash. 
            // Foto1 siempre será "Ene 2020", Foto2 siempre será "Feb 2021", etc.
            return new DateTime(2020, 1, 1).AddDays(seed % 2000).AddMinutes(seed % 1000);
        }

        #region 1. NAVEGACIÓN Y METADATOS

        public NtStatus GetFileInformation(string fileName, out FileInformation fileInfo, IDokanFileInfo info)
        {
            if (fileName == "\\" || fileName == "/")
            {
                fileInfo = new FileInformation
                {
                    FileName = fileName,
                    Attributes = FileAttributes.Directory,
                    CreationTime = DateTime.Now,
                    LastAccessTime = DateTime.Now,
                    LastWriteTime = DateTime.Now,
                    Length = 0
                };
                return DokanResult.Success;
            }

            // 1. Intentamos usar el Contexto (Si el archivo ya está abierto, es lo más seguro)
            string pathFisico = info.Context as string;

            // 2. Si no hay contexto, resolvemos la ruta
            if (string.IsNullOrEmpty(pathFisico)) pathFisico = ResolverRutaFisica(fileName);

            if (pathFisico == null || (!File.Exists(pathFisico) && !Directory.Exists(pathFisico)))
            {
                fileInfo = default;
                return DokanResult.FileNotFound;
            }

            var item = (FileSystemInfo)new FileInfo(pathFisico);
            if (!File.Exists(pathFisico)) item = new DirectoryInfo(pathFisico);

            var entry = _map.GetByPhysicalName(item.Name);
            bool isDir = (item.Attributes & FileAttributes.Directory) != 0 || (entry != null && entry.IsDirectory);

            DateTime fechaFalsa = ObtenerFechaConsistente(item.Name);
            string displayName = Path.GetFileName(fileName);
            if (string.IsNullOrEmpty(displayName)) displayName = fileName;

            var attributes = item.Attributes & ~FileAttributes.Hidden & ~FileAttributes.System;
            if (isDir) attributes |= FileAttributes.Directory;

            fileInfo = new FileInformation
            {
                FileName = displayName,
                Attributes = attributes,
                Length = isDir ? 0 : ((FileInfo)item).Length,
                CreationTime = fechaFalsa,
                LastWriteTime = fechaFalsa, // Fecha consistente para la miniatura
                LastAccessTime = DateTime.Now
            };

            return DokanResult.Success;
        }

        public NtStatus FindFiles(string fileName, out IList<FileInformation> files, IDokanFileInfo info)
        {
            files = new List<FileInformation>();
            string pathFisico = ResolverRutaFisica(fileName);

            if (pathFisico == null || !Directory.Exists(pathFisico)) return DokanResult.PathNotFound;

            try
            {
                foreach (var item in new DirectoryInfo(pathFisico).GetFileSystemInfos())
                {
                    if (item.Name.Equals("dir.idx", StringComparison.OrdinalIgnoreCase) ||
                        item.Name.Equals("locker.id", StringComparison.OrdinalIgnoreCase)) continue;

                    string nombreLogico = item.Name;
                    var entry = _map.GetByPhysicalName(item.Name);
                    if (entry != null) nombreLogico = entry.RealName;

                    bool isDir = (item.Attributes & FileAttributes.Directory) != 0 || (entry != null && entry.IsDirectory);
                    var attributes = item.Attributes & ~FileAttributes.Hidden & ~FileAttributes.System;
                    if (isDir) attributes |= FileAttributes.Directory;

                    DateTime fechaFalsa = ObtenerFechaConsistente(item.Name);

                    files.Add(new FileInformation
                    {
                        FileName = nombreLogico,
                        Attributes = attributes,
                        CreationTime = fechaFalsa,
                        LastAccessTime = DateTime.Now,
                        LastWriteTime = fechaFalsa, // Debe coincidir con GetFileInformation
                        Length = isDir ? 0 : ((item is FileInfo f) ? f.Length : 0)
                    });
                }
                return DokanResult.Success;
            }
            catch { return DokanResult.AccessDenied; }
        }

        public NtStatus FindFilesWithPattern(string fileName, string searchPattern, out IList<FileInformation> files, IDokanFileInfo info)
        {
            files = new List<FileInformation>();
            var status = FindFiles(fileName, out var allFiles, info);
            if (status != DokanResult.Success)
                return status;

            if (string.IsNullOrEmpty(searchPattern) || searchPattern == "*")
            {
                files = allFiles;
                return DokanResult.Success;
            }

            files = allFiles
                .Where(f => DokanHelper.DokanIsNameInExpression(searchPattern, f.FileName, true))
                .ToList();

            return DokanResult.Success;
        }

        #endregion

        #region 2. GESTIÓN DE ARCHIVOS (Create, Open, Close)

        public NtStatus CreateFile(string fileName, DokanFileAccess access, FileShare share, FileMode mode, FileOptions options, FileAttributes attributes, IDokanFileInfo info)
        {
            if (fileName == "\\" || fileName == "/") { info.IsDirectory = true; return DokanResult.Success; }

            string pathReal = ResolverRutaFisica(fileName);
            string pathPadreLogico = Path.GetDirectoryName(fileName);
            string nombreLogico = Path.GetFileName(fileName);

            // Lógica de Creación (Nuevo Archivo)
            if (pathReal == null && (mode == FileMode.Create || mode == FileMode.CreateNew || mode == FileMode.OpenOrCreate))
            {
                string pathPadreFisico = ResolverRutaFisica(pathPadreLogico);
                if (pathPadreFisico == null) return DokanResult.PathNotFound;

                bool esCarpeta = info.IsDirectory || (attributes & FileAttributes.Directory) != 0;
                var entry = _map.AddEntry(nombreLogico, esCarpeta, fileName.TrimStart('\\', '/')); // Genera GUID nuevo y registra ruta relativa
                pathReal = Path.Combine(pathPadreFisico, entry.PhysicalName);
            }

            if (pathReal == null) return DokanResult.FileNotFound;

            bool exists = File.Exists(pathReal) || Directory.Exists(pathReal);
            if (Directory.Exists(pathReal)) info.IsDirectory = true;

            // Manejo de Directorios
            if (info.IsDirectory)
            {
                if (mode == FileMode.CreateNew && exists) return DokanResult.FileExists;
                if (mode == FileMode.Open && !exists) return DokanResult.PathNotFound;
                if (mode == FileMode.CreateNew || (mode == FileMode.OpenOrCreate && !exists))
                {
                    Directory.CreateDirectory(pathReal);
                }

                info.Context = pathReal; // Guardamos contexto
                return DokanResult.Success;
            }

            // Manejo de Archivos
            if (mode == FileMode.Open && !exists) return DokanResult.FileNotFound;

            try
            {
                if (!exists)
                {
                    if (mode == FileMode.Create || mode == FileMode.CreateNew || mode == FileMode.OpenOrCreate)
                    {
                        using (var fs = new FileStream(pathReal, FileMode.CreateNew, System.IO.FileAccess.ReadWrite, FileShare.ReadWrite)) { }
                    }
                    else
                    {
                        return DokanResult.FileNotFound;
                    }
                }
                else
                {
                    if (mode == FileMode.CreateNew) return DokanResult.FileExists;
                    if (mode == FileMode.Truncate || mode == FileMode.Create)
                    {
                        using (var fs = new FileStream(pathReal, FileMode.Truncate, System.IO.FileAccess.ReadWrite, FileShare.ReadWrite)) { }
                    }
                }

                // Guardamos la ruta física en el Contexto.
                info.Context = pathReal;

                return DokanResult.Success;
            }
            catch (UnauthorizedAccessException) { return DokanResult.AccessDenied; }
            catch { return DokanResult.Unsuccessful; }
        }

        public void Cleanup(string fileName, IDokanFileInfo info)
        {
            string path = (info.Context as string) ?? ResolverRutaFisica(fileName);
            if (info.DeletePending && path != null)
            {
                try
                {
                    if (info.IsDirectory)
                    {
                        if (Directory.Exists(path))
                        {
                            Directory.Delete(path, true);
                            var entry = _map.GetByPhysicalName(Path.GetFileName(path));
                            if (entry != null) _map.RemoveEntryByPhysical(entry.PhysicalName);
                            _map.RemoveEntriesUnderDirectory(fileName.TrimStart('\\', '/'));
                        }
                    }
                    else
                    {
                        if (File.Exists(path))
                        {
                            File.Delete(path);
                            var entry = _map.GetByPhysicalName(Path.GetFileName(path));
                            if (entry != null) _map.RemoveEntryByPhysical(entry.PhysicalName);
                        }
                    }
                }
                catch { }
            }
            info.Context = null;
        }

        public void CloseFile(string fileName, IDokanFileInfo info)
        {
            info.Context = null;
        }

        #endregion

        #region 3. LECTURA Y ESCRITURA (Usando Contexto)

        public NtStatus ReadFile(string fileName, byte[] buffer, out int bytesRead, long offset, IDokanFileInfo info)
        {
            // Usamos el Contexto si está disponible (Más rápido y seguro)
            string path = (info.Context as string) ?? ResolverRutaFisica(fileName);

            if (path == null) { bytesRead = 0; return DokanResult.FileNotFound; }

            try
            {
                using var stream = new FileStream(path, FileMode.Open, System.IO.FileAccess.Read, FileShare.ReadWrite);
                stream.Seek(offset, SeekOrigin.Begin);
                bytesRead = stream.Read(buffer, 0, buffer.Length);
                _crypto.TransformarDatos(buffer, offset, bytesRead);
                return DokanResult.Success;
            }
            catch { bytesRead = 0; return DokanResult.Unsuccessful; }
        }

        public NtStatus WriteFile(string fileName, byte[] buffer, out int bytesWritten, long offset, IDokanFileInfo info)
        {
            string path = (info.Context as string) ?? ResolverRutaFisica(fileName);
            if (path == null) { bytesWritten = 0; return DokanResult.FileNotFound; }

            try
            {
                byte[] bufferCifrado = new byte[buffer.Length];
                Array.Copy(buffer, bufferCifrado, buffer.Length);
                _crypto.TransformarDatos(bufferCifrado, offset, bufferCifrado.Length);

                using var stream = new FileStream(path, FileMode.Open, System.IO.FileAccess.Write, FileShare.ReadWrite);
                stream.Seek(offset, SeekOrigin.Begin);
                stream.Write(bufferCifrado, 0, bufferCifrado.Length);
                bytesWritten = bufferCifrado.Length;
                return DokanResult.Success;
            }
            catch { bytesWritten = 0; return DokanResult.Unsuccessful; }
        }

        #endregion

        #region 4. MOVIMIENTO Y RENOMBRADO (IMPLEMENTADO)

        // Esta es la función que permite Renombrar y Mover archivos
        public NtStatus MoveFile(string oldName, string newName, bool replace, IDokanFileInfo info)
        {
            // 1. Resolver ruta de origen (El archivo que queremos mover/renombrar)
            string sourcePath = (info.Context as string) ?? ResolverRutaFisica(oldName);
            if (sourcePath == null || (!File.Exists(sourcePath) && !Directory.Exists(sourcePath)))
                return DokanResult.FileNotFound;

            // --- CORRECCIÓN CRÍTICA: DETECCIÓN DE COLISIÓN ---
            // Verificamos si el "Nuevo Nombre" ya está siendo usado por otro archivo en el sistema
            string destCheck = ResolverRutaFisica(newName);
            bool destinoLogicoExiste = destCheck != null && (File.Exists(destCheck) || Directory.Exists(destCheck));

            // Si el destino existe y NO nos han dicho "Reemplazar" (replace es false),
            // devolvemos error. Esto hace que Windows muestre la alerta "¿Desea reemplazar?".
            if (destinoLogicoExiste && !replace)
            {
                return DokanResult.FileExists;
            }
            // --------------------------------------------------

            // 2. Preparar rutas
            string destParentPath = ResolverRutaFisica(Path.GetDirectoryName(newName));
            if (destParentPath == null) return DokanResult.PathNotFound;

            string newLogicalName = Path.GetFileName(newName);
            string physicalName = Path.GetFileName(sourcePath); // El GUID actual

            // Obtenemos la entrada del mapa del archivo origen
            var entry = _map.GetByPhysicalName(physicalName);
            if (entry == null) return DokanResult.AccessDenied; // Seguridad: no tocar archivos sin mapa

            // Ruta física final (Mismo GUID, nueva carpeta padre)
            string destPath = Path.Combine(destParentPath, physicalName);

            try
            {
                // 3. Manejo de Reemplazo (Si el usuario dijo "Sí, reemplazar")
                if (destinoLogicoExiste && replace)
                {
                    // Hay que borrar el archivo "victima" que está ocupando el nombre
                    string nombreFisicoVictima = Path.GetFileName(destCheck);

                    // Borrar del mapa por nombre físico único
                    var entryVictima = _map.GetByPhysicalName(nombreFisicoVictima);
                    if (entryVictima != null) _map.RemoveEntryByPhysical(entryVictima.PhysicalName);

                    // Borrar del disco físico
                    if (Directory.Exists(destCheck)) Directory.Delete(destCheck, true);
                    else if (File.Exists(destCheck)) File.Delete(destCheck);
                }

                // 4. Mover Físicamente (Solo si cambiamos de carpeta)
                // Si es solo renombrar en la misma carpeta, sourcePath y destPath son iguales (porque el GUID no cambia)
                if (sourcePath != destPath)
                {
                    if (Directory.Exists(sourcePath)) Directory.Move(sourcePath, destPath);
                    else File.Move(sourcePath, destPath);
                }

                // 5. Actualizar el Mapa (Renombrado Lógico y ruta relativa)
                string oldRel = oldName.TrimStart('\\', '/');
                string newRel = newName.TrimStart('\\', '/');

                entry.RealName = newLogicalName;
                entry.RelativePath = newRel;

                if (entry.IsDirectory)
                {
                    _map.UpdateDirectoryPath(oldRel, newRel);
                }
                _map.GuardarIndice();

                info.Context = destPath;
                return DokanResult.Success;
            }
            catch (IOException)
            {
                // Doble seguridad por si el sistema de archivos está ocupado
                return DokanResult.FileExists;
            }
            catch (UnauthorizedAccessException)
            {
                return DokanResult.AccessDenied;
            }
        }

        #endregion

        #region 5. RESOLVER Y OTROS

        private string ResolverRutaFisica(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return _path;
            string rutaLogica = fileName.TrimStart('\\', '/');
            if (string.IsNullOrEmpty(rutaLogica)) return _path;

            string[] partes = rutaLogica.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
            string rutaActual = _path;

            foreach (var parte in partes)
            {
                if (string.IsNullOrEmpty(parte)) continue;
                if (!Directory.Exists(rutaActual)) return null;
                bool encontrado = false;
                foreach (var item in new DirectoryInfo(rutaActual).GetFileSystemInfos())
                {
                    if (item.Name.Equals("dir.idx", StringComparison.OrdinalIgnoreCase) ||
                        item.Name.Equals("locker.id", StringComparison.OrdinalIgnoreCase)) continue;

                    var entry = _map.GetByPhysicalName(item.Name);

                    if ((entry != null && entry.RealName.Equals(parte, StringComparison.OrdinalIgnoreCase)) ||
                        (entry == null && item.Name.Equals(parte, StringComparison.OrdinalIgnoreCase)))
                    {
                        rutaActual = Path.Combine(rutaActual, item.Name);
                        encontrado = true;
                        break;
                    }
                }
                if (!encontrado) return null;
            }
            return rutaActual;
        }

        public NtStatus DeleteFile(string fileName, IDokanFileInfo info)
        {
            string path = (info.Context as string) ?? ResolverRutaFisica(fileName);
            if (path == null || !File.Exists(path)) return DokanResult.FileNotFound;
            return DokanResult.Success;
        }

        public NtStatus DeleteDirectory(string fileName, IDokanFileInfo info)
        {
            string path = (info.Context as string) ?? ResolverRutaFisica(fileName);
            if (path == null || !Directory.Exists(path)) return DokanResult.PathNotFound;

            try
            {
                bool hasFiles = Directory.EnumerateFileSystemEntries(path).Any(p =>
                {
                    string name = Path.GetFileName(p);
                    return !name.Equals("dir.idx", StringComparison.OrdinalIgnoreCase) &&
                           !name.Equals("locker.id", StringComparison.OrdinalIgnoreCase);
                });

                if (hasFiles) return DokanResult.DirectoryNotEmpty;
            }
            catch
            {
                return DokanResult.AccessDenied;
            }

            return DokanResult.Success;
        }

        public NtStatus GetVolumeInformation(out string label, out FileSystemFeatures features, out string name, out uint serialNumber, IDokanFileInfo info)
        {
            label = "LockerDrive";
            features = FileSystemFeatures.CasePreservedNames | FileSystemFeatures.UnicodeOnDisk | FileSystemFeatures.SupportsRemoteStorage;
            name = "NTFS";
            serialNumber = (uint)new Random().Next(100000, 999999);
            return DokanResult.Success;
        }

        public NtStatus SetAllocationSize(string fileName, long length, IDokanFileInfo info)
        {
            return SetEndOfFile(fileName, length, info);
        }

        public NtStatus SetEndOfFile(string fileName, long length, IDokanFileInfo info)
        {
            string path = (info.Context as string) ?? ResolverRutaFisica(fileName);
            if (path == null || !File.Exists(path)) return DokanResult.FileNotFound;
            try
            {
                using var stream = new FileStream(path, FileMode.Open, System.IO.FileAccess.Write, FileShare.ReadWrite);
                stream.SetLength(length);
                return DokanResult.Success;
            }
            catch { return DokanResult.Unsuccessful; }
        }

        public NtStatus SetFileAttributes(string f, FileAttributes a, IDokanFileInfo i) => DokanResult.Success;
        public NtStatus SetFileTime(string f, DateTime? c, DateTime? a, DateTime? w, IDokanFileInfo i) => DokanResult.Success;
        public NtStatus GetFileSecurity(string f, out FileSystemSecurity s, AccessControlSections c, IDokanFileInfo i) { s = null; return DokanResult.NotImplemented; }
        public NtStatus SetFileSecurity(string f, FileSystemSecurity s, AccessControlSections c, IDokanFileInfo i) => DokanResult.NotImplemented;
        public NtStatus GetDiskFreeSpace(out long f, out long t, out long tf, IDokanFileInfo i) { f = 1024L * 1024 * 1024 * 50; t = f; tf = f; return DokanResult.Success; }
        public NtStatus Mounted(string m, IDokanFileInfo i) => DokanResult.Success;
        public NtStatus Unmounted(IDokanFileInfo i) => DokanResult.Success;
        public NtStatus FlushFileBuffers(string f, IDokanFileInfo i) => DokanResult.Success;
        public NtStatus LockFile(string f, long o, long l, IDokanFileInfo i) => DokanResult.Success;
        public NtStatus UnlockFile(string f, long o, long l, IDokanFileInfo i) => DokanResult.Success;
        public NtStatus FindStreams(string f, out IList<FileInformation> s, IDokanFileInfo i) { s = null; return DokanResult.NotImplemented; }

        #endregion
    }
}