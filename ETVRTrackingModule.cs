using System.Reflection;
using Microsoft.Extensions.Logging;
using VRCFaceTracking;
using VRCFaceTracking.Core.Library;

namespace ETVRTrackingModule
{
    public class ETVRTrackingModule : ExtTrackingModule
    {
        private OSCManager? _oscManager;
        private ExpressionsMapperManager? _expressionMapper;
        public override (bool SupportsEye, bool SupportsExpression) Supported => (true, false);
        public override (bool eyeSuccess, bool expressionSuccess) Initialize(bool eyeAvailable, bool expressionAvailable)
        {
            ModuleInformation.Name = "ETVR Eye Tracking module";
            var stream = GetType().Assembly.GetManifestResourceStream("ETVRTrackingModule.Assets.ETVRLogo.png");
            ModuleInformation.StaticImages = stream != null? new List<Stream> { stream } : ModuleInformation.StaticImages;

            var currentPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            ETVRConfigManager configManager = new ETVRConfigManager(currentPath, Logger);
            configManager.LoadConfig();
            
            _expressionMapper = new ExpressionsMapperManager(Logger, configManager.Config);
            _expressionMapper.RegisterSelf(ref configManager);
            
            _oscManager = new OSCManager(Logger, _expressionMapper);
            _oscManager.RegisterSelf(ref configManager);
            _oscManager.Start();
            
            if (_oscManager.State == OSCState.CONNECTED) return (true, false);
            
            Logger.LogError("ETVR Module could not connect to the specified port.");
            return (false, false);
        }

        public override void Teardown()
        {
            _oscManager?.TearDown();
        }

        public override void Update()
        {
            _expressionMapper!.UpdateVRCFTState();
            Thread.Sleep(5);
        }
    }
}