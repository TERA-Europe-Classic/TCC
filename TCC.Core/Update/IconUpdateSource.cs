using Nostrum;
using System.IO;

namespace TCC.Update;

public static class IconUpdateSource
{
    private const string Owner = "TERA-Europe-Classic";
    private const string Repository = "tera-used-icons";
    private const string Branch = "master";
    private const string RawBaseUrl = $"https://raw.githubusercontent.com/{Owner}/{Repository}/{Branch}";

    public static string ArchiveUrl => $"https://github.com/{Owner}/{Repository}/archive/{Branch}.zip";
    public static string HashesUrl => $"{RawBaseUrl}/hashes.json";

    public static string GetIconUrl(string directoryName, string iconName)
    {
        return $"{RawBaseUrl}/{directoryName}/{iconName}";
    }

    public static bool NeedsUpdate(string filePath, string expectedHash)
    {
        return !File.Exists(filePath) || HashUtils.GenerateFileHash(filePath) != expectedHash;
    }
}
