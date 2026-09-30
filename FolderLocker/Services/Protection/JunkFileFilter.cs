using System;
using System.Collections.Generic;
using System.IO;
using FolderLocker.Services.Audit;

namespace FolderLocker.Services.Protection
{
    /// <summary>
    /// Servicio de detección, filtrado y saneamiento de archivos temporales, basura y bloqueos de Windows.
    /// Evita fallos de concurrencia (IOException por locks de sistema en desktop.ini / thumbs.db)
    /// y acelera el cifrado atómico omitiendo archivos innecesarios.
    /// </summary>
    public static class JunkFileFilter
    {
        // 1. Nombres exactos de archivos basura o metadatos del sistema (insensible a mayúsculas)
        private static readonly HashSet<string> ExactJunkFileNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "thumbs.db",
            "ehthumbs.db",
            "ehthumbs_vista.db",
            "desktop.ini",
            ".ds_store",
            "iconcache.db",
            "locker.id",
            "dir.idx",
            "dir.idx.bak",
            "dir.idx.tmp"
        };

        // 2. Extensiones de archivos temporales, swap o descargas incompletas
        private static readonly HashSet<string> JunkExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".tmp",
            ".~tmp",
            ".temp",
            ".bak",
            ".crdownload",
            ".part"
        };

        // 3. Prefijos de bloqueo (ej. archivos de bloqueo de Word/Excel/PowerPoint)
        private static readonly string[] JunkPrefixes =
        {
            "~$",        // Microsoft Office lock file
            "._",        // AppleDouble metadata en particiones Windows
            ".fl_tmp_",  // Archivos de prueba o staging interno de FolderLocker
            ".fl_atomic_"
        };

        // 4. Nombres de directorios de sistema o papelera
        private static readonly HashSet<string> JunkDirectoryNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "$recycle.bin",
            "recycled",
            "system volume information",
            ".trash",
            ".trashes"
        };

        private static bool? _filterEnabledOverride = null;

        /// <summary>
        /// Determina si el filtrado activo está habilitado globalmente.
        /// </summary>
        public static bool IsFilterEnabled
        {
            get
            {
                if (_filterEnabledOverride.HasValue) return _filterEnabledOverride.Value;
                try
                {
                    return Properties.Settings.Default.FiltroArchivosTemporales;
                }
                catch
                {
                    return true;
                }
            }
            set
            {
                _filterEnabledOverride = value;
                try
                {
                    Properties.Settings.Default.FiltroArchivosTemporales = value;
                    Properties.Settings.Default.Save();
                }
                catch { }
            }
        }

        /// <summary>
        /// Comprueba si una ruta de archivo corresponde a un archivo basura, temporal o de metadatos del sistema.
        /// </summary>
        public static bool IsJunkOrTemporaryFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return false;

            string fileName;
            try
            {
                fileName = Path.GetFileName(filePath);
            }
            catch
            {
                return false;
            }

            if (string.IsNullOrEmpty(fileName)) return false;

            // Metadatos internos de FolderLocker siempre se omiten del cifrado de usuario
            if (fileName.Equals("locker.id", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("dir.idx", StringComparison.OrdinalIgnoreCase) ||
                fileName.StartsWith(".fl_", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Si el usuario desactivó el filtro de archivos basura, no omitir los demás
            if (!IsFilterEnabled) return false;

            // 1. Verificación de nombre exacto
            if (ExactJunkFileNames.Contains(fileName))
            {
                return true;
            }

            // 2. Verificación de extensión
            string ext = Path.GetExtension(fileName);
            if (!string.IsNullOrEmpty(ext) && JunkExtensions.Contains(ext))
            {
                return true;
            }

            // 3. Verificación de prefijo
            foreach (string prefix in JunkPrefixes)
            {
                if (fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            // 4. Verificación de directorio padre (ej. si está dentro de $RECYCLE.BIN)
            string? dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir))
            {
                string dirName = Path.GetFileName(dir);
                if (JunkDirectoryNames.Contains(dirName))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Comprueba si un directorio corresponde a una carpeta del sistema o papelera que deba omitirse.
        /// </summary>
        public static bool IsJunkOrTemporaryDirectory(string dirPath)
        {
            if (string.IsNullOrWhiteSpace(dirPath)) return false;

            try
            {
                string dirName = Path.GetFileName(dirPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                return JunkDirectoryNames.Contains(dirName);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Realiza una limpieza segura y silenciosa de archivos temporales desbloqueados (ej. *.tmp, thumbs.db huérfanos).
        /// Nunca lanza excepciones si Windows o una aplicación mantiene un archivo bloqueado.
        /// </summary>
        /// <param name="folderPath">Directorio a sanear antes de proteger.</param>
        /// <returns>Cantidad de archivos basura eliminados con éxito.</returns>
        public static int CleanTemporaryFiles(string folderPath)
        {
            if (!IsFilterEnabled || string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            {
                return 0;
            }

            int eliminados = 0;

            try
            {
                var files = Directory.EnumerateFiles(folderPath, "*.*", SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    string name = Path.GetFileName(file);

                    // No tocar metadatos de FolderLocker
                    if (name.Equals("locker.id", StringComparison.OrdinalIgnoreCase) ||
                        name.Equals("dir.idx", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (IsJunkOrTemporaryFile(file))
                    {
                        try
                        {
                            // Remover atributos de solo lectura u ocultos para permitir eliminación
                            File.SetAttributes(file, FileAttributes.Normal);
                            File.Delete(file);
                            eliminados++;
                        }
                        catch
                        {
                            // Si Windows u otro proceso tiene el archivo abierto/bloqueado, omitir silenciosamente
                        }
                    }
                }

                if (eliminados > 0)
                {
                    SecurityAuditLogger.LogInfo("JUNK_CLEANUP", $"Saneamiento preventivo: Se eliminaron {eliminados} archivos temporales/basura antes del cifrado.", folderPath);
                }
            }
            catch (Exception ex)
            {
                SecurityAuditLogger.LogWarning("JUNK_CLEANUP_WARN", $"Excepción menor durante escaneo de limpieza: {ex.Message}", folderPath);
            }

            return eliminados;
        }
    }
}
