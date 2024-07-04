using VRCFaceTracking.Core.Params.Data;

namespace ETVRTrackingModule.ExpressionStrategies;

public interface IMappingStrategy
{
    public void HandleOSCMessage(OSCMessage message);
}