using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Nostrum.WPF.Extensions;
using TCC.Utilities;
using TCC.Utils;

namespace TCC.Settings;

/// <summary>
/// Special JsonConvert resolver that allows you to ignore properties. See https://stackoverflow.com/a/13588192/1037948
/// </summary>
public class JsonIgnoreResolver : DefaultContractResolver
{
    private readonly Dictionary<Type, HashSet<string>> _ignores = new();

    /// <summary>
    /// Explicitly ignore the given property(s) for the given type
    /// </summary>
    /// <param name="type"></param>
    /// <param name="propertyName">one or more properties to ignore.  Leave empty to ignore the type entirely.</param>
    public void Ignore(Type type, params string[] propertyName)
    {
        // start bucket if DNE
        if (!_ignores.ContainsKey(type)) _ignores[type] = new HashSet<string>();

        foreach (var prop in propertyName)
        {
            _ignores[type].Add(prop);
        }
    }

    /// <summary>
    /// Is the given property for the given type ignored?
    /// </summary>
    /// <param name="type"></param>
    /// <param name="propertyName"></param>
    /// <returns></returns>
    private bool IsIgnored(Type type, string propertyName)
    {
        if (!_ignores.TryGetValue(type, out var props)) return false;

        // if no properties provided, ignore the type entirely
        return props.Count == 0
            || props.Contains(propertyName);
    }

    /// <summary>
    /// The decision logic goes here
    /// </summary>
    /// <param name="member"></param>
    /// <param name="memberSerialization"></param>
    /// <returns></returns>
    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        var property = base.CreateProperty(member, memberSerialization);

        if (!IsIgnored(property.DeclaringType!, property.PropertyName!)) return property;
        property.ShouldSerialize = _ => false;
        property.ShouldDeserialize = _ => false;
        property.Ignored = true;

        return property;
    }
}
public class JsonSettingsReader : SettingsReaderBase
{

    public JsonSettingsReader()
    {
        FileName = SettingsGlobals.SettingsFileName;

    }

    public SettingsContainer LoadSettings(string path)
    {
        if (!File.Exists(path)) return new SettingsContainer();

        try
        {
            return LoadExistingSettings(path);
        }
        catch (Exception primaryException)
        {
            Log.F($"Failed to load settings from {path}: {primaryException}");
        }

        var backupPath = SettingsGlobals.GetBackupPath(path);
        if (File.Exists(backupPath))
        {
            try
            {
                var settings = LoadExistingSettings(backupPath);
                RestoreBackup(path, backupPath);
                return settings;
            }
            catch (Exception backupException)
            {
                Log.F($"Failed to load settings backup from {backupPath}: {backupException}");
            }
        }

        QuarantineUnreadableSettings(path);
        return new SettingsContainer();
    }

    private static SettingsContainer LoadExistingSettings(string path)
    {
        var file = File.ReadAllText(path);

        #region Compatibility

        file = file.Replace("\"TabName\"", "\"Name\"")
            .Replace("\"ExcludedAuthors\"", "\"HiddenAuthors\"")
            .Replace("\"ExcludedChannels\"", "\"HiddenChannels\"")
            .Replace("\"Channels\"", "\"ShowedChannels\"")
            .Replace("\"Authors\"", "\"ShowedAuthors\"")
            .Replace("\"LanguageOverride\": \"\"", "\"LanguageOverride\" : 0");

        #endregion

        var ret = JsonConvert.DeserializeObject<SettingsContainer>(file, TccUtils.GetDefaultJsonSerializerSettings())
                  ?? new SettingsContainer();

        #region Compatibility

#pragma warning disable CS0612 // Il tipo o il membro è obsoleto

        if (ret.StatSentVersion != App.AppVersion)
        {
            foreach (var special in ret.BuffWindowSettings.Specials)
            {
                ret.AbnormalitySettings.Favorites.Add(special);
            }
            ret.BuffWindowSettings.Specials.Clear();

            foreach (var hidden in ret.BuffWindowSettings.Hidden)
            {
                ret.AbnormalitySettings.Self.Collapsible.Add(hidden);
            }
            ret.BuffWindowSettings.Hidden.Clear();

            foreach (var hidden in ret.GroupWindowSettings.Hidden)
            {
                ret.AbnormalitySettings.Group.Collapsible.Add(hidden);
            }
            ret.GroupWindowSettings.Hidden.Clear();

            foreach (var (cl, list) in ret.BuffWindowSettings.MyAbnormals)
            {
                ret.AbnormalitySettings.Self.Whitelist[cl] = list;
            }
            ret.BuffWindowSettings.MyAbnormals.Clear();

            foreach (var (cl, list) in ret.GroupWindowSettings.GroupAbnormals)
            {
                ret.AbnormalitySettings.Group.Whitelist[cl] = list;
            }
            ret.GroupWindowSettings.GroupAbnormals.Clear();

            ret.AbnormalitySettings.Self.ShowAll = ret.BuffWindowSettings.ShowAll;
            ret.AbnormalitySettings.Group.ShowAll = ret.GroupWindowSettings.ShowAllAbnormalities;
        }

#pragma warning restore CS0612 // Il tipo o il membro è obsoleto

        #endregion

        return ret;
    }

    private static void RestoreBackup(string path, string backupPath)
    {
        try
        {
            QuarantineUnreadableSettings(path);
            File.Copy(backupPath, path, true);
        }
        catch (Exception ex)
        {
            Log.F($"Failed to restore settings backup {backupPath} to {path}: {ex}");
        }
    }

    private static void QuarantineUnreadableSettings(string path)
    {
        try
        {
            if (!File.Exists(path)) return;

            var corruptPath = $"{path}.corrupt-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
            File.Move(path, corruptPath);
        }
        catch (Exception ex)
        {
            Log.F($"Failed to quarantine unreadable settings file {path}: {ex}");
        }
    }
}
