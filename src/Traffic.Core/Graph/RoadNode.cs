namespace Traffic.Core.Graph;

using System;
using System.Numerics;
using System.Collections.Generic;

public sealed class RoadNode(Vector3 position)
{
    public Guid Id { get; } =  Guid.NewGuid();
    public Vector3 Position { get; set; } = position;

    public List<Guid> ConnectedSegmentIds { get; } = [];
}