using System.Text;
using FolderLocker;

namespace FolderLocker.Services.Audit
{
    public static class SecurityAuditLogger
    {
        private static readonly object _lock = new();
        private static readonly string _logDir;
        private static readonly string _logFile;
        private const long MaxLogSize = 5 * 1024 * 1024; // 5 MB

        static SecurityAuditLogger()
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                _logDir = Path.Combine(appData, "FolderLocker", "logs");
            }
            catch
            {
                _logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            }

            try
            {
                if (!Directory.Exists(_logDir))
                {
                    Directory.CreateDirectory(_logDir);
                }
            }
            catch { }

            _logFile = Path.Combine(_logDir, "security.log");
        }

        public static string LogFilePath => _logFile;

        public static void LogInfo(string eventType, string message, string? targetPath = null)
        {
            EscribirEntrada("INFO", eventType, message, targetPath);
        }

        public static void LogWarning(string eventType, string message, string? targetPath = null)
        {
            EscribirEntrada("WARN", eventType, message, targetPath);
        }

        public static void LogError(string eventType, string message, Exception? ex = null, string? targetPath = null)
        {
            string detalle = ex != null ? $"{message} | Excepción: {ex.GetType().Name}: {ex.Message}" : message;
            EscribirEntrada("ERROR", eventType, detalle, targetPath);
        }

        public static void LogSecurity(string eventType, string action, bool success, string details, string? targetPath = null)
        {
            string nivel = success ? "AUDIT_OK" : "AUDIT_FAIL";
            string mensaje = $"Acción: {action} | Resultado: {(success ? "EXITOSA" : "RECHAZADA")} | Detalle: {details}";
            EscribirEntrada(nivel, eventType, mensaje, targetPath);
        }

        private static void EscribirEntrada(string nivel, string eventType, string message, string? targetPath)
        {
            try
            {
                string usuario = UserManager.CurrentUser?.Username ?? "SYSTEM/ANON";
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

                var sb = new StringBuilder();
                sb.Append($"[{timestamp}] [{nivel,-9}] [USER: {usuario}] [EVENT: {eventType}] ");

                if (!string.IsNullOrWhiteSpace(targetPath))
                {
                    sb.Append($"[PATH: {targetPath}] ");
                }

                sb.Append(message);

                string linea = sb.ToString();

                lock (_lock)
                {
                    RotarLogSiExcedeLimite();
                    File.AppendAllLines(_logFile, new[] { linea });
                }
            }
            catch
            {
                // La auditoría nunca debe provocar una excepción que detenga el flujo de la aplicación
            }
        }

        private static void RotarLogSiExcedeLimite()
        {
            try
            {
                if (File.Exists(_logFile))
                {
                    var fi = new FileInfo(_logFile);
                    if (fi.Length > MaxLogSize)
                    {
                        string backupFile = Path.Combine(_logDir, "security.log.bak");
                        if (File.Exists(backupFile))
                        {
                            File.Delete(backupFile);
                        }
                        File.Move(_logFile, backupFile);
                    }
                }
            }
            catch { }
        }
    }
}
