using System.Security.Cryptography.X509Certificates;
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
            if (message.value is not OSCFloat oscF)
            {
                _logger.LogInformation("ParamMapper got passed a wrong type of message: {}", message.value.Type);
                return;
            }
            else
            {
                _parameterValues[paramToMap] = oscF.value;

            }
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

        var baseRightEyeOpenness = (float)_leftOneEuroFilter.Filter(_parameterValues["RightEyeLidExpandedSqueeze"], 1);
        var baseLeftEyeOpenness = (float)_rightOneEuroFilter.Filter(_parameterValues["LeftEyeLidExpandedSqueeze"], 1);

        _handleSingleEyeOpenness(ref eyeData.Right, ref eyeShapes, UnifiedExpressions.EyeWideRight,
            UnifiedExpressions.EyeSquintRight, baseRightEyeOpenness, _config);

        _handleSingleEyeOpenness(ref eyeData.Left, ref eyeShapes, UnifiedExpressions.EyeWideLeft,
            UnifiedExpressions.EyeSquintLeft, baseLeftEyeOpenness, _config);
    }

    private void _handleSingleEyeOpenness(
        ref UnifiedSingleEyeData eye,
        ref UnifiedExpressionShape[] eyeShapes,
        UnifiedExpressions widenParam,
        UnifiedExpressions squintParam,
        float baseEyeOpenness,
        Config config
    )
    {
        eye.Openness = baseEyeOpenness;
        if (_config.ShouldEmulateEyeWiden && baseEyeOpenness >= config.WidenThresholdV1[0])
        {
            eye.Openness = 0.8f;
            var widenValue = Utils.SmoothStep(
                config.WidenThresholdV1[0],
                config.WidenThresholdV1[1],
                baseEyeOpenness
            ) * config.OutputMultiplier;
            eyeShapes[(int)widenParam].Weight = widenValue;
            eyeShapes[(int)squintParam].Weight = 0;
        }

        if (_config.ShouldEmulateEyeSquint && baseEyeOpenness <= config.SqueezeThresholdV1[0])
        {
            eyeShapes[(int)widenParam].Weight = 0;
            var squintValue = Utils.SmoothStep(
                config.SqueezeThresholdV1[1],
                config.SqueezeThresholdV1[0],
                baseEyeOpenness
            ) * config.OutputMultiplier;
            eyeShapes[(int)squintParam].Weight = squintValue;
        }
    }

    private void EmulateEyeBrows(ref UnifiedExpressionShape[] eyeShapes)
    {
        var baseRightEyeOpenness = (float)_leftOneEuroFilter.Filter(_parameterValues["RightEyeLidExpandedSqueeze"], 1);
        var baseLeftEyeOpenness = (float)_rightOneEuroFilter.Filter(_parameterValues["LeftEyeLidExpandedSqueeze"], 1);

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