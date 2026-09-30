using FolderLocker.Services.VirtualDisk;

namespace FolderLocker.Services.Protection
{
    public interface IFolderProtectionService
    {
        FolderScanResult ScanFolder(string path);
        ProtectionValidationResult ValidateCanProtect(string path, IVaultService vaultService);
        ProtectionValidationResult ValidateCanRestore(string path, IVaultService vaultService);
        System.Threading.Tasks.Task ProtectFolderAsync(string path, string password, IProgress<Tuple<int, string>> progress, System.Threading.ManualResetEventSlim? pauseEvent = null, CancellationToken ct = default);
        System.Threading.Tasks.Task RestoreFolderAsync(string path, string password, IProgress<Tuple<int, string>> progress, System.Threading.ManualResetEventSlim? pauseEvent = null, CancellationToken ct = default);
        void CrearMarcador(string path);
        void QuitarMarcador(string path);
        bool EsCarpetaProtegida(string path);
    }
}
