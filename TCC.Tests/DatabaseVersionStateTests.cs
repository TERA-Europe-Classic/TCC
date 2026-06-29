using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using TCC;
using TCC.Data.Databases;
using TCC.Update;

namespace TCC.Tests;

public class DatabaseVersionStateTests
{
    [Fact]
    public void CheckVersionClearsPreviousOutdatedStateWhenFileNowMatchesHash()
    {
        using var temp = new TempDataRoot();
        var originalDataPath = App.DataPath;
        SetAppDataPath(temp.Path);

        try
        {
            Directory.CreateDirectory(Path.Combine(temp.Path, "testdb"));
            var dbPath = Path.Combine(temp.Path, "testdb", "testdb-EU-EN.txt");
            File.WriteAllText(dbPath, "old");

            UpdateManager.DatabaseHashes.Clear();
            UpdateManager.DatabaseHashes["testdb/testdb-EU-EN.txt"] = Sha256("new");

            var db = new TestDatabase("EU-EN");
            db.CheckVersion();
            Assert.False(db.IsUpToDate);

            File.WriteAllText(dbPath, "new");
            db.CheckVersion();

            Assert.True(db.IsUpToDate);
        }
        finally
        {
            UpdateManager.DatabaseHashes.Clear();
            SetAppDataPath(originalDataPath);
        }
    }

    private static void SetAppDataPath(string value)
    {
        typeof(App).GetProperty(nameof(App.DataPath), BindingFlags.Public | BindingFlags.Static)!
            .SetValue(null, value);
    }

    private static string Sha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private sealed class TestDatabase(string lang) : DatabaseBase(lang)
    {
        protected override string FolderName => "testdb";
        protected override string Extension => "txt";

        public override void Load()
        {
        }
    }

    private sealed class TempDataRoot : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "tcc-database-version-tests",
            Guid.NewGuid().ToString("N"));

        public TempDataRoot()
        {
            Directory.CreateDirectory(Path);
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
