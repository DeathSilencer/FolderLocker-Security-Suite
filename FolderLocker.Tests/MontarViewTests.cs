using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using FolderLocker.UI.Views;
using Xunit;

namespace FolderLocker.Tests
{
    public class MontarViewTests
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
        public void MontarView_CargarCarpetas_LoadsAllVaultsCorrectly()
        {
            RunOnSta(() =>
            {
                var view = new MontarView();
                var vaults = new List<string>
                {
                    @"C:\Users\User\BovedaPersonal",
                    @"D:\Trabajo\DocumentosConfidenciales",
                    @"E:\Finanzas2026"
                };

                view.CargarCarpetas(vaults);

                Assert.Equal(3, view.TotalCarpetasCount);
                Assert.Equal(3, view.CarpetasFiltradasCount);
                Assert.Equal(@"C:\Users\User\BovedaPersonal", view.CarpetaSeleccionada);
            });
        }

        [Fact]
        public void MontarView_Filter_FiltersBySubstringInRealTime()
        {
            RunOnSta(() =>
            {
                var view = new MontarView();
                var vaults = new List<string>
                {
                    @"C:\Users\User\BovedaPersonal",
                    @"D:\Trabajo\DocumentosConfidenciales",
                    @"E:\Finanzas2026"
                };

                view.CargarCarpetas(vaults);

                // Filtrar por término que solo coincide con una bóveda
                view.SearchBox.Text = "Confidenciales";

                Assert.Equal(3, view.TotalCarpetasCount);
                Assert.Equal(1, view.CarpetasFiltradasCount);
                Assert.Equal(@"D:\Trabajo\DocumentosConfidenciales", view.CarpetaSeleccionada);
            });
        }

        [Fact]
        public void MontarView_Filter_MultiWordMatchingWorks()
        {
            RunOnSta(() =>
            {
                var view = new MontarView();
                var vaults = new List<string>
                {
                    @"C:\Users\User\BovedaPersonal",
                    @"D:\Trabajo\DocumentosConfidenciales",
                    @"E:\Finanzas2026"
                };

                view.CargarCarpetas(vaults);

                // Multi-palabra: "Trabajo Confidenciales"
                view.SearchBox.Text = "Trabajo Confidenciales";

                Assert.Equal(1, view.CarpetasFiltradasCount);
                Assert.Equal(@"D:\Trabajo\DocumentosConfidenciales", view.CarpetaSeleccionada);
            });
        }

        [Fact]
        public void MontarView_Filter_CaseInsensitiveSearch()
        {
            RunOnSta(() =>
            {
                var view = new MontarView();
                var vaults = new List<string>
                {
                    @"C:\Users\User\BovedaPersonal",
                    @"D:\Trabajo\DocumentosConfidenciales",
                    @"E:\Finanzas2026"
                };

                view.CargarCarpetas(vaults);

                view.SearchBox.Text = "bovedapersonal";

                Assert.Equal(1, view.CarpetasFiltradasCount);
                Assert.Equal(@"C:\Users\User\BovedaPersonal", view.CarpetaSeleccionada);
            });
        }

        [Fact]
        public void MontarView_Filter_ClearingFilterRestoresAllVaults()
        {
            RunOnSta(() =>
            {
                var view = new MontarView();
                var vaults = new List<string>
                {
                    @"C:\Users\User\BovedaPersonal",
                    @"D:\Trabajo\DocumentosConfidenciales",
                    @"E:\Finanzas2026"
                };

                view.CargarCarpetas(vaults);

                view.SearchBox.Text = "Finanzas";
                Assert.Equal(1, view.CarpetasFiltradasCount);

                view.LimpiarFiltro();

                Assert.Equal(3, view.CarpetasFiltradasCount);
            });
        }

        [Fact]
        public void MontarView_Filter_NoMatchesYieldsZeroItems()
        {
            RunOnSta(() =>
            {
                var view = new MontarView();
                var vaults = new List<string>
                {
                    @"C:\Users\User\BovedaPersonal",
                    @"D:\Trabajo\DocumentosConfidenciales"
                };

                view.CargarCarpetas(vaults);

                view.SearchBox.Text = "RutaInexistente999";

                Assert.Equal(0, view.CarpetasFiltradasCount);
                Assert.Null(view.CarpetaSeleccionada);
            });
        }

        [Fact]
        public void MontarView_CargarCarpetas_SpecificSelectionRetained()
        {
            RunOnSta(() =>
            {
                var view = new MontarView();
                var vaults = new List<string>
                {
                    @"C:\Users\User\BovedaPersonal",
                    @"D:\Trabajo\DocumentosConfidenciales",
                    @"E:\Finanzas2026"
                };

                view.CargarCarpetas(vaults, @"E:\Finanzas2026");

                Assert.Equal(@"E:\Finanzas2026", view.CarpetaSeleccionada);
            });
        }
    }
}
