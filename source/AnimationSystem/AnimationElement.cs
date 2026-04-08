using ImGuiNET;
using OpenTK.Mathematics;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace AnimationsLib;

public readonly struct AnimationElement
{
    public readonly AnimationElementValue OffsetX;
    public readonly AnimationElementValue OffsetY;
    public readonly AnimationElementValue OffsetZ;
    public readonly AnimationElementRotation Rotation;

    public static readonly AnimationElement Empty = new();
    public static readonly AnimationElement Zero = new(0, 0, 0, 0, 0, 0);
    public const float OffsetFactor = 1 / 16f;

    public AnimationElement()
    {
        OffsetX = AnimationElementValue.Empty;
        OffsetY = AnimationElementValue.Empty;
        OffsetZ = AnimationElementValue.Empty;
        Rotation = AnimationElementRotation.Empty;
    }
    public AnimationElement(params AnimationElementValue[] values)
    {
        OffsetX = values[0];
        OffsetY = values[1];
        OffsetZ = values[2];
        Rotation = new(Angle.FromDegrees(values[3].Value), Angle.FromDegrees(values[4].Value), Angle.FromDegrees(values[5].Value), values[3].HasValue || values[4].HasValue || values[5].HasValue);
    }
    public AnimationElement(AnimationElementValue x, AnimationElementValue y, AnimationElementValue z, AnimationElementRotation rotation)
    {
        OffsetX = x;
        OffsetY = y;
        OffsetZ = z;
        Rotation = rotation;
    }
    public AnimationElement(params float?[] values)
    {
        OffsetX = new(values[0] ?? 0, values[0].HasValue);
        OffsetY = new(values[1] ?? 0, values[1].HasValue);
        OffsetZ = new(values[2] ?? 0, values[2].HasValue);
        Rotation = new(Angle.FromDegrees(values[3] ?? 0), Angle.FromDegrees(values[4] ?? 0), Angle.FromDegrees(values[5] ?? 0));
    }

    public void Apply(ElementPose pose)
    {
        if (OffsetX.HasValue) pose.translateX = OffsetX.Value * OffsetFactor;
        if (OffsetY.HasValue) pose.translateY = OffsetY.Value * OffsetFactor;
        if (OffsetZ.HasValue) pose.translateZ = OffsetZ.Value * OffsetFactor;
        if (Rotation.HasValue) Rotation.Apply(pose);
    }

#if DEBUG
    public AnimationElement Edit(string title, float multiplier = 10)
    {
        float speed = ImGui.GetIO().KeysDown[(int)ImGuiKey.LeftShift] ? 0.1f : 1;

        AnimationElementValue offsetX = EditValue(OffsetX, multiplier, speed, $"X##translation{title}"); ImGui.SameLine();
        AnimationElementValue offsetY = EditValue(OffsetY, multiplier, speed, $"Y##translation{title}"); ImGui.SameLine();
        AnimationElementValue offsetZ = EditValue(OffsetZ, multiplier, speed, $"Z##translation{title}"); ImGui.SameLine();
        ImGui.Text("Translation"); ImGui.SameLine();

        ImGui.SameLine();
        if (ImGui.Button($"Copy##{title}"))
        {
            _buffer = this;
        }

        Vector3 angles = Rotation.Orientation.ToEulerAngles();

        AnimationElementValue rotationX = EditValue(new(angles.X * GameMath.RAD2DEG, Rotation.HasValue), 1, speed, $"X##rotation{title}"); ImGui.SameLine();
        AnimationElementValue rotationY = EditValue(new(angles.Y * GameMath.RAD2DEG, Rotation.HasValue), 1, speed, $"Y##rotation{title}"); ImGui.SameLine();
        AnimationElementValue rotationZ = EditValue(new(angles.Z * GameMath.RAD2DEG, Rotation.HasValue), 1, speed, $"Z##rotation{title}"); ImGui.SameLine();
        ImGui.Text("Rotation     "); ImGui.SameLine();
        Quaternion rotation = Quaternion.FromEulerAngles(rotationX.Value * GameMath.DEG2RAD, rotationY.Value * GameMath.DEG2RAD, rotationZ.Value * GameMath.DEG2RAD);

        /*Rotation.Value.ToAxisAngle(out Vector3 axis, out float angle);
        angle = GameMath.RAD2DEG * angle;
        AnimationElementValue rotationX = EditValue(new(axis.X, Rotation.HasValue), 1, speed, $"X##rotation{title}"); ImGui.SameLine();
        AnimationElementValue rotationY = EditValue(new(axis.Y, Rotation.HasValue), 1, speed, $"Y##rotation{title}"); ImGui.SameLine();
        AnimationElementValue rotationZ = EditValue(new(axis.Z, Rotation.HasValue), 1, speed, $"Z##rotation{title}"); ImGui.SameLine();
        AnimationElementValue rotationW = EditValue(new(angle, Rotation.HasValue), 1, speed, $"Deg##rotation{title}"); ImGui.SameLine();
        angle = GameMath.DEG2RAD * rotationW.Value;
        axis = new(rotationX.Value, rotationY.Value, rotationZ.Value);
        Quaternion rotation = Quaternion.FromAxisAngle(axis, angle);*/


        ImGui.SameLine();
        if (ImGui.Button($"Paste##{title}"))
        {
            return _buffer;
        }

        return new(
            offsetX,
            offsetY,
            offsetZ,
            new AnimationElementRotation(rotation)
            );
    }
#endif

    public float?[] ToArray() =>
    [
        OffsetX.HasValue ? OffsetX.Value : null,
        OffsetY.HasValue ? OffsetY.Value : null,
        OffsetZ.HasValue ? OffsetZ.Value : null,
        Rotation.HasValue ? Rotation.Orientation.ToEulerAngles().X : null,
        Rotation.HasValue ? Rotation.Orientation.ToEulerAngles().Y : null,
        Rotation.HasValue ? Rotation.Orientation.ToEulerAngles().Z : null
    ];

    public static AnimationElement Interpolate(AnimationElement from, AnimationElement to, float progress, bool shortestAngleDistance = false)
    {
        return new(
            new(from.OffsetX.Value + (to.OffsetX.Value - from.OffsetX.Value) * progress, from.OffsetX.HasValue || to.OffsetX.HasValue),
            new(from.OffsetY.Value + (to.OffsetY.Value - from.OffsetY.Value) * progress, from.OffsetY.HasValue || to.OffsetY.HasValue),
            new(from.OffsetZ.Value + (to.OffsetZ.Value - from.OffsetZ.Value) * progress, from.OffsetZ.HasValue || to.OffsetZ.HasValue),
            AnimationElementRotation.Interpolate(from.Rotation, to.Rotation, progress, shortestAngleDistance)
            );
    }
    public static AnimationElement Compose(IEnumerable<(AnimationElement element, float weight)> elements)
    {
        AnimationElementValue offsetX = AnimationElementValue.Empty;
        AnimationElementValue offsetY = AnimationElementValue.Empty;
        AnimationElementValue offsetZ = AnimationElementValue.Empty;
        AnimationElementRotation rotation = AnimationElementRotation.Empty;

        float offsetXMaxWeight = 0;
        float offsetYMaxWeight = 0;
        float offsetZMaxWeight = 0;
        float rotationMaxWeight = 0;

        foreach ((AnimationElement element, float weight) in elements.Where(entry => entry.weight > 0))
        {
            if (weight >= offsetXMaxWeight && element.OffsetX.HasValue) { offsetXMaxWeight = weight; offsetX = element.OffsetX; }
            if (weight >= offsetYMaxWeight && element.OffsetY.HasValue) { offsetYMaxWeight = weight; offsetY = element.OffsetY; }
            if (weight >= offsetZMaxWeight && element.OffsetZ.HasValue) { offsetZMaxWeight = weight; offsetZ = element.OffsetZ; }
            if (weight >= rotationMaxWeight && element.Rotation.HasValue) { rotationMaxWeight = weight; rotation = element.Rotation; }
        }

        foreach ((AnimationElement element, float weight) in elements.Where(entry => entry.weight <= 0))
        {
            if (element.OffsetX.HasValue) offsetX += element.OffsetX;
            if (element.OffsetY.HasValue) offsetY += element.OffsetY;
            if (element.OffsetZ.HasValue) offsetZ += element.OffsetZ;
            if (element.Rotation.HasValue) rotation += element.Rotation;
        }

        return new(
            offsetX,
            offsetY,
            offsetZ,
            rotation
            );
    }
    public static AnimationElement FromVanilla(AnimationKeyFrameElement frame)
    {
        return new(
            new((float)(frame.OffsetX ?? 0), frame.OffsetX != null),
            new((float)(frame.OffsetY ?? 0), frame.OffsetY != null),
            new((float)(frame.OffsetZ ?? 0), frame.OffsetZ != null),
            new AnimationElementRotation(Angle.FromDegrees((float)(frame.RotationX ?? 0)), Angle.FromDegrees((float)(frame.RotationY ?? 0)), Angle.FromDegrees((float)(frame.RotationZ ?? 0))));
    }

    public override string ToString() => $"[{OffsetX}, {OffsetY}, {OffsetZ}, {Rotation.Orientation.ToEulerAngles()}]";

#if DEBUG
    private static AnimationElementValue EditValue(AnimationElementValue value, float multiplier, float speed, string title)
    {
        bool enabled = value.HasValue;
        float newValue = value.Value;
        if (enabled)
        {
            float valueValue = value.Value * multiplier;
            ImGui.SetNextItemWidth(90);
            ImGui.DragFloat($"##{title}value", ref valueValue, speed); ImGui.SameLine();
            ImGui.Checkbox($"##{title}checkbox", ref enabled);

            newValue = valueValue / multiplier;
        }
        else
        {
            ImGui.Checkbox($"{title}##checkbox", ref enabled);
        }
        return new(newValue, enabled);
    }
    private static AnimationElement _buffer = AnimationElement.Zero;
#endif
}

public readonly struct AnimationElementRotation
{
    /// <summary>
    /// Element orientation in space
    /// </summary>
    public readonly Quaternion Orientation;
    /// <summary>
    /// Should value be applied to element and used for interpolation
    /// </summary>
    public readonly bool HasValue;
    /// <summary>
    /// Number of full rotations, it meant to preserve multiple roations encoded via Euler angles, but not accounted for via quaternion
    /// </summary>
    public readonly int Spin;

    public static readonly AnimationElementRotation Empty = new();
    public static readonly AnimationElementRotation Zero = new(Quaternion.Identity);

    public AnimationElementRotation()
    {
        Orientation = Quaternion.Identity;
        HasValue = false;
        Spin = 0;
    }
    public AnimationElementRotation(Angle x, Angle y, Angle z, bool hasValue = true)
    {
        Orientation = Quaternion.Normalize(Quaternion.FromEulerAngles(x.Radians, y.Radians, z.Radians));

        Spin = EstimateSpin(x, y, z);
        HasValue = hasValue;
    }
    public AnimationElementRotation(Quaternion value)
    {
        Orientation = Quaternion.Normalize(value);
        Spin = 0;
        HasValue = true;
    }
    public AnimationElementRotation(Quaternion value, int spin, bool hasValue)
    {
        Orientation = Quaternion.Normalize(value);
        Spin = spin;
        HasValue = hasValue;
    }

    public void Apply(ElementPose pose)
    {
        if (!HasValue) return;

        Vector3 angles = Orientation.ToEulerAngles();

        pose.degX = angles.X * GameMath.RAD2DEG;
        pose.degY = angles.Y * GameMath.RAD2DEG;
        pose.degZ = angles.Z * GameMath.RAD2DEG;
    }

    public AnimationElementRotation Add(AnimationElementRotation element)
    {
        if (!HasValue) return element;
        if (!element.HasValue) return this;

        Quaternion result = Quaternion.Normalize(Orientation * element.Orientation);

        return new AnimationElementRotation(result, Spin + element.Spin, true);
    }

    public static AnimationElementRotation operator +(AnimationElementRotation first, AnimationElementRotation second) => first.Add(second);

    public static AnimationElementRotation Interpolate(AnimationElementRotation from, AnimationElementRotation to, float progress, bool shortestAngleDistance)
    {
        if (!from.HasValue) return to;
        if (!to.HasValue) return from;

        Quaternion toOrientation = to.Orientation;

        if (QuaternionDot(from.Orientation, to.Orientation) < 0f)
        {
            toOrientation = QuaternionNegate(toOrientation);
        }

        Quaternion noSpinInterpolation = Quaternion.Slerp(from.Orientation, toOrientation, progress);

        int spin = to.Spin - from.Spin;

        if (false)//!shortestAngleDistance && spin != 0)
        {
            noSpinInterpolation.ToAxisAngle(out Vector3 axis, out float angle);

            angle += spin * MathF.PI * 2f * progress;

            noSpinInterpolation = Quaternion.FromAxisAngle(axis, angle);
        }

        return new AnimationElementRotation(Quaternion.Normalize(noSpinInterpolation), spin: 0, hasValue: true);
    }

    public override string ToString() => HasValue ? $"{Orientation:F3}, spin: {Spin}" : "-";



    private static float QuaternionDot(Quaternion a, Quaternion b)
    {
        return a.X * b.X + a.Y * b.Y + a.Z * b.Z + a.W * b.W;
    }

    private static Quaternion QuaternionNegate(Quaternion q)
    {
        return new Quaternion(-q.X, -q.Y, -q.Z, -q.W);
    }

    private static int EstimateSpin(Angle x, Angle y, Angle z)
    {
        return (int)MathF.Round((MathF.Abs(x.Degrees) + MathF.Abs(y.Degrees) + MathF.Abs(z.Degrees)) / 360f);
    }
}

public readonly struct AnimationElementValue
{
    public readonly float Value;
    public readonly bool HasValue;

    public static readonly AnimationElementValue Empty = new();
    public static readonly AnimationElementValue Zero = new(0);

    public AnimationElementValue()
    {
        HasValue = false;
    }
    public AnimationElementValue(float value)
    {
        Value = value;
        HasValue = true;
    }
    public AnimationElementValue(float value, bool hasValue)
    {
        Value = value;
        HasValue = hasValue;
    }

    public AnimationElementValue Add(AnimationElementValue element)
    {
        if (HasValue)
        {
            if (element.HasValue)
            {
                return new AnimationElementValue(Value + element.Value);
            }
            else
            {
                return this;
            }
        }
        else
        {
            return element;
        }
    }

    public static AnimationElementValue operator +(AnimationElementValue first, AnimationElementValue second) => first.Add(second);

    public override string ToString() => HasValue ? $"{Value:F3}" : "-";
}
