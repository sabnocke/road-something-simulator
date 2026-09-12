//TODO add all colors into a common class
//TODO maybe move common functions into a class and inherit from it

using Traffic.Core.Kinematics;
using Traffic.Core.Simulation;

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
	[Export] public float RoadHalfWidth { get; set; } = 3.5f;
	
	public RoadGraph Graph { get; } = new();
	public bool IsBuildModeActive { get; set; } = true;
	
#nullable enable
	private RoadNode? _snappedNode;
	private RoadNode? _pendingStartNode;
	private Vector3? _currentHoverPoint;
	private Numerics.Vector3? _lastExitDirection;
	private CircuitSimulation? _sim;
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
			_lastExitDirection = null;
			GD.Print($"Started road chain at Node {_pendingStartNode.Id}");
		}
		else
		{
			// Reject clicking the exact same node twice without moving
			if (_snappedNode != null && _snappedNode.Id == _pendingStartNode.Id) 
			{
				GD.Print("Ignored click: snapped to current start node.");
				return;
			}
			
			var endNode = _snappedNode ?? Graph.CreateNode(currentSysPos);
			var closedLoop = _snappedNode != null;

			var chord = endNode.Position - _pendingStartNode.Position;
			var chordLength = chord.Length();
			if (chordLength < 0.1f)
			{
				GD.PrintErr($"Segment rejected: chord length too short ({chordLength:F3}m).");
				return;
			}

			var handleLength = chordLength * 0.4f;

			// Start handle: inherit from previous exit vector if chaining, otherwise follow chord
			var startDir = _lastExitDirection ?? Numerics.Vector3.Normalize(chord);
			var startHandle = startDir * handleLength;
			
			// End handle: points back along the approach vector
			Numerics.Vector3 endHandle;
			if (closedLoop)
			{
				// Align with the existing road leaving this node: P2 must point opposite to P1 of the first segment
				var targetOutgoing = Graph.GetNodeOutgoingTangent(endNode.Id);
				var approachDir = targetOutgoing ?? Numerics.Vector3.Normalize(chord);
				endHandle = -approachDir * handleLength;
			}
			else
			{
				var approachDir = Numerics.Vector3.Normalize(chord);
				endHandle = -approachDir * handleLength;
				_lastExitDirection = approachDir;
			}

			var segment = Graph.CreateSegment(_pendingStartNode.Id, endNode.Id, startHandle, endHandle);
			GD.Print($"Created segment {segment.Id}: {_pendingStartNode.Id} -> {endNode.Id}. Total segments: {Graph.Segments.Count}");
			
			// _pendingStartNode = closedLoop ? null : endNode;
			_snappedNode = null;
			if (closedLoop)
			{
				_lastExitDirection = null;
				_pendingStartNode = null;
				TryStartCircuitSimulation();
			}
			else
			{
				_pendingStartNode = endNode;
			}
			
			
		}
		
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
		_lastExitDirection = null;
		
		// Create a clean graph
		var newGraph = new RoadGraph();
		//TODO add Clear() method to RoadGraph
		_immediateMesh.ClearSurfaces();
	}

	private void TriggerAgentRender()
	{
		if (_sim == null) 
			return;
		
		foreach (var agent in _sim.Agents)
		{
			var (seg, localDist) = _sim.Route.ResolvePosition(agent.DistanceAlongSpline);
			var center = seg.Centerline.EvaluateAtDistance(localDist).ToGodot();
			var forward = seg.Centerline.EvaluateTangentAtDistance(localDist).ToGodot();
			var right = forward.Cross(Vector3.Up).Normalized();

			var halfLen = agent.Length * 0.5f;
			var halfWid = 1.0f;
			
			var p1 = center + forward * halfLen + right * halfWid;
			var p2 = center + forward * halfLen - right * halfWid;
			var p3 = center  - forward * halfLen - right * halfWid;
			var p4 = center - forward * halfLen - right * halfWid;

			DrawLine(p1, p2);
			DrawLine(p2, p3);
			DrawLine(p3, p4);
			DrawLine(p4, p1);

			return;

			void DrawLine(Vector3 start, Vector3 end)
			{
				SurfaceSetColorVertex(Utils.Colors.Yellow(), start);
				SurfaceSetColorVertex(Utils.Colors.Yellow(), end);
			}
		}
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
		{
			DrawRoadWithBorders(
				segment.Centerline,
				centerColor:  Utils.Colors.Cyan(0.5f), // Dim cyan center
				borderColor: Utils.Colors.White() // White road borders 
			);
		}
		
		// Draw confirmed nodes
		foreach (var node in Graph.Nodes)
			DrawCross(node.Position.ToGodot(), 1.5f, Utils.Colors.Red());

		
		
		// Draw live preview spline when dragging
		if (_pendingStartNode != null && _currentHoverPoint.HasValue)
		{
			// GD.Print("Triggered preview spline.");
			var p0 = _pendingStartNode.Position;
			var p3 = new Numerics.Vector3(_currentHoverPoint.Value.X, _currentHoverPoint.Value.Y, _currentHoverPoint.Value.Z);
			var chord = p3 - p0;

			if (chord.LengthSquared() > 0.05f)
			{
				// GD.Print("Actually attempted to draw it.");
				var handleLen = chord.Length() * 0.4f;
				var startDir = _lastExitDirection ?? Numerics.Vector3.Normalize(chord);
				var approachDir = Numerics.Vector3.Normalize(chord);

				var previewCurve = new BezierCurve3D(
					p0,
					p0 + startDir * handleLen,
					p3 - approachDir * handleLen,
					p3
				);
				
				DrawRoadWithBorders(
					previewCurve,
					centerColor: Utils.Colors.Yellow(0.4f), // new Color(1.0f, 1.0f, 0.2f, 0.4f),
					borderColor: Utils.Colors.Yellow(0.9f)
				);
			}
		}
		
		if (_snappedNode != null)
		{
			DrawDiamond(_snappedNode.Position.ToGodot(), 3.5f, Utils.Colors.RadiantGreen());
			// Bright green snap indicator
		} else if (_currentHoverPoint.HasValue)
		{
			// Small white dot at cursor ground contact
			DrawCross(_currentHoverPoint.Value, 0.8f, Utils.Colors.White(0.6f));
		}
		
		TriggerAgentRender();
		
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
			SurfaceSetColorVertex(color, from);
			SurfaceSetColorVertex(color, to);
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
			SurfaceSetColorVertex(color, a);
			SurfaceSetColorVertex(color, b);
		}
	}

	private void SurfaceSetColorVertex(Color color, Vector3 vertex)
	{
		_immediateMesh.SurfaceSetColor(color);
		_immediateMesh.SurfaceAddVertex(vertex);
	}
	
	private void DrawRoadWithBorders(BezierCurve3D spline, Color centerColor, Color borderColor, int steps = 40)
	{
		var stepSize = 1.0f / steps;
		
		// Evaluate initial step
		var c0 = spline.Evaluate(0f).ToGodot();
		var t0 = spline.EvaluateTangent(0f).ToGodot();
		var n0 = t0.Cross(Vector3.Up).Normalized();

		var left0 = c0 - n0 * RoadHalfWidth;
		var right0 = c0 + n0 * RoadHalfWidth;

		for (var i = 1; i <= steps; i++)
		{
			var t = i * stepSize;
			var c1 = spline.Evaluate(t).ToGodot();
			var t1 = spline.EvaluateTangent(t).ToGodot();
			var n1 = t1.Cross(Vector3.Up).Normalized();
			
			var left1 = c1 - n1 * RoadHalfWidth;
			var right1 = c1 + n1 * RoadHalfWidth;
			
			// Center dashed/solid line
			SurfaceSetColorVertex(centerColor, c0);
			SurfaceSetColorVertex(centerColor, c1);
			
			// Left curb border
			SurfaceSetColorVertex(borderColor, left0);
			SurfaceSetColorVertex(borderColor, left1);
			
			// Right curb border
			SurfaceSetColorVertex(borderColor, right0);
			SurfaceSetColorVertex(borderColor, right1);

			c0 = c1;
			left0 = left1;
			right0 = right1;
		}
	}

	public void TryStartCircuitSimulation()
	{
		var route = Graph.ExtractClosedCircuit();
		if (route == null)
		{
			GD.Print("Couldn't find a valid closed loop.");
			GD.Print($"Segments: {Graph.Segments.Count}; Nodes: {Graph.Nodes.Count}");
			foreach (var seg in Graph.Segments)
			{
				GD.Print($"  Seg {seg.Id}: {seg.StartNodeId} -> {seg.EndNodeId}");
			}
			return;
		}

		_sim = new CircuitSimulation(route);

		_sim.SpawnAgent(route.TotalLength * 0.75f, 8f, new IdmParameters(desiredSpeed: 10f));
		_sim.SpawnAgent(route.TotalLength * 0.5f, 14f, new IdmParameters(desiredSpeed: 18f));
		_sim.SpawnAgent(route.TotalLength * 0.25f, 14f, new IdmParameters(desiredSpeed: 16f));
		_sim.SpawnAgent(0, 12f, new IdmParameters(desiredSpeed: 15f));
		
		GD.Print($"Circuit simulation started! Track length: {route.TotalLength:F1}m, Agents: {_sim.Agents.Count}");
	}

	public override void _Process(double delta)
	{
		if (_sim == null) return;
		_sim.Step((float)delta);
		Redraw();
	}
}
