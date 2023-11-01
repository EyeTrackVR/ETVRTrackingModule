using ETVRTrackingModule.ExpressionStrategies;
using Microsoft.Extensions.Logging;
using VRCFaceTracking;

namespace ETVRTrackingModule
{
    public class ExpressionsMapper
    {
        private ImappingStategy _mappingStrategy;
        
        ILogger _logger;
        public ExpressionsMapper(ILogger logger) 
        {
            _logger = logger;
            _mappingStrategy = new V2Mapper(_logger);
        }
        public void MapMessage(OSCMessage msg)
        {
            _logger.LogInformation("parsing message");
            if (!msg.success)
                return;
            
            var nextStrategy =  IsV2Param(msg) ? (ImappingStategy) new V2Mapper(_logger) : new V1Mapper(_logger);
            
            if (_mappingStrategy.GetType() != nextStrategy.GetType())
            {
                _logger.LogInformation($"Detected differing strategy, changing from {_mappingStrategy.GetType()} to {nextStrategy.GetType()}");
                _mappingStrategy = nextStrategy;
            }
            _mappingStrategy.handleOSCMessage(msg);
            _mappingStrategy.UpdateVRCFTEyeData(ref UnifiedTracking.Data.Eye, ref UnifiedTracking.Data.Shapes);
        }

        private bool IsV2Param(OSCMessage oscMessage)
        {
            var isv2Param = oscMessage.address.Contains("/v2/");
            _logger.LogInformation($"is V2 param: {isv2Param.ToString()}");
            return isv2Param;
        }
    }
}
