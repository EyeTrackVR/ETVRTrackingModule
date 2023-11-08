using Microsoft.Extensions.Logging;
using VRCFaceTracking;
using VRCFaceTracking.Core.Params.Data;
using VRCFaceTracking.Core.Types;

namespace ETVRTrackingModule.ExpressionStrategies;

public class V2Mapper : ImappingStategy
{
    private readonly string[] _singleEyeParamNames =
    {
        "EyeX",
        "EyeY",
        "EyeLid"
    };
    
    private Dictionary<string, float> _parameterValues = new()
    {
        { "EyeX", 0f },
        { "EyeY", 0f },
        { "EyeLid", 1f },
        
        { "EyeLeftX", 0f },
        { "EyeLeftY", 0f },
        { "EyeRightX", 0f },
        { "EyeRightY", 0f },
        { "EyeLidLeft", 1f },
        { "EyeLidRight", 1f },
    };
    
    private ILogger _logger;

    public V2Mapper(ILogger logger)
    {
        _logger = logger;
    }
    public void handleOSCMessage(OSCMessage message)
    {
        string paramToMap = ImappingStategy.GetParamToMap(message.address);
        if (!_parameterValues.ContainsKey(paramToMap))
            return;
        
        _parameterValues[paramToMap] = message.value;
        var singleEyeMode = _singleEyeParamNames.Contains(paramToMap);
        UpdateVRCFTEyeData(ref UnifiedTracking.Data.Eye, ref UnifiedTracking.Data.Shapes, singleEyeMode);
    }

    private void UpdateVRCFTEyeData(ref UnifiedEyeData eyeData, ref UnifiedExpressionShape[] eyeShapes, bool isSingleEyeMode = false)
    {
        HandleEyeGaze(ref eyeData, isSingleEyeMode);
        HandleEyeOpenness(ref eyeData, ref eyeShapes, isSingleEyeMode);
    }
    
    private void HandleEyeGaze(ref UnifiedEyeData eyeData, bool isSingleEyeMode)
    {
        if (isSingleEyeMode)
        {
            var combinedGaze = new Vector2(_parameterValues["EyeX"], _parameterValues["EyeY"]);
            eyeData.Left.Gaze = combinedGaze;
            eyeData.Right.Gaze = combinedGaze;
            return;
        }

        eyeData.Left.Gaze = new Vector2(_parameterValues["EyeLeftX"], _parameterValues["EyeLeftY"]);
        eyeData.Right.Gaze = new Vector2(_parameterValues["EyeRightX"], _parameterValues["EyeRightY"]);
    }

    private void HandleEyeOpenness(ref UnifiedEyeData eyeData, ref UnifiedExpressionShape[] eyeShapes, bool isSingleEyeMode = false)
    {
        if (isSingleEyeMode)
        {
            var eyeOpenness = _parameterValues["EyeLid"];
            
            eyeData.Left.Openness = eyeOpenness;
            eyeData.Right.Openness = eyeOpenness;
            return;
        }
        
        eyeData.Left.Openness = _parameterValues["EyeLidLeft"];
        eyeData.Right.Openness = _parameterValues["EyeLidRight"];
    }
}