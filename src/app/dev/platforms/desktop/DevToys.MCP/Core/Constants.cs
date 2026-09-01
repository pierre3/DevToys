using DevToys.Core;

namespace DevToys.MCP.Core;

internal static class Constants
{
    internal static readonly string AppCacheDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppHelper.IsPreviewVersion.Value ? "DevToys-MCP-preview" : "DevToys-MCP");

    internal static string AppTempFolder => Path.Combine(AppCacheDirectory, "Temp");
}
