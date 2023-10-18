
using Microsoft.Extensions.Logging;
using VRCFaceTracking.Core.Params.Data;
using VRCFaceTracking.Core.Params.Expressions;

namespace ETVRTrackingModule
{
    public class ExpressionsMapper
    {
        private Dictionary<string, float> parameterValues = new()
        {
            // v1, legacy support
            { "RightEyeLidExpandedSqueeze", 0f },
            { "LeftEyeLidExpandedSqueeze", 0f },
            { "LeftEyeX", 0f },
            { "RightEyeX", 0f },
            { "EyesY", 0f },
            // v2
            { "EyeX", 0f },
            { "EyeLeftX", 0f },
            { "EyeRightX", 0f },
            { "EyeLeftY", 0f },
            { "EyeRightY", 0f },
            { "EyeLid", 0f },
            { "EyeLidLeft", 0f },
            { "EyeLidRight", 0f },
        };

        ILogger _logger;
        public ExpressionsMapper(ILogger logger) 
        {
            _logger = logger;
        }
        public void MapMessage(OSCMessage msg)
        {
            if (!msg.success)
                return;
            if (IsV2Param(msg))
            {
                string paramToMap = GetParamToMap(msg.address);
                if (parameterValues.ContainsKey(paramToMap))
                {
                    parameterValues[paramToMap] = msg.value;
                }
            }
        }

        private bool IsV2Param(OSCMessage oscMessage)
        {
            return oscMessage.address.Contains("/v2/");
        }

        private static string GetParamToMap(string oscAddress)
        {
            var oscUrlSplit = oscAddress.Split("/");
            return oscUrlSplit[^1];
        }

        public void UpdateVRCFTEyeData()
        {
        }
    }
}
