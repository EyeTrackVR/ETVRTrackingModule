using VRCFaceTracking.Core.Params.Data;

namespace ETVRTrackingModule.ExpressionStrategies;

public interface ImappingStategy
{
    public static string GetParamToMap(string oscAddress)
    {
        var oscUrlSplit = oscAddress.Split("/");
        return oscUrlSplit[^1];
    }
    
    public void handleOSCMessage(OSCMessage message);
    public void UpdateVRCFTEyeData( ref UnifiedEyeData eyeData, ref UnifiedExpressionShape[] eyeShapes);
}