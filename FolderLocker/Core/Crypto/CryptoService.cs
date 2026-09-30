using System.Security.Cryptography;
using System.Text;

namespace FolderLocker
{
    /// <summary>
    /// Motor de cifrado simétrico ligero basado en XOR posicional.
    /// Diseñado para operaciones de lectura/escritura aleatoria (Random Access) en Dokan.
    /// </summary>
    public class CryptoService
    {
        #region CAMPOS PRIVADOS

        // Llave de 32 bytes derivada de la contraseña
        private readonly byte[] _key;

        #endregion

        #region CONSTRUCTOR

        public CryptoService(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentNullException(nameof(password), "La contraseña no puede estar vacía.");

            // Generamos una llave de 32 bytes única y determinista para esta instancia
            using (var sha = SHA256.Create())
            {
                _key = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            }
        }

        #endregion

        #region MÉTODOS DE TRANSFORMACIÓN

        /// <summary>
        /// Aplica una transformación XOR al buffer basándose en la posición física del archivo.
        /// Esta operación es simétrica: sirve tanto para cifrar como para descifrar.
        /// </summary>
        /// <param name="buffer">Datos a transformar (in-place).</param>
        /// <param name="offsetArchivo">Posición absoluta en el archivo (Seek).</param>
        /// <param name="bytesALeer">Cantidad de bytes a procesar.</param>
        public void TransformarDatos(byte[] buffer, long offsetArchivo, int bytesALeer)
        {
            if (_key == null || _key.Length == 0) return;

            // Cacheamos la longitud para evitar accesos repetitivos a la propiedad en el bucle
            int keyLen = _key.Length;

            for (int i = 0; i < bytesALeer; i++)
            {
                // Calculamos la posición absoluta del byte actual
                long posicionReal = offsetArchivo + i;

                // Determinamos qué byte de la llave corresponde a esta posición (Ciclo)
                byte llaveByte = _key[posicionReal % keyLen];

                // Operación XOR: (Datos ^ Llave) = Cifrado  <->  (Cifrado ^ Llave) = Datos
                buffer[i] = (byte)(buffer[i] ^ llaveByte);
            }
        }

        #endregion
    }
}