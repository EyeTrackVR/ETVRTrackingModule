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
            var eyeOpenness = (float)_leftOneEuroFilter.Filter(_parameterValues["EyeLid"], 1);

            HandleSingleEyeOpenness(ref eyeData.Left, eyeOpenness, _config);
            HandleSingleEyeOpenness(ref eyeData.Right, eyeOpenness, _config);
            return;
        }

        HandleSingleEyeOpenness(
            ref eyeData.Left,
            (float)_leftOneEuroFilter.Filter(_parameterValues["EyeLidLeft"], 1),
            _config);
        HandleSingleEyeOpenness(
            ref eyeData.Right,
            (float)_rightOneEuroFilter.Filter(_parameterValues["EyeLidRight"], 1),
            _config);
    }

    private void HandleSingleEyeOpenness(
        ref UnifiedSingleEyeData eyeData,
        float baseOpenness,
        Config config
    )
    {
        eyeData.Openness = baseOpenness;
        if (_config.ShouldEmulateEyeWiden && baseOpenness >= config.WidenThresholdV2[0])
        {
            var opennessValue = Utils.SmoothStep(
                config.WidenThresholdV2[0],
                config.WidenThresholdV2[1],
                baseOpenness
            ) * config.OutputMultiplier;
            eyeData.Openness = opennessValue;
        }

        if (_config.ShouldEmulateEyeSquint && baseOpenness <= config.SqueezeThresholdV2[0])
        {
            var opennessValue = Utils.SmoothStep(
                config.SqueezeThresholdV2[0],
                config.SqueezeThresholdV2[1],
                baseOpenness
            ) * config.OutputMultiplier;
            eyeData.Openness = opennessValue;
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
                ref _leftOneEuroFilter,
                eyeOpenness,
                _config.EyebrowThresholdRising,
                _config.EyebrowThresholdLowering
            );

            _emulateEyeBrow(
                ref eyeShapes,
                UnifiedExpressions.BrowLowererLeft,
                UnifiedExpressions.BrowOuterUpLeft,
                ref _rightOneEuroFilter,
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
            ref _leftOneEuroFilter,
            baseRightEyeOpenness,
            _config.EyebrowThresholdRising,
            _config.EyebrowThresholdLowering
        );

        _emulateEyeBrow(
            ref eyeShapes,
            UnifiedExpressions.BrowLowererLeft,
            UnifiedExpressions.BrowOuterUpLeft,
            ref _rightOneEuroFilter,
            baseLeftEyeOpenness,
            _config.EyebrowThresholdRising,
            _config.EyebrowThresholdLowering
        );
    }
}