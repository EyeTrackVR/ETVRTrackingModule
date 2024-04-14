namespace ETVRTrackingModule
{

    public enum OSCType
    {
        Integer,
        Float,
        Bool,
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
