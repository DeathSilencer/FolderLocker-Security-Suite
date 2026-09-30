namespace FolderLocker.Services.VirtualDisk
{
    public interface IVaultService : IDisposable
    {
        bool IsDokanAvailable(out string errorMessage);
        bool IsMounted(string physicalPath);
        string? GetMountedDriveLetter(string physicalPath);
        IReadOnlyDictionary<string, string> GetActiveMounts();
        bool Mount(string physicalPath, string driveLetter, string password, out string errorMessage);
        bool Unmount(string physicalPath, out string errorMessage);
        void UnmountAll();

        event Action<string, string>? VaultMounted;
        event Action<string>? VaultUnmounted;
    }
}
