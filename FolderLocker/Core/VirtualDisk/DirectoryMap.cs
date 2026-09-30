using System.Text.Json;

namespace FolderLocker
{
    public class FileEntry
    {
        public string RealName { get; set; } = string.Empty;
        public string PhysicalName { get; set; } = string.Empty;
        public string RelativePath { get; set; } = string.Empty;
        public bool IsDirectory { get; set; }
        public DateTime CreationTime { get; set; } = DateTime.Now;
    }

    public class DirectoryMap
    {
        #region CAMPOS PRIVADOS

        private List<FileEntry> _entries;
        private readonly string _indexPath;
        private readonly CryptoService _crypto;
        private readonly bool _autoSave;

        // Objeto de bloqueo para garantizar thread-safety
        private readonly object _syncLock = new object();

        #endregion

        #region CONSTRUCTOR & CARGA

        public DirectoryMap(string folderPath, CryptoService crypto, bool autoSave = true)
        {
            _indexPath = Path.Combine(folderPath, "dir.idx");
            _crypto = crypto;
            _autoSave = autoSave;
            CargarIndice();
        }

        private void CargarIndice()
        {
            lock (_syncLock)
            {
                if (File.Exists(_indexPath))
                {
                    byte[] encryptedBytes = File.ReadAllBytes(_indexPath);
                    byte[] copyBytes = (byte[])encryptedBytes.Clone();

                    // INTENTO 1: Descifrar con el nuevo estándar AES-256 CTR
                    try
                    {
                        _crypto.IsLegacyMode = false;
                        _crypto.TransformarDatos(encryptedBytes, 0, encryptedBytes.Length);

                        string json = System.Text.Encoding.UTF8.GetString(encryptedBytes);
                        _entries = JsonSerializer.Deserialize<List<FileEntry>>(json);

                        if (_entries != null) return;
                    }
                    catch
                    {
                        // Si falla con AES-256 CTR, probamos si es una bóveda creada con el método legacy XOR
                    }

                    // INTENTO 2: Compatibilidad retroactiva con bóvedas previas (Legacy XOR)
                    try
                    {
                        _crypto.IsLegacyMode = true;
                        _crypto.TransformarDatos(copyBytes, 0, copyBytes.Length);

                        string legacyJson = System.Text.Encoding.UTF8.GetString(copyBytes);
                        _entries = JsonSerializer.Deserialize<List<FileEntry>>(legacyJson);

                        if (_entries != null)
                        {
                            return; // Bóveda legacy detectada y cargada exitosamente
                        }
                    }
                    catch
                    {
                        // Si ambos fallan, la contraseña es incorrecta o el archivo está dañado
                    }

                    _crypto.IsLegacyMode = false;
                    throw new Exception("¡FALLO DE SEGURIDAD! Contraseña incorrecta o índice corrupto.");
                }
                else
                {
                    // Solo si el archivo NO existe iniciamos una lista nueva (Primera vez)
                    _entries = new List<FileEntry>();
                }
            }
        }

        #endregion

        #region PERSISTENCIA (GUARDADO)

        public void GuardarIndice()
        {
            lock (_syncLock)
            {
                int intentos = 0;
                while (intentos < 3)
                {
                    try
                    {
                        // Quitar atributos para poder escribir
                        if (File.Exists(_indexPath)) File.SetAttributes(_indexPath, FileAttributes.Normal);

                        string json = JsonSerializer.Serialize(_entries);
                        byte[] plainBytes = System.Text.Encoding.UTF8.GetBytes(json);

                        // Cifrar antes de escribir
                        _crypto.TransformarDatos(plainBytes, 0, plainBytes.Length);

                        File.WriteAllBytes(_indexPath, plainBytes);

                        // Restaurar atributos ocultos
                        File.SetAttributes(_indexPath, FileAttributes.Hidden | FileAttributes.System);

                        break;
                    }
                    catch (IOException)
                    {
                        intentos++;
                        Thread.Sleep(50);
                    }
                    catch
                    {
                        break; // Error fatal
                    }
                }
            }
        }

        #endregion

        #region MÉTODOS DE ACCESO (CRUD)

        public FileEntry GetByRealName(string name)
        {
            lock (_syncLock)
            {
                return _entries.FirstOrDefault(e => e.RealName.Equals(name, StringComparison.OrdinalIgnoreCase));
            }
        }

        public FileEntry GetByRelativePath(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return null;
            string normalized = relativePath.Replace('/', '\\').TrimStart('\\');
            lock (_syncLock)
            {
                return _entries.FirstOrDefault(e =>
                    !string.IsNullOrEmpty(e.RelativePath) &&
                    e.RelativePath.Replace('/', '\\').TrimStart('\\').Equals(normalized, StringComparison.OrdinalIgnoreCase));
            }
        }

        public FileEntry GetByPhysicalName(string name)
        {
            lock (_syncLock)
            {
                return _entries.FirstOrDefault(e => e.PhysicalName.Equals(name, StringComparison.OrdinalIgnoreCase));
            }
        }

        public FileEntry AddEntry(string realName, bool isDir, string relativePath = "")
        {
            lock (_syncLock)
            {
                string newPhysical;
                do
                {
                    newPhysical = Guid.NewGuid().ToString("N").Substring(0, 12) + ".lock";
                }
                while (_entries.Any(e => e.PhysicalName.Equals(newPhysical, StringComparison.OrdinalIgnoreCase)));

                string normalizedRelPath = string.IsNullOrEmpty(relativePath)
                    ? realName
                    : relativePath.Replace('/', '\\').TrimStart('\\');

                var newEntry = new FileEntry
                {
                    RealName = realName,
                    PhysicalName = newPhysical,
                    RelativePath = normalizedRelPath,
                    IsDirectory = isDir,
                    CreationTime = DateTime.Now
                };

                _entries.Add(newEntry);

                if (_autoSave) GuardarIndice();

                return newEntry;
            }
        }

        public void RemoveEntryByPhysical(string physicalName)
        {
            if (string.IsNullOrEmpty(physicalName)) return;
            lock (_syncLock)
            {
                var entry = _entries.FirstOrDefault(e => e.PhysicalName.Equals(physicalName, StringComparison.OrdinalIgnoreCase));
                if (entry != null)
                {
                    _entries.Remove(entry);
                    if (_autoSave) GuardarIndice();
                }
            }
        }

        public void RemoveEntriesUnderDirectory(string relativeDirPath)
        {
            if (string.IsNullOrEmpty(relativeDirPath)) return;
            string prefix = relativeDirPath.Replace('/', '\\').TrimEnd('\\') + "\\";
            string dirExact = relativeDirPath.Replace('/', '\\').TrimEnd('\\');
            lock (_syncLock)
            {
                _entries.RemoveAll(e =>
                {
                    if (string.IsNullOrEmpty(e.RelativePath)) return false;
                    string norm = e.RelativePath.Replace('/', '\\');
                    return norm.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
                           norm.Equals(dirExact, StringComparison.OrdinalIgnoreCase);
                });
                if (_autoSave) GuardarIndice();
            }
        }

        public void UpdateDirectoryPath(string oldRelativePath, string newRelativePath)
        {
            if (string.IsNullOrEmpty(oldRelativePath) || string.IsNullOrEmpty(newRelativePath)) return;
            string oldPrefix = oldRelativePath.Replace('/', '\\').TrimEnd('\\') + "\\";
            string newPrefix = newRelativePath.Replace('/', '\\').TrimEnd('\\') + "\\";
            string oldExact = oldRelativePath.Replace('/', '\\').TrimEnd('\\');
            string newExact = newRelativePath.Replace('/', '\\').TrimEnd('\\');

            lock (_syncLock)
            {
                foreach (var e in _entries)
                {
                    if (string.IsNullOrEmpty(e.RelativePath)) continue;
                    string norm = e.RelativePath.Replace('/', '\\');
                    if (norm.Equals(oldExact, StringComparison.OrdinalIgnoreCase))
                    {
                        e.RelativePath = newExact;
                    }
                    else if (norm.StartsWith(oldPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        e.RelativePath = newPrefix + norm.Substring(oldPrefix.Length);
                    }
                }
                if (_autoSave) GuardarIndice();
            }
        }

        public void RemoveEntry(string realName)
        {
            lock (_syncLock)
            {
                var entry = _entries.FirstOrDefault(e => e.RealName.Equals(realName, StringComparison.OrdinalIgnoreCase));
                if (entry != null)
                {
                    _entries.Remove(entry);
                    if (_autoSave) GuardarIndice();
                }
            }
        }

        public List<FileEntry> GetAll()
        {
            lock (_syncLock)
            {
                return new List<FileEntry>(_entries);
            }
        }

        #endregion
    }
}