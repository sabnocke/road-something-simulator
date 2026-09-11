namespace Traffic.Core.Simulation;

using System;
using Traffic.Core.Kinematics;
using Traffic.Core.Math;

public class Agent(float startDistance, float initialSpeed, IdmParameters idm)
{
    public Guid Id { get; } = Guid.NewGuid();
    public float DistanceAlongSpline { get; set; } = startDistance;     // Position s in meters
    public float Speed { get; set; } = initialSpeed;                    // v in m/s
    public float Length { get; } = 4.5f;                                // Vehicle physical length // TODO add option to change this
    public IdmParameters Idm { get; } = idm;

    public void Step(float dt, BezierCurve3D spline, Agent? leader)
    {
        float? netGap = null;
        float? leaderSpeed = null;

        if (leader != null)
        {
            // Bumper-to-bumper distance
            netGap = leader.DistanceAlongSpline - DistanceAlongSpline - leader.Length;
            leaderSpeed = leader.Speed;
        }

        var acceleration = VehicleKinematics.CalculateAcceleration(Speed, Idm, netGap, leaderSpeed);

        Speed = Math.Max(0f, Speed + acceleration * dt);
        DistanceAlongSpline += Speed * dt;
        
        // Loop around the spline for continuous testing
        if (DistanceAlongSpline > spline.TotalLength)
            DistanceAlongSpline -= spline.TotalLength;
    }
}