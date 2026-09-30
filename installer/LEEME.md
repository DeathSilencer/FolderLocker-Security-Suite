# Instalador y Prerrequisitos de FolderLocker

Para ejecutar FolderLocker en una máquina nueva o clonar el repositorio:

### 1. Prerrequisito obligatorio: Controlador Dokan
El montaje de la unidad virtual encriptada (`M:`) requiere el driver a nivel de sistema de Dokan.
- **Ejecutar:** `DokanSetup.exe` (incluido en esta carpeta).
- Instalarlo con permisos de Administrador y reiniciar el equipo si el instalador lo solicita.

### 2. Ejecución desde Visual Studio
Una vez instalado `DokanSetup.exe`:
- Abre `FolderLocker.sln` en Visual Studio.
- Presiona `F5` (las DLLs nativas de C++ y Dokan ya están configuradas en el proyecto y se copian automáticamente al compilar).

### 3. Generación del instalador completo (opcional)
- El archivo `FolderLocker.iss` contiene el script para compilar el instalador final usando **Inno Setup**.
