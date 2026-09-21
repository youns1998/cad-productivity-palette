using System.IO;
using System.Text.Json;

namespace CadProductivityPalette.Services;

public sealed class UserSettingsService
{
    private readonly string _storagePath;

    public UserSettingsService(string? storagePath = null)
    {
        _storagePath = storagePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CadProductivityPalette",
            "user-settings.json");
        IsSimpleMode = Load();
    }

    public bool IsSimpleMode { get; private set; }

    public void SetSimpleMode(bool value)
    {
        if (IsSimpleMode == value)
        {
            return;
        }

        IsSimpleMode = value;
        Save();
    }

    private bool Load()
    {
        if (!File.Exists(_storagePath))
        {
            return true;
        }

        try
        {
            string json = File.ReadAllText(_storagePath);
            UserSettings settings = JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();
            return settings.IsSimpleMode;
        }
        catch (Exception exception) when (exception is IOException
                                          or UnauthorizedAccessException
                                          or JsonException)
        {
            // New users and unreadable settings both fall back to the safer simple layout.
            return true;
        }
    }

    private void Save()
    {
        try
        {
            string? directory = Path.GetDirectoryName(_storagePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string temporaryPath = _storagePath + ".tmp";
            string json = JsonSerializer.Serialize(
                new UserSettings { IsSimpleMode = IsSimpleMode },
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(temporaryPath, json);
            File.Move(temporaryPath, _storagePath, overwrite: true);
        }
        catch (Exception exception) when (exception is IOException
                                          or UnauthorizedAccessException)
        {
            // A settings write failure must not prevent the in-session mode change.
        }
    }

    private sealed class UserSettings
    {
        public bool IsSimpleMode { get; set; } = true;
    }
}
