using OpenTK.Mathematics;
using Vintagestory.API.MathTools;

namespace AnimationsLib;

public readonly struct Angle
{
    public float Radians => _value;
    public float Degrees => _value * GameMath.RAD2DEG;
    public float Minutes => _value * GameMath.RAD2DEG * 60f;
    public float Seconds => _value * GameMath.RAD2DEG * 3600f;

    public override string ToString() => $"{Degrees:F2} deg";
    public override bool Equals(object? obj) => ((Angle?)obj)?._value == _value;
    public override int GetHashCode() => _value.GetHashCode();

    public static Angle Zero => new(0);

    public static Angle FromRadians(float radians) => new(radians);
    public static Angle FromRadians(double radians) => new((float)radians);
    public static Angle FromDegrees(float degrees) => new(degrees * GameMath.DEG2RAD);
    public static Angle FromDegrees(double degrees) => new((float)degrees * GameMath.DEG2RAD);
    public static Angle FromMinutes(float minutes) => new(minutes * GameMath.DEG2RAD / 60f);
    public static Angle FromMinutes(double minutes) => new((float)minutes * GameMath.DEG2RAD / 60f);
    public static Angle FromSeconds(float seconds) => new(seconds * GameMath.DEG2RAD / 3600f);
    public static Angle FromSeconds(double seconds) => new((float)seconds * GameMath.DEG2RAD / 3600f);

    public static Angle BetweenVectors(Vector3d a, Vector3d b)
    {
        double dot = Vector3d.Dot(a, b);
        double magProduct = a.Length * b.Length;

        if (magProduct == 0)
        {
            return FromRadians(0);
        }

        double cosTheta = Math.Clamp(dot / magProduct, -1.0, 1.0);

        return FromRadians((float)Math.Acos(cosTheta));
    }
    public static Angle BetweenVectors(Vector3 a, Vector3 b)
    {
        float dot = Vector3.Dot(a, b);
        float magProduct = a.Length * b.Length;

        if (magProduct == 0)
        {
            return FromRadians(0);
        }

        float cosTheta = Math.Clamp(dot / magProduct, -1.0f, 1.0f);

        return FromRadians(MathF.Acos(cosTheta));
    }

    public static Angle operator +(Angle a, Angle b) => new(a._value + b._value);
    public static Angle operator -(Angle a, Angle b) => new(a._value - b._value);
    public static Angle operator *(Angle a, float b) => new(a._value * b);
    public static Angle operator *(float a, Angle b) => new(a * b._value);
    public static Angle operator /(Angle a, float b) => new(a._value / b);
    public static float operator /(Angle a, Angle b) => a._value / b._value;

    public static bool operator ==(Angle a, Angle b) => MathF.Abs(a._value - b._value) < float.Epsilon;
    public static bool operator !=(Angle a, Angle b) => MathF.Abs(a._value - b._value) >= float.Epsilon;
    public static bool operator <(Angle a, Angle b) => a._value < b._value && a != b;
    public static bool operator >(Angle a, Angle b) => a._value > b._value && a != b;
    public static bool operator <=(Angle a, Angle b) => a._value <= b._value;
    public static bool operator >=(Angle a, Angle b) => a._value >= b._value;



    private Angle(float radians) => _value = radians;

    private readonly float _value;
}