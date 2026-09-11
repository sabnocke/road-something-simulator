namespace Traffic.Core.Graph;

using System;
using System.Collections.Generic;
using System.Numerics;
using Traffic.Core.Math;

public class RoadGraph
{
    private readonly Dictionary<Guid, RoadNode> _nodes = new();
    private readonly Dictionary<Guid, RoadSegment> _segments = new();
    
    public IReadOnlyCollection<RoadNode> Nodes => _nodes.Values;
    public IReadOnlyCollection<RoadSegment> Segments => _segments.Values;

    public RoadNode CreateNode(Vector3 position)
    {
        var node = new RoadNode(position);
        _nodes[node.Id] = node;
        return node;
    }

    public RoadSegment CreateSegment(
        Guid startNodeId,
        Guid endNodeId,
        Vector3 startControlOffset,
        Vector3 endControlOffset)
    {
        if (!_nodes.TryGetValue(startNodeId, out var startNode))
            throw new ArgumentException($"Start node '{startNodeId}' does not exist.");
        if (!_nodes.TryGetValue(endNodeId, out var endNode))
            throw new ArgumentException($"End node '{endNodeId}' does not exist.");
        
        // Compute absolute control handles from offsets
        Vector3 p0 = startNode.Position;
        Vector3 p1 = startNode.Position + startControlOffset;
        Vector3 p2 = endNode.Position + endControlOffset;
        Vector3 p3 = endNode.Position;

        var spline = new BezierCurve3D(p0, p1, p2, p3);
        var segment = new RoadSegment(startNodeId, endNodeId, spline);
        
        _segments[segment.Id] = segment;
        startNode.ConnectedSegmentIds.Add(segment.Id);
        endNode.ConnectedSegmentIds.Add(segment.Id);
        
        return segment;
    }
    
    public bool TryGetNode(Guid id, out RoadNode? node) => _nodes.TryGetValue(id, out node);
    public bool TryGetSegment(Guid id, out RoadSegment? segment) => _segments.TryGetValue(id, out segment);
}