namespace Traffic.Core.Simulation;

using System;
using System.Collections.Generic;
using Traffic.Core.Graph;
using Traffic.Core.Math;

public class Route
{
    private readonly List<RoadSegment> _segments = [];
    
    public IReadOnlyList<RoadSegment> Segments => _segments;
    public float TotalLength { get; private set; }

    public Route(IEnumerable<RoadSegment> segments)
    {
        _segments.AddRange(segments);
        RecalculateLength();
    }

    public void RecalculateLength()
    {
        TotalLength = _segments.Sum((item) => item.Centerline.TotalLength);
        
        /*TotalLength = 0f;
        foreach (var seg in _segments)
            TotalLength += seg.Centerline.TotalLength;*/
    }

    public (RoadSegment segment, float localDistance) ResolvePosition(float routeDistance)
    {
        if (_segments.Count == 0)
            throw new InvalidOperationException("Route contains no segments");

        routeDistance = ((routeDistance % TotalLength) + TotalLength) % TotalLength; // ???
        var acc = 0f;

        for (var i = 0; i < _segments.Count; i++)
        {
            var segLen = _segments[i].Centerline.TotalLength;
            if (routeDistance <= acc + segLen || i == _segments.Count - 1)
                return (_segments[i], routeDistance - acc);
            acc += segLen;
        }

        return (_segments[^1], _segments[^1].Centerline.TotalLength);
    }
}