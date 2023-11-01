using Microsoft.Extensions.Logging;
using VRCFaceTracking.Core.Params.Data;

namespace ETVRTrackingModule.ExpressionStrategies;

public class V2Mapper : ImappingStategy
{
    private Dictionary<string, float> parameterValues = new()
    {
        { "EyeX", 0f },
        { "EyeLeftX", 0f },
        { "EyeRightX", 0f },
        { "EyeLeftY", 0f },
        { "EyeRightY", 0f },
        { "EyeLid", 0f },
        { "EyeLidLeft", 0f },
        { "EyeLidRight", 0f },
    };
    
    private ILogger _logger;

    public V2Mapper(ILogger logger)
    {
        _logger = logger;
    }
    public void handleOSCMessage(OSCMessage message)
    {
        string paramToMap = ImappingStategy.GetParamToMap(message.address);
        if (parameterValues.ContainsKey(paramToMap))
        {
            parameterValues[paramToMap] = message.value;
        }
    }

    public void UpdateVRCFTEyeData(ref UnifiedEyeData eyeData, ref UnifiedExpressionShape[] eyeShapes)
    {
    }
}