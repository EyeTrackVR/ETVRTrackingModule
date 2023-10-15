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

    struct OSCMessage
    {
        public string address;
        public float value;
        public bool success;
    }

    public class OSCManager
    {
        private Socket _receiver;
        private Socket _sender;

        private ILogger _logger;

        private volatile bool _shouldRun = true;
        private Thread _literningThread;

        public OSCState State { get; private set; } = OSCState.IDLE;

        private int _receivingPort;
        private const int _defaultPort = 8889;
        private const int connectionTimeout = 10000;

        public OSCManager(ILogger iLogger, int? port = null) {
            _logger = iLogger;
            _receivingPort = port ?? _defaultPort;
            _receiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            try
            {
                // we should make this a setting, how do settings work in vrcft?
                _receiver.Bind(new IPEndPoint(IPAddress.Loopback, _receivingPort));
                _receiver.ReceiveTimeout = connectionTimeout;
                State = OSCState.CONNECTED;
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
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
                        // map the message
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
            // now, let's skip the types section
            for (int i=currentStep; i < length; i++)
            {
                if (buffer[i] == 0)
                {
                    currentStep = i + 1;
                    // we've reached the end of this segment, let's normalize it to 4 bytes and skip ahead
                    if (currentStep % 4 != 0) { currentStep += 4 - (currentStep % 4); }
                    break;
                }
            }
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
