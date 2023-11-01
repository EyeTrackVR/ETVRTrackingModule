using Microsoft.Extensions.Logging;
using VRCFaceTracking;
using VRCFaceTracking.Core.Library;

namespace ETVRTrackingModule
{
    public class ETVRTrackingModule : ExtTrackingModule
    {
        private OSCManager? _OSCManager;
        private ExpressionsMapper? _expressionMapper;

        public override (bool SupportsEye, bool SupportsExpression) Supported => (true, true);
        public override (bool eyeSuccess, bool expressionSuccess) Initialize(bool eyeAvailable, bool expressionAvailable)
        {
            _expressionMapper = new ExpressionsMapper(Logger);
            _OSCManager = new OSCManager(Logger, _expressionMapper);

            if (_OSCManager.State != OSCState.CONNECTED) {
                Logger.LogError("ETVR Module could not connect to the specified port.");
                return (false, false);
            }

            ModuleInformation.Name = "ETVR Eye Tracking module";
            var stream = GetType().Assembly.GetManifestResourceStream("ETVRTrackingModule.Assets.ETVRLogo.png");
            ModuleInformation.StaticImages = stream != null? new List<Stream> { stream } : ModuleInformation.StaticImages;

            return (true, true);
        }

        public override void Teardown()
        {
            _OSCManager?.TearDown();
        }

        public override void Update()
        {
            Thread.Sleep(10);
        }
    }
}