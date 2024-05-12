using Microsoft.Extensions.Logging;
using VRCFaceTracking.Core.Params.Data;
using VRCFaceTracking.Core.Params.Expressions;

namespace ETVRTrackingModule.ExpressionStrategies;

public class BaseParamMapper : IMappingStrategy
{
    protected ILogger _logger;
    protected Config _config;

    protected OneEuroFilter _leftOneEuroFilter;
    protected OneEuroFilter _rightOneEuroFilter;

    public BaseParamMapper(ILogger logger, Config config)
    {
        _leftOneEuroFilter = new OneEuroFilter(minCutoff: 0.1f, beta: 15.0f);
        _rightOneEuroFilter = new OneEuroFilter(minCutoff: 0.1f, beta: 15.0f);
        _logger = logger;
        _config = config;
    }

    public void UpdateConfig(Config config)
    {
        _config = config;
        _logger.LogInformation("config update: {}", config.ShouldEmulateEyeWiden);
    }

    public virtual void HandleOSCMessage(OSCMessage message)
    {
    }

    protected static string GetParamToMap(string oscAddress)
    {
        var oscUrlSplit = oscAddress.Split("/");
        return oscUrlSplit[^1];
    }

    public virtual void UpdateVRCFTEyeData(ref UnifiedEyeData eyeData, ref UnifiedExpressionShape[] eyeShapes)
    {
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