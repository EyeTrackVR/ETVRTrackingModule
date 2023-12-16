using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace ETVRTrackingModule;

public struct Config
{
    [JsonInclude] public ushort PortNumber;
    [JsonInclude] public bool ShouldEmulateEyeWiden;
    [JsonInclude] public bool ShouldEmulateEyeSquint;
    [JsonInclude] public bool ShouldEmulateEyebrows;

    [JsonIgnore] private float[] _widenSqueezeThreshold;

    [JsonInclude]
    public float[] WidenSqueezeThreshold
    {
        get => _widenSqueezeThreshold;
        set { _widenSqueezeThreshold = new[] { Math.Clamp(value[0], 0f, 1f), Math.Clamp(value[1], 0f, 1f) }; }
    }

    [JsonIgnore] private float[] _maxWidenSqueezeThresholdV1;

    [JsonInclude]
    public float[] MaxWidenSqueezeThresholdV1
    {
        get => _maxWidenSqueezeThresholdV1;
        set { _maxWidenSqueezeThresholdV1 = new[] { Math.Clamp(value[0], 0f, 1f), Math.Clamp(value[1], 0f, 1f) }; }
    }


    [JsonIgnore] private float[] _maxWidenSqueezeThresholdV2;

    [JsonInclude]
    public float[] MaxWidenSqueezeThresholdV2
    {
        get => _maxWidenSqueezeThresholdV2;
        set { _maxWidenSqueezeThresholdV2 = new[] { Math.Clamp(value[0], -1.4f, 0), Math.Clamp(value[1], 0, 1.4f) }; }
    }

    [JsonInclude] public float EyebrowThresholdRising;
    [JsonInclude] public float EyebrowThresholdLowering;

    public static Config Default
    {
        get => new()
        {
            PortNumber = 8889,
            ShouldEmulateEyeWiden = true,
            ShouldEmulateEyeSquint = true,
            ShouldEmulateEyebrows = true,
            WidenSqueezeThreshold = new[] { 0.05f, 0.95f },
            EyebrowThresholdRising = 0.9f,
            EyebrowThresholdLowering = 0.05f,
            MaxWidenSqueezeThresholdV1 = new[] { 0f, 1f },
            MaxWidenSqueezeThresholdV2 = new[] { -1.4f, 1.4f },
        };
    }
}

public class ETVRConfigManager
{
    private readonly string _configurationFileName = "ETVRModuleConfig.json";
    private readonly string _configFilePath;
    private Config _config = Config.Default;
    public Config Config => _config;

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

        object boxedConfig = _config;

        var type = Nullable.GetUnderlyingType(field.FieldType) ?? field.FieldType;
        var safeValue = Convert.ChangeType(value, type);

        _logger.LogInformation($"[UPDATE] updating field {field} to {safeValue}");
        field.SetValue(boxedConfig, safeValue);
        _config = (Config)boxedConfig;

        SaveConfig();
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