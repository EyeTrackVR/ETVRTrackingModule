using ETVRTrackingModule.ExpressionStrategies;
using Microsoft.Extensions.Logging;
using VRCFaceTracking;

namespace ETVRTrackingModule
{
    public class ExpressionsMapperManager
    {
        private V1Mapper _v1Mapper;
        private V2Mapper _v2Mapper;
        private BaseParamMapper _currentMapper;
        
        ILogger _logger;
        public ExpressionsMapperManager(ILogger logger, ref ETVRConfigManager configManager)
        {
            var config = configManager.Config;
            _logger = logger;
            _v1Mapper = new V1Mapper(_logger, ref config);
            _v2Mapper = new V2Mapper(_logger, ref config);
            _currentMapper = _v1Mapper;
        }

        public void MapMessage(OSCMessage msg)
        {
            if (!msg.success)
                return;

            if (IsV2Param(msg))
            {
                _currentMapper = _v2Mapper;
                _v2Mapper.handleOSCMessage(msg);
                return;
            }

            _currentMapper = _v1Mapper;
            _v1Mapper.handleOSCMessage(msg);
        }

        private bool IsV2Param(OSCMessage oscMessage)
        {
            var isv2Param = oscMessage.address.Contains("/v2/");
            return isv2Param;
        }

        public void UpdateVRCFTState()
        {
            _currentMapper.UpdateVRCFTEyeData(ref UnifiedTracking.Data.Eye, ref UnifiedTracking.Data.Shapes);
        }
    }
}