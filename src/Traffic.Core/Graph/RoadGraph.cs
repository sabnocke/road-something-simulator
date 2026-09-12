using System.Threading.Tasks.Dataflow;
using Traffic.Core.Simulation;

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
        var p0 = startNode.Position;
        var p1 = startNode.Position + startControlOffset;
        var p2 = endNode.Position + endControlOffset;
        var p3 = endNode.Position;

        var spline = new BezierCurve3D(p0, p1, p2, p3);
        var segment = new RoadSegment(startNodeId, endNodeId, spline);

        _segments[segment.Id] = segment;
        startNode.ConnectedSegmentIds.Add(segment.Id);
        endNode.ConnectedSegmentIds.Add(segment.Id);

        return segment;
    }

    public bool TryGetNode(Guid id, out RoadNode? node) => _nodes.TryGetValue(id, out node);
    public bool TryGetSegment(Guid id, out RoadSegment? segment) => _segments.TryGetValue(id, out segment);

    public Vector3? GetNodeOutgoingTangent(Guid nodeId)
    {
        if (!_nodes.TryGetValue(nodeId, out var node))
            return null;

        // Find any segment that starts at this node
        foreach (var segId in node.ConnectedSegmentIds)
        {
            if (_segments.TryGetValue(segId, out var seg) && seg.StartNodeId == nodeId)
                return Vector3.Normalize(seg.Centerline.P1 - seg.Centerline.P0);
        }

        return null;
    }

    public Route? ExtractClosedCircuit()
    {
        if (_segments.Count < 2)
        {
            Console.WriteLine($"Not enough segments for closed circuit");
            return null;
        }
            

        // Build directed adjacency: StartNodeId -> List of outgoing segments
        var outgoing = new Dictionary<Guid, List<RoadSegment>>();
        foreach (var seg in _segments.Values)
        {
            if (!outgoing.TryGetValue(seg.StartNodeId, out var list))
            {
                list = [];
                outgoing[seg.StartNodeId] = list;
            }
            list.Add(seg);
        }

        // Try finding a cycle starting from each segment
        foreach (var seedSegment in _segments.Values)
        {
            var path = new List<RoadSegment> { seedSegment };
            var visited = new HashSet<Guid> { seedSegment.Id };
            var currentNode = seedSegment.EndNodeId;
            var originNode = seedSegment.StartNodeId;

            while (true)
            {
                if (currentNode == originNode)
                {
                    // Found a valid closed loop!
                    return new Route(path);
                }

                if (!outgoing.TryGetValue(currentNode, out var nextOptions))
                    break;

                // Find an unvisited outgoing segment
                var nextSegment = nextOptions.Find(opt => !visited.Contains(opt.Id));
                    
                /*foreach (var opt in nextOptions)
                {
                    if (!visited.Contains(opt.Id))
                    {
                        nextSegment = opt;
                        break;
                    }
                }*/
                if (nextSegment == null)
                    break; // Dead end
                
                path.Add(nextSegment);
                visited.Add(nextSegment.Id);
                currentNode = nextSegment.EndNodeId;
            }
        }

        return null;
    }
}