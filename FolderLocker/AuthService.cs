using System.Security.Cryptography;
using System.Text;

namespace FolderLocker
{
    public static class AuthService
    {
        #region 1. HASHING CON SALT (SHA256)

        // Genera un hash irreversible. Ahora pide un "salt" (ej: nombre de usuario)
        // para que la misma contraseña genere hashes distintos en usuarios distintos.
        public static string CalcularHash(string rawData, string salt = "")
        {
            if (string.IsNullOrEmpty(rawData)) return string.Empty;

            using var sha = SHA256.Create();

            // --- IMPLEMENTACIÓN DE SALT ---
            // Mezclamos: Password + Separador + Salt (Usuario)
            string dataConSalt = rawData + "_FL_SALT_" + salt.ToLowerInvariant();

            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(dataConSalt));

            StringBuilder builder = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes) builder.Append(b.ToString("x2"));

            return builder.ToString();
        }

        #endregion

        #region 2. ENCRIPTACIÓN AES (Recuperación)

        // Encripta texto plano usando el Código de Recuperación como llave
        public static string EncriptarAES(string textoPlano, string codigoRecuperacion)
        {
            try
            {
                using var aes = CrearMotorAes(codigoRecuperacion);
                using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                using var ms = new MemoryStream();

                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                using (var sw = new StreamWriter(cs))
                {
                    sw.Write(textoPlano);
                }
                return Convert.ToBase64String(ms.ToArray());
            }
            catch
            {
                return null;
            }
        }

        public static string DesencriptarAES(string textoCifrado, string codigoRecuperacion)
        {
            try
            {
                if (string.IsNullOrEmpty(textoCifrado)) return null;

                byte[] buffer = Convert.FromBase64String(textoCifrado);

                using var aes = CrearMotorAes(codigoRecuperacion);
                using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using var ms = new MemoryStream(buffer);
                using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using var sr = new StreamReader(cs);

                return sr.ReadToEnd();
            }
            catch
            {
                return null;
            }
        }

        private static Aes CrearMotorAes(string keyString)
        {
            var aes = Aes.Create();
            aes.Padding = PaddingMode.PKCS7;
            aes.Mode = CipherMode.CBC;

            using (var sha = SHA256.Create())
            {
                aes.Key = sha.ComputeHash(Encoding.UTF8.GetBytes(keyString));
            }

            aes.IV = new byte[16]; // IV Estático para recuperación local determinista
            return aes;
        }

        #endregion

        #region 3. CALCULADORA DE FORTALEZA

        public static int CalcularFortaleza(string pass)
        {
            if (string.IsNullOrEmpty(pass)) return 0;
            if (pass.Length < 6) return 0;

            double score = 0;
            score += pass.Length * 4;
            if (pass.Any(char.IsLower)) score += 5;
            if (pass.Any(char.IsUpper)) score += 10;
            if (pass.Any(char.IsDigit)) score += 10;
            if (pass.Any(ch => !char.IsLetterOrDigit(ch))) score += 15;

            if (pass.All(char.IsDigit)) score -= 25;
            if (pass.All(char.IsLetter)) score -= 10;
            if (TieneSecuencias(pass)) score -= 30;
            if (TieneRepeticiones(pass)) score -= 15;

            if (score < 20) return 0;
            if (score < 40) return 1;
            if (score < 60) return 2;
            if (score < 80) return 3;
            if (score < 95) return 4;
            return 5;
        }

        private static bool TieneSecuencias(string s)
        {
            if (s.Length < 3) return false;
            s = s.ToLower();
            for (int i = 0; i < s.Length - 2; i++)
            {
                if (char.IsLetterOrDigit(s[i]) && char.IsLetterOrDigit(s[i + 1]) && char.IsLetterOrDigit(s[i + 2]))
                {
                    if (s[i] + 1 == s[i + 1] && s[i + 1] + 1 == s[i + 2]) return true;
                    if (s[i] - 1 == s[i + 1] && s[i + 1] - 1 == s[i + 2]) return true;
                }
            }
            return false;
        }

        private static bool TieneRepeticiones(string s)
        {
            if (s.Length < 3) return false;
            for (int i = 0; i < s.Length - 2; i++)
            {
                if (s[i] == s[i + 1] && s[i + 1] == s[i + 2]) return true;
            }
            return false;
        }

        #endregion
    }
}