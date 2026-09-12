



namespace RoadTrafficSim.Visualization;

using Godot;
using Traffic.Core.Graph;
using Traffic.Core.Math;
using Traffic.Core.Kinematics;
using Traffic.Core.Simulation;
using RoadTrafficSim.Utils;
using System.Collections.Generic;

[GlobalClass]
public partial class RoadGraphVisualizer : MeshInstance3D
{
	private ImmediateMesh _immediateMesh = null!;
	private StandardMaterial3D _lineMaterial = null!;
	private RoadGraph _graph = new();
	private BezierCurve3D _testSpline;
	private readonly List<Agent> _agents = new();

	public override void _Ready()
	{
		_immediateMesh = new ImmediateMesh();
		Mesh = _immediateMesh;

		_lineMaterial = new StandardMaterial3D
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			VertexColorUseAsAlbedo = true,
			CullMode = BaseMaterial3D.CullModeEnum.Disabled,
		};
		
		MaterialOverride = _lineMaterial;

		_testSpline = new BezierCurve3D(
			new System.Numerics.Vector3(-60, 0, -20),
			new System.Numerics.Vector3(-20, 0, 50),
			new System.Numerics.Vector3(20, 0, -50),
			new System.Numerics.Vector3(60, 0, 20)
		);
		
		_agents.Add(new Agent(40f, 6.0f, new IdmParameters(desiredSpeed: 7f))); //Leader
		_agents.Add(new Agent(20f, 12.0f, new IdmParameters(desiredSpeed: 15f))); // Follower
		_agents.Add(new Agent(0f, 15.0f, new IdmParameters(desiredSpeed: 18f))); // Follower
	}

	public override void _Process(double delta)
	{
		var dt = (float)delta;
		
		_agents.Sort((a, b) => b.DistanceAlongSpline.CompareTo(a.DistanceAlongSpline));
		
		for (var i = 0; i < _agents.Count; i++)
		{
			var leader = (i > 0) ? _agents[i - 1] : null;
			_agents[i].Step(dt, _testSpline, leader);
		}
		
		Redraw();
	}

	private void BuildSampleGraph()
	{
		var n0 = _graph.CreateNode(new System.Numerics.Vector3(-40f, 0f, 0f));
		var n1 = _graph.CreateNode(new System.Numerics.Vector3(0f, 5f, 40f));
		var n2 = _graph.CreateNode(new System.Numerics.Vector3(50f, 0f, 0f));
		
		// Connect n0 -> n1 with elevation change
		_graph.CreateSegment(
			n0.Id,
			n1.Id,
			new System.Numerics.Vector3(20f, 0f, 0f),
			new System.Numerics.Vector3(0f, 0f, -20f)
		);

		_graph.CreateSegment(
			n1.Id,
			n2.Id,
			new System.Numerics.Vector3(0f, 0f, -20f),
			new System.Numerics.Vector3(-20f, 0f, 0f)
		);
	}

	public void Redraw()
	{
		_immediateMesh.ClearSurfaces();
		_immediateMesh.SurfaceBegin(Mesh.PrimitiveType.Lines);

		DrawSpline(_testSpline, new Color(0.2f, 0.8f, 1.0f));
		
		DrawSpline(_testSpline.GenerateOffsetCurve(-1.75f), new Color(0.1f, 0.4f, 0.5f));
		DrawSpline(_testSpline.GenerateOffsetCurve(1.75f), new Color(0.1f, 0.4f, 0.5f));

		foreach (var agent in _agents)
		{
			DrawVehicle(agent);
		}
		
		_immediateMesh.SurfaceEnd();
	}

	private void DrawSpline(BezierCurve3D spline, Color color, int steps = 50)
	{
		var prev = spline.Evaluate(0f).ToGodot();
		for (var i = 1; i <= steps; i++)
		{
			var t = (float)i / steps;
			var curr = spline.Evaluate(t).ToGodot();
			
			_immediateMesh.SurfaceSetColor(color);
			_immediateMesh.SurfaceAddVertex(prev);
			
			_immediateMesh.SurfaceSetColor(color);
			_immediateMesh.SurfaceAddVertex(curr);

			prev = curr;
		}
	}

	private void DrawVehicle(Agent agent)
	{
		var center = _testSpline.EvaluateAtDistance(agent.DistanceAlongSpline).ToGodot();
		var forward = _testSpline.EvaluateTangent(agent.DistanceAlongSpline).ToGodot();
		var right = forward.Cross(Vector3.Up).Normalized();

		var halfLen = agent.Length * 0.5f;
		var halfWid = 1f;
		
		var frontRight = center + forward * halfLen + right * halfWid;
		var frontLeft = center + forward * halfLen - right * halfWid;
		var backRight = center - forward * halfLen + right * halfWid;
		var backLeft = center - forward * halfLen - right * halfWid;

		Color carColor = new(1.0f, 0.9f, 0.2f); // Yellow vehicle boxes

		void AddEdge(Vector3 a, Vector3 b)
		{
			_immediateMesh.SurfaceSetColor(carColor);
			_immediateMesh.SurfaceAddVertex(a);
			_immediateMesh.SurfaceSetColor(carColor);
			_immediateMesh.SurfaceAddVertex(b);
		}
		
		AddEdge(frontRight, frontLeft);
		AddEdge(frontLeft, backLeft);
		AddEdge(backLeft, backRight);
		AddEdge(backRight, frontRight);
	}

	private void DrawHandles(RoadSegment segment)
	{
		var orange = new Color(1.0f, 0.5f, 0.2f, 0.5f);

		var p0 = segment.Centerline.P0.ToGodot();
		var p1 = segment.Centerline.P1.ToGodot();
		var p2 = segment.Centerline.P2.ToGodot();
		var p3 = segment.Centerline.P3.ToGodot();
		
		_immediateMesh.SurfaceSetColor(orange);
		_immediateMesh.SurfaceAddVertex(p0);
		_immediateMesh.SurfaceSetColor(orange);
		_immediateMesh.SurfaceAddVertex(p1);
		
		_immediateMesh.SurfaceSetColor(orange);
		_immediateMesh.SurfaceAddVertex(p2);
		_immediateMesh.SurfaceSetColor(orange);
		_immediateMesh.SurfaceAddVertex(p3);
	}

	private void DrawNodeCross(Vector3 position, float size)
	{
		_immediateMesh.SurfaceSetColor(new Color(1f, 0.2f, 0.2f)); // Red node markers
		
		_immediateMesh.SurfaceAddVertex(position + Vector3.Left * size);
		_immediateMesh.SurfaceAddVertex(position + Vector3.Right * size);
		
		_immediateMesh.SurfaceAddVertex(position + Vector3.Up * size);
		_immediateMesh.SurfaceAddVertex(position + Vector3.Down * size);
		
		_immediateMesh.SurfaceAddVertex(position + Vector3.Back * size);
		_immediateMesh.SurfaceAddVertex(position + Vector3.Forward * size);
	}
}
