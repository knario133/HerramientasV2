namespace HerramientasV2
{
    [Command(PackageIds.GeneraMetodosEnLinq)]
    internal sealed class GeneraMetodosEnLinq : BaseCommand<GeneraMetodosEnLinq>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await FormularioGeneraMetodosEnLinq.ShowAsync();
        }
    }
}