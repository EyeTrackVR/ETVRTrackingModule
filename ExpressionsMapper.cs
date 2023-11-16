using ETVRTrackingModule.ExpressionStrategies;
using Microsoft.Extensions.Logging;

namespace ETVRTrackingModule
{
    public class ExpressionsMapper
    {
        private ImappingStategy _mappingStrategy;
        private ETVRConfigManager _config;
        
        ILogger _logger;
        public ExpressionsMapper(ILogger logger, ETVRConfigManager config) 
        {
            _logger = logger;
            _config = config;
            _mappingStrategy = new V2Mapper(_logger);
        }
        public void MapMessage(OSCMessage msg)
        {
            if (!msg.success)
                return;
            
            var nextStrategy =  IsV2Param(msg) ? (ImappingStategy) new V2Mapper(_logger) : new V1Mapper(_logger, _config.Config);
            
            if (_mappingStrategy.GetType() != nextStrategy.GetType())
            {
                _mappingStrategy = nextStrategy;
            }
            _mappingStrategy.handleOSCMessage(msg);
        }

        private bool IsV2Param(OSCMessage oscMessage)
        {
            var isv2Param = oscMessage.address.Contains("/v2/");
            return isv2Param;
        }
    }
}
