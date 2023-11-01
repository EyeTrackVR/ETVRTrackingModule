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
            if (!msg.success)
                return;
            
            var nextStrategy =  IsV2Param(msg) ? (ImappingStategy) new V2Mapper(_logger) : new V1Mapper(_logger);
            
            if (_mappingStrategy.GetType() != nextStrategy.GetType())
            {
                _mappingStrategy = nextStrategy;
            }
            _mappingStrategy.handleOSCMessage(msg);
            _mappingStrategy.UpdateVRCFTEyeData(ref UnifiedTracking.Data.Eye, ref UnifiedTracking.Data.Shapes);
        }

        private bool IsV2Param(OSCMessage oscMessage)
        {
            var isv2Param = oscMessage.address.Contains("/v2/");
            return isv2Param;
        }
    }
}
