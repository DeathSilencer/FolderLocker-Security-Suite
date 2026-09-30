using System;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace FolderLocker.Services.Security
{
    /// <summary>
    /// Servicio que monitorea la actividad del usuario (teclado, ratón y bloqueo de Windows)
    /// para proteger las bóvedas de datos mediante auto-bloqueo tras inactividad.
    /// </summary>
    public class InactivityService : IInactivityService, IMessageFilter
    {
        private readonly System.Windows.Forms.Timer _timer;
        private readonly SynchronizationContext? _syncContext;
        private DateTime _lastActivityUtc;
        private int _timeoutMinutes;
        private bool _isStarted;
        private bool _disposed;

        public int TimeoutMinutes
        {
            get => _timeoutMinutes;
            set
            {
                _timeoutMinutes = Math.Max(0, value);
                if (_timeoutMinutes == 0)
                {
                    _timer.Stop();
                }
                else if (_isStarted && !_timer.Enabled)
                {
                    _timer.Start();
                }
            }
        }

        public bool IsEnabled => _timeoutMinutes > 0;
        public DateTime LastActivityUtc => _lastActivityUtc;
        public TimeSpan ElapsedSinceLastActivity => DateTime.UtcNow - _lastActivityUtc;

        public event Action? InactivityTimeoutElapsed;

        public InactivityService(int timeoutMinutes = 5)
        {
            _timeoutMinutes = Math.Max(0, timeoutMinutes);
            _lastActivityUtc = DateTime.UtcNow;
            _syncContext = SynchronizationContext.Current;

            _timer = new System.Windows.Forms.Timer
            {
                Interval = 3000 // Verificación cada 3 segundos
            };
            _timer.Tick += OnTimerTick;
        }

        public void Start()
        {
            if (_isStarted) return;
            _isStarted = true;
            _lastActivityUtc = DateTime.UtcNow;

            Application.AddMessageFilter(this);
            try
            {
                SystemEvents.SessionSwitch += OnSessionSwitch;
            }
            catch
            {
                // Entornos no interactivos / headless / pruebas unitarias
            }

            if (_timeoutMinutes > 0)
            {
                _timer.Start();
            }
        }

        public void Stop()
        {
            if (!_isStarted) return;
            _isStarted = false;

            _timer.Stop();
            Application.RemoveMessageFilter(this);
            try
            {
                SystemEvents.SessionSwitch -= OnSessionSwitch;
            }
            catch { }
        }

        public void ResetTimer()
        {
            _lastActivityUtc = DateTime.UtcNow;
        }

        public void NotifyUserActivity()
        {
            _lastActivityUtc = DateTime.UtcNow;
        }

        public bool PreFilterMessage(ref Message m)
        {
            // Mensajes de interacción de ratón y teclado en Windows
            const int WM_MOUSEMOVE = 0x0200;
            const int WM_LBUTTONDOWN = 0x0201;
            const int WM_RBUTTONDOWN = 0x0204;
            const int WM_MBUTTONDOWN = 0x0207;
            const int WM_MOUSEWHEEL = 0x020A;
            const int WM_KEYDOWN = 0x0100;
            const int WM_SYSKEYDOWN = 0x0104;

            if (m.Msg is WM_MOUSEMOVE or WM_LBUTTONDOWN or WM_RBUTTONDOWN or WM_MBUTTONDOWN or WM_MOUSEWHEEL or WM_KEYDOWN or WM_SYSKEYDOWN)
            {
                // Throttling: actualizar como máximo una vez por segundo para 0% overhead
                if ((DateTime.UtcNow - _lastActivityUtc).TotalMilliseconds > 1000)
                {
                    _lastActivityUtc = DateTime.UtcNow;
                }
            }

            return false;
        }

        private void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            // Bloqueo inmediato al presionar Win + L si el servicio está habilitado
            if (e.Reason == SessionSwitchReason.SessionLock && IsEnabled)
            {
                DispararTimeout();
            }
        }

        private void OnTimerTick(object? sender, EventArgs e)
        {
            if (!IsEnabled) return;

            if (ElapsedSinceLastActivity.TotalMinutes >= _timeoutMinutes)
            {
                DispararTimeout();
            }
        }

        private void DispararTimeout()
        {
            _lastActivityUtc = DateTime.UtcNow;

            if (_syncContext != null && SynchronizationContext.Current != _syncContext)
            {
                _syncContext.Post(_ => InactivityTimeoutElapsed?.Invoke(), null);
            }
            else
            {
                InactivityTimeoutElapsed?.Invoke();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Stop();
            _timer.Dispose();
        }
    }
}
