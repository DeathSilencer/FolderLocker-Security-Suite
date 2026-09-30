[Setup]
; --- INFORMACIÓN BÁSICA ---
AppName=FolderLocker Security Suite
AppVersion=5.2
AppPublisher=David Platas
; Directorio de instalación por defecto (Archivos de Programa)
DefaultDirName={autopf}\FolderLocker Security Suite
PrivilegesRequired=admin

; --- COMPRESIÓN Y SALIDA ---
Compression=lzma2
SolidCompression=yes
OutputDir=Output_Installer
OutputBaseFilename=FolderLocker Setup v5.2

; --- ICONO DEL INSTALADOR (Corregido según tu captura) ---
SetupIconFile=Iconopredeterminado.ico

; --- CONFIGURACIÓN VISUAL ---
DisableDirPage=no
RestartIfNeededByRun=yes
AlwaysRestart=no
WizardStyle=modern

[Languages]
Name: "Spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Files]
; 1. ARCHIVO PRINCIPAL (Corregido: Source coincide con tu captura)
;    DestName: Lo renombra a "FolderLocker.exe" al instalar para que sea más limpio.
Source: "FolderLocker.exe"; DestDir: "{app}"; DestName: "FolderLocker.exe"; Flags: ignoreversion

; 2. ICONO (Lo copiamos también para usarlo en accesos directos si hace falta)
Source: "Iconopredeterminado.ico"; DestDir: "{app}"; Flags: ignoreversion

; 3. INSTALADOR DOKAN (Para instalarlo si falta)
Source: "DokanSetup.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall

; 4. LIBRERÍAS NATIVAS DOKAN Y RUNTIME VISUAL C++ (Garantiza ejecución en cualquier PC)
Source: "dokan2.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "msvcp140.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "vcruntime140.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "vcruntime140_1.dll"; DestDir: "{app}"; Flags: ignoreversion

[Run]
; --- INSTALAR DOKAN (Solo si la función Check lo aprueba) ---
Filename: "{tmp}\DokanSetup.exe"; \
    Parameters: "/passive /norestart"; \
    Description: "Instalando controladores necesarios (Dokan)..."; \
    Flags: waituntilterminated; \
    Check: NeedsDokan

; --- EJECUTAR AL FINALIZAR ---
Filename: "{app}\FolderLocker.exe"; \
    Description: "Ejecutar FolderLocker Security Suite"; \
    Flags: nowait postinstall skipifsilent

[Icons]
; 4. ACCESOS DIRECTOS (Apuntan al nombre limpio "FolderLocker.exe")
Name: "{group}\FolderLocker Security Suite"; Filename: "{app}\FolderLocker.exe"; IconFilename: "{app}\Iconopredeterminado.ico"
Name: "{autodesktop}\FolderLocker Security Suite"; Filename: "{app}\FolderLocker.exe"; IconFilename: "{app}\Iconopredeterminado.ico"
Name: "{group}\Desinstalar FolderLocker"; Filename: "{uninstallexe}"

[UninstallRun]
; NOTA: He comentado la desinstalación forzada de Dokan. 
; Es peligroso borrar drivers compartidos al desinstalar tu app, 
; ya que otros programas podrían estar usándolos.
; Filename: "{sys}\msiexec.exe"; Parameters: "/x {{98EE451F-0205-4B82-90A0-983D4594A1F6}} /qn"; Flags: waituntilterminated skipifdoesntexist

[Code]
// --- LÓGICA ROBUSTA PARA DETECTAR EL CONTROLADOR DOKAN 2 ---

function NeedsDokan(): Boolean;
var
  DriverExists: Boolean;
  ServiceExists: Boolean;
begin
  // 1. Comprobar si el archivo del driver dokan2.sys está físicamente instalado en System32\drivers
  DriverExists := FileExists(ExpandConstant('{sys}\drivers\dokan2.sys'));

  // 2. Comprobar si el servicio dokan2 está registrado en el Registro de Windows
  ServiceExists := RegKeyExists(HKEY_LOCAL_MACHINE, 'SYSTEM\CurrentControlSet\Services\dokan2') or
                   RegKeyExists(HKEY_LOCAL_MACHINE, 'SYSTEM\CurrentControlSet\Services\Dokan2');

  // Si tanto el driver como el servicio existen, Dokan 2 ya está instalado y listo
  if DriverExists and ServiceExists then
  begin
    Result := False;
  end
  else
  begin
    Result := True; // Falta el driver o el servicio, es necesario instalarlo
  end;
end;