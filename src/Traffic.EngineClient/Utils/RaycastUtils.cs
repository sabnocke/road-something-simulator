using Godot;

namespace RoadTrafficSim.Utils;

public class RaycastUtils
{
    private static readonly Plane GroundPlane = new(Vector3.Up, 0f);

    public static bool TryGetGroundPoint(Camera3D camera, Vector2 screenPos, out Vector3 worldPos)
    {
        var rayOrigin = camera.ProjectRayOrigin(screenPos);
        var rayDir = camera.ProjectRayNormal(screenPos);

        var hit = GroundPlane.IntersectsRay(rayOrigin, rayDir);
        if (hit.HasValue)
        {
            worldPos = hit.Value;
            return true;
        }
        
        worldPos = Vector3.Zero;
        return false;
    }
}