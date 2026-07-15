using System.Text.RegularExpressions;

namespace PaisApp.Services;

public static partial class SecurityHelper
{
    public static string SanitizeInput(string? input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        return ScriptTagRegex().Replace(input, "");
    }

    public static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return string.Empty;
        var name = fileName.Replace("..", "").Replace("~", "").Replace("/", "").Replace("\\", "");
        return name;
    }

    [GeneratedRegex(@"<script[^>]*>.*?</script>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex ScriptTagRegex();
}
