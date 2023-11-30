using ETVRTrackingModule.ExpressionStrategies;
using Microsoft.Extensions.Logging;

namespace ETVRTrackingModule
{
    public class ExpressionsMapper
    {
        private IMappingStategy _mappingStrategy;
        private Config _config;
        ILogger _logger;
        public ExpressionsMapper(ILogger logger, ref ETVRConfigManager configManager) 
        {
            _logger = logger;
            _config = configManager.Config; 
            _mappingStrategy = new V2Mapper(_logger, ref _config);
        }
        public void MapMessage(OSCMessage msg)
        {
            if (!msg.success)
                return;
            
            var nextStrategy =  IsV2Param(msg) ? (IMappingStategy) new V2Mapper(_logger, ref _config) : new V1Mapper(_logger, ref _config);
            
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
