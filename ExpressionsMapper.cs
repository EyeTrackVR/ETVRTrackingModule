using ETVRTrackingModule.ExpressionStrategies;
using Microsoft.Extensions.Logging;
using VRCFaceTracking;

namespace ETVRTrackingModule
{
    public class ExpressionsMapper
    {
        private IExpressionMapper _mappingStrategy;
        
        ILogger _logger;
        public ExpressionsMapper(ILogger logger) 
        {
            _logger = logger;
            _mappingStrategy = new V2Mapper();
        }
        public void MapMessage(OSCMessage msg)
        {
            if (!msg.success)
                return;
            
            var nextStrategy =  IsV2Param(msg) ? (IExpressionMapper) new V2Mapper() : new V1Mapper();
            if (_mappingStrategy.GetType() != nextStrategy.GetType())
            {
                _mappingStrategy = nextStrategy;
            }
            _mappingStrategy.handleOSCMessage(msg);
        }

        private bool IsV2Param(OSCMessage oscMessage)
        {
            return oscMessage.address.Contains("/v2/");
        }

        public void UpdateVRCFTEyeData()
        {
            _mappingStrategy.UpdateVRCFTEyeData(ref UnifiedTracking.Data.Eye, ref UnifiedTracking.Data.Shapes);
        }
    }
}
