namespace Traffic.Core.Math;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;

public readonly struct BezierCurve3D
{
    /// <summary>
    /// Defines cubic Bézier curve for 3D space
    /// </summary>
    public readonly Vector3 P0; // Start point
    public readonly Vector3 P1; // Control point 1
    public readonly Vector3 P2; // Control point 2
    public readonly Vector3 P3; // End point

    private readonly float[] _arcLengths;
    public readonly float TotalLength;

    public const int DefaultSampleCount = 64;

    public BezierCurve3D(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, int sampleCount = DefaultSampleCount)
    {
        P0 = p0;
        P1 = p1;
        P2 = p2;
        P3 = p3;
        
        if (sampleCount < 2)
            throw new ArgumentOutOfRangeException(nameof(sampleCount), "Sample count must be at least 2.");
        
        _arcLengths = new float[sampleCount];
        _arcLengths[0] = 0f;

        var previousPoint = p0;
        var accumulated = 0f;

        for (var i = 1; i < sampleCount; i++)
        {
            var t = (float)i / (sampleCount - 1);
            var currentPoint = Evaluate(t);
            accumulated += Vector3.Distance(previousPoint, currentPoint);
            _arcLengths[i] = accumulated;
            previousPoint = currentPoint;
        }
        
        TotalLength = accumulated;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 Evaluate(float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        var u = 1f - t;
        var tt = t * t;
        var uu = u * u;
        var uuu = uu * u;
        var ttt = tt * t;

        return (uuu * P0) + (3f * uu * t * P1) + (3f * u * tt * P2) + (ttt * P3);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 EvaluateDerivative(float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        float u = 1f - t;
        
        // First derivative: 3*(1-t)^2*(P1-P0) + 6*(1-t)*t*(P2-P1) + 3*t^2*(P3-P2)
        return (3f * u * u * (P1 - P0)) +
               (6f * u * t * (P2 - P1)) +
               (3f * t * t * (P3 - P2));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 EvaluateTangent(float t)
    {
        var d = EvaluateDerivative(t);
        var lenSq = d.LengthSquared();
        return lenSq > 1e-8f ? Vector3.Normalize(d) : Vector3.UnitZ;
    }
    
    /// <summary>
    /// Most likely returns a second curve that is shifted to the right by lateralOffset
    /// </summary>
    /// <param name="lateralOffset">Length of normal vector and space between the two (this and return of this) curves</param>
    /// <returns>A new curve</returns>
    public BezierCurve3D GenerateOffsetCurve(float lateralOffset)
    {
        var n0 = Vector3.Normalize(Vector3.Cross(EvaluateTangent(0f), Vector3.UnitY));
        var n1 = Vector3.Normalize(Vector3.Cross(EvaluateTangent(1/3f), Vector3.UnitY));
        var n2 = Vector3.Normalize(Vector3.Cross(EvaluateTangent(2/3f), Vector3.UnitY));
        var n3 = Vector3.Normalize(Vector3.Cross(EvaluateTangent(3/3f), Vector3.UnitY));

        return new BezierCurve3D(
            P0 + n0 * lateralOffset,
            P1 + n1 * lateralOffset,
            P2 + n2 * lateralOffset,
            P3 + n3 * lateralOffset
        );
    }
    
    public float GetTAtDistance(float meters)
    {
        if (TotalLength <= 1e-6f || meters <= 0f)
            return 0f;

        if (meters >= TotalLength)
            return 1f;
        
        // Binary search
        var low = 0;
        var high = _arcLengths.Length - 1;

        while (low < high - 1)
        {
            var mid = (low + high) >> 1;
            if (_arcLengths[mid] <= meters)
                low = mid;
            else
                high = mid;
        }
        
        // Linear interpolation between the two closest sample points
        var segStartDist = _arcLengths[low];
        var segEndDist = _arcLengths[high];
        var segLength = segEndDist - segStartDist;

        var segFraction = segLength > 1e-6f ? (meters - segStartDist) / segLength : 0f;

        var tLow = (float)low / (_arcLengths.Length - 1);
        var tHigh = (float)high / (_arcLengths.Length - 1);
        
        return tLow + segFraction * (tHigh - tLow);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 EvaluateAtDistance(float meters)
    {
        return Evaluate(GetTAtDistance(meters));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 EvaluateTangentAtDistance(float meters)
    {
        return EvaluateTangent(GetTAtDistance(meters));
    }
}