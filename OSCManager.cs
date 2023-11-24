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
        //private Socket _sender;

        private ILogger _logger;

        private readonly ManualResetEvent _terminate = new(false);
        private Thread? _listeningThread;

        public OSCState State { get; private set; }
        private readonly ExpressionsMapper _expressionMapper;
        private const int ConnectionTimeout = 10000;

        private readonly ETVRConfigManager _config;
        
        public OSCManager(ILogger iLogger, ExpressionsMapper expressionsMapper, ref ETVRConfigManager configManager) {
            _logger = iLogger;
            _expressionMapper = expressionsMapper;
            _config = configManager; 
            configManager.RegisterListener(this.HandleConfigUpdate);
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
                _receiver.Bind(new IPEndPoint(IPAddress.Loopback, _config.Config.PortNumber));
                _receiver.ReceiveTimeout = ConnectionTimeout;
                State = OSCState.CONNECTED;
            }
            catch (Exception e)
            {
                _logger.LogError($"Connecting to {_config.Config.PortNumber} port failed, with error: {e}");
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
            _config.UpdateConfig(parts[3], oscMessage.value);
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

            ParseOSCTypes(buffer, length, ref currentStep); // we purposefully ignore the types, for now

            float value = ParseOSCFloat(buffer, length, ref currentStep);

            msg.value = value;
            msg.success = true;
            return msg;
        }

        string ParseOSCAddress(byte[] buffer, int length, ref int step)
        {
            string oscAddress = "";

            // check if the message starts with /, every OSC adress should 
            if (buffer[0] != 47)
                return oscAddress;

            for (int i = 0; i<length; i++)
            {
                // we've reached the end of the address section, let's update the steps counter
                // to point at the value section
                if (buffer[i] == 0)
                {
                    // we need to ensure that we include the null terminator
                    step = i + 1;
                    // the size of a packet is a multiple of 4, we need to round it up 
                    if (step % 4 != 0) { step += 4 - (step % 4); }
                    break;
                }
                oscAddress += (char)buffer[i];
            }
            return oscAddress;
        }

        string ParseOSCTypes(byte[] buffer, int length, ref int step)
        {
            string types = "";
            // now, let's skip the types section
            for (int i = step; i < length; i++)
            {
                if (buffer[i] == 0)
                {
                    step = i + 1;
                    // we've reached the end of this segment, let's normalize it to 4 bytes and skip ahead
                    if (step % 4 != 0) { step += 4 - (step % 4); }
                    break;
                }
                types += (char)buffer[i];
            }
            return types;
        }

        float ParseOSCFloat(byte[] buffer, int length, ref int step)
        {
            var valueSection = buffer[step..length];
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(valueSection);
            }

            float OSCValue = BitConverter.ToSingle(valueSection, 0);
            return OSCValue;
        }
    }
}
