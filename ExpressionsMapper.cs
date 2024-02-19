using ETVRTrackingModule.ExpressionStrategies;
using Microsoft.Extensions.Logging;

namespace ETVRTrackingModule
{
    public class ExpressionsMapper
    {
        private V1Mapper _v1Mapper;
        private V2Mapper _v2Mapper;

        ILogger _logger;
        public ExpressionsMapper(ILogger logger, ref ETVRConfigManager configManager)
        {
            var config = configManager.Config;
            _logger = logger;
            _v1Mapper = new V1Mapper(_logger, ref config);
            _v2Mapper = new V2Mapper(_logger, ref config);
        }

        public void MapMessage(OSCMessage msg)
        {
            if (!msg.success)
                return;

            if (IsV2Param(msg))
            {
                _v2Mapper.handleOSCMessage(msg);
                return;
            }

            _v1Mapper.handleOSCMessage(msg);
        }

        private bool IsV2Param(OSCMessage oscMessage)
        {
            var isv2Param = oscMessage.address.Contains("/v2/");
            return isv2Param;
        }
    }
}