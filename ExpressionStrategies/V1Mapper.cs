using VRCFaceTracking.Core.Params.Data;

namespace ETVRTrackingModule.ExpressionStrategies;

public class V1Mapper : IExpressionMapper
{
    private Dictionary<string, float> parameterValues = new()
    {
        { "RightEyeLidExpandedSqueeze", 0f }, // no fucking idea if this should be EyeWide bullfuck
        { "LeftEyeLidExpandedSqueeze", 0f },
        { "LeftEyeX", 0f },
        { "RightEyeX", 0f },
        { "EyesY", 0f },
    };
    
    private string[] eyeExpressions = new[]
    {
        "RightEyeLidExpandedSqueeze",
        "LeftEyeLidExpandedSqueeze"
    };
    
    public void handleOSCMessage(OSCMessage message)
    {
        string paramToMap = IExpressionMapper.GetParamToMap(message.address);
        if (parameterValues.ContainsKey(paramToMap))
        {
            parameterValues[paramToMap] = message.value;
        }
    }

    public void UpdateVRCFTEyeData(ref UnifiedEyeData eyeData, ref UnifiedExpressionShape[] eyeShapes)
    {
        throw new NotImplementedException();
    }
}