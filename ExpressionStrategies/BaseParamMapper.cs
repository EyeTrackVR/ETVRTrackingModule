using Microsoft.Extensions.Logging;
using VRCFaceTracking.Core.Params.Data;
using VRCFaceTracking.Core.Params.Expressions;

namespace ETVRTrackingModule.ExpressionStrategies;

public class BaseParamMapper : IMappingStategy
{
    protected ILogger _logger;
    protected readonly Config _config;

    protected OneEuroFilter _leftOneEuroFilter;
    protected OneEuroFilter _rightOneEuroFilter;

    public BaseParamMapper(ILogger logger, ref Config config)
    {
        _leftOneEuroFilter = new OneEuroFilter(minCutoff: 0.1f, beta: 15.0f);
        _rightOneEuroFilter = new OneEuroFilter(minCutoff: 0.1f, beta: 15.0f);
        _logger = logger;
        _config = config;
    }

    public virtual void handleOSCMessage(OSCMessage message)
    {
    }

    protected static string GetParamToMap(string oscAddress)
    {
        var oscUrlSplit = oscAddress.Split("/");
        return oscUrlSplit[^1];
    }

    private protected void _emulateEyeBrow(
        ref UnifiedExpressionShape[] eyeShapes,
        UnifiedExpressions eyebrowExpressionLowerrer,
        UnifiedExpressions eyebrowExpressionUpper,
        ref OneEuroFilter oneEuroFilter,
        float baseEyeOpenness,
        float riseThreshold,
        float lowerThreshold)
    {
        if (!_config.ShouldEmulateEyebrows)
            return;

        var filteredBaseOpenness = (float)oneEuroFilter.Filter(baseEyeOpenness, 1);
        if (filteredBaseOpenness >= riseThreshold)
        {
            eyeShapes[(int)eyebrowExpressionUpper].Weight = Utils.SmoothStep(
                riseThreshold,
                1,
                filteredBaseOpenness
            );
        }

        if (filteredBaseOpenness <= lowerThreshold)
        {
            eyeShapes[(int)eyebrowExpressionLowerrer].Weight = Utils.SmoothStep(
                lowerThreshold,
                1,
                filteredBaseOpenness
            );
        }
    }
}