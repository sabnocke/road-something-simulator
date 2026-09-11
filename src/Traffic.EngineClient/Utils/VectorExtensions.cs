namespace RoadTrafficSim.Utils;

using System.Runtime.CompilerServices;

public static class VectorExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Godot.Vector3 ToGodot(this System.Numerics.Vector3 vector) => new(vector.X, vector.Y, vector.Z);
}