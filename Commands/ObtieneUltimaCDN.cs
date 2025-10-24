namespace HerramientasV2
{
    [Command(PackageIds.ObtieneUltimaCDN)]
    internal sealed class ObtieneUltimaCDN : BaseCommand<ObtieneUltimaCDN>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ObtieneUltimaCDNWindow.ShowAsync();
        }
    }
}