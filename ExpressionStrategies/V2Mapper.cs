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
        "EyeLid",
        "BrowExpression",
        "EyeSquint"
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
        { "PupilDilation", 0f },
        { "BrowExpression", 0.5f },
        { "BrowExpressionLeft", 0.5f },
        { "BrowExpressionRight", 0.5f }, 
        { "CheekSquintRight", 0f },
        { "CheekSquintLeft", 0f },
        { "EyeSquintRight", 0f },
        { "EyeSquintLeft", 0f },
        { "EyeSquint", 0f },
    };

    private readonly string[] _gazeParameters =
    {
        "EyeX",
        "EyeY",
        "EyeLeftX",
        "EyeLeftY",
        "EyeRightX",
        "EyeRightY",
    };

    private readonly string[] _opennessParameters =
    {
        "EyeLid",
        "EyeLidLeft",
        "EyeLidRight",
    };

    private bool _isSingleEye = false;

    public V2Mapper(ILogger logger, Config config) : base(logger, config) {}

    public override void HandleOSCMessage(OSCMessage message)
    {
        var paramToMap = GetParamToMap(message.address);
        if (!_parameterValues.ContainsKey(paramToMap))
            return;

        if (message.value is not OSCFloat oscF)
        {
            _logger.LogInformation("ParamMapper got passed a wrong type of message: {}", message.value.Type);
            return;
        }
        
        _parameterValues[paramToMap] = oscF.value;

        if (paramToMap == "EyeX" || paramToMap == "EyeY")
            _isSingleEye = true;
        else if (paramToMap == "EyeLeftX" || paramToMap == "EyeRightX")
            _isSingleEye = false;
    }

    public override void UpdateVRCFTEyeData(ref UnifiedEyeData eyeData, ref UnifiedExpressionShape[] eyeShapes)
    {
        HandleEyeGaze(ref eyeData, _isSingleEye);
        HandleEyeDilation(ref eyeData);
        HandleEyeOpenness(ref eyeData, ref eyeShapes, _isSingleEye);
        HandleEyebrows(ref eyeShapes, _isSingleEye);
        HandleSquints(ref eyeShapes, _isSingleEye);
        EmulateEyebrows(ref eyeShapes, _isSingleEye);
    }

    private void HandleSquints(ref UnifiedExpressionShape[] eyeShapes, bool isSingleEyeMode = false)
    {
        if (isSingleEyeMode)
        {
            var eyeSquint = _parameterValues["EyeSquint"];
            eyeShapes[(int)UnifiedExpressions.EyeSquintRight].Weight = eyeSquint;
            eyeShapes[(int)UnifiedExpressions.EyeSquintLeft].Weight = eyeSquint;
        }
        else
        {
            eyeShapes[(int)UnifiedExpressions.EyeSquintRight].Weight = _parameterValues["EyeSquintRight"];
            eyeShapes[(int)UnifiedExpressions.EyeSquintLeft].Weight = _parameterValues["EyeSquintLeft"];
        }

        eyeShapes[(int)UnifiedExpressions.CheekSquintRight].Weight = _parameterValues["CheekSquintRight"];
        eyeShapes[(int)UnifiedExpressions.CheekSquintLeft].Weight = _parameterValues["CheekSquintLeft"];
    }

    private void HandleEyebrows(ref UnifiedExpressionShape[] eyeShapes, bool isSingleEyeMode = false)
    {
        if (isSingleEyeMode)
        {
            var browExp = _parameterValues["BrowExpression"];
            MapBrowExpression(ref eyeShapes, browExp, true);
            MapBrowExpression(ref eyeShapes, browExp, false);
            return;
        }

        MapBrowExpression(ref eyeShapes, _parameterValues["BrowExpressionRight"], true);
        MapBrowExpression(ref eyeShapes, _parameterValues["BrowExpressionLeft"], false);
    }

    private void MapBrowExpression(ref UnifiedExpressionShape[] eyeShapes, float browExp, bool isRight)
    {
        float lowerWeight = 0f;
        float raiseWeight = 0f;

        if (browExp < 0.5f)
        {
            lowerWeight = (0.5f - browExp) * 2f;
        }
        else if (browExp > 0.5f)
        {
            raiseWeight = (browExp - 0.5f) * 2f;
        }

        if (isRight)
        {
            eyeShapes[(int)UnifiedExpressions.BrowLowererRight].Weight = lowerWeight;
            eyeShapes[(int)UnifiedExpressions.BrowPinchRight].Weight = lowerWeight;
            eyeShapes[(int)UnifiedExpressions.BrowInnerUpRight].Weight = raiseWeight;
            eyeShapes[(int)UnifiedExpressions.BrowOuterUpRight].Weight = raiseWeight;
        }
        else
        {
            eyeShapes[(int)UnifiedExpressions.BrowLowererLeft].Weight = lowerWeight;
            eyeShapes[(int)UnifiedExpressions.BrowPinchLeft].Weight = lowerWeight;
            eyeShapes[(int)UnifiedExpressions.BrowInnerUpLeft].Weight = raiseWeight;
            eyeShapes[(int)UnifiedExpressions.BrowOuterUpLeft].Weight = raiseWeight;
        }
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
    
    private void HandleEyeDilation(ref UnifiedEyeData eyeData)
    {
        eyeData.Left.PupilDiameter_MM = _parameterValues["PupilDilation"];
        eyeData.Right.PupilDiameter_MM = _parameterValues["PupilDilation"];
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
            var opennessValue = Utils.MathUtils.SmoothStep(
                config.WidenThresholdV2[0],
                config.WidenThresholdV2[1],
                baseOpenness
            ) * config.OutputMultiplier;
            eyeData.Openness = baseOpenness + opennessValue;
        }

        if (_config.ShouldEmulateEyeSquint && baseOpenness <= config.SqueezeThresholdV2[0])
        {
            var opennessValue = Utils.MathUtils.SmoothStep(
                config.SqueezeThresholdV2[0],
                config.SqueezeThresholdV2[1],
                baseOpenness
            ) * config.OutputMultiplier;
            eyeData.Openness = baseOpenness - opennessValue;
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
        
        
        var baseRightEyeOpenness = _parameterValues["EyeLidLeft"];
        var baseLeftEyeOpenness = _parameterValues["EyeLidRight"];

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