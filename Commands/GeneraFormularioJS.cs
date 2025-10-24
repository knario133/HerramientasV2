using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HerramientasV2.Commands
{
    [Command(PackageIds.GeneraFormularioJS)]
    internal sealed class GeneraFormularioJS : BaseCommand<GeneraFormularioJS>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await FormularioGeneraFormularioJS.ShowAsync();
        }
    }

    
}
