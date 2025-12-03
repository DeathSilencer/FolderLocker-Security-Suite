<div align="center">
    
<img src="assets/icon.png" style="width: 150px; height: auto;" >

# `>_` FolderLocker Security Suite

**Enterprise-grade "Zero-Knowledge" encryption suite for Windows. Virtualize, lock, and vanish your files.**

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-0078D6.svg)](https://www.microsoft.com/)
[![Framework](https://img.shields.io/badge/.NET-8.0-512BD4.svg)](https://dotnet.microsoft.com/)
[![Driver](https://img.shields.io/badge/Dokan-v2.0.6-orange.svg)](https://dokan-dev.github.io/)
[![Status](https://img.shields.io/badge/Status-Stable%20v5.0-success.svg)]()

<br>

| 🛡️ | **New Release v5.0:** | *Now featuring Stealth Architecture & Dokan Driver Integration.* <br> Download the installer below! |
|--|-------------|:---------------------------------------------------------------------------------------------------------------------------------------------------------------|

<br>
</div>

<p align="center">
    <img src="https://raw.githubusercontent.com/bornmay/bornmay/Update/svg/Bottom.svg" alt="Github Stats" />
</p>

---

<details>
    <summary>Expand Table of Contents</summary>
    
<br>
        
- [Purpose](#-purpose)
- [Screenshots](#--screenshots)
- [Download](#--download)
- [Features](#--features)
- [Architecture](#-architecture)
- [Installation](#-installation)
- [Contributions](#-contributions)
- [Credits](#--credits)

</details>

---

## `>_` Purpose

FolderLocker is developed with the goal of providing a powerful, **privacy-focused tool** for Windows users. Unlike traditional folder hiders, this suite uses a **Virtual File System Driver (Dokan)** to encrypt data on-the-fly.

This app is intended for **absolute privacy**. If the vault is not mounted, your files are mathematically inaccessible and invisible on the physical disk.

**Key Use Cases:**
- **Personal Privacy:** Secure photos, documents, and videos away from prying eyes.
- **Data Transport:** Create portable vaults that can only be opened with your credentials.
- **Theft Protection:** Even if your drive is stolen, the file names and contents remain obfuscated (GUIDs).

> [!Caution]
> **Loss of Data Disclaimer:** <br>
> FolderLocker uses AES-256 and SHA-256 encryption. If you lose your Master Password AND your Recovery Code, **your data is mathematically lost forever**. There are no backdoors.

---

## `>_` 📱 Screenshots

<div align="center">
    <br>
    <table>
        <tr>
            <td align="center">
                <strong>Secure Virtual Explorer (M:)</strong><br>
                <img src="assets/screenshot_main.png" width="450" alt="Main Explorer">
            </td>
            <td align="center">
                <strong>Access Control</strong><br>
                <img src="assets/screenshot_login.png" width="350" alt="Login Error">
            </td>
        </tr>
        <tr>
            <td align="center">
                <strong>Process Visualization</strong><br>
                <img src="assets/screenshot_loading.png" width="450" alt="Loading Bar">
            </td>
            <td align="center">
                <strong>Stealth Tray Notification</strong><br>
                <img src="assets/screenshot_tray.png" width="350" alt="Tray Notification">
            </td>
        </tr>
    </table>
    <br>
</div>

---

## `>_` ⬇️ Download

Download the latest `installer.exe` file directly from the [releases page](https://github.com/DeathSilencer/FolderLocker-Security-Suite/releases).

<div align="center">
  <a href="https://github.com/DeathSilencer/FolderLocker-Security-Suite/releases/latest">
    <img src="https://img.shields.io/badge/Download_for_Windows-0078D6?style=for-the-badge&logo=windows&logoColor=white" height="60" />
  </a>
</div>

---

## `>_` Features:

- **Stealth Architecture:** File names are obfuscated on the physical disk (random GUIDs).
- **On-The-Fly Encryption:** Files are decrypted in RAM only when requested. Nothing is stored plain-text.
- **Modern UI:** Clean, "Red Security" theme inspired by modern dashboard designs.
- **Virtual Drive (M:):** Mounts your vault as a real removable drive in "This PC".
- **Multi-User Database:** Encrypted `users.db` supporting multiple isolated accounts.
- **Smart Drag & Drop:** Secure folders instantly by dragging them into the app.
- **Auto-Lock:** Vaults unmount automatically when the application closes.
- **System Tray:** Runs silently in the background with non-intrusive notifications.
- **Fail-Safe:** Transactional database updates to prevent corruption during power loss.

## `>_` Upcoming Features:

- **Cloud Sync:** Encrypted auto-upload to Google Drive / OneDrive.
- **Biometric Login:** Integration with Windows Hello (Fingerprint/Face).
- **Panic Button:** Global hotkey to instantly unmount all drives.
- **Portable Mode:** Run directly from a USB stick without installation.

---

## `>_` Architecture

This project is built using cutting-edge .NET technologies:

| Component | Tech Stack | Description |
| :--- | :--- | :--- |
| **Core** | C# .NET 8.0 | High-performance desktop framework. |
| **Kernel** | DokanNet 2.0.6 | User-mode file system driver wrapper. |
| **Crypto** | AES + SHA256 | Military-grade hashing and stream encryption. |
| **Data** | JSON + Obfuscation | Secure local storage for user profiles. |

---

## `>_` Installation

1.  Download `FolderLocker_Setup.exe`.
2.  Run the installer.
    * *Note:* It will automatically detect if you need the **Dokan Driver**. If missing, it will install it for you silently.
3.  Restart your PC (if drivers were installed).
4.  Launch **FolderLocker** from your desktop.

---

## `>_` Contributions

We welcome contributions! Whether it's bug reports or feature suggestions.

### `>_` How to Contribute
1. **Check Issues**: Browse the issues to see where you can help.
2. **Fork the Repo**: Fork the repository to make your changes.
3. **Submit a PR**: Create a pull request with a description.

---

### `>_` 🙌 Credits & Developer

- 👨‍💻 Developed with ❤️ by **David Platas**
- 🛡️ Powered by the **Dokan Library** project.
- 🎨 UI Icons by **Icons8** & **Flaticon**.

<div align="center">
  <a href="https://github.com/DeathSilencer">
    <img src="https://img.shields.io/badge/GitHub-Profile-black?style=for-the-badge&logo=github" />
  </a>
</div>
