using Microsoft.Extensions.Logging;
using VRCFaceTracking;
using VRCFaceTracking.Core.Library;

namespace ETVRTrackingModule
{
    public class ETVRTrackingModule : ExtTrackingModule
    {
        private OSCManager? _OSCManager;

        public override (bool SupportsEye, bool SupportsExpression) Supported => (true, false);
        public override (bool eyeSuccess, bool expressionSuccess) Initialize(bool eyeAvailable, bool expressionAvailable)
        {

            _OSCManager = new OSCManager(Logger);
            if (_OSCManager.State != OSCState.CONNECTED) {
                Logger.LogError("ETVR Module could not connect to the specified port.");
                return (false, false);
            }

            ModuleInformation.Name = "ETVR Eye Tracking module";
            var stream = GetType().Assembly.GetManifestResourceStream("ETVRTrackingModule.Assets.ETVRLogo.png");
            ModuleInformation.StaticImages = stream != null? new List<Stream> { stream } : ModuleInformation.StaticImages;

            return (true, false);
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