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

    [JsonIgnore] private float[] _squeezeThresholdV1;
    [JsonIgnore] private float[] _widenThresholdV1;

    [JsonIgnore] private float[] _squeezeThresholdV2;
    [JsonIgnore] private float[] _widenThresholdV2;


    // describes the minimum and maximum activation thresholds for squeeze for V1 parameters
    // meaning, it will start detecting from value constrained in 0 - 1 space, and stop at 0 - 2.
    [JsonInclude]
    public float[] SqueezeThresholdV1
    {
        get => _squeezeThresholdV1;
        set
        {
            _squeezeThresholdV1 = new[] { Math.Clamp(value[0], 0f, 1f), Math.Clamp(value[1], 0f, 2f) };
        }
    }
    
    [JsonInclude]
    public float[] WidenThresholdV1
    {
        get => _widenThresholdV1;
        set
        {
            _widenThresholdV1 = new[] { Math.Clamp(value[0], 0f, 1f), Math.Clamp(value[1], 0f, 2f) };
        }
    }
    
    [JsonInclude]
    public float[] SqueezeThresholdV2
    {
        get => _squeezeThresholdV2;
        set
        {
            _squeezeThresholdV2 = new[] { Math.Clamp(value[0], 0f, 1f), Math.Clamp(value[1], -2f, 0f) };
        }
    }
    
    [JsonInclude]
    public float[] WidenThresholdV2
    {
        get => _widenThresholdV2;
        set
        {
            _widenThresholdV2 = new[] { Math.Clamp(value[0], 0f, 1f), Math.Clamp(value[1], 0f, 2f) };
        }
    }
    
    // describes by how much the output should be multiplied. 1 by default, 0-2 range. 
    [JsonIgnore] private float _outputMultiplier;

    [JsonInclude]
    public float OutputMultiplier
    {
        get => _outputMultiplier;
        set => _outputMultiplier = Math.Clamp(value, 0f, 2f);
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
            WidenThresholdV1 = new []{ 0.95f, 2f },
            WidenThresholdV2 = new []{ 0.95f, 2f },
            SqueezeThresholdV1 = new []{ 0.05f, 2f },
            SqueezeThresholdV2 = new []{ 0.05f, -2f },
            EyebrowThresholdRising = 0.9f,
            EyebrowThresholdLowering = 0.05f,
            OutputMultiplier = 1f,
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