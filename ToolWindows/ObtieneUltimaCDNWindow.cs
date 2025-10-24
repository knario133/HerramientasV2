using Microsoft.VisualStudio.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell.Flavor;
using Community.VisualStudio.Toolkit;


namespace HerramientasV2
{
    public class ObtieneUltimaCDNWindow : BaseToolWindow<ObtieneUltimaCDNWindow>
    {
        public override string GetTitle(int toolWindowId) => "Obtiene Ultimas Librerias y CSS";

        public override Type PaneType => typeof(Pane);

        public override Task<FrameworkElement> CreateAsync(int toolWindowId, CancellationToken cancellationToken)
        {
            return Task.FromResult<FrameworkElement>(new ObtieneUltimaCDNWindowControl());
        }

        [Guid("0bb7d8ea-f65a-4893-9842-b23fd1093041")]
        internal class Pane : ToolkitToolWindowPane
        {
            public Pane()
            {
                BitmapImageMoniker = KnownMonikers.PasteAppend;
            }
        }
    }
}
