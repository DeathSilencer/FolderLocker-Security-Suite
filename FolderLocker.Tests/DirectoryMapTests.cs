namespace FolderLocker.Tests
{
    public class DirectoryMapTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly CryptoService _crypto;

        public DirectoryMapTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "DirMapTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
            _crypto = new CryptoService("PasswordMapTest123");
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
        public void AddEntry_And_Persist_ReloadsCorrectly()
        {
            var map = new DirectoryMap(_tempDir, _crypto, autoSave: true);
            var entry1 = map.AddEntry("documento.pdf", false, "documento.pdf");
            var entry2 = map.AddEntry("fotos", true, "fotos");
            var entry3 = map.AddEntry("vacaciones.jpg", false, "fotos\\vacaciones.jpg");

            // Recargar mapa desde disco en nueva instancia
            var reloadedMap = new DirectoryMap(_tempDir, _crypto, autoSave: false);
            var reloadedEntry1 = reloadedMap.GetByPhysicalName(entry1.PhysicalName);
            var reloadedEntry2 = reloadedMap.GetByRelativePath("fotos");
            var reloadedEntry3 = reloadedMap.GetByRelativePath("fotos\\vacaciones.jpg");

            Assert.NotNull(reloadedEntry1);
            Assert.Equal("documento.pdf", reloadedEntry1.RealName);

            Assert.NotNull(reloadedEntry2);
            Assert.True(reloadedEntry2.IsDirectory);

            Assert.NotNull(reloadedEntry3);
            Assert.Equal("vacaciones.jpg", reloadedEntry3.RealName);
        }

        [Fact]
        public void UpdateDirectoryPath_UpdatesNestedRelativePaths()
        {
            var map = new DirectoryMap(_tempDir, _crypto, autoSave: true);
            map.AddEntry("Carpeta A", true, "Carpeta A");
            map.AddEntry("archivo1.txt", false, "Carpeta A\\archivo1.txt");
            map.AddEntry("Subcarpeta", true, "Carpeta A\\Subcarpeta");
            map.AddEntry("archivo2.txt", false, "Carpeta A\\Subcarpeta\\archivo2.txt");

            // Renombrar carpeta
            map.UpdateDirectoryPath("Carpeta A", "Carpeta B");

            Assert.NotNull(map.GetByRelativePath("Carpeta B"));
            Assert.NotNull(map.GetByRelativePath("Carpeta B\\archivo1.txt"));
            Assert.NotNull(map.GetByRelativePath("Carpeta B\\Subcarpeta"));
            Assert.NotNull(map.GetByRelativePath("Carpeta B\\Subcarpeta\\archivo2.txt"));

            // Rutas viejas no deben existir
            Assert.Null(map.GetByRelativePath("Carpeta A"));
            Assert.Null(map.GetByRelativePath("Carpeta A\\archivo1.txt"));
        }

        [Fact]
        public void RemoveEntriesUnderDirectory_RemovesAllDescendants()
        {
            var map = new DirectoryMap(_tempDir, _crypto, autoSave: true);
            map.AddEntry("Carpeta X", true, "Carpeta X");
            map.AddEntry("doc1.txt", false, "Carpeta X\\doc1.txt");
            map.AddEntry("doc2.txt", false, "Carpeta X\\doc2.txt");
            map.AddEntry("fuera.txt", false, "fuera.txt");

            map.RemoveEntriesUnderDirectory("Carpeta X");

            Assert.Null(map.GetByRelativePath("Carpeta X\\doc1.txt"));
            Assert.Null(map.GetByRelativePath("Carpeta X\\doc2.txt"));
            Assert.NotNull(map.GetByRelativePath("fuera.txt"));
        }
    }
}
