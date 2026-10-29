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

        private List<FileEntry> _entries = new();
        private readonly Dictionary<string, FileEntry> _byPhysical = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, FileEntry> _byRelative = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, FileEntry> _byRealName = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _physicalSet = new(StringComparer.OrdinalIgnoreCase);

        private readonly string _indexPath;
        private readonly string _journalPath;
        private readonly CryptoService _crypto;
        private readonly bool _autoSave;

        // Objeto de bloqueo para garantizar thread-safety
        private readonly object _syncLock = new object();

        #endregion

        #region CONSTRUCTOR & CARGA

        public DirectoryMap(string folderPath, CryptoService crypto, bool autoSave = true)
        {
            _indexPath = Path.Combine(folderPath, "dir.idx");
            _journalPath = Path.Combine(folderPath, "dir.journal");
            _crypto = crypto;
            _autoSave = autoSave;
            CargarIndice();
        }

        private void RebuildLookupTables()
        {
            _byPhysical.Clear();
            _byRelative.Clear();
            _byRealName.Clear();
            _physicalSet.Clear();

            if (_entries == null) return;

            foreach (var e in _entries)
            {
                if (!string.IsNullOrEmpty(e.PhysicalName))
                {
                    _byPhysical[e.PhysicalName] = e;
                    _physicalSet.Add(e.PhysicalName);
                }
                if (!string.IsNullOrEmpty(e.RelativePath))
                {
                    string normRel = e.RelativePath.Replace('/', '\\').TrimStart('\\');
                    _byRelative[normRel] = e;
                }
                if (!string.IsNullOrEmpty(e.RealName) && !_byRealName.ContainsKey(e.RealName))
                {
                    _byRealName[e.RealName] = e;
                }
            }
        }

        private void CargarIndice()
        {
            lock (_syncLock)
            {
                bool cargoExitoso = false;
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
                        _entries = JsonSerializer.Deserialize<List<FileEntry>>(json) ?? new List<FileEntry>();

                        cargoExitoso = true;
                    }
                    catch
                    {
                        // Si falla con AES-256 CTR, probamos si es una bóveda creada con el método legacy XOR
                    }

                    if (!cargoExitoso)
                    {
                        // INTENTO 2: Compatibilidad retroactiva con bóvedas previas (Legacy XOR)
                        try
                        {
                            _crypto.IsLegacyMode = true;
                            _crypto.TransformarDatos(copyBytes, 0, copyBytes.Length);

                            string legacyJson = System.Text.Encoding.UTF8.GetString(copyBytes);
                            _entries = JsonSerializer.Deserialize<List<FileEntry>>(legacyJson) ?? new List<FileEntry>();

                            cargoExitoso = true;
                        }
                        catch
                        {
                            // Si ambos fallan, la contraseña es incorrecta o el archivo está dañado
                        }
                    }

                    if (!cargoExitoso)
                    {
                        _crypto.IsLegacyMode = false;
                        throw new Exception("¡FALLO DE SEGURIDAD! Contraseña incorrecta o índice corrupto.");
                    }
                }
                else
                {
                    // Solo si el archivo NO existe iniciamos una lista nueva (Primera vez)
                    _entries = new List<FileEntry>();
                }

                // Replay WAL journal si existe (recuperación atómica tras interrupción/cierre forzado)
                bool huboRecuperacionJournal = false;
                if (File.Exists(_journalPath))
                {
                    try
                    {
                        var lineas = File.ReadAllLines(_journalPath);
                        foreach (var linea in lineas)
                        {
                            if (string.IsNullOrWhiteSpace(linea)) continue;
                            try
                            {
                                byte[] encryptedEntry = Convert.FromBase64String(linea.Trim());
                                _crypto.TransformarDatos(encryptedEntry, 0, encryptedEntry.Length);
                                string entryJson = System.Text.Encoding.UTF8.GetString(encryptedEntry);
                                var entry = JsonSerializer.Deserialize<FileEntry>(entryJson);
                                if (entry != null && !string.IsNullOrEmpty(entry.PhysicalName))
                                {
                                    var existente = _entries.FirstOrDefault(e => e.PhysicalName.Equals(entry.PhysicalName, StringComparison.OrdinalIgnoreCase));
                                    if (existente != null)
                                    {
                                        _entries.Remove(existente);
                                    }
                                    _entries.Add(entry);
                                    huboRecuperacionJournal = true;
                                }
                            }
                            catch
                            {
                                // Línea truncada por apagón o corte de energía abrupto
                            }
                        }
                    }
                    catch
                    {
                    }
                }

                RebuildLookupTables();

                if (huboRecuperacionJournal)
                {
                    // Consolidar automáticamente en dir.idx y purgar el journal
                    GuardarIndice();
                }
            }
        }

        #endregion

        #region PERSISTENCIA (GUARDADO ATÓMICO)

        public void GuardarIndice()
        {
            lock (_syncLock)
            {
                int intentos = 0;
                while (intentos < 3)
                {
                    try
                    {
                        string json = JsonSerializer.Serialize(_entries);
                        byte[] plainBytes = System.Text.Encoding.UTF8.GetBytes(json);

                        // Cifrar antes de escribir
                        _crypto.TransformarDatos(plainBytes, 0, plainBytes.Length);

                        string tempPath = _indexPath + ".tmp";
                        if (File.Exists(tempPath))
                        {
                            try { File.Delete(tempPath); } catch { }
                        }

                        File.WriteAllBytes(tempPath, plainBytes);

                        // Reemplazo atómico: si el archivo original existe, se remueven atributos y se sobreescribe atómicamente
                        if (File.Exists(_indexPath))
                        {
                            File.SetAttributes(_indexPath, FileAttributes.Normal);
                        }
                        File.Move(tempPath, _indexPath, overwrite: true);

                        // Restaurar atributos ocultos y de sistema
                        File.SetAttributes(_indexPath, FileAttributes.Hidden | FileAttributes.System);

                        // Purgar journal una vez consolidado el índice de forma atómica
                        if (File.Exists(_journalPath))
                        {
                            try
                            {
                                File.SetAttributes(_journalPath, FileAttributes.Normal);
                                File.Delete(_journalPath);
                            }
                            catch { }
                        }

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

        /// <summary>
        /// Registra inmediatamente una entrada recién cifrada en el diario WAL (Write-Ahead Log) a prueba de cortes de energía.
        /// </summary>
        public void AppendJournal(FileEntry entry)
        {
            if (entry == null) return;
            lock (_syncLock)
            {
                try
                {
                    string json = JsonSerializer.Serialize(entry);
                    byte[] plainBytes = System.Text.Encoding.UTF8.GetBytes(json);
                    byte[] encrypted = (byte[])plainBytes.Clone();

                    // Cifrado simétrico AES-256 CTR (offset 0 para independencia de línea)
                    _crypto.TransformarDatos(encrypted, 0, encrypted.Length);
                    string base64 = Convert.ToBase64String(encrypted);

                    using (var sw = new StreamWriter(new FileStream(_journalPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)))
                    {
                        sw.WriteLine(base64);
                        sw.Flush();
                    }

                    try
                    {
                        File.SetAttributes(_journalPath, FileAttributes.Hidden | FileAttributes.System);
                    }
                    catch { }
                }
                catch
                {
                }
            }
        }

        public bool HasJournal
        {
            get
            {
                lock (_syncLock)
                {
                    return File.Exists(_journalPath);
                }
            }
        }

        #endregion

        #region MÉTODOS DE ACCESO (CRUD O(1))

        public FileEntry? GetByRealName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            lock (_syncLock)
            {
                if (_byRealName.TryGetValue(name, out var entry)) return entry;
                return _entries.FirstOrDefault(e => e.RealName.Equals(name, StringComparison.OrdinalIgnoreCase));
            }
        }

        public FileEntry? GetByRelativePath(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return null;
            string normalized = relativePath.Replace('/', '\\').TrimStart('\\');
            lock (_syncLock)
            {
                if (_byRelative.TryGetValue(normalized, out var entry)) return entry;
                return null;
            }
        }

        public FileEntry? GetByPhysicalName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            lock (_syncLock)
            {
                if (_byPhysical.TryGetValue(name, out var entry)) return entry;
                return null;
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
                while (_physicalSet.Contains(newPhysical)); // O(1) hash check

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
                _byPhysical[newPhysical] = newEntry;
                _physicalSet.Add(newPhysical);
                _byRelative[normalizedRelPath] = newEntry;
                if (!_byRealName.ContainsKey(realName))
                {
                    _byRealName[realName] = newEntry;
                }

                if (_autoSave) GuardarIndice();

                return newEntry;
            }
        }

        public void RemoveEntryByPhysical(string physicalName)
        {
            if (string.IsNullOrEmpty(physicalName)) return;
            lock (_syncLock)
            {
                if (_byPhysical.TryGetValue(physicalName, out var entry))
                {
                    _entries.Remove(entry);
                    _byPhysical.Remove(physicalName);
                    _physicalSet.Remove(physicalName);

                    if (!string.IsNullOrEmpty(entry.RelativePath))
                    {
                        string normRel = entry.RelativePath.Replace('/', '\\').TrimStart('\\');
                        _byRelative.Remove(normRel);
                    }

                    if (!string.IsNullOrEmpty(entry.RealName))
                    {
                        _byRealName.Remove(entry.RealName);
                        var another = _entries.FirstOrDefault(e => e.RealName.Equals(entry.RealName, StringComparison.OrdinalIgnoreCase));
                        if (another != null) _byRealName[another.RealName] = another;
                    }

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
                int removed = _entries.RemoveAll(e =>
                {
                    if (string.IsNullOrEmpty(e.RelativePath)) return false;
                    string norm = e.RelativePath.Replace('/', '\\');
                    return norm.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
                           norm.Equals(dirExact, StringComparison.OrdinalIgnoreCase);
                });

                if (removed > 0)
                {
                    RebuildLookupTables();
                    if (_autoSave) GuardarIndice();
                }
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
                bool changed = false;
                foreach (var e in _entries)
                {
                    if (string.IsNullOrEmpty(e.RelativePath)) continue;
                    string norm = e.RelativePath.Replace('/', '\\');
                    if (norm.Equals(oldExact, StringComparison.OrdinalIgnoreCase))
                    {
                        e.RelativePath = newExact;
                        changed = true;
                    }
                    else if (norm.StartsWith(oldPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        e.RelativePath = newPrefix + norm.Substring(oldPrefix.Length);
                        changed = true;
                    }
                }

                if (changed)
                {
                    RebuildLookupTables();
                    if (_autoSave) GuardarIndice();
                }
            }
        }

        public void RemoveEntry(string realName)
        {
            lock (_syncLock)
            {
                var entry = _entries.FirstOrDefault(e => e.RealName.Equals(realName, StringComparison.OrdinalIgnoreCase));
                if (entry != null)
                {
                    RemoveEntryByPhysical(entry.PhysicalName);
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