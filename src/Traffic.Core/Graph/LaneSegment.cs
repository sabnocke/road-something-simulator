namespace Traffic.Core.Graph;

using System;
using System.Collections.Generic;
using Traffic.Core.Math;

public sealed class LaneSegment(
    Guid parentSegmentId,
    Guid startNodeId,
    Guid endNodeId,
    BezierCurve3D curve
    )
{
    public Guid Id { get; } = Guid.NewGuid();
    public Guid ParentSegmentId { get; } = parentSegmentId;
    
    // Topological endpoints defining flow direction
    public Guid StartNodeId { get; } = startNodeId;
    public Guid EndNodeId { get; } = endNodeId;
    
    public BezierCurve3D Curve { get; } = curve;
    public float Length => Curve.TotalLength;

    public float SpeedLimit { get; set; } = 13.88f; // m/s (~50km/h)

    public List<Guid> NextLaneIds { get; } = []; // Downstream lanes reachable across EndNode
    public List<Guid> PreviousLaneIds { get; } = []; // Upstream lanes feeding into StartNode
    
    public Guid? LeftNeighborLaneId { get; set; } // For MOBIL lane-change logic
    public Guid? RightNeighborLaneId { get; set; }
    
    
}