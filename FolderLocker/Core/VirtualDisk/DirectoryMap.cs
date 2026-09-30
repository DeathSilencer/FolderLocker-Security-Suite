using System.Text.Json;

namespace FolderLocker
{
    public class FileEntry
    {
        public string RealName { get; set; } = string.Empty;
        public string PhysicalName { get; set; } = string.Empty;
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
                    try
                    {
                        byte[] encryptedBytes = File.ReadAllBytes(_indexPath);

                        // Descifrar en memoria
                        // Si la contraseña es incorrecta, esto generará "basura" binaria
                        _crypto.TransformarDatos(encryptedBytes, 0, encryptedBytes.Length);

                        string json = System.Text.Encoding.UTF8.GetString(encryptedBytes);

                        // Intentamos deserializar. Si es basura binaria, esto fallará.
                        _entries = JsonSerializer.Deserialize<List<FileEntry>>(json);

                        if (_entries == null) throw new Exception("El índice deserializado es nulo.");
                    }
                    catch (Exception ex)
                    {
                        // --- CORRECCIÓN CRÍTICA ---
                        // Ya no "silenciamos" el error. Si el índice existe pero falla,
                        // DEBEMOS avisar y detener todo.
                        // Esto previene montar una bóveda "zombie" vacía.
                        throw new Exception("¡FALLO DE SEGURIDAD! Contraseña incorrecta o índice corrupto. " + ex.Message);
                    }
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

        public FileEntry GetByPhysicalName(string name)
        {
            lock (_syncLock)
            {
                return _entries.FirstOrDefault(e => e.PhysicalName.Equals(name, StringComparison.OrdinalIgnoreCase));
            }
        }

        public FileEntry AddEntry(string realName, bool isDir)
        {
            lock (_syncLock)
            {
                string newPhysical;
                do
                {
                    newPhysical = Guid.NewGuid().ToString("N").Substring(0, 12) + ".lock";
                }
                while (_entries.Any(e => e.PhysicalName == newPhysical));

                var newEntry = new FileEntry
                {
                    RealName = realName,
                    PhysicalName = newPhysical,
                    IsDirectory = isDir,
                    CreationTime = DateTime.Now
                };

                _entries.Add(newEntry);

                if (_autoSave) GuardarIndice();

                return newEntry;
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