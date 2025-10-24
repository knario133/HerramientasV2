using Microsoft.VisualStudio.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace HerramientasV2
{
    public class FormularioGeneraMetodosEnLinq : BaseToolWindow<FormularioGeneraMetodosEnLinq>
    {
        public override string GetTitle(int toolWindowId) => "Genera Metodos en base a Linq";

        public override Type PaneType => typeof(Pane);

        public override Task<FrameworkElement> CreateAsync(int toolWindowId, CancellationToken cancellationToken)
        {
            return Task.FromResult<FrameworkElement>(new FormularioGeneraMetodosEnLinqControl());
        }

        [Guid("4ffa2206-9af4-4d9d-8738-b6d53ffcaba0")]
        internal class Pane : ToolkitToolWindowPane
        {
            public Pane()
            {
                BitmapImageMoniker = KnownMonikers.PasteAppend;
            }
        }
    }
}
