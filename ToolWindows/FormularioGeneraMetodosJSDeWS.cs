using Microsoft.VisualStudio.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace HerramientasV2
{
    public class FormularioGeneraMetodosJSDeWS : BaseToolWindow<FormularioGeneraMetodosJSDeWS>
    {
        public override string GetTitle(int toolWindowId) => "Genera Metodos de llamada JS->ASXM";

        public override Type PaneType => typeof(Pane);

        public override Task<FrameworkElement> CreateAsync(int toolWindowId, CancellationToken cancellationToken)
        {
            return Task.FromResult<FrameworkElement>(new FormularioGeneraMetodosJSDeWSControl());
        }

        [Guid("902c262f-617a-4caf-9f83-342abbae3390")]
        internal class Pane : ToolkitToolWindowPane
        {
            public Pane()
            {
                BitmapImageMoniker = KnownMonikers.ToolWindow;
            }
        }
    }
}
