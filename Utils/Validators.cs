using System.Net;

namespace ETVRTrackingModule.Utils;

public static class Validators
{
    public static bool CheckIfIPAddress(string value)
    {
        IPAddress? result;
        return !String.IsNullOrEmpty(value) && IPAddress.TryParse(value, out result);
    }
}