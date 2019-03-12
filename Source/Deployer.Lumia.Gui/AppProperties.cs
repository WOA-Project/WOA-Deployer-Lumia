namespace Deployer.Lumia.Gui
{
    public static class AppProperties
    {
        public const string GitHubBaseUrl = "https://github.com/WOA-Project/WOA-Deployer-Lumia";
        public static string AppTitle => string.Format(Resources.AppTitle, AppVersionMixin.VersionString);
    }
}