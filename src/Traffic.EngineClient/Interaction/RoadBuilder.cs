//TODO add all colors into a common class
//TODO maybe move common functions into a class and inherit from it

namespace RoadTrafficSim.Interaction;

using System;
using Godot;
using Traffic.Core.Graph;
using Traffic.Core.Math;
using RoadTrafficSim.Utils;
using Numerics = System.Numerics;

public partial class RoadBuilder : Node3D
{
	[Export] public Camera3D Camera { get; set; } = null!;
	[Export] public float SnapRadius { get; set; } = 8.0f;
	
	public RoadGraph Graph { get; } = new();
	public bool IsBuildModeActive { get; set; } = true;
	
#nullable enable
	private RoadNode? _snappedNode;
	private RoadNode? _pendingStartNode;
	private Vector3? _currentHoverPoint;
#nullable disable
	
	private ImmediateMesh _immediateMesh = null!;
	private MeshInstance3D _meshInstance = null!;
	private StandardMaterial3D _lineMaterial = null!;

	public override void _Ready()
	{
		_lineMaterial = new StandardMaterial3D()
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			VertexColorUseAsAlbedo = true,
			CullMode = BaseMaterial3D.CullModeEnum.Disabled
		};

		_immediateMesh = new ImmediateMesh();
		_meshInstance = new MeshInstance3D
		{
			Mesh = _immediateMesh,
			MaterialOverride = _lineMaterial,
		};
		AddChild(_meshInstance);

		// Redraw();
	}

	public override void _Input(InputEvent @event)
	{
		if (!IsBuildModeActive)
			return;

		switch (@event)
		{
			case InputEventMouseMotion mouseMotion:
			{
				UpdateHover(mouseMotion.Position);
				Redraw();
				break;
			}
			case InputEventMouseButton { Pressed: true } mouseButton:
				switch (mouseButton.ButtonIndex)
				{
					case MouseButton.Left:
						UpdateHover(mouseButton.Position);
						HandleLeftClick(mouseButton.Position);
						break;
					case MouseButton.Right:
						CancelPlacement();
						break;
					case MouseButton.None:
					case MouseButton.Middle:
					case MouseButton.WheelUp:
					case MouseButton.WheelDown:
					case MouseButton.WheelLeft:
					case MouseButton.WheelRight:
					case MouseButton.Xbutton1:
					case MouseButton.Xbutton2:
					default:
						break;
				}
				break;
		}
	}

	private void UpdateHover(Vector2 screenPos)
	{
		if (!RaycastUtils.TryGetGroundPoint(Camera, screenPos, out var hit)) return;
		_snappedNode = FindClosestNode(hit, SnapRadius);
		_currentHoverPoint = _snappedNode?.Position.ToGodot() ?? hit;
	}
	
#nullable enable
	private RoadNode? FindClosestNode(Vector3 worldPoint, float maxDistance)
	{
		RoadNode? closest = null;
		var bestDistSq = maxDistance * maxDistance;
		
		var targetPos = new Numerics.Vector3(worldPoint.X, worldPoint.Y, worldPoint.Z);

		foreach (var node in Graph.Nodes)
		{
			// Don't snap to the current start node of the active preview segment
			if (_pendingStartNode != null && node.Id == _pendingStartNode.Id)
				continue;
			
			var distSq = Numerics.Vector3.DistanceSquared(targetPos, node.Position);
			if (!(distSq < bestDistSq)) 
				continue;
			
			bestDistSq = distSq;
			closest = node;
		}

		return closest;
	}
#nullable disable
	
	private void HandleLeftClick(Vector2 mouseScreenPos)
	{
		GD.Print($"Click fired! HoverPoint: {_currentHoverPoint}, BuildMode: {IsBuildModeActive}");

		if (!_currentHoverPoint.HasValue)
			return;
		
		var currentSysPos = _snappedNode?.Position
		                    ?? new Numerics.Vector3(_currentHoverPoint.Value.X, 0f, _currentHoverPoint.Value.Z);

		if (_pendingStartNode == null)
		{
			// First click: pick or place starting anchor
			_pendingStartNode = _snappedNode ?? Graph.CreateNode(currentSysPos);
		}
		else
		{
			// Reject clicking the exact same node twice without moving
			if (_snappedNode != null && _snappedNode.Id == _pendingStartNode.Id)
				return;

			RoadNode endNode;
			var closedLoop = false;

			if (_snappedNode != null)
			{
				endNode = _snappedNode;
				closedLoop = true;
			}
			else
			{
				endNode = Graph.CreateNode(currentSysPos);
			}

			var delta = endNode.Position - _pendingStartNode.Position;
			var handleLength = delta.Length() * 0.33f;
			var dir = Numerics.Vector3.Normalize(delta);

			Graph.CreateSegment(_pendingStartNode.Id, endNode.Id, dir * handleLength, -dir * handleLength);
			
			// Close loop ends the chain; snapping to new road continues it
			_pendingStartNode = closedLoop ? null : endNode;
			_snappedNode = null;
		}
		
		/*var clickedSysVec = new Numerics.Vector3(
			_currentHoverPoint.Value.X, 
			_currentHoverPoint.Value.Y,
			_currentHoverPoint.Value.Z
		);

		if (_pendingStartNode == null)
		{
			// Start a chain from an existing snapped node, or create a new node
			_pendingStartNode = _snappedNode ?? Graph.CreateNode(clickedSysVec);
		}
		else
		{
			RoadNode endNode;
			var isCircuitClosed = false;

			if (_snappedNode != null)
			{
				// Connect to the existing node to close a loop or form an intersection
				endNode = _snappedNode;
				isCircuitClosed = true;
			}
			else
			{
				// Plant a brand-new node
				endNode = Graph.CreateNode(clickedSysVec);
			}

			var delta = endNode.Position - _pendingStartNode.Position;
			var handleLength = delta.Length() * 0.33f;
			var dir = Numerics.Vector3.Normalize(delta);

			var startHandle = dir * handleLength;
			var endHandle = -dir * handleLength;

			Graph.CreateSegment(_pendingStartNode.Id, endNode.Id, startHandle, endHandle);
			
			// If circuit is closed, terminate placement; otherwise continue chaining
			_pendingStartNode = isCircuitClosed ? null : endNode;
			_snappedNode = null;
		}*/
		
		Redraw();
	}

	public void CancelPlacement()
	{
		_pendingStartNode = null;
		_snappedNode = null;
		Redraw();
	}

	public void ClearAll()
	{
		// Reset state
		_pendingStartNode = null;
		_currentHoverPoint = null;
		
		// Create a clean graph
		var newGraph = new RoadGraph();
		//TODO add Clear() method to RoadGraph
		_immediateMesh.ClearSurfaces();
	}
	
	
	private void Redraw()
	{
		_immediateMesh.ClearSurfaces();
		
		var hasSegments = Graph.Segments.Count > 0;
		var hasNodes = Graph.Nodes.Count > 0;
		var hasPreview = _pendingStartNode != null && _currentHoverPoint.HasValue;

		// Guard: Godot crashes SurfaceEnd() if no vertices are pushed
		if (!hasSegments && !hasNodes && !hasPreview)
			return;
		
		_immediateMesh.SurfaceBegin(Mesh.PrimitiveType.Lines);
		
		// Draw confirmed roads in the graph
		foreach (var segment in Graph.Segments)
			DrawSpline(segment.Centerline, new Color(0.2f, 0.8f, 1f)); // Cyan
		
		// Draw confirmed nodes
		foreach (var node in Graph.Nodes)
			DrawCross(node.Position.ToGodot(), 1.5f, new Color(1.0f, 0.2f, 0.2f)); // Red

		
		
		// Draw live preview spline when dragging
		if (_pendingStartNode != null && _currentHoverPoint.HasValue)
		{
			GD.Print("Triggered preview spline.");
			var p0 = _pendingStartNode.Position;
			var p3 = new Numerics.Vector3(_currentHoverPoint.Value.X, _currentHoverPoint.Value.Y, _currentHoverPoint.Value.Z);
			var delta = p3 - p0;

			if (delta.LengthSquared() > 0.05f)
			{
				GD.Print("Actually attempted to draw it.");
				var handleLen = delta.Length() * 0.33f;
				var dir = Numerics.Vector3.Normalize(delta);
				var previewCurve = new BezierCurve3D(p0, p0 + dir * handleLen, p3 - dir * handleLen, p3);
				
				DrawSpline(previewCurve, new Color(1f, 1f, 0.2f, 0.7f));
			}
		}
		
		if (_snappedNode != null)
		{
			DrawDiamond(_snappedNode.Position.ToGodot(), 3.5f, new Color(0.2f, 1.0f, 0.3f));
			// Bright green snap indicator
		} else if (_currentHoverPoint.HasValue)
		{
			// Small white dot at cursor ground contact
			DrawCross(_currentHoverPoint.Value, 0.8f, new Color(1f, 1f, 1f, 0.6f));
		}
		
		_immediateMesh.SurfaceEnd();
	}

	private void DrawSpline(BezierCurve3D spline, Color color, int steps = 32)
	{
		var prev = spline.Evaluate(0f).ToGodot();
		for (var i = 1; i <= steps; i++)
		{
			var curr = spline.Evaluate((float)i / steps).ToGodot();
			_immediateMesh.SurfaceSetColor(color);
			_immediateMesh.SurfaceAddVertex(prev);
			_immediateMesh.SurfaceSetColor(color);
			_immediateMesh.SurfaceAddVertex(curr);
			prev = curr;
		}
	}

	private void DrawCross(Vector3 pos, float size, Color color)
	{
		Line(pos + Vector3.Left * size, pos + Vector3.Right * size);
		Line(pos + Vector3.Forward * size, pos + Vector3.Back * size);
		Line(pos + Vector3.Up * size, pos + Vector3.Down * size);
		return;

		void Line(Vector3 from, Vector3 to)
		{
			_immediateMesh.SurfaceSetColor(color);
			_immediateMesh.SurfaceAddVertex(from);
			_immediateMesh.SurfaceSetColor(color);
			_immediateMesh.SurfaceAddVertex(to);
		}
	}

	private void DrawDiamond(Vector3 pos, float size, Color color)
	{
		var top = pos + Vector3.Forward * size;
		var bottom = pos + Vector3.Back * size;
		var left = pos + Vector3.Left * size;
		var right = pos + Vector3.Right * size;

		AddEdge(top, right);
		AddEdge(right, bottom);
		AddEdge(bottom, left);
		AddEdge(left, top);

		return;
		
		void AddEdge(Vector3 a, Vector3 b)
		{
			_immediateMesh.SurfaceSetColor(color);
			_immediateMesh.SurfaceAddVertex(a);
			_immediateMesh.SurfaceSetColor(color);
			_immediateMesh.SurfaceAddVertex(b);
		}
	}
}
