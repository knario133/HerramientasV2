using Microsoft.VisualStudio.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace HerramientasV2
{
    public class FormularioGeneraFormularioJS : BaseToolWindow<FormularioGeneraFormularioJS>
    {
        public override string GetTitle(int toolWindowId) => "FormularioGeneraFormularioJS";

        public override Type PaneType => typeof(Pane);

        public override Task<FrameworkElement> CreateAsync(int toolWindowId, CancellationToken cancellationToken)
        {
            return Task.FromResult<FrameworkElement>(new FormularioGeneraFormularioJSControl());
        }

        [Guid("af1ec365-8eab-4c45-b397-fe26f94c2f18")]
        internal class Pane : ToolkitToolWindowPane
        {
            public Pane()
            {
                BitmapImageMoniker = KnownMonikers.ToolWindow;
            }
        }
    }
}
