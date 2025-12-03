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

            bool isDir = (item.Attributes & FileAttributes.Directory) != 0;
            DateTime fechaFalsa = ObtenerFechaConsistente(item.Name);

            fileInfo = new FileInformation
            {
                FileName = fileName,
                Attributes = item.Attributes & ~FileAttributes.Hidden & ~FileAttributes.System,
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

                    DateTime fechaFalsa = ObtenerFechaConsistente(item.Name);

                    files.Add(new FileInformation
                    {
                        FileName = nombreLogico,
                        Attributes = item.Attributes & ~FileAttributes.Hidden & ~FileAttributes.System,
                        CreationTime = fechaFalsa,
                        LastAccessTime = DateTime.Now,
                        LastWriteTime = fechaFalsa, // Debe coincidir con GetFileInformation
                        Length = (item is FileInfo f) ? f.Length : 0
                    });
                }
                return DokanResult.Success;
            }
            catch { return DokanResult.AccessDenied; }
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
                var entry = _map.AddEntry(nombreLogico, esCarpeta); // Genera GUID nuevo
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
                if (mode == FileMode.CreateNew) Directory.CreateDirectory(pathReal);

                info.Context = pathReal; // Guardamos contexto
                return DokanResult.Success;
            }

            // Manejo de Archivos
            if (mode == FileMode.Open && !exists) return DokanResult.FileNotFound;
            if (mode == FileMode.CreateNew && exists) return DokanResult.FileExists;

            try
            {
                // Abrimos el stream solo para verificar acceso/crear
                using (var fs = new FileStream(pathReal, mode, System.IO.FileAccess.ReadWrite, share, 4096, options)) { }

                // *** IMPORTANTE ***
                // Guardamos la ruta física en el Contexto. Esto soluciona el bug de "Abrir foto incorrecta".
                // Windows sabrá que este Handle específico apunta a ESTE archivo físico.
                info.Context = pathReal;

                return DokanResult.Success;
            }
            catch (UnauthorizedAccessException) { return DokanResult.AccessDenied; }
            catch { return DokanResult.Unsuccessful; }
        }

        public void Cleanup(string fileName, IDokanFileInfo info)
        {
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
            string sourcePath = ResolverRutaFisica(oldName);
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

                    // Borrar del mapa
                    var entryVictima = _map.GetByPhysicalName(nombreFisicoVictima);
                    if (entryVictima != null) _map.RemoveEntry(entryVictima.RealName);

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

                // 5. Actualizar el Mapa (Renombrado Lógico)
                // Aquí es donde realmente cambia el nombre visible para el usuario
                entry.RealName = newLogicalName;
                _map.GuardarIndice();

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
            string rutaLogica = fileName.TrimStart('\\');
            if (string.IsNullOrEmpty(rutaLogica)) return _path;
            string[] partes = rutaLogica.Split(Path.DirectorySeparatorChar);
            string rutaActual = _path;

            foreach (var parte in partes)
            {
                if (string.IsNullOrEmpty(parte)) continue;
                if (!Directory.Exists(rutaActual)) return null;
                bool encontrado = false;
                foreach (var item in new DirectoryInfo(rutaActual).GetFileSystemInfos())
                {
                    if (item.Name == "dir.idx" || item.Name == "locker.id") continue;
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
            string path = ResolverRutaFisica(fileName);
            if (!File.Exists(path)) return DokanResult.FileNotFound;
            try
            {
                File.Delete(path);
                var entry = _map.GetByPhysicalName(Path.GetFileName(path));
                if (entry != null) _map.RemoveEntry(entry.RealName);
                return DokanResult.Success;
            }
            catch { return DokanResult.AccessDenied; }
        }

        public NtStatus DeleteDirectory(string fileName, IDokanFileInfo info)
        {
            string path = ResolverRutaFisica(fileName);
            if (!Directory.Exists(path)) return DokanResult.PathNotFound;
            try
            {
                Directory.Delete(path, true);
                var entry = _map.GetByPhysicalName(Path.GetFileName(path));
                if (entry != null) _map.RemoveEntry(entry.RealName);
                return DokanResult.Success;
            }
            catch { return DokanResult.AccessDenied; }
        }

        public NtStatus GetVolumeInformation(out string label, out FileSystemFeatures features, out string name, out uint serialNumber, IDokanFileInfo info)
        {
            label = "LockerDrive";
            features = FileSystemFeatures.CasePreservedNames | FileSystemFeatures.UnicodeOnDisk | FileSystemFeatures.SupportsRemoteStorage;
            name = "NTFS";
            serialNumber = (uint)new Random().Next(100000, 999999);
            return DokanResult.Success;
        }

        // Boilerplate
        public NtStatus SetAllocationSize(string f, long l, IDokanFileInfo i) => DokanResult.Success;
        public NtStatus SetEndOfFile(string f, long l, IDokanFileInfo i) => DokanResult.Success;
        public NtStatus SetFileAttributes(string f, FileAttributes a, IDokanFileInfo i) => DokanResult.Success;
        public NtStatus SetFileTime(string f, DateTime? c, DateTime? a, DateTime? w, IDokanFileInfo i) => DokanResult.Success;
        public NtStatus GetFileSecurity(string f, out FileSystemSecurity s, AccessControlSections c, IDokanFileInfo i) { s = null; return DokanResult.NotImplemented; }
        public NtStatus SetFileSecurity(string f, FileSystemSecurity s, AccessControlSections c, IDokanFileInfo i) => DokanResult.NotImplemented;
        public NtStatus GetDiskFreeSpace(out long f, out long t, out long tf, IDokanFileInfo i) { f = 1024L * 1024 * 1024 * 50; t = f; tf = f; return DokanResult.Success; }
        public NtStatus Mounted(string m, IDokanFileInfo i) => DokanResult.Success;
        public NtStatus Unmounted(IDokanFileInfo i) => DokanResult.Success;
        public NtStatus FlushFileBuffers(string f, IDokanFileInfo i) => DokanResult.Success;
        public NtStatus FindFilesWithPattern(string f, string s, out IList<FileInformation> fl, IDokanFileInfo i) => FindFiles(f, out fl, i);
        public NtStatus LockFile(string f, long o, long l, IDokanFileInfo i) => DokanResult.Success;
        public NtStatus UnlockFile(string f, long o, long l, IDokanFileInfo i) => DokanResult.Success;
        public NtStatus FindStreams(string f, out IList<FileInformation> s, IDokanFileInfo i) { s = null; return DokanResult.NotImplemented; }

        #endregion
    }
}