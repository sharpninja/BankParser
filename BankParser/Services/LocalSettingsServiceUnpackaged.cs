namespace BankParser.Services;

public class LocalSettingsServiceUnpackaged : ILocalSettingsService
{
    private readonly IFileService _fileService;
    private readonly LocalSettingsOptions _options;
    private readonly string _localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

    private IDictionary<string, object> _settings = new ConcurrentDictionary<string, object>();

    public LocalSettingsServiceUnpackaged(IFileService fileService, IOptions<LocalSettingsOptions> options)
    {
        _fileService = fileService;
        _options = options.Value;
    }

    private async Task InitializeAsync()
    {
        if (_settings is null)
        {
            string folderPath = Path.Combine(_localAppData, _options.ApplicationDataFolder!);
            string fileName = _options.LocalSettingsFile!;
            _settings = await Task.Run(() => _fileService.Read<IDictionary<string, object>>(folderPath, fileName)) ?? new Dictionary<string, object>();
        }
    }

    public async Task<T?> ReadSettingAsync<T>(string key)
    {
        await InitializeAsync();

        object? obj;

        if (_settings.TryGetValue(key, out obj))
        {
            return await Json.ToObjectAsync<T>((string)obj);
        }

        return default;
    }

    public async Task SaveSettingAsync<T>(string key, T value)
    {
        await InitializeAsync();

        _settings[key] = await Json.StringifyAsync(value);

        string folderPath = Path.Combine(_localAppData, _options.ApplicationDataFolder!);
        string fileName = _options.LocalSettingsFile!;
        await Task.Run(() => _fileService.Save(folderPath, fileName, _settings));
    }
}
