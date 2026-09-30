using System;

namespace FolderLocker.Services.Security
{
    /// <summary>
    /// Contrato de servicio para el monitoreo de inactividad del usuario y auto-bloqueo de seguridad.
    /// </summary>
    public interface IInactivityService : IDisposable
    {
        /// <summary>
        /// Tiempo de inactividad requerido en minutos para disparar el bloqueo. 0 desactiva el auto-bloqueo.
        /// </summary>
        int TimeoutMinutes { get; set; }

        /// <summary>
        /// Indica si el auto-bloqueo está actualmente activo (TimeoutMinutes > 0).
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Marca de tiempo UTC de la última interacción detectada del usuario.
        /// </summary>
        DateTime LastActivityUtc { get; }

        /// <summary>
        /// Tiempo transcurrido desde la última interacción registrada.
        /// </summary>
        TimeSpan ElapsedSinceLastActivity { get; }

        /// <summary>
        /// Evento emitido cuando se cumple el tiempo límite de inactividad o la sesión de Windows se bloquea.
        /// </summary>
        event Action? InactivityTimeoutElapsed;

        /// <summary>
        /// Inicia el monitoreo de eventos de teclado, ratón y temporizador de inactividad.
        /// </summary>
        void Start();

        /// <summary>
        /// Detiene el monitoreo y remueve los filtros de mensajes del sistema.
        /// </summary>
        void Stop();

        /// <summary>
        /// Reinicia manualmente el contador de inactividad (útil al iniciar operaciones largas).
        /// </summary>
        void ResetTimer();

        /// <summary>
        /// Notifica una interacción explícita del usuario para refrescar la marca de tiempo.
        /// </summary>
        void NotifyUserActivity();
    }
}
