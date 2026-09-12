namespace Traffic.Core.Simulation;

using System;
using System.Collections.Generic;
using Traffic.Core.Kinematics;

public sealed class CircuitSimulation(Route route)
{
    public Route Route { get; } = route;
    public List<Agent> Agents { get; } = [];

    public Agent SpawnAgent(float routeDistance, float initialSpeed, IdmParameters idm)
    {
        var agent = new Agent(routeDistance, initialSpeed, idm);
        Agents.Add(agent);
        return agent;
    }

    public void Step(float dt)
    {
        if (Agents.Count == 0 || Route.TotalLength <= 0f)
            return;
        
        // Sort downstream: largest route distance first
        Agents.Sort((a, b) => b.DistanceAlongSpline.CompareTo(a.DistanceAlongSpline));

        for (var i = 0; i < Agents.Count; i++)
        {
            var current = Agents[i];
            Agent? leader;
            float? netGap;
            float? leaderSpeed;

            if (i > 0)
            {
                leader = Agents[i - 1];
                netGap = leader.DistanceAlongSpline - current.DistanceAlongSpline - leader.Length;
                leaderSpeed = leader.Speed;
            } else if (Agents.Count > 1)
            {
                // Foremost car wraps leader across loop boundary
                leader = Agents[^1];
                netGap = (Route.TotalLength - current.DistanceAlongSpline) + leader.DistanceAlongSpline - leader.Length;
                leaderSpeed = leader.Speed;
            }
            else
            {
                netGap = null;
                leaderSpeed = null;
            }

            current.StepWithExplicitLeader(dt, Route.TotalLength, netGap, leaderSpeed);     // TODO
        }
    }
}