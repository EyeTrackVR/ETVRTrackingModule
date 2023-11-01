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
        private Socket _receiver;
        //private Socket _sender;

        private ILogger _logger;

        private volatile bool _shouldRun = true;
        private Thread _literningThread;

        public OSCState State { get; private set; } = OSCState.IDLE;

        private ExpressionsMapper _expressionMapper;

        private int _receivingPort;
        private const int DefaultPort = 8889;
        private const int ConnectionTimeout = 10000;

        public OSCManager(ILogger iLogger, ExpressionsMapper expressionsMapper, int? port = null) {
            _logger = iLogger;
            _expressionMapper = expressionsMapper;
            _receivingPort = port ?? DefaultPort;
            _receiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            try
            {
                // we should make this a setting, how do settings work in vrcft?
                _receiver.Bind(new IPEndPoint(IPAddress.Loopback, _receivingPort));
                _receiver.ReceiveTimeout = ConnectionTimeout;
                State = OSCState.CONNECTED;
            }
            catch (Exception e)
            {
                _logger.LogError($"Connecting to {_receivingPort} port failed, with error: {e}");
                State = OSCState.ERROR;
            }

            _literningThread = new Thread(OSCListen);
            _literningThread.Start();
        }

        private void OSCListen()
        {
            var buffer = new byte[4096];
            while (_shouldRun) 
            {
                try
                {
                    if (_receiver.IsBound)
                    {
                        var length = _receiver.Receive(buffer);
                        OSCMessage msg = ParseOSCMessage(buffer, length);
                        _expressionMapper.MapMessage(msg);
                    }
                }
                catch (Exception){}
            }
        }

        public void TearDown()
        {
            _logger.LogInformation("ETVR module closing");
            _shouldRun = false;
            _receiver.Close();
            _receiver.Dispose();
            _literningThread.Join();
        }

        OSCMessage ParseOSCMessage(byte[] buffer, int length)
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
