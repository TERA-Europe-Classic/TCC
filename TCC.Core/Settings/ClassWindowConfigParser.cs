using System;
using System.IO;
using Newtonsoft.Json;
using TCC.Utilities;
using TeraDataLite;

namespace TCC.Settings;

public class ClassWindowConfigParser
{
    public ClassWindowConfigData Data { get; }

    public ClassWindowConfigParser(Class c, string? resourcesPath = null)
    {
        var filePath = ConfigPath(c, resourcesPath);
        if (!File.Exists(filePath))
        {
            Data = new ClassWindowConfigData();
            return;
        }

        Data = JsonConvert.DeserializeObject<ClassWindowConfigData>(
                   File.ReadAllText(filePath),
                   TccUtils.GetDefaultJsonSerializerSettings())
               ?? new ClassWindowConfigData();
    }

    public static void Save(Class c, ClassWindowConfigData data, string? resourcesPath = null)
    {
        if (c is Class.None) return;

        var filePath = ConfigPath(c, resourcesPath);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? throw new InvalidOperationException());
        File.WriteAllText(filePath, JsonConvert.SerializeObject(data, TccUtils.GetDefaultJsonSerializerSettings()));
    }

    private static string ConfigPath(Class c, string? resourcesPath = null)
    {
        return Path.Combine(
            resourcesPath ?? App.ResourcesPath,
            "config",
            "class-window-skills",
            $"{c.ToString().ToLower()}-skills.json");
    }
}
