namespace ETVRTrackingModule;

public static class Utils
{
    public static float SmoothStep(float edge0, float edge1, float x)
    {
        x = Math.Clamp((x - edge0) / (edge1 - edge0), 0, 1);
        return x * x * (3 - 2 * x);
    }   
}