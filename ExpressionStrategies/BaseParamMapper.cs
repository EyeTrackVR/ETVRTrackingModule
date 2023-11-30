using Microsoft.Extensions.Logging;
using VRCFaceTracking.Core.Params.Data;
using VRCFaceTracking.Core.Params.Expressions;

namespace ETVRTrackingModule.ExpressionStrategies;

public class BaseParamMapper : IMappingStategy
{
    protected ILogger _logger;
    protected readonly Config _config;

    public BaseParamMapper(ILogger logger, ref Config config)
    {
        _logger = logger;
        _config = config;
    }
    
    public virtual void handleOSCMessage(OSCMessage message) {}

    protected static string GetParamToMap(string oscAddress)
    {
        var oscUrlSplit = oscAddress.Split("/");
        return oscUrlSplit[^1];
    }
    
    private protected void _emulateEyeBrow(
        ref UnifiedExpressionShape[] eyeShapes,
        UnifiedExpressions eyebrowExpressionLowerrer,
        UnifiedExpressions eyebrowExpressionUpper,
        float baseEyeOpenness,
        float riseThreshold,
        float lowerThreshold)
    {
        if (!_config.ShouldEmulateEyebrows)
            return;

        if (baseEyeOpenness >= riseThreshold)
        {
            eyeShapes[(int)eyebrowExpressionUpper].Weight = Utils.SmoothStep(
                riseThreshold,
                1,
                baseEyeOpenness
            );
        }

        if (baseEyeOpenness <= lowerThreshold)
        {
            eyeShapes[(int)eyebrowExpressionLowerrer].Weight = Utils.SmoothStep(
                lowerThreshold,
                1,
                baseEyeOpenness
            );
        }
    }
}