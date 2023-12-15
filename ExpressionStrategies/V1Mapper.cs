using Microsoft.Extensions.Logging;
using VRCFaceTracking;
using VRCFaceTracking.Core.Params.Data;
using VRCFaceTracking.Core.Params.Expressions;
using VRCFaceTracking.Core.Types;

namespace ETVRTrackingModule.ExpressionStrategies;

public class V1Mapper : BaseParamMapper
{
    private Dictionary<string, float> _parameterValues = new()
    {
        { "RightEyeLidExpandedSqueeze", 1f },
        { "LeftEyeLidExpandedSqueeze", 1f },
        { "LeftEyeX", 0f },
        { "RightEyeX", 0f },
        { "EyesY", 0f },
    };

    public V1Mapper(ILogger logger, ref Config config) : base(logger, ref config)
    {
    }

    public override void handleOSCMessage(OSCMessage message)
    {
        var paramToMap = GetParamToMap(message.address);
        if (_parameterValues.ContainsKey(paramToMap))
        {
            _parameterValues[paramToMap] = message.value;
            UpdateVRCFTEyeData(ref UnifiedTracking.Data.Eye, ref UnifiedTracking.Data.Shapes);
        }
    }

    private void UpdateVRCFTEyeData(ref UnifiedEyeData eyeData, ref UnifiedExpressionShape[] eyeShapes)
    {
        HandleEyeGaze(ref eyeData);
        HandleEyeOpenness(ref eyeData, ref eyeShapes);
        EmulateEyeBrows(ref eyeShapes);
    }

    private void HandleEyeGaze(ref UnifiedEyeData eyeData)
    {
        eyeData.Right.Gaze = new Vector2(_parameterValues["RightEyeX"], _parameterValues["EyesY"]);
        eyeData.Left.Gaze = new Vector2(_parameterValues["LeftEyeX"], _parameterValues["EyesY"]);
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
            UnifiedExpressions.EyeSquintRight, baseRightEyeOpenness, _config.WidenThreshold, _config.SqueezeThreshold,
            _config.MaxWidenThreshold, _config.MaxSqueezeThreshold);

        _handleSingleEyeOpenness(ref eyeData.Left, ref eyeShapes, UnifiedExpressions.EyeWideLeft,
            UnifiedExpressions.EyeSquintLeft, baseLeftEyeOpenness, _config.WidenThreshold, _config.SqueezeThreshold,
            _config.MaxWidenThreshold, _config.MaxSqueezeThreshold);
    }

    private void _handleSingleEyeOpenness(
        ref UnifiedSingleEyeData eye,
        ref UnifiedExpressionShape[] eyeShapes,
        UnifiedExpressions widenParam,
        UnifiedExpressions squintParam,
        float baseEyeOpenness,
        float widenThreshold,
        float squeezeThreshold,
        float maxWidenThreshold,
        float maxSqueezeThreshold
    )
    {
        eye.Openness = baseEyeOpenness;
        if (_config.ShouldEmulateEyeWiden && baseEyeOpenness >= widenThreshold)
        {
            eyeShapes[(int)widenParam].Weight = Utils.SmoothStep(
                widenThreshold,
                maxWidenThreshold,
                baseEyeOpenness
            );
            eyeShapes[(int)squintParam].Weight = 0;
        }

        if (_config.ShouldEmulateEyeSquint && baseEyeOpenness <= squeezeThreshold)
        {
            eyeShapes[(int)widenParam].Weight = 0;
            eyeShapes[(int)squintParam].Weight = Utils.SmoothStep(
                squeezeThreshold,
                maxSqueezeThreshold,
                baseEyeOpenness
            );
        }
    }

    private void EmulateEyeBrows(ref UnifiedExpressionShape[] eyeShapes)
    {
        var baseRightEyeOpenness = _parameterValues["RightEyeLidExpandedSqueeze"];
        var baseLeftEyeOpenness = _parameterValues["LeftEyeLidExpandedSqueeze"];

        _emulateEyeBrow(
            ref eyeShapes,
            UnifiedExpressions.BrowLowererRight,
            UnifiedExpressions.BrowOuterUpRight,
            baseRightEyeOpenness,
            _config.EyebrowThresholdRising,
            _config.EyebrowThresholdLowering
        );

        _emulateEyeBrow(
            ref eyeShapes,
            UnifiedExpressions.BrowLowererLeft,
            UnifiedExpressions.BrowOuterUpLeft,
            baseLeftEyeOpenness,
            _config.EyebrowThresholdRising,
            _config.EyebrowThresholdLowering
        );
    }
}