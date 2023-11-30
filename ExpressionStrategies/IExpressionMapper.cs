using VRCFaceTracking.Core.Params.Data;

namespace ETVRTrackingModule.ExpressionStrategies;

public interface IMappingStategy
{
    public void handleOSCMessage(OSCMessage message);
}