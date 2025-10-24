namespace HerramientasV2
{
    [Command(PackageIds.GeneraMetodosJSDeWS)]
    internal sealed class GeneraMetodosJSDeWS : BaseCommand<GeneraMetodosJSDeWS>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await FormularioGeneraMetodosJSDeWS.ShowAsync();
        }
    }
}