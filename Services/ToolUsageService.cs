using System.IO;
using System.Text.Json;
using CadProductivityPalette.Models;

namespace CadProductivityPalette.Services;

public sealed class ToolUsageService
{
    private const int VisibleToolLimit = 6;
    private readonly string _storagePath;
    private readonly Dictionary<string, ToolUsageEntry> _entries =
        new(StringComparer.OrdinalIgnoreCase);

    public ToolUsageService(string? storagePath = null)
    {
        _storagePath = storagePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CadProductivityPalette",
            "tool-usage.json");
        Load();
    }

    public bool HasUsage => _entries.Count > 0;

    public void Record(ToolAction tool)
    {
        if (string.IsNullOrWhiteSpace(tool.CommandText))
        {
            return;
        }

        if (!_entries.TryGetValue(tool.CommandText, out ToolUsageEntry? entry))
        {
            entry = new ToolUsageEntry { CommandText = tool.CommandText };
            _entries.Add(tool.CommandText, entry);
        }

        entry.Count = entry.Count == int.MaxValue ? int.MaxValue : entry.Count + 1;
        entry.LastUsedUtc = DateTimeOffset.UtcNow;
        Save();
    }

    public IReadOnlyList<ToolUsageItem> GetRecent(IEnumerable<ToolAction> tools)
    {
        Dictionary<string, ToolAction> catalog = CreateCatalog(tools);
        return _entries.Values
            .Where(entry => catalog.ContainsKey(entry.CommandText))
            .OrderByDescending(entry => entry.LastUsedUtc)
            .Take(VisibleToolLimit)
            .Select(entry => new ToolUsageItem(
                catalog[entry.CommandText],
                FormatRecentTime(entry.LastUsedUtc)))
            .ToArray();
    }

    public IReadOnlyList<ToolUsageItem> GetFrequent(IEnumerable<ToolAction> tools)
    {
        Dictionary<string, ToolAction> catalog = CreateCatalog(tools);
        return _entries.Values
            .Where(entry => entry.Count >= 2 && catalog.ContainsKey(entry.CommandText))
            .OrderByDescending(entry => entry.Count)
            .ThenByDescending(entry => entry.LastUsedUtc)
            .Take(VisibleToolLimit)
            .Select(entry => new ToolUsageItem(
                catalog[entry.CommandText],
                $"{entry.Count:N0}회"))
            .ToArray();
    }

    public void Clear()
    {
        _entries.Clear();
        Save();
    }

    private void Load()
    {
        if (!File.Exists(_storagePath))
        {
            return;
        }

        try
        {
            string json = File.ReadAllText(_storagePath);
            ToolUsageEntry[] entries = JsonSerializer.Deserialize<ToolUsageEntry[]>(json) ?? [];
            foreach (ToolUsageEntry entry in entries.Where(entry =>
                         !string.IsNullOrWhiteSpace(entry.CommandText) && entry.Count > 0))
            {
                _entries[entry.CommandText] = entry;
            }
        }
        catch (Exception exception) when (exception is IOException
                                          or UnauthorizedAccessException
                                          or JsonException)
        {
            // A missing, locked, or malformed history file must never block the palette.
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
                _entries.Values.OrderByDescending(entry => entry.LastUsedUtc),
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(temporaryPath, json);
            File.Move(temporaryPath, _storagePath, overwrite: true);
        }
        catch (Exception exception) when (exception is IOException
                                          or UnauthorizedAccessException)
        {
            // Usage history is optional; command execution remains available.
        }
    }

    private static Dictionary<string, ToolAction> CreateCatalog(IEnumerable<ToolAction> tools)
    {
        return tools.ToDictionary(tool => tool.CommandText, StringComparer.OrdinalIgnoreCase);
    }

    private static string FormatRecentTime(DateTimeOffset utcTime)
    {
        DateTimeOffset localTime = utcTime.ToLocalTime();
        DateTime today = DateTime.Today;
        if (localTime.Date == today)
        {
            return localTime.ToString("HH:mm");
        }

        if (localTime.Date == today.AddDays(-1))
        {
            return "어제";
        }

        return localTime.ToString("MM/dd");
    }

    public sealed class ToolUsageEntry
    {
        public string CommandText { get; set; } = string.Empty;
        public int Count { get; set; }
        public DateTimeOffset LastUsedUtc { get; set; }
    }
}
