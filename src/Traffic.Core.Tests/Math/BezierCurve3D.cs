namespace Traffic.Core.Tests.Math;

using System.Numerics;
using Traffic.Core.Math;
using Xunit;

public class BezierCurve3DTests
{
    [Fact]
    public void StraightLine_TotalLengthMatchesDistance()
    {
        var p0 = new Vector3(0, 0, 0);
        var p1 = new Vector3(0, 0, 33.33f);
        var p2 = new Vector3(0f, 0, 66.66f);
        var p3 = new Vector3(0f, 0f, 100f);
        
        var curve = new BezierCurve3D(p0, p1, p2, p3);
        Assert.Equal(100f, curve.TotalLength, precision: 1);
    }

    [Fact]
    public void EvaluateAtDistance_ReturnsEvenMidpoint()
    {
        var p0 = new Vector3(0, 0, 0);
        var p1 = new Vector3(0, 0, 25f);
        var p2 = new Vector3(0, 0, 75f);
        var p3 = new Vector3(0f, 0f, 100f);
        
        var curve = new BezierCurve3D(p0, p1, p2, p3);
        var midPoint = curve.EvaluateAtDistance(50f);
        
        Assert.Equal(0f, midPoint.X, precision: 2);
        Assert.Equal(0f, midPoint.Y, precision: 2);
        Assert.Equal(50f, midPoint.Z, precision: 2);
    }

    [Fact]
    public void EvaluateTangent_IsNormalized()
    {
        var curve = new BezierCurve3D(
            new Vector3(0, 0, 0),
            new Vector3(10f, 5f, 20f),
            new Vector3(30f, 10f, 40f),
            new Vector3(50f, 0f, 50f)
        );

        var tangent = curve.EvaluateTangent(0.5f);
        
        Assert.Equal(1f, tangent.Length(), precision: 4);
    }
}