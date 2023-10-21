using VRCFaceTracking.Core.Params.Data;
using VRCFaceTracking.Core.Params.Expressions;
using VRCFaceTracking.Core.Types;

namespace ETVRTrackingModule.ExpressionStrategies;

public class V1Mapper : IExpressionMapper
{
    private Dictionary<string, float> _parameterValues = new()
    {
        { "RightEyeLidExpandedSqueeze", 0f },
        { "LeftEyeLidExpandedSqueeze", 0f },
        { "LeftEyeX", 0f },
        { "RightEyeX", 0f },
        { "EyesY", 0f },
    };
    
    public void handleOSCMessage(OSCMessage message)
    {
        var paramToMap = IExpressionMapper.GetParamToMap(message.address);
        if (_parameterValues.ContainsKey(paramToMap))
        {
            _parameterValues[paramToMap] = message.value;
        }
    }

    public void UpdateVRCFTEyeData(ref UnifiedEyeData eyeData, ref UnifiedExpressionShape[] eyeShapes)
    {
        HandleEyeOpenness(ref eyeData, ref eyeShapes);
        HandleEyeGaze(ref eyeData);
    }

    private void HandleEyeOpenness(ref UnifiedEyeData eyeData, ref UnifiedExpressionShape[] eyeShapes)
    {
        // we should widen the eye 
        if (_parameterValues["RightEyeLidExpandedSqueeze"] > 0.8f)
        {
            eyeData.Right.Openness = _parameterValues["RightEyeLidExpandedSqueeze"];
            eyeShapes[(int)UnifiedExpressions.EyeWideRight].Weight = 1;
        }
        if (_parameterValues["RightEyeLidExpandedSqueeze"] < 0.0f)
        {
            eyeData.Right.Openness = _parameterValues["RightEyeLidExpandedSqueeze"];
            eyeShapes[(int)UnifiedExpressions.EyeSquintRight].Weight = -1;
        }
        eyeData.Left.Openness = _parameterValues["RightEyeLidExpandedSqueeze"];
        
        if (_parameterValues["LeftEyeLidExpandedSqueeze"] > 0.8f)
        {
            eyeData.Left.Openness = _parameterValues["LeftEyeLidExpandedSqueeze"];
            eyeShapes[(int)UnifiedExpressions.EyeWideLeft].Weight = 1;
        }
        if (_parameterValues["LeftEyeLidExpandedSqueeze"] < 0.0f)
        {
            eyeData.Left.Openness = _parameterValues["LeftEyeLidExpandedSqueeze"];
            eyeShapes[(int)UnifiedExpressions.EyeSquintLeft].Weight = -1;
        }
        eyeData.Left.Openness = _parameterValues["LeftEyeLidExpandedSqueeze"];
    }

    private void HandleEyeGaze(ref UnifiedEyeData eyeData)
    {
        eyeData.Right.Gaze = new Vector2(_parameterValues["RightEyeX"], _parameterValues["EyesY"]);
        eyeData.Left.Gaze = new Vector2(_parameterValues["LeftEyeX"], _parameterValues["EyesY"]);
    }
}