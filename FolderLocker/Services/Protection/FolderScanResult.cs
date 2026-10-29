namespace FolderLocker.Services.Protection
{
    public class FolderScanResult
    {
        public string Path { get; set; } = string.Empty;
        public int TotalFiles { get; set; }
        public long TotalBytes { get; set; }
        public string FormattedSize { get; set; } = "0 B";
        public string EstimatedTime { get; set; } = "< 5s";
        public bool IsLargeVolume { get; set; }
        public int IgnoredJunkFiles { get; set; }
        public int PendingFiles { get; set; }
        public long PendingBytes { get; set; }
        public int AlreadyEncryptedFiles { get; set; }
        public long AlreadyEncryptedBytes { get; set; }
        public bool HasInterruptedProtection { get; set; }
    }

    public class InterruptedProtectionInfo
    {
        public bool IsInterrupted { get; set; }
        public string Path { get; set; } = string.Empty;
        public int EncryptedFilesCount { get; set; }
        public int PendingFilesCount { get; set; }
        public long EncryptedBytes { get; set; }
        public long PendingBytes { get; set; }
        public bool HasJournal { get; set; }
        public string? LastUser { get; set; }
        public DateTime? InterruptedAt { get; set; }
    }

    public class ProtectionValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public bool IsCritical { get; set; }

        public static ProtectionValidationResult Ok() => new() { IsValid = true };
        public static ProtectionValidationResult Fail(string error, bool isCritical = false) => new() { IsValid = false, ErrorMessage = error, IsCritical = isCritical };
    }
}
