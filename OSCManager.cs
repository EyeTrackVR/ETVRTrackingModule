using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace ETVRTrackingModule
{
    public enum OSCState
    {
        IDLE,
        CONNECTED,
        ERROR,
    }

    public class OSCManager
    {
        private Socket? _receiver;

        private ILogger _logger;

        private readonly ManualResetEvent _terminate = new(false);
        private Thread? _listeningThread;

        public OSCState State { get; private set; }
        private readonly ExpressionsMapperManager _expressionMapper;
        private const int ConnectionTimeout = 10000;

        private ETVRConfigManager _configManager;
        
        public OSCManager(ILogger iLogger, ExpressionsMapperManager expressionsMapperManager) {
            _logger = iLogger;
            _expressionMapper = expressionsMapperManager;
        }

        public void RegisterSelf(ref ETVRConfigManager configManager)
        {
            _configManager = configManager; 
            configManager.RegisterListener(HandleConfigUpdate);
        }
        
        private void HandleConfigUpdate(Config config)
        {
            _terminate.Set();
            _receiver?.Dispose();
            State = OSCState.IDLE;
            _terminate.Reset();
            Start();
        }
        
        public void Start()
        {
            _receiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            try
            {
                _receiver.Bind(new IPEndPoint(_configManager.Config.ListeningAddress, _configManager.Config.PortNumber));
                _receiver.ReceiveTimeout = ConnectionTimeout;
                State = OSCState.CONNECTED;
            }
            catch (Exception e)
            {
                _logger.LogError($"Connecting to {_configManager.Config.PortNumber} port at address {_configManager.Config.ListeningAddress} failed, with error: {e}");
                State = OSCState.ERROR;
            }

            _listeningThread = new Thread(OSCListen);
            _listeningThread.Start();
        }

        private void OSCListen()
        {
            var buffer = new byte[4096];
            while (!_terminate.WaitOne(0)) 
            {
                try
                {
                    if (_receiver is null)
                    {
                        continue;
                    }
                    
                    if (_receiver.IsBound)
                    {
                        var length = _receiver.Receive(buffer);
                        OSCMessage msg = ParseOSCMessage(buffer, length);
                        HandleOscMessage(msg);
                    }
                }
                catch (Exception)
                {
                    // we purposefully ignore any exceptions
                }
            }
        }

        public void TearDown()
        {
            _terminate.Set();
            _receiver?.Close();
            _receiver?.Dispose();
            _listeningThread?.Join();
        }

        private void HandleOscMessage(OSCMessage oscMessage)
        {
            if (!oscMessage.address.Contains("/command/"))
            {
                HandleExpressionMessage(oscMessage);
                return;
            }
            HandleSettingsMessage(oscMessage);
        }

        private void HandleExpressionMessage(OSCMessage oscMessage)
        {
            _expressionMapper.MapMessage(oscMessage);
        }

        private void HandleSettingsMessage(OSCMessage oscMessage)
        {
            /***
             * message we're expecting looks like
             * /command/set/field/ value
             * which translates to
             * [empty] command set field
             * after splitting
             * we're already getting the value in OSCMessage DTO
             * we also know that it is a command, let's verify what to set and do it. 
            ***/
            var parts = oscMessage.address.Split("/");
            
            if (parts[2].ToLower() != "set")
            {
                return;
            }
            _configManager.UpdateConfig(parts[3], oscMessage.value);
        }

        private OSCMessage ParseOSCMessage(byte[] buffer, int length)
        {
            OSCMessage msg = new OSCMessage();
            int currentStep = 0;
            string address = ParseOSCAddress(buffer, length, ref currentStep);

            if (address == "")
                return msg;
            msg.address = address;

            // OSC adresses are composed of /address , types value, so we need to check if we have a type
            if (buffer[currentStep] != 44)
                return msg;
            // skipping , char
            currentStep++;

            // todo, at one point we're gonna be getting stuf like sffi -> string, float, float, int in one message
            // make sure to add support for that sometime
            var types = ParseOSCTypes(buffer, length, ref currentStep);
            switch (types){
                case "s":
                    msg.success = true;
                    var value = ParseOSCString(buffer, length, ref currentStep);
                    if (Utils.Validators.CheckIfIPAddress(value))
                        msg.value = new OSCIPAddress(IPAddress.Parse(value));
                    else
                        msg.value = new OSCString(value);
                    break;
                case "i":
                    msg.success = true;
                    msg.value = new OSCInteger(ParseOSCInt(buffer, length, ref currentStep));
                    break;
                case "f":
                    msg.success = true;
                    msg.value = new OSCFloat(ParseOSCFloat(buffer, length, ref currentStep));
                    break;
                case "F":
                    msg.success = true;
                    msg.value = new OSCBool(false);
                    break;
                case "T":
                    msg.success = true;
                    msg.value = new OSCBool(true);
                    break;
                
                default:
                    _logger.LogInformation("Encountered unsupported type: {} from {}", types, msg.address);
                    msg.success = false;
                    break;
            }

            return msg;
        }

        string ParseOSCAddress(byte[] buffer, int length, ref int step)
        {
            string oscAddress = "";

            // check if the message starts with /, every OSC address should 
            if (buffer[0] != 47)
                return oscAddress;

            OSCValueUtils.ExtractStringData(buffer, length, ref step, out oscAddress);
            return oscAddress;
        }

        string ParseOSCTypes(byte[] buffer, int length, ref int step)
        {
            OSCValueUtils.ExtractStringData(buffer, length, ref step, out var types);
            return types;
        }

        byte[] ConvertToBigEndian(byte[] buffer)
        {
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(buffer);
            }
            return buffer;
        }

        float ParseOSCFloat(byte[] buffer, int length, ref int step)
        {
            var valueSection = ConvertToBigEndian(buffer[step..length]);
            float OSCValue = BitConverter.ToSingle(valueSection, 0);
            return OSCValue;
        }

        int ParseOSCInt(byte[] buffer, int length, ref int step) {
            var valueSection = ConvertToBigEndian(buffer[step..length]);
            int OSCValue = BitConverter.ToInt32(valueSection, 0);
            return OSCValue;
        }

        string ParseOSCString(byte[] buffer, int length, ref int step)
        {
            OSCValueUtils.ExtractStringData(buffer, length, ref step, out var value);
            return value;
        }
    }
}
