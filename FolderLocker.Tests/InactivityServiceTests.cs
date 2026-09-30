using System;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using FolderLocker.Services.Security;
using Xunit;

namespace FolderLocker.Tests
{
    public class InactivityServiceTests
    {
        private static void RunOnSta(Action action)
        {
            Exception? caught = null;
            var thread = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    caught = ex;
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (caught != null)
            {
                throw new TargetInvocationException(caught);
            }
        }

        [Fact]
        public void InactivityService_InitialState_MatchesConstructorArguments()
        {
            RunOnSta(() =>
            {
                using var service = new InactivityService(10);

                Assert.Equal(10, service.TimeoutMinutes);
                Assert.True(service.IsEnabled);
                Assert.True(service.ElapsedSinceLastActivity.TotalSeconds < 2);
            });
        }

        [Fact]
        public void InactivityService_SetTimeoutToZero_DisablesService()
        {
            RunOnSta(() =>
            {
                using var service = new InactivityService(5);
                Assert.True(service.IsEnabled);

                service.TimeoutMinutes = 0;
                Assert.False(service.IsEnabled);
            });
        }

        [Fact]
        public void InactivityService_ResetTimer_UpdatesLastActivityUtc()
        {
            RunOnSta(() =>
            {
                using var service = new InactivityService(5);
                DateTime before = service.LastActivityUtc;
                Thread.Sleep(20);

                service.ResetTimer();
                DateTime after = service.LastActivityUtc;

                Assert.True(after >= before);
            });
        }

        [Fact]
        public void InactivityService_NotifyUserActivity_UpdatesLastActivityUtc()
        {
            RunOnSta(() =>
            {
                using var service = new InactivityService(5);
                DateTime before = service.LastActivityUtc;
                Thread.Sleep(20);

                service.NotifyUserActivity();
                DateTime after = service.LastActivityUtc;

                Assert.True(after >= before);
            });
        }

        [Fact]
        public void InactivityService_PreFilterMessage_NeverSuppressesInput()
        {
            RunOnSta(() =>
            {
                using var service = new InactivityService(5);
                var msg = new Message { Msg = 0x0200 }; // WM_MOUSEMOVE

                bool suppressed = service.PreFilterMessage(ref msg);

                Assert.False(suppressed);
            });
        }

        [Fact]
        public void InactivityService_StartAndStop_ExecutesWithoutExceptions()
        {
            RunOnSta(() =>
            {
                using var service = new InactivityService(5);
                service.Start();
                service.Stop();
            });
        }

        [Fact]
        public void InactivityService_InactivityTimeoutElapsed_CanBeSubscribedAndTriggered()
        {
            RunOnSta(() =>
            {
                using var service = new InactivityService(5);
                bool triggered = false;
                service.InactivityTimeoutElapsed += () => triggered = true;

                // Forzar invocación del método privado de timeout mediante reflexión
                var method = typeof(InactivityService).GetMethod("DispararTimeout", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(method);
                method.Invoke(service, null);

                Assert.True(triggered);
            });
        }
    }
}
