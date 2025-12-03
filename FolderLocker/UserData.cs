using System.Text.Json;

namespace FolderLocker
{
    // --- MODELO DE DATOS DEL USUARIO ---
    public class UserProfile
    {
        public string Username { get; set; } = string.Empty;
        public string LoginPasswordHash { get; set; } = string.Empty;
        public string RecoveryCodeHash { get; set; } = string.Empty;
        public string MasterPasswordHash { get; set; } = string.Empty;
        public string EncryptedLoginPassword { get; set; } = string.Empty;
        public List<string> LockedFolders { get; set; } = new List<string>();
    }

    // --- GESTOR DE USUARIOS (DATABASE BLINDADA EN APPDATA) ---
    public static class UserManager
    {
        #region CAMPOS Y PROPIEDADES

        public static UserProfile CurrentUser { get; private set; }
        private static List<UserProfile> _users = new List<UserProfile>();

        // Rutas de Archivos (Ahora dinámicas)
        private static readonly string _dbPath;
        private static readonly string _bakPath;

        // LLAVE DEL SISTEMA (Ofuscación)
        private const string _sysInternalKey = "FolderLocker_System_Key_v5_Protection_Layer_2025";

        #endregion

        #region CONSTRUCTOR ESTÁTICO (Configuración de Rutas y Migración)

        static UserManager()
        {
            // 1. Definir la ruta profesional: C:\Users\TuUsuario\AppData\Local\FolderLockerSuite
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string myAppFolder = Path.Combine(appDataFolder, "FolderLockerSuite");

            // Asegurarnos de que la carpeta exista
            if (!Directory.Exists(myAppFolder))
            {
                Directory.CreateDirectory(myAppFolder);
            }

            // Definir las rutas finales
            _dbPath = Path.Combine(myAppFolder, "users_v6.db");
            _bakPath = Path.Combine(myAppFolder, "users_v6.db.bak");

            // --- 2. AUTO-MIGRACIÓN (Mover datos viejos si existen) ---
            // Si hay una DB vieja junto al .exe, la movemos a la nueva carpeta segura
            string oldDbPath = Path.Combine(Application.StartupPath, "users_v6.db");
            string oldBakPath = Path.Combine(Application.StartupPath, "users_v6.db.bak");

            try
            {
                if (File.Exists(oldDbPath) && !File.Exists(_dbPath))
                {
                    File.Move(oldDbPath, _dbPath);
                }

                if (File.Exists(oldBakPath) && !File.Exists(_bakPath))
                {
                    File.Move(oldBakPath, _bakPath);
                }
            }
            catch
            {
                // Si falla mover (ej: permisos), no hacemos nada, se creará una nueva.
            }
        }

        #endregion

        #region CARGA Y GUARDADO (I/O)

        public static void LoadDatabase()
        {
            // 1. Intentar cargar DB Principal
            if (IntentarCargar(_dbPath)) return;

            // 2. Si falla, intentar cargar Backup
            if (File.Exists(_bakPath))
            {
                if (IntentarCargar(_bakPath))
                {
                    SaveDatabase(); // Restaurar integridad
                    return;
                }
            }

            // 3. Si todo falla, iniciamos limpio
            _users = new List<UserProfile>();
        }

        private static bool IntentarCargar(string ruta)
        {
            if (!File.Exists(ruta)) return false;
            try
            {
                string rawContent = File.ReadAllText(ruta);
                if (string.IsNullOrWhiteSpace(rawContent)) return false;

                string json = string.Empty;

                // INTENTO 1: ¿ESTÁ ENCRIPTADA?
                string decrypted = AuthService.DesencriptarAES(rawContent, _sysInternalKey);

                if (decrypted != null) json = decrypted;
                else json = rawContent; // INTENTO 2: Legacy (Texto plano)

                var data = JsonSerializer.Deserialize<List<UserProfile>>(json);
                if (data != null)
                {
                    _users = data;
                    return true;
                }
            }
            catch { }
            return false;
        }

        public static void SaveDatabase()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = false };
                string json = JsonSerializer.Serialize(_users, options);

                string encryptedContent = AuthService.EncriptarAES(json, _sysInternalKey);

                if (File.Exists(_dbPath)) File.Copy(_dbPath, _bakPath, true);

                File.WriteAllText(_dbPath, encryptedContent);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error crítico guardando usuarios: " + ex.Message, "Error de Sistema", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region LÓGICA DE NEGOCIO (AUTH)

        public static bool Register(string username, string password, string recoveryCode)
        {
            if (_users.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
                return false;

            var newUser = new UserProfile
            {
                Username = username,
                LoginPasswordHash = AuthService.CalcularHash(password, username), // Con Salt
                RecoveryCodeHash = AuthService.CalcularHash(recoveryCode, username), // Con Salt
                EncryptedLoginPassword = AuthService.EncriptarAES(password, recoveryCode),
                LockedFolders = new List<string>()
            };

            _users.Add(newUser);
            SaveDatabase();
            return true;
        }

        public static bool Login(string username, string password)
        {
            string passHash = AuthService.CalcularHash(password, username);

            var user = _users.FirstOrDefault(u =>
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

            if (user != null && user.LoginPasswordHash == passHash)
            {
                CurrentUser = user;
                return true;
            }
            return false;
        }

        public static string RecoverLoginPassword(string username, string recoveryCode)
        {
            var user = _users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            if (user == null) return null;

            string codeHash = AuthService.CalcularHash(recoveryCode, username);
            if (user.RecoveryCodeHash != codeHash) return null;

            return AuthService.DesencriptarAES(user.EncryptedLoginPassword, recoveryCode);
        }

        public static void SetMasterPassword(string masterPass)
        {
            if (CurrentUser != null)
            {
                CurrentUser.MasterPasswordHash = AuthService.CalcularHash(masterPass, CurrentUser.Username);
                SaveDatabase();
            }
        }

        public static void DeleteCurrentUser()
        {
            if (CurrentUser != null && _users.Contains(CurrentUser))
            {
                _users.Remove(CurrentUser);
                CurrentUser = null;
                SaveDatabase();
            }
        }

        #endregion
    }
}