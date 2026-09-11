namespace Traffic.Core.Kinematics;
/// <summary>
/// IDM stands for Intelligent Driver Model
/// </summary>
/// <param name="desiredSpeed">v0 (m/s)</param>
/// <param name="safeTimeHeadway">T (seconds)</param>
/// <param name="maxAcceleration">a (m/s^2)</param>
/// <param name="desiredDeceleration">b (m/s^2) - comfortable braking</param>
/// <param name="jamDistance">s0 (meters) - minimum bumper-to-bumper gap</param>
/// <param name="accelerationExp">delta (usually 4.0)</param>
public readonly struct IdmParameters(
    float desiredSpeed = 13.88f,
    float safeTimeHeadway = 1.5f,
    float maxAcceleration = 1.5f,
    float desiredDeceleration = 2.0f,
    float jamDistance = 2.0f,
    float accelerationExp = 4.0f
)
{
    public readonly float DesiredSpeed = desiredSpeed; 
    public readonly float SafeTimeHeadway = safeTimeHeadway; 
    public readonly float MaxAcceleration = maxAcceleration; 
    public readonly float DesiredDeceleration = desiredDeceleration;
    public readonly float JamDistance = jamDistance;
    public readonly float AccelerationExponent = accelerationExp;
}