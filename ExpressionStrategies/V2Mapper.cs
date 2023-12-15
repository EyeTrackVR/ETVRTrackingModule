using Microsoft.Extensions.Logging;
using VRCFaceTracking;
using VRCFaceTracking.Core.Params.Data;
using VRCFaceTracking.Core.Params.Expressions;
using VRCFaceTracking.Core.Types;

namespace ETVRTrackingModule.ExpressionStrategies;

public class V2Mapper : BaseParamMapper
{
    private readonly string[] _singleEyeParamNames =
    {
        "EyeX",
        "EyeY",
        "EyeLid"
    };

    private Dictionary<string, float> _parameterValues = new()
    {
        { "EyeX", 0f },
        { "EyeY", 0f },
        { "EyeLid", 1f },

        { "EyeLeftX", 0f },
        { "EyeLeftY", 0f },
        { "EyeRightX", 0f },
        { "EyeRightY", 0f },
        { "EyeLidLeft", 1f },
        { "EyeLidRight", 1f },
    };

    public V2Mapper(ILogger logger, ref Config config) : base(logger, ref config)
    {
    }

    public override void handleOSCMessage(OSCMessage message)
    {
        string paramToMap = GetParamToMap(message.address);
        if (!_parameterValues.ContainsKey(paramToMap))
            return;

        _parameterValues[paramToMap] = message.value;
        var singleEyeMode = _singleEyeParamNames.Contains(paramToMap);
        UpdateVRCFTEyeData(ref UnifiedTracking.Data.Eye, ref UnifiedTracking.Data.Shapes, singleEyeMode);
    }

    private void UpdateVRCFTEyeData(ref UnifiedEyeData eyeData, ref UnifiedExpressionShape[] eyeShapes,
        bool isSingleEyeMode = false)
    {
        HandleEyeGaze(ref eyeData, isSingleEyeMode);
        HandleEyeOpenness(ref eyeData, ref eyeShapes, isSingleEyeMode);
        EmulateEyebrows(ref eyeShapes, isSingleEyeMode);
    }

    private void HandleEyeGaze(ref UnifiedEyeData eyeData, bool isSingleEyeMode)
    {
        if (isSingleEyeMode)
        {
            var combinedGaze = new Vector2(_parameterValues["EyeX"], _parameterValues["EyeY"]);
            eyeData.Left.Gaze = combinedGaze;
            eyeData.Right.Gaze = combinedGaze;
            return;
        }

        eyeData.Left.Gaze = new Vector2(_parameterValues["EyeLeftX"], _parameterValues["EyeLeftY"]);
        eyeData.Right.Gaze = new Vector2(_parameterValues["EyeRightX"], _parameterValues["EyeRightY"]);
    }

    private void HandleEyeOpenness(ref UnifiedEyeData eyeData, ref UnifiedExpressionShape[] eyeShapes,
        bool isSingleEyeMode = false)
    {
        if (isSingleEyeMode)
        {
            var eyeOpenness = _parameterValues["EyeLid"];

            HandleSingleEyeOpenness(ref eyeData.Left, eyeOpenness, _config.WidenThreshold, _config.SqueezeThreshold,
                _config.MaxOpennessThreshold, _config.MaxSquintThreshold);
            HandleSingleEyeOpenness(ref eyeData.Right, eyeOpenness, _config.WidenThreshold, _config.SqueezeThreshold,
                _config.MaxOpennessThreshold, _config.MaxSquintThreshold);
            return;
        }

        HandleSingleEyeOpenness(ref eyeData.Left, _parameterValues["EyeLidLeft"], _config.WidenThreshold,
            _config.SqueezeThreshold, _config.MaxOpennessThreshold, _config.MaxSquintThreshold);
        HandleSingleEyeOpenness(ref eyeData.Right, _parameterValues["EyeLidRight"], _config.WidenThreshold,
            _config.SqueezeThreshold, _config.MaxOpennessThreshold, _config.MaxSquintThreshold);
    }

    private void HandleSingleEyeOpenness(
        ref UnifiedSingleEyeData eyeData,
        float baseOpenness,
        float widenThreshold,
        float squeezeThreshold,
        float maxOpennessThreshold,
        float maxSquintThreshold
    )
    {
        eyeData.Openness = baseOpenness;
        if (_config.ShouldEmulateEyeWiden && baseOpenness >= widenThreshold)
        {
            eyeData.Openness = Utils.SmoothStep(
                widenThreshold,
                maxOpennessThreshold,
                baseOpenness
            );
        }

        if (_config.ShouldEmulateEyeSquint && baseOpenness <= squeezeThreshold)
        {
            eyeData.Openness = Utils.SmoothStep(
                squeezeThreshold,
                maxSquintThreshold,
                baseOpenness
            );
        }
    }

    private void EmulateEyebrows(ref UnifiedExpressionShape[] eyeShapes, bool isSingleEyeMode = false)
    {
        if (isSingleEyeMode)
        {
            var eyeOpenness = _parameterValues["EyeLid"];

            _emulateEyeBrow(
                ref eyeShapes,
                UnifiedExpressions.BrowLowererRight,
                UnifiedExpressions.BrowOuterUpRight,
                eyeOpenness,
                _config.EyebrowThresholdRising,
                _config.EyebrowThresholdLowering
            );

            _emulateEyeBrow(
                ref eyeShapes,
                UnifiedExpressions.BrowLowererLeft,
                UnifiedExpressions.BrowOuterUpLeft,
                eyeOpenness,
                _config.EyebrowThresholdRising,
                _config.EyebrowThresholdLowering
            );

            return;
        }

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