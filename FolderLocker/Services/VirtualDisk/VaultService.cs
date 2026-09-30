using DokanNet;
using FolderLocker;
using FolderLocker.Services.Audit;

namespace FolderLocker.Services.VirtualDisk
{
    public class VaultService : IVaultService
    {
        private readonly Dictionary<string, DokanInstance> _instanciasDokan = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _montajesActivos = new(StringComparer.OrdinalIgnoreCase);
        private readonly object _lock = new();

        public event Action<string, string>? VaultMounted;
        public event Action<string>? VaultUnmounted;

        public bool IsDokanAvailable(out string errorMessage)
        {
            try
            {
                var dokan = new DokanNet.Dokan(null!);
                int driverVersion = dokan.DriverVersion;
                if (driverVersion == 0)
                {
                    errorMessage = "El controlador de disco virtual (Dokan) no está activo en el sistema.\n\nPor favor reinicia tu computadora para que Windows inicialice el servicio del controlador.";
                    return false;
                }
                errorMessage = string.Empty;
                return true;
            }
            catch (DllNotFoundException)
            {
                errorMessage = "No se encontró la librería 'dokan2.dll' o faltan componentes de Microsoft Visual C++ Redistributable (x64) en esta computadora.";
                return false;
            }
            catch (DokanException ex)
            {
                errorMessage = "El servicio del controlador Dokan no está en ejecución (" + ex.Message + ").\n\nPor favor reinicia tu computadora o ejecuta la aplicación como Administrador.";
                return false;
            }
            catch (Exception ex)
            {
                errorMessage = "No fue posible verificar el servicio de disco virtual: " + ex.Message;
                return false;
            }
        }

        public bool IsMounted(string physicalPath)
        {
            string norm = NormalizarRuta(physicalPath);
            lock (_lock)
            {
                return _montajesActivos.ContainsKey(norm);
            }
        }

        public string? GetMountedDriveLetter(string physicalPath)
        {
            string norm = NormalizarRuta(physicalPath);
            lock (_lock)
            {
                return _montajesActivos.TryGetValue(norm, out string? letra) ? letra : null;
            }
        }

        public IReadOnlyDictionary<string, string> GetActiveMounts()
        {
            lock (_lock)
            {
                return new Dictionary<string, string>(_montajesActivos, StringComparer.OrdinalIgnoreCase);
            }
        }

        public bool Mount(string physicalPath, string driveLetter, string password, out string errorMessage)
        {
            string norm = NormalizarRuta(physicalPath);

            if (!IsDokanAvailable(out errorMessage))
            {
                SecurityAuditLogger.LogError("VAULT_MOUNT_FAIL", $"Dokan no disponible: {errorMessage}", targetPath: norm);
                return false;
            }

            lock (_lock)
            {
                if (_montajesActivos.ContainsKey(norm))
                {
                    errorMessage = $"Esta carpeta ya se encuentra montada como la unidad '{_montajesActivos[norm]}'.";
                    SecurityAuditLogger.LogWarning("VAULT_MOUNT_FAIL", errorMessage, norm);
                    return false;
                }

                if (_montajesActivos.ContainsValue(driveLetter) || Directory.Exists(driveLetter))
                {
                    errorMessage = $"La letra de unidad '{driveLetter}' ya está ocupada o en uso por otro dispositivo.";
                    SecurityAuditLogger.LogWarning("VAULT_MOUNT_FAIL", errorMessage, norm);
                    return false;
                }

                try
                {
                    var dokan = new DokanNet.Dokan(null!);
                    try { dokan.RemoveMountPoint(driveLetter); } catch { }

                    var espejo = new Mirror(norm, password);
                    var builder = new DokanInstanceBuilder(dokan)
                        .ConfigureOptions(o =>
                        {
                            o.Options = DokanOptions.RemovableDrive | DokanOptions.MountManager;
                            o.MountPoint = driveLetter;
                        });

                    var instance = builder.Build(espejo);
                    _instanciasDokan[norm] = instance;
                    _montajesActivos[norm] = driveLetter;

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
                            lock (_lock)
                            {
                                _instanciasDokan.Remove(norm);
                                _montajesActivos.Remove(norm);
                            }
                            VaultUnmounted?.Invoke(norm);
                            SecurityAuditLogger.LogInfo("VAULT_AUTO_UNMOUNT", $"La unidad virtual '{driveLetter}' fue cerrada o desconectada por el sistema.", norm);
                        }
                    })
                    {
                        IsBackground = true,
                        Name = $"DokanWatcher_{driveLetter.Replace(":", "").Replace("\\", "")}"
                    };
                    t.Start();

                    errorMessage = string.Empty;
                    VaultMounted?.Invoke(norm, driveLetter);
                    SecurityAuditLogger.LogSecurity("VAULT_MOUNT", "Montaje de Unidad Virtual", true, $"Montado exitosamente en unidad '{driveLetter}'", norm);
                    return true;
                }
                catch (Exception ex)
                {
                    errorMessage = $"Error al montar la unidad virtual: {ex.Message}";
                    SecurityAuditLogger.LogError("VAULT_MOUNT_ERROR", errorMessage, ex, norm);
                    return false;
                }
            }
        }

        public bool Unmount(string physicalPath, out string errorMessage)
        {
            string norm = NormalizarRuta(physicalPath);
            string? letra;
            DokanInstance? instance;

            lock (_lock)
            {
                if (!_montajesActivos.TryGetValue(norm, out letra))
                {
                    errorMessage = "La carpeta no se encuentra montada actualmente.";
                    return false;
                }
                _instanciasDokan.TryGetValue(norm, out instance);
            }

            try
            {
                var dokan = new DokanNet.Dokan(null!);
                try { dokan.RemoveMountPoint(letra); } catch { }

                if (instance != null)
                {
                    try { instance.Dispose(); } catch { }
                }

                lock (_lock)
                {
                    _instanciasDokan.Remove(norm);
                    _montajesActivos.Remove(norm);
                }

                VaultUnmounted?.Invoke(norm);
                SecurityAuditLogger.LogSecurity("VAULT_UNMOUNT", "Desmontaje de Unidad Virtual", true, $"Desmontado exitosamente de la unidad '{letra}'", norm);
                errorMessage = string.Empty;
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Error al desmontar la unidad: {ex.Message}";
                SecurityAuditLogger.LogError("VAULT_UNMOUNT_ERROR", errorMessage, ex, norm);
                return false;
            }
        }

        public void UnmountAll()
        {
            List<string> rutasActivas;
            lock (_lock)
            {
                rutasActivas = _montajesActivos.Keys.ToList();
            }

            foreach (var r in rutasActivas)
            {
                Unmount(r, out _);
            }
        }

        public void Dispose()
        {
            UnmountAll();
        }

        private static string NormalizarRuta(string ruta)
        {
            try
            {
                return Path.GetFullPath(ruta).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            catch
            {
                return ruta.TrimEnd('\\', '/');
            }
        }
    }
}
