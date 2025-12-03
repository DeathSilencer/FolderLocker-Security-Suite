<div align="center">

  <img src="assets/icon.png" alt="FolderLocker Logo" width="120" height="120" />

  # FolderLocker Security Suite
  
  **Suite de Seguridad "Zero-Knowledge" para Windows con Virtualización de Sistema de Archivos**

  [![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
  [![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-0078D6.svg)](https://www.microsoft.com/)
  [![Framework](https://img.shields.io/badge/.NET-8.0-512BD4.svg)](https://dotnet.microsoft.com/)
  [![Driver](https://img.shields.io/badge/Dokan-v2.0.6-orange.svg)](https://dokan-dev.github.io/)
  [![Status](https://img.shields.io/badge/Status-Stable%20v5.0-success.svg)]()

  <p align="center">
    <a href="#-descarga">Descargar</a> •
    <a href="#-características">Características</a> •
    <a href="#-instalación">Instalación</a> •
    <a href="#-tecnología">Tecnología</a>
  </p>

</div>

---

> **FolderLocker v5.0** transforma la seguridad de archivos en Windows. A diferencia de los ocultadores de carpetas tradicionales, FolderLocker utiliza un **Driver de Sistema de Archivos Virtual (Dokan)** para encriptar los datos al vuelo. Si la unidad no está montada, tus archivos son matemáticamente inaccesibles e invisibles.

## 📥 Descarga

Descarga la última versión estable desde la sección de Releases:

<div align="center">
  <a href="https://github.com/DeathSilencer/FolderLocker-Security-Suite/releases/latest">
    <img src="https://img.shields.io/badge/Descargar-Instalador_v5.0-red?style=for-the-badge&logo=windows&logoColor=white" height="50" />
  </a>
</div>

---

## 📸 Capturas de Pantalla

<div align="center">
  <table>
    <tr>
      <td align="center">
        <strong>Acceso Seguro</strong><br>
        <img src="assets/screenshot_login.png" width="400" alt="Login Screen">
      </td>
      <td align="center">
        <strong>Gestión de Bóvedas</strong><br>
        <img src="assets/screenshot_main.png" width="400" alt="Main Dashboard">
      </td>
    </tr>
  </table>
</div>

---

## 🔥 Características Principales

### 🛡️ Seguridad de Grado Militar
* **Arquitectura "Stealth":** Los nombres de archivo se ofuscan en el disco físico (GUIDs). Nadie sabrá qué archivos tienes aunque accedan al disco duro.
* **Cifrado On-The-Fly:** Los archivos se descifran en memoria RAM solo cuando los usas. Nunca se guardan descifrados en el disco.
* **Multi-Usuario:** Base de datos encriptada (`users_v6.db`) con soporte para múltiples cuentas aisladas.

### 💾 Virtualización Avanzada
* **Unidad Virtual (M:):** Tus carpetas protegidas aparecen como una unidad USB extraíble real en "Este Equipo".
* **Compatibilidad Total:** Abre fotos, edita Words o reproduce videos directamente desde la unidad virtual sin tener que extraerlos.
* **Driver Dokan:** Utiliza el motor `DokanNet` para una integración nativa con el Kernel de Windows.

### 🎨 Experiencia de Usuario (UX)
* **Interfaz "Red Security":** Diseño moderno en modo oscuro, sin bordes y con controles personalizados.
* **Drag & Drop:** Arrastra carpetas para protegerlas al instante.
* **Protección contra Errores:** Evita proteger carpetas de sistema (Windows) o unidades raíz para prevenir daños al SO.
* **Sistema de Bandeja:** Se minimiza al reloj del sistema para trabajar en segundo plano sin estorbar.

---

## 🛠️ Instalación

1.  Descarga el archivo `FolderLocker_Setup.exe`.
2.  Ejecuta el instalador.
    * *Nota:* El instalador detectará automáticamente si necesitas los controladores de **Dokan Library**. Si no los tienes, los instalará por ti silenciosamente.
3.  Reinicia tu PC si se instalaron los controladores por primera vez.
4.  Ejecuta **FolderLocker** desde el escritorio.

---

## 💻 Tecnología y Desarrollo

Este proyecto ha sido desarrollado en **C# (.NET 8.0)** utilizando tecnologías de vanguardia para escritorio:

| Componente | Tecnología | Descripción |
| :--- | :--- | :--- |
| **Core** | .NET 8.0 | Windows Forms con gestión de memoria optimizada. |
| **Driver** | DokanNet 2.0.6 | Wrapper para el driver de sistema de archivos en modo usuario. |
| **Criptografía** | SHA256 + AES | Hashing con Salt para usuarios y cifrado de flujo para archivos. |
| **Base de Datos** | System.Text.Json | Persistencia local ofuscada y encriptada. |
| **UI** | GDI+ Custom | Controles personalizados dibujados a mano para el tema oscuro. |

---

## ⚠️ Disclaimer

> Este software está diseñado para proteger la privacidad personal. El desarrollador no se hace responsable de la pérdida de datos causada por el olvido de contraseñas maestras o códigos de recuperación. **Guarda tu código de recuperación (REC-XXXX) en un lugar seguro.**

---

<div align="center">
  <p>Desarrollado con ❤️ y mucho ☕ por <strong>David Platas</strong></p>
  <p>
    <a href="https://github.com/DeathSilencer">Perfil de GitHub</a>
  </p>
</div>
