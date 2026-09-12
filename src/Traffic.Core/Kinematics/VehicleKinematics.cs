namespace Traffic.Core.Kinematics;

using System;

public static class VehicleKinematics
{
    public static float CalculateAcceleration(
        float currentSpeed,
        IdmParameters idm,
        float? netDistanceToLeader,
        float? leaderSpeed
    )
    {
        var freeRoadFactor =
            1.0f - MathF.Pow(Math.Max(0f, currentSpeed) / idm.DesiredSpeed, idm.AccelerationExponent);

        if (!netDistanceToLeader.HasValue || !leaderSpeed.HasValue)
        {
            // Free driving
            return idm.MaxAcceleration * freeRoadFactor;
        }
        
        //TODO Max() vs Clamp()
        
        var deltaV = currentSpeed - leaderSpeed.Value;
        var sStar = idm.JamDistance +
                    Math.Max(0f, (currentSpeed * idm.SafeTimeHeadway) + (currentSpeed * deltaV) / 
                        (2.0f * MathF.Sqrt(idm.MaxAcceleration * idm.DesiredDeceleration)));

        var interactionFactor = MathF.Pow(sStar / Math.Max(0.1f, netDistanceToLeader.Value), 2.0f);
        
        return idm.MaxAcceleration * (freeRoadFactor - interactionFactor);
    }
}