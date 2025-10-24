global using Community.VisualStudio.Toolkit;
global using Microsoft.VisualStudio.Shell;
global using System;
global using Task = System.Threading.Tasks.Task;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace HerramientasV2
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.HerramientasV2String)]
    [ProvideToolWindow(typeof(ObtieneUltimaCDNWindow.Pane))]
    [ProvideToolWindow(typeof(FormularioGeneraMetodosEnLinq.Pane))]
    [ProvideToolWindow(typeof(FormularioGeneraMetodosJSDeWS.Pane))]
    [ProvideToolWindow(typeof(FormularioGeneraFormularioJS.Pane))]
    [ProvideToolWindow(typeof(FormularioGeneraServicioRest.Pane))]
    public sealed class HerramientasV2Package : ToolkitPackage
    {
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            try
            {
                await this.RegisterCommandsAsync();
                this.RegisterToolWindows();
            }
            catch (ReflectionTypeLoadException ex)
            {
                string errorMsg = "Tipos que no se pudieron cargar:\n";
                foreach (var loaderException in ex.LoaderExceptions)
                {
                    errorMsg += loaderException.Message + "\n";
                    if (loaderException is FileNotFoundException fileNotFoundException)
                    {
                        errorMsg += $"Archivo no encontrado: {fileNotFoundException.FileName}\n";
                        errorMsg += $"FusionLog: {fileNotFoundException.FusionLog}\n";
                    }
                }
                System.Diagnostics.Debug.WriteLine(errorMsg);
                throw;
            }

        }
    }
}