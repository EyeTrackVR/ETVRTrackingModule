using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using ETVRTrackingModule.Utils;
using Microsoft.Extensions.Logging;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace ETVRTrackingModule;

public struct Config
{
    [JsonConverter(typeof(IPAddressJsonConverter))]
    [JsonInclude] public IPAddress ListeningAddress;
    [JsonInclude] public ushort PortNumber;
    [JsonInclude] public bool ShouldEmulateEyeWiden;
    [JsonInclude] public bool ShouldEmulateEyeSquint;
    [JsonInclude] public bool ShouldEmulateEyebrows;

    [JsonIgnore] private float[] _squeezeThresholdV1;
    [JsonIgnore] private float[] _widenThresholdV1;

    [JsonIgnore] private float[] _squeezeThresholdV2;
    [JsonIgnore] private float[] _widenThresholdV2;


     // describes the minimum and maximum activation thresholds for squeeze emulation for V1 parameters
     // meaning, it will start detecting from value constrained in 0 - 1 space, and do a smooth step
     // with the lower value acting as the lower edge and vice versa.
     // The higher the first value, the later it will activate,
     // and the higher the second value, the less pronounced the effect will be 
    [JsonInclude]
    public float[] SqueezeThresholdV1
    {
        get => _squeezeThresholdV1;
        set
        {
            _squeezeThresholdV1 = new[] { Math.Clamp(value[0], 0f, 1f), Math.Clamp(value[1], 0f, 2f) };
        }
    }
    
     // describes the minimum and maximum activation thresholds for widen emulation for V1 parameters
     // meaning, it will start detecting from value constrained in 0 - 1 space, and do a smooth step
     // with the lower value acting as the lower edge and vice versa.
     // The higher the first value, the later it will activate,
     // and the higher the second value, the less pronounced the effect will be
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
            ListeningAddress = IPAddress.Loopback,
            PortNumber = 8889,
            ShouldEmulateEyeWiden = false,
            ShouldEmulateEyeSquint = false,
            ShouldEmulateEyebrows = false,
            WidenThresholdV1 = new []{ 0.95f, 1f },
            WidenThresholdV2 = new []{ 0.95f, 1.05f },
            SqueezeThresholdV1 = new []{ 0.05f, 0.5f },
            SqueezeThresholdV2 = new []{ 0.05f, -1f },
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

    private Dictionary<string, string> etvr_to_config_map = new()
    {
        {"gui_VRCFTModuleIPAddress", "ListeningAddress"},
        {"gui_VRCFTModulePort", "PortNumber" },
        {"gui_ShouldEmulateEyeWiden", "ShouldEmulateEyeWiden"},
        {"gui_ShouldEmulateEyeSquint", "ShouldEmulateEyeSquint"},
        {"gui_ShouldEmulateEyebrows", "ShouldEmulateEyebrows"},
        {"gui_WidenThresholdV1_min", "WidenThresholdV1"},
        {"gui_WidenThresholdV1_max", "WidenThresholdV1"},
        {"gui_WidenThresholdV2_min", "WidenThresholdV2"},
        {"gui_WidenThresholdV2_max", "WidenThresholdV2"},
        {"gui_SqueezeThresholdV1_min", "SqueezeThresholdV1"},
        {"gui_SqueezeThresholdV1_max", "SqueezeThresholdV1"},
        {"gui_SqueezeThresholdV2_min", "SqueezeThresholdV2"},
        {"gui_SqueezeThresholdV2_max", "SqueezeThresholdV2"},
        {"gui_EyebrowThresholdRising", "EyebrowThresholdRising"},
        {"gui_EyebrowThresholdLowering", "EyebrowThresholdLowering"},
        {"gui_OutputMultiplier", "OutputMultiplier"},
    };

    private List<string> etvr_half_values = new()
    {
        "gui_WidenThresholdV1_min",
        "gui_WidenThresholdV1_max",
        "gui_WidenThresholdV2_min",
        "gui_WidenThresholdV2_max",
        "gui_SqueezeThresholdV1_min",
        "gui_SqueezeThresholdV1_max",
        "gui_SqueezeThresholdV2_min",
        "gui_SqueezeThresholdV2_max",
    };

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

    public void UpdateConfig(string OSCFieldName, OSCValue value)
    {
        string? fieldName;
        if (!etvr_to_config_map.TryGetValue(OSCFieldName, out fieldName))
            return;

        (Type, Type) mappers;
        if (!OSCValueUtils.OSCTypeMap.TryGetValue(value.Type, out mappers))
            return;

        var oscValueInstance = Convert.ChangeType(value, mappers.Item2);
        var field = oscValueInstance.GetType().GetField("value");
        var oscValue = field!.GetValue(oscValueInstance);
        
        if (etvr_half_values.Contains(OSCFieldName))
            HandleHalfFields(OSCFieldName, fieldName, oscValue);
        else
            HandleSingleField(fieldName, oscValue);

        SaveConfig();
        NotifyListeners();
    }

    private void HandleSingleField<T>(string fieldName, T value)
    {
        var field = _config.GetType().GetField(fieldName);
        if (field is null) return;

        object boxedConfig = _config;

        var type = Nullable.GetUnderlyingType(field.FieldType) ?? field.FieldType;
        var safeValue = Convert.ChangeType(value, type);

        _logger.LogInformation($"[UPDATE] updating field {field} to {safeValue}");
        field.SetValue(boxedConfig, safeValue);
        _config = (Config)boxedConfig;
    }

    private void HandleHalfFields<T>(string OSCFieldName, string fieldName, T value)
    {
        var propertyInfo = _config.GetType().GetProperty(fieldName);
        if (propertyInfo is null) return;
        
        var oldValueInfo = propertyInfo.GetValue(_config);
        if (oldValueInfo is null) return;

        object boxedConfig = _config;

        var minMaxIndice = OSCFieldName.Split("_").Last() == "min" ? 1 : 0;
        var valueToPreserve = ((Array)oldValueInfo).GetValue(minMaxIndice)!;

        // TODO I kinda hate this, preferably this should support any type, but I don't have any 
        // idea on how to do that yet
        var safeValue = Convert.ChangeType(value, typeof(float))!;
        float[] updatedValue; 
        
        if(minMaxIndice == 0)
            updatedValue = new float[] { (float)valueToPreserve, (float)safeValue };
        else
            updatedValue = new float[] { (float)safeValue, (float)valueToPreserve };

        _logger.LogInformation($"[UPDATE] updating field {fieldName} to [{updatedValue[0]}, {updatedValue[1]}]");

        propertyInfo.SetValue(boxedConfig, updatedValue);
        _config = (Config)boxedConfig;
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