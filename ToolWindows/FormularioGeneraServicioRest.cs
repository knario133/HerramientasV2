using Microsoft.VisualStudio.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace HerramientasV2
{
    public class FormularioGeneraServicioRest : BaseToolWindow<FormularioGeneraServicioRest>
    {
        public override string GetTitle(int toolWindowId) => "FormularioGeneraServicioRest";

        public override Type PaneType => typeof(Pane);

        public override Task<FrameworkElement> CreateAsync(int toolWindowId, CancellationToken cancellationToken)
        {
            return Task.FromResult<FrameworkElement>(new FormularioGeneraServicioRestControl());
        }

        [Guid("45f2385d-6afd-4c9b-83b6-fb17b5ea429b")]
        internal class Pane : ToolkitToolWindowPane
        {
            public Pane()
            {
                BitmapImageMoniker = KnownMonikers.ToolWindow;
            }
        }
    }
}
