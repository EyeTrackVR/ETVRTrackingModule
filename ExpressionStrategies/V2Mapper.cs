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
        if (parameterValues.ContainsKey(paramToMap))
        {
            parameterValues[paramToMap] = message.value;

            var singleEyeMode = singleEyeParamNames.Contains(paramToMap);
            UpdateVRCFTEyeData(ref UnifiedTracking.Data.Eye, ref UnifiedTracking.Data.Shapes, singleEyeMode);
        }
    }

    public void UpdateVRCFTEyeData(ref UnifiedEyeData eyeData, ref UnifiedExpressionShape[] eyeShapes, bool singleEyeMode = false)
    {
        handleEyeGaze(ref eyeData, singleEyeMode);
    }
    
    private void handleEyeGaze(ref UnifiedEyeData eyeData, bool singleEyeMode)
    {
        // todo, we can probably drop support but EyeX/EyeY but I'll leave it be for now
        if (singleEyeMode)
        {
            var combinedGaze = new Vector2(parameterValues["EyeX"], parameterValues["EyeY"]);
            eyeData.Left.Gaze = combinedGaze;
            eyeData.Right.Gaze = combinedGaze;
            return;
        }

        eyeData.Left.Gaze = new Vector2(parameterValues["EyeLeftX"], parameterValues["EyeLeftY"]);
        eyeData.Right.Gaze = new Vector2(parameterValues["EyeRightX"], parameterValues["EyeRightY"]);
    }
}