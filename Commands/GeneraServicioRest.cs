namespace HerramientasV2
{
    [Command(PackageIds.GeneraServicioRest)]
    internal sealed class GeneraServicioRest : BaseCommand<GeneraServicioRest>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await FormularioGeneraServicioRest.ShowAsync();
        }
    }
}