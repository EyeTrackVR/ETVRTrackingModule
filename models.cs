using System.Net;

namespace ETVRTrackingModule
{

    public enum OSCType
    {
        Integer,
        Float,
        Bool,
        IPAddress,
        String,
    }

    public static class OSCValueUtils
    {
        public static readonly Dictionary<OSCType, (Type, Type)> OSCTypeMap = new()
        {
            {OSCType.Integer, (typeof(int), typeof(OSCInteger))},
            {OSCType.Float, (typeof(float), typeof(OSCFloat))},
            {OSCType.Bool, (typeof(bool), typeof(OSCBool))},
            {OSCType.String, (typeof(string), typeof(OSCString))},
            {OSCType.IPAddress, (typeof(string), typeof(OSCIPAddress))},
        };

        public static void ExtractStringData(byte[] buffer, int length, ref int step, out string result)
        {
            string value = "";
            for (int i = step; i < length; i++)
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
                value += (char)buffer[i];
            }

            result = value;
        }
    }

    public abstract class OSCValue
    {
        public abstract OSCType Type { get; }
    }

    public class OSCInteger: OSCValue { 
        public override OSCType Type => OSCType.Integer;
        public int value;

        public OSCInteger(int value)
        {
            this.value = value;
        }
    }

    public class OSCFloat: OSCValue
    {
        public override OSCType Type => OSCType.Float;
        public float value;

        public OSCFloat(float value)
        {
            this.value = value;
        }
    }

    public class OSCBool: OSCValue
    {
        public override OSCType Type => OSCType.Bool;
        public bool value;

        public OSCBool(bool value)
        {
            this.value = value;
        }
    }

    public class OSCString : OSCValue
    {
        public override OSCType Type => OSCType.String;
        public string value;

        public OSCString(string value)
        {
            this.value = value;
        }
    }

    public class OSCIPAddress : OSCValue
    {
        public override OSCType Type => OSCType.IPAddress;
        public IPAddress value;

        public OSCIPAddress(IPAddress value)
        {
            this.value = value;
        }
    }
    
    public struct OSCMessage
    {
        public string address;
        public OSCValue value;
        public bool success;
    }
}
