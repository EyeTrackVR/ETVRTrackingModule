namespace ETVRTrackingModule
{

    public enum OSCType
    {
        Integer,
        Float,
        Bool,
    }

    public static class OSCValueUtils
    {
        public static readonly Dictionary<OSCType, (Type, Type)> OSCTypeMap = new()
        {
            {OSCType.Integer, (typeof(int), typeof(OSCInteger))},
            {OSCType.Float, (typeof(float), typeof(OSCFloat))},
            {OSCType.Bool, (typeof(bool), typeof(OSCBool))},
        };
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

    public struct OSCMessage
    {
        public string address;
        public OSCValue value;
        public bool success;
    }
}
