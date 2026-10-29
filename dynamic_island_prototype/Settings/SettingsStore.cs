using System.IO;
using System.Text.Json;

namespace dynamic_island_prototype.Settings;

public sealed record IslandSettings
{
    public string CalendarUrl { get; init; } = "";
    public string City { get; init; } = "";
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
}

public sealed class SettingsStore
{
    public static string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DynamicIsland", "settings.json");
    public IslandSettings Current { get; private set; } = new();
    public event EventHandler? Changed;
    public SettingsStore()
    {
        try { Current = JsonSerializer.Deserialize<IslandSettings>(File.ReadAllText(FilePath)) ?? new(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException) { }
    }
    public void Save(IslandSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath + ".tmp", JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(FilePath + ".tmp", FilePath, true);
        Current = settings;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
