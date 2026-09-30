using System.Text;

namespace FolderLocker.Tests
{
    public class CryptoServiceTests
    {
        [Fact]
        public void TransformarDatos_Roundtrip_RestoresOriginalBytes()
        {
            var crypto = new CryptoService("ClaveSecretaPruebas123!");
            byte[] original = Encoding.UTF8.GetBytes("Este es un texto de prueba para cifrado y descifrado con AES-256 CTR.");
            byte[] cifrado = (byte[])original.Clone();

            // Cifrar
            crypto.TransformarDatos(cifrado, 0, cifrado.Length);
            Assert.NotEqual(original, cifrado);

            // Descifrar con otra instancia con la misma contraseña
            var decryptor = new CryptoService("ClaveSecretaPruebas123!");
            byte[] descifrado = (byte[])cifrado.Clone();
            decryptor.TransformarDatos(descifrado, 0, descifrado.Length);

            Assert.Equal(original, descifrado);
            Assert.Equal(Encoding.UTF8.GetString(original), Encoding.UTF8.GetString(descifrado));
        }

        [Fact]
        public void TransformarDatos_DifferentOffsets_MaintainsStreamIntegrity()
        {
            var crypto = new CryptoService("PasswordStreamingTest");
            byte[] original = new byte[1024];
            new Random(42).NextBytes(original);

            // Cifrar completo
            byte[] cifradoCompleto = (byte[])original.Clone();
            crypto.TransformarDatos(cifradoCompleto, 0, cifradoCompleto.Length);

            // Descifrar en chunks arbitrarios con offsets
            byte[] descifradoChunks = new byte[original.Length];
            var decryptor = new CryptoService("PasswordStreamingTest");

            // Chunk 1: offset 0 a 100
            byte[] c1 = new byte[100];
            Array.Copy(cifradoCompleto, 0, c1, 0, 100);
            decryptor.TransformarDatos(c1, 0, 100);
            Array.Copy(c1, 0, descifradoChunks, 0, 100);

            // Chunk 2: offset 100 a 512
            byte[] c2 = new byte[412];
            Array.Copy(cifradoCompleto, 100, c2, 0, 412);
            decryptor.TransformarDatos(c2, 100, 412);
            Array.Copy(c2, 0, descifradoChunks, 100, 412);

            // Chunk 3: offset 512 al final
            int rem = original.Length - 512;
            byte[] c3 = new byte[rem];
            Array.Copy(cifradoCompleto, 512, c3, 0, rem);
            decryptor.TransformarDatos(c3, 512, rem);
            Array.Copy(c3, 0, descifradoChunks, 512, rem);

            Assert.Equal(original, descifradoChunks);
        }

        [Fact]
        public void TransformarDatos_WrongPassword_ProducesCorruptedData()
        {
            var cryptoCorrecto = new CryptoService("MiPasswordCorrecto");
            var cryptoErroneo = new CryptoService("PasswordIncorrecto");

            byte[] original = Encoding.UTF8.GetBytes("Mensaje confidencial.");
            byte[] data = (byte[])original.Clone();

            cryptoCorrecto.TransformarDatos(data, 0, data.Length);
            cryptoErroneo.TransformarDatos(data, 0, data.Length);

            Assert.NotEqual(original, data);
        }
    }
}
