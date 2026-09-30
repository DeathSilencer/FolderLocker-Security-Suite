using DokanNet;
using System.Text;
using DokanFileAccess = DokanNet.FileAccess;

namespace FolderLocker.Tests
{
    public class MirrorTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly Mirror _mirror;
        private readonly string _password = "TestPassword123!";

        public MirrorTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "FolderLockerTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
            _mirror = new Mirror(_tempDir, _password);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_tempDir))
                {
                    Directory.Delete(_tempDir, true);
                }
            }
            catch { }
        }

        [Fact]
        public void FindFilesWithPattern_FiltersCorrectFile_WhenMultipleFilesExist()
        {
            // Simula el escenario del usuario:
            // Dos archivos en el directorio: "Captura de pantalla (2).png" y "das.txt"
            var mockInfo1 = new MockDokanFileInfo();
            var status1 = _mirror.CreateFile(
                "\\Captura de pantalla (2).png",
                DokanFileAccess.GenericWrite,
                FileShare.ReadWrite,
                FileMode.CreateNew,
                FileOptions.None,
                FileAttributes.Normal,
                mockInfo1);
            Assert.Equal(DokanResult.Success, status1);

            var mockInfo2 = new MockDokanFileInfo();
            var status2 = _mirror.CreateFile(
                "\\das.txt",
                DokanFileAccess.GenericWrite,
                FileShare.ReadWrite,
                FileMode.CreateNew,
                FileOptions.None,
                FileAttributes.Normal,
                mockInfo2);
            Assert.Equal(DokanResult.Success, status2);

            // Verificamos que FindFiles devuelva ambos
            var infoDir = new MockDokanFileInfo();
            var statusFind = _mirror.FindFiles("\\", out var allFiles, infoDir);
            Assert.Equal(DokanResult.Success, statusFind);
            Assert.Equal(2, allFiles.Count);

            // Verificamos que al buscar exactamente "das.txt", devuelva ÚNICAMENTE "das.txt" y NO la captura de pantalla
            var statusPattern = _mirror.FindFilesWithPattern("\\", "das.txt", out var filteredFiles, infoDir);
            Assert.Equal(DokanResult.Success, statusPattern);
            Assert.Single(filteredFiles);
            Assert.Equal("das.txt", filteredFiles[0].FileName);

            // Verificamos que al buscar "*.png", devuelva solo la captura de pantalla
            var statusPng = _mirror.FindFilesWithPattern("\\", "*.png", out var pngFiles, infoDir);
            Assert.Equal(DokanResult.Success, statusPng);
            Assert.Single(pngFiles);
            Assert.Equal("Captura de pantalla (2).png", pngFiles[0].FileName);
        }

        [Fact]
        public void CreateFile_And_WriteFile_And_ReadFile_PreservesData_And_EncryptsOnDisk()
        {
            var mockInfo = new MockDokanFileInfo();
            var statusCreate = _mirror.CreateFile(
                "\\documento.txt",
                DokanFileAccess.GenericWrite | DokanFileAccess.GenericRead,
                FileShare.ReadWrite,
                FileMode.CreateNew,
                FileOptions.None,
                FileAttributes.Normal,
                mockInfo);
            Assert.Equal(DokanResult.Success, statusCreate);

            string textoOriginal = "Hola desde las pruebas unitarias de FolderLocker con AES-256 CTR!";
            byte[] bufferEscritura = Encoding.UTF8.GetBytes(textoOriginal);

            var statusWrite = _mirror.WriteFile("\\documento.txt", bufferEscritura, out int bytesEscritos, 0, mockInfo);
            Assert.Equal(DokanResult.Success, statusWrite);
            Assert.Equal(bufferEscritura.Length, bytesEscritos);

            // Leer a través de Dokan (debe devolver el texto descifrado original)
            byte[] bufferLectura = new byte[bytesEscritos];
            var statusRead = _mirror.ReadFile("\\documento.txt", bufferLectura, out int bytesLeidos, 0, mockInfo);
            Assert.Equal(DokanResult.Success, statusRead);
            Assert.Equal(bytesEscritos, bytesLeidos);
            string textoLeido = Encoding.UTF8.GetString(bufferLectura);
            Assert.Equal(textoOriginal, textoLeido);

            // Verificar que físicamente en disco los datos NO estén en texto plano
            string? physicalPath = mockInfo.Context as string;
            Assert.NotNull(physicalPath);
            Assert.True(File.Exists(physicalPath));
            byte[] bytesFisicos = File.ReadAllBytes(physicalPath);
            string textoFisico = Encoding.UTF8.GetString(bytesFisicos);
            Assert.NotEqual(textoOriginal, textoFisico); // Está cifrado en disco
        }

        [Fact]
        public void SetEndOfFile_TruncatesContentProperly()
        {
            var mockInfo = new MockDokanFileInfo();
            _mirror.CreateFile(
                "\\truncar.txt",
                DokanFileAccess.GenericWrite,
                FileShare.ReadWrite,
                FileMode.CreateNew,
                FileOptions.None,
                FileAttributes.Normal,
                mockInfo);

            byte[] datos = Encoding.UTF8.GetBytes("0123456789ABCDEF");
            _mirror.WriteFile("\\truncar.txt", datos, out _, 0, mockInfo);

            // Truncar a 5 bytes
            var statusSetLength = _mirror.SetEndOfFile("\\truncar.txt", 5, mockInfo);
            Assert.Equal(DokanResult.Success, statusSetLength);

            // GetFileInformation debe reportar 5 bytes
            var statusGetInfo = _mirror.GetFileInformation("\\truncar.txt", out var fileInfo, mockInfo);
            Assert.Equal(DokanResult.Success, statusGetInfo);
            Assert.Equal(5, fileInfo.Length);

            // ReadFile debe leer solo los 5 bytes ("01234")
            byte[] buffer = new byte[10];
            _mirror.ReadFile("\\truncar.txt", buffer, out int bytesLeidos, 0, mockInfo);
            Assert.Equal(5, bytesLeidos);
            Assert.Equal("01234", Encoding.UTF8.GetString(buffer, 0, bytesLeidos));
        }

        [Fact]
        public void MoveFile_RenamesLogicalFileCorrectly()
        {
            var mockInfo = new MockDokanFileInfo();
            _mirror.CreateFile(
                "\\archivo_viejo.txt",
                DokanFileAccess.GenericWrite,
                FileShare.ReadWrite,
                FileMode.CreateNew,
                FileOptions.None,
                FileAttributes.Normal,
                mockInfo);

            byte[] datos = Encoding.UTF8.GetBytes("Contenido prueba renombrado");
            _mirror.WriteFile("\\archivo_viejo.txt", datos, out _, 0, mockInfo);

            // Renombrar
            var statusMove = _mirror.MoveFile("\\archivo_viejo.txt", "\\archivo_nuevo.txt", false, mockInfo);
            Assert.Equal(DokanResult.Success, statusMove);

            // El archivo viejo ya no debe existir
            var infoCheck = new MockDokanFileInfo();
            var statusOld = _mirror.GetFileInformation("\\archivo_viejo.txt", out _, infoCheck);
            Assert.Equal(DokanResult.FileNotFound, statusOld);

            // El archivo nuevo debe existir y tener los datos intactos
            var statusNew = _mirror.GetFileInformation("\\archivo_nuevo.txt", out var infoNuevo, infoCheck);
            Assert.Equal(DokanResult.Success, statusNew);
            Assert.Equal("archivo_nuevo.txt", infoNuevo.FileName);

            byte[] buffer = new byte[datos.Length];
            _mirror.ReadFile("\\archivo_nuevo.txt", buffer, out int bytesRead, 0, infoCheck);
            Assert.Equal("Contenido prueba renombrado", Encoding.UTF8.GetString(buffer, 0, bytesRead));
        }

        [Fact]
        public void DeleteFile_And_Cleanup_RemovesFileCorrectly()
        {
            var mockInfo = new MockDokanFileInfo();
            _mirror.CreateFile(
                "\\borrar.txt",
                DokanFileAccess.GenericWrite,
                FileShare.ReadWrite,
                FileMode.CreateNew,
                FileOptions.None,
                FileAttributes.Normal,
                mockInfo);

            string? pathFisico = mockInfo.Context as string;
            Assert.NotNull(pathFisico);
            Assert.True(File.Exists(pathFisico));

            // Paso 1 de eliminación: DeleteFile marca la intención
            var statusDelete = _mirror.DeleteFile("\\borrar.txt", mockInfo);
            Assert.Equal(DokanResult.Success, statusDelete);

            // Paso 2: El driver marca DeletePending = true y llama a Cleanup al cerrar el Handle
            mockInfo.DeletePending = true;
            _mirror.Cleanup("\\borrar.txt", mockInfo);

            // Verificar que ya no existe físicamente
            Assert.False(File.Exists(pathFisico));

            // Verificar que Dokan reporta FileNotFound
            var infoCheck = new MockDokanFileInfo();
            var statusCheck = _mirror.GetFileInformation("\\borrar.txt", out _, infoCheck);
            Assert.Equal(DokanResult.FileNotFound, statusCheck);
        }

        [Fact]
        public void Subfolder_WithSameFileName_ResolvesCorrectlyWithoutColliding()
        {
            // 1. Crear das.txt en raíz
            var infoRoot = new MockDokanFileInfo();
            _mirror.CreateFile(
                "\\das.txt",
                DokanFileAccess.GenericWrite,
                FileShare.ReadWrite,
                FileMode.CreateNew,
                FileOptions.None,
                FileAttributes.Normal,
                infoRoot);

            byte[] datosRaiz = Encoding.UTF8.GetBytes("Soy das.txt de la RAIZ");
            _mirror.WriteFile("\\das.txt", datosRaiz, out _, 0, infoRoot);

            // 2. Crear Carpeta A
            var infoDir = new MockDokanFileInfo { IsDirectory = true };
            _mirror.CreateFile(
                "\\Carpeta A",
                DokanFileAccess.GenericWrite,
                FileShare.ReadWrite,
                FileMode.CreateNew,
                FileOptions.None,
                FileAttributes.Directory,
                infoDir);

            // 3. Crear das.txt dentro de Carpeta A
            var infoSub = new MockDokanFileInfo();
            _mirror.CreateFile(
                "\\Carpeta A\\das.txt",
                DokanFileAccess.GenericWrite,
                FileShare.ReadWrite,
                FileMode.CreateNew,
                FileOptions.None,
                FileAttributes.Normal,
                infoSub);

            byte[] datosSub = Encoding.UTF8.GetBytes("Soy das.txt de CARPETA A");
            _mirror.WriteFile("\\Carpeta A\\das.txt", datosSub, out _, 0, infoSub);

            // 4. Leer ambos y verificar que cada uno contiene sus propios datos
            byte[] bufRaiz = new byte[datosRaiz.Length];
            var mockRead1 = new MockDokanFileInfo();
            _mirror.ReadFile("\\das.txt", bufRaiz, out int r1, 0, mockRead1);
            Assert.Equal("Soy das.txt de la RAIZ", Encoding.UTF8.GetString(bufRaiz, 0, r1));

            byte[] bufSub = new byte[datosSub.Length];
            var mockRead2 = new MockDokanFileInfo();
            _mirror.ReadFile("\\Carpeta A\\das.txt", bufSub, out int r2, 0, mockRead2);
            Assert.Equal("Soy das.txt de CARPETA A", Encoding.UTF8.GetString(bufSub, 0, r2));

            // 5. Verificar que FindFilesWithPattern en subcarpeta devuelve solo el archivo correspondiente
            var mockDirSearch = new MockDokanFileInfo();
            var statusFind = _mirror.FindFilesWithPattern("\\Carpeta A", "das.txt", out var filesSub, mockDirSearch);
            Assert.Equal(DokanResult.Success, statusFind);
            Assert.Single(filesSub);
            Assert.Equal("das.txt", filesSub[0].FileName);
        }
    }
}
