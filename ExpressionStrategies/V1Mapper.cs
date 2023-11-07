using Microsoft.Extensions.Logging;
using VRCFaceTracking;
using VRCFaceTracking.Core.Params.Data;
using VRCFaceTracking.Core.Params.Expressions;
using VRCFaceTracking.Core.Types;

namespace ETVRTrackingModule.ExpressionStrategies;

public class V1Mapper : ImappingStategy
{
    private Dictionary<string, float> _parameterValues = new()
    {
        { "RightEyeLidExpandedSqueeze", 0f },
        { "LeftEyeLidExpandedSqueeze", 0f },
        { "LeftEyeX", 0f },
        { "RightEyeX", 0f },
        { "EyesY", 0f },
    };

    private ILogger _logger;

    public V1Mapper(ILogger logger)
    {
        _logger = logger;
    }
    
    public void handleOSCMessage(OSCMessage message)
    {
        var paramToMap = ImappingStategy.GetParamToMap(message.address);
        if (_parameterValues.ContainsKey(paramToMap))
        {
            _parameterValues[paramToMap] = message.value;
            UpdateVRCFTEyeData(ref UnifiedTracking.Data.Eye, ref UnifiedTracking.Data.Shapes);
        }
    }

    public void UpdateVRCFTEyeData(ref UnifiedEyeData eyeData, ref UnifiedExpressionShape[] eyeShapes)
    {
        HandleEyeOpenness(ref eyeData, ref eyeShapes);
        HandleEyeGaze(ref eyeData);
    }

    private void HandleEyeOpenness(ref UnifiedEyeData eyeData, ref UnifiedExpressionShape[] eyeShapes)
    {
        // so how it works, currently we cannot output values above 1.0 and below 0.0
        // which means, we cannot really output whether someone's squeezing their eyes
        // or making a surprised face. Therefore, we kinda have to cheat. 
        // If we detect that the values provided by ETVR are below or above a certain threshold 
        // we fake the squeeze and widen
        
        // todo, make this configurable via OSC commands I guess, or we switch to sockets
        float squeezeThreshold = 0.1f;
        float widenThreshold = 0.95f;

        float baseRightEyeOpenness = _parameterValues["RightEyeLidExpandedSqueeze"];
        float baseLeftEyeOpenness = _parameterValues["LeftEyeLidExpandedSqueeze"];
        
        float rightYyeOpenness = Math.Clamp(baseRightEyeOpenness, squeezeThreshold, widenThreshold);
        float leftYyeOpenness = Math.Clamp(baseLeftEyeOpenness, squeezeThreshold, widenThreshold);


        eyeData.Right.Openness = rightYyeOpenness;
        eyeData.Left.Openness = leftYyeOpenness;
        
        if (baseRightEyeOpenness >= widenThreshold)
        {
            // todo, figure out how to make this more responsive
            // todo, maybe use a curve to determine the wideness factor based
            // todo, based on the difference between the base openness and clamped? 
            eyeShapes[(int)UnifiedExpressions.EyeWideRight].Weight = 1;
            eyeShapes[(int)UnifiedExpressions.EyeSquintRight].Weight = 0;
        }

        if (baseLeftEyeOpenness >= widenThreshold)
        {
            eyeShapes[(int)UnifiedExpressions.EyeWideLeft].Weight = 1;
            eyeShapes[(int)UnifiedExpressions.EyeSquintLeft].Weight = 0;
        }

        if (baseLeftEyeOpenness <= squeezeThreshold)
        {
            eyeShapes[(int)UnifiedExpressions.EyeWideLeft].Weight = 0;
            eyeShapes[(int)UnifiedExpressions.EyeSquintLeft].Weight = 1;
        }
        
        if (baseRightEyeOpenness <= squeezeThreshold)
        {
            eyeShapes[(int)UnifiedExpressions.EyeWideRight].Weight = 0;
            eyeShapes[(int)UnifiedExpressions.EyeSquintRight].Weight = 1;
        }
    }

    private void HandleEyeGaze(ref UnifiedEyeData eyeData)
    {
        eyeData.Right.Gaze = new Vector2(_parameterValues["RightEyeX"], _parameterValues["EyesY"]);
        eyeData.Left.Gaze = new Vector2(_parameterValues["LeftEyeX"], _parameterValues["EyesY"]);
    }
}