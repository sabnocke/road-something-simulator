namespace Traffic.Core.Graph;

using Traffic.Core.Math;

public class RoadSegment(Guid startNodeId, Guid endNodeId, BezierCurve3D curve)
{
    public Guid Id { get; } = Guid.NewGuid();
    public Guid StartNodeId { get; set; } = startNodeId;
    public Guid EndNodeId { get; set; } = endNodeId;

    public BezierCurve3D Centerline { get; set; } = curve;

    public List<LaneSegment> Lanes { get; } = [];
}