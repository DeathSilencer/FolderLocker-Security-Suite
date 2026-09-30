using System.Security.Cryptography;
using System.Text;

namespace FolderLocker
{
    /// <summary>
    /// Motor de cifrado simétrico robusto AES-256 en modo Counter (CTR).
    /// Proporciona seguridad criptográfica militar de estándar industrial (NIST SP 800-38A)
    /// manteniendo soporte nativo para operaciones de lectura/escritura aleatoria (Random Access/Seek)
    /// sin alterar la longitud de los archivos, indispensable para sistemas de archivos virtuales (Dokan).
    /// </summary>
    public class CryptoService
    {
        #region CAMPOS PRIVADOS

        // Llaves para el modo AES-256 CTR (Estándar v5.2+)
        private readonly byte[] _aesKey;
        private readonly byte[] _nonce;

        // Llave para compatibilidad retroactiva con bóvedas previas (Legacy XOR)
        private readonly byte[] _legacyKey;

        #endregion

        #region PROPIEDADES

        /// <summary>
        /// Indica si la instancia actual está operando en modo de compatibilidad retroactiva (Legacy XOR).
        /// </summary>
        public bool IsLegacyMode { get; set; } = false;

        #endregion

        #region CONSTRUCTOR

        public CryptoService(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentNullException(nameof(password), "La contraseña no puede estar vacía.");

            using (var sha = SHA256.Create())
            {
                // Llave legacy (XOR simple de versiones anteriores)
                _legacyKey = sha.ComputeHash(Encoding.UTF8.GetBytes(password));

                // Derivación criptográfica para AES-256 CTR con salts de dominio específicos
                _aesKey = sha.ComputeHash(Encoding.UTF8.GetBytes("FolderLocker_AES256_Key_Domain_2026::" + password));
                byte[] nonceHash = sha.ComputeHash(Encoding.UTF8.GetBytes("FolderLocker_AES256_Nonce_Domain_2026::" + password));
                _nonce = new byte[8];
                Array.Copy(nonceHash, 0, _nonce, 0, 8);
            }
        }

        #endregion

        #region MÉTODOS PÚBLICOS DE TRANSFORMACIÓN

        /// <summary>
        /// Aplica la transformación simétrica (Cifrado o Descifrado in-place) al buffer.
        /// Utiliza AES-256 CTR por defecto, o Legacy XOR si se detectó una bóveda previa.
        /// </summary>
        /// <param name="buffer">Datos a transformar (in-place).</param>
        /// <param name="offsetArchivo">Posición absoluta en el archivo (Seek).</param>
        /// <param name="bytesALeer">Cantidad de bytes a procesar.</param>
        public void TransformarDatos(byte[] buffer, long offsetArchivo, int bytesALeer)
        {
            if (buffer == null || bytesALeer <= 0) return;
            if (offsetArchivo < 0) throw new ArgumentOutOfRangeException(nameof(offsetArchivo));
            if (bytesALeer > buffer.Length) bytesALeer = buffer.Length;

            if (IsLegacyMode)
            {
                TransformarLegacyXor(buffer, offsetArchivo, bytesALeer);
            }
            else
            {
                TransformarAesCtr(buffer, offsetArchivo, bytesALeer);
            }
        }

        #endregion

        #region MOTORES DE CIFRADO

        /// <summary>
        /// Cifrado/Descifrado AES-256 en modo Counter (CTR).
        /// Permite acceso aleatorio (Seek) en tiempo O(1) con aceleración por hardware (AES-NI).
        /// </summary>
        private void TransformarAesCtr(byte[] buffer, long offsetArchivo, int bytesALeer)
        {
            if (_aesKey == null || _nonce == null) return;

            using var aes = Aes.Create();
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.None;
            aes.Key = _aesKey;

            using var encryptor = aes.CreateEncryptor();

            const int BlockSize = 16;
            const int BatchBlockCount = 64; // Procesa hasta 1024 bytes por lote de aceleración AES
            const int BatchByteSize = BatchBlockCount * BlockSize;

            byte[] counterBatch = new byte[BatchByteSize];
            byte[] keystreamBatch = new byte[BatchByteSize];

            ulong currentBlock = (ulong)(offsetArchivo / BlockSize);
            int offsetInBlock = (int)(offsetArchivo % BlockSize);

            int bufferOffset = 0;
            while (bufferOffset < bytesALeer)
            {
                int bytesRestantes = bytesALeer - bufferOffset;
                int bloquesEnLote = Math.Min(BatchBlockCount, (bytesRestantes + offsetInBlock + BlockSize - 1) / BlockSize);
                int bytesLote = bloquesEnLote * BlockSize;

                // Generar los bloques del contador: [Nonce (8 bytes) | BlockIndex (8 bytes Big-Endian)]
                for (int b = 0; b < bloquesEnLote; b++)
                {
                    int baseIdx = b * BlockSize;
                    Array.Copy(_nonce, 0, counterBatch, baseIdx, 8);

                    ulong blockNum = currentBlock + (ulong)b;
                    for (int i = 0; i < 8; i++)
                    {
                        counterBatch[baseIdx + 15 - i] = (byte)((blockNum >> (i * 8)) & 0xFF);
                    }
                }

                // Generación de keystream mediante cifrado AES-256 acelerado por CPU
                encryptor.TransformBlock(counterBatch, 0, bytesLote, keystreamBatch, 0);

                // Aplicar keystream mediante XOR in-place
                int ksIdx = offsetInBlock;
                while (ksIdx < bytesLote && bufferOffset < bytesALeer)
                {
                    buffer[bufferOffset] ^= keystreamBatch[ksIdx];
                    bufferOffset++;
                    ksIdx++;
                }

                currentBlock += (ulong)bloquesEnLote;
                offsetInBlock = 0;
            }
        }

        /// <summary>
        /// Motor Legacy XOR para descifrar bóvedas creadas con versiones anteriores.
        /// </summary>
        private void TransformarLegacyXor(byte[] buffer, long offsetArchivo, int bytesALeer)
        {
            if (_legacyKey == null || _legacyKey.Length == 0) return;
            int keyLen = _legacyKey.Length;

            for (int i = 0; i < bytesALeer; i++)
            {
                long posicionReal = offsetArchivo + i;
                byte llaveByte = _legacyKey[posicionReal % keyLen];
                buffer[i] = (byte)(buffer[i] ^ llaveByte);
            }
        }

        #endregion
    }
}