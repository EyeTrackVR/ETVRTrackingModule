using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace  ETVRTrackingModule;

public struct Config
{
    [JsonInclude] public ushort PortNumber;
    [JsonInclude] public bool ShouldEmulateEyeWiden;
    [JsonInclude] public bool ShouldEmulateEyeSquint;
    [JsonInclude] public bool ShouldEmulateEyebrows;
    [JsonInclude] public float SqueezeThreshold;
    [JsonInclude] public float WidenThreshold;
    [JsonInclude] public float EyebrowThreshold;
    
    public static Config Default
    {
        get => new () 
        {
            PortNumber = 8889,
            ShouldEmulateEyeWiden = true, 
            ShouldEmulateEyeSquint = true,
            ShouldEmulateEyebrows = true,
            SqueezeThreshold = 0.05f,
            WidenThreshold = 0.95f,
            EyebrowThreshold = 0.9f,
        };
    }
}


public class ETVRConfigManager
{
    private readonly string _configurationFileName = "ETVRModuleConfig.json";
    private readonly string _configFilePath;
    private Config _config = Config.Default;
    public Config Config
    {
        get => _config; 
    }
    
    private List<Action<Config>> _listeners;
    private ILogger _logger;

    public ETVRConfigManager(string configFilePath, ILogger logger)
    {
        _logger = logger;
        _configFilePath = Path.Combine(configFilePath, _configurationFileName);
        _listeners = new List<Action<Config>>();
    }

    public void LoadConfig()
    {
        if (!File.Exists(_configFilePath))
        {
            _logger.LogInformation($"Config file did not exist, creating one at {_configFilePath}");
            SaveConfig();
            return;
        }
        
        
        _logger.LogInformation($"Loading config from {_configFilePath}");
        var jsonData = File.ReadAllText(_configFilePath);
        try
        {
            _config = JsonSerializer.Deserialize<Config>(jsonData);
            NotifyListeners();
        }
        catch (JsonException)
        {
            _logger.LogInformation("Something went wrong during config decoding. Overwriting it with defaults");
            SaveConfig();
        }    
    }

    private void SaveConfig()
    {
        _logger.LogInformation($"Saving config at {_configFilePath}");
        var jsonData = JsonSerializer.Serialize(_config);
        File.WriteAllText(_configFilePath, jsonData);
    }

    public void UpdateConfig<T>(string fieldName, T value)
    {
        var field = _config.GetType().GetField(fieldName);
        
        if (field is null) return;
        
        var type = Nullable.GetUnderlyingType(field.FieldType) ?? field.FieldType;
        var safeValue = Convert.ChangeType(value, type);
        field.SetValue(_config, safeValue);
        NotifyListeners();
    }
    
    private void NotifyListeners()
    {
        foreach (var listener in _listeners)
        {
            listener(_config);
        }
    }

    public void RegisterListener(Action<Config> listener)
    {
        _listeners.Add(listener);
    }
}
