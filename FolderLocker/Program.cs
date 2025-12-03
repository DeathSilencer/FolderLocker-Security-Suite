using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FolderLocker
{
    internal static class Program
    {
        // --- IMPORTACIONES DE WINDOWS (Para traer la ventana al frente) ---
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        private const int SW_RESTORE = 9;

        // Identificador único para que Windows sepa si ya estamos corriendo
        static readonly string MutexName = "FolderLocker_App_Mutex_v5_Unique_ID";

        [STAThread]
        static void Main()
        {
            // Intentamos reservar el identificador único (Mutex)
            using (Mutex mutex = new Mutex(true, MutexName, out bool createdNew))
            {
                if (createdNew)
                {
                    // --- SOY LA PRIMERA INSTANCIA (Arranca normal) ---
                    // Aquí está tu código original:
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new FormCarpetas());
                }
                else
                {
                    // --- YA EXISTE OTRA VENTANA (Traer al frente y cerrar esta) ---
                    Process current = Process.GetCurrentProcess();
                    foreach (Process process in Process.GetProcessesByName(current.ProcessName))
                    {
                        if (process.Id != current.Id)
                        {
                            IntPtr handle = process.MainWindowHandle;

                            // Si la otra ventana está minimizada, la restauramos
                            if (IsIconic(handle))
                            {
                                ShowWindowAsync(handle, SW_RESTORE);
                            }

                            // La traemos al frente para que el usuario la vea
                            SetForegroundWindow(handle);
                            break;
                        }
                    }

                    // Cerramos esta segunda instancia para no duplicar
                    return;
                }
            }
        }
    }
}