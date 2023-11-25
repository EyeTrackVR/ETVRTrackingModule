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
        { "RightEyeLidExpandedSqueeze", 1f },
        { "LeftEyeLidExpandedSqueeze", 1f },
        { "LeftEyeX", 0f },
        { "RightEyeX", 0f },
        { "EyesY", 0f },
    };

    private ILogger _logger;
    private readonly Config _config;

    public V1Mapper(ILogger logger, ref Config config)
    {
        _logger = logger;
        _config = config;
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

    private void UpdateVRCFTEyeData(ref UnifiedEyeData eyeData, ref UnifiedExpressionShape[] eyeShapes)
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
        var baseRightEyeOpenness = _parameterValues["RightEyeLidExpandedSqueeze"];
        var baseLeftEyeOpenness = _parameterValues["LeftEyeLidExpandedSqueeze"];

        _handleSingleEyeOpenness(ref eyeData.Right, ref eyeShapes, UnifiedExpressions.EyeWideRight,
            UnifiedExpressions.EyeSquintRight, baseRightEyeOpenness, _config.WidenThreshold, _config.SqueezeThreshold);

        _handleSingleEyeOpenness(ref eyeData.Left, ref eyeShapes, UnifiedExpressions.EyeWideLeft,
            UnifiedExpressions.EyeSquintLeft, baseLeftEyeOpenness, _config.WidenThreshold, _config.SqueezeThreshold);
    }

    private void _handleSingleEyeOpenness(
        ref UnifiedSingleEyeData eye,
        ref UnifiedExpressionShape[] eyeShapes,
        UnifiedExpressions widenParam,
        UnifiedExpressions squintParam,
        float baseEyeOpenness,
        float widenThreshold,
        float squeezeThreshold
    )
    {
        eye.Openness = baseEyeOpenness;
        if (_config.ShouldEmulateEyeWiden && baseEyeOpenness >= widenThreshold)
        {
            eyeShapes[(int)widenParam].Weight = Utils.SmoothStep(
                widenThreshold,
                1,
                baseEyeOpenness
            );
            eyeShapes[(int)squintParam].Weight = 0;
        }

        if (_config.ShouldEmulateEyeSquint && baseEyeOpenness <= squeezeThreshold)
        {
            eyeShapes[(int)widenParam].Weight = 0;
            eyeShapes[(int)squintParam].Weight = Utils.SmoothStep(
                squeezeThreshold,
                0,
                baseEyeOpenness
            );
        }
    }

    private void HandleEyeGaze(ref UnifiedEyeData eyeData)
    {
        eyeData.Right.Gaze = new Vector2(_parameterValues["RightEyeX"], _parameterValues["EyesY"]);
        eyeData.Left.Gaze = new Vector2(_parameterValues["LeftEyeX"], _parameterValues["EyesY"]);
    }
}