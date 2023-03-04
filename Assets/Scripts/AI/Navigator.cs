using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Navigator : MonoBehaviour
{
	public float distanceToTargetThreshold = 0.5f;

	private Dictionary<Waypoint, List<Waypoint>> graph;
	private Waypoint currentWaypoint;
	private List<Waypoint> pathToTarget;

	[SerializeField] private NPCController agent;

	// Use this for initialization
	void Start()
	{
		graph = new Dictionary<Waypoint, List<Waypoint>>();
		Waypoint[] waypoints = GetComponentsInChildren<Waypoint>();

		// Constructing the graph
		// do this during compile? 
		foreach (Waypoint waypoint in waypoints)
		{
			List<Waypoint> edges = new List<Waypoint>();
			foreach (Waypoint edge in waypoint.Neighbors)
			{
				edges.Add(edge);
			}
			graph.Add(waypoint, edges);
		}

		// Find nearest waypoint to start with
		float minDistance = Mathf.Infinity;
		foreach (Waypoint waypoint in waypoints)
		{
			float distance = Vector3.Distance(transform.position, waypoint.transform.position);
			if (distance < minDistance)
			{
				minDistance = distance;
				currentWaypoint = waypoint;
			}
		}
		currentWaypoint.ChangeGizmoColor(Color.blue);

		// Select random node as target and build path towards it
		Waypoint target = null;
		while (!target || target == currentWaypoint)
			target = waypoints[Random.Range(0, waypoints.Length)];
		target.ChangeGizmoColor(Color.red);
		pathToTarget = getPath(graph, currentWaypoint, target);
	}

	// Update is called once per frame
	void Update()
	{
		float distanceToTarget = Vector3.Distance(transform.position, currentWaypoint.transform.position);

		if (distanceToTarget < distanceToTargetThreshold)
		{ // If the waypoint has been reached
		  // Find next waypoint
			if (pathToTarget.Count == 0)
			{
				// Reached target
				currentWaypoint.GetComponent<MeshRenderer>().material.color = Color.black;
				Waypoint target = null;
				while (!target || target == currentWaypoint) // Choose a new target, different from the current waypoint
					target = new List<Waypoint>(graph.Keys)[Random.Range(0, graph.Count)];
				target.ChangeGizmoColor(Color.red);
				pathToTarget = getPath(graph, currentWaypoint, target);
			}

			// Select next waypoint
			currentWaypoint.ChangeGizmoColor(Color.white);
			currentWaypoint = pathToTarget[0];
			pathToTarget.RemoveAt(0);
			if (pathToTarget.Count > 0)//currentWaypoint != pathToTarget[pathToTarget.Count - 1])
				currentWaypoint.ChangeGizmoColor(Color.blue);
		}

		Vector3 direction = currentWaypoint.transform.position - transform.position;
		agent.Move(direction.normalized);
	}

	// Calculates the path towards the goal waypoints, using A* search.
	List<Waypoint> getPath(Dictionary<Waypoint, List<Waypoint>> graph, Waypoint start, Waypoint goal)
	{
		SortedList<float, Waypoint> frontier = new SortedList<float, Waypoint>();
		Dictionary<Waypoint, Waypoint> visitedFrom = new Dictionary<Waypoint, Waypoint>();
		Dictionary<Waypoint, float> g = new Dictionary<Waypoint, float>(); // costsFromStart

		visitedFrom.Add(start, null);
		g.Add(start, 0);
		frontier.Add(Vector3.Distance(start.transform.position, goal.transform.position), start);

		while (frontier.Count > 0)
		{
			Waypoint current = frontier.Values[0];
			frontier.RemoveAt(0);

			if (current == goal)
				break;

			foreach (Waypoint next in graph[current])
			{
				float newG = g[current] + Vector3.Distance(next.transform.position, current.transform.position);
				if (!g.ContainsKey(next) || newG < g[next])
				{
					if (frontier.ContainsValue(next))
					{
						frontier.RemoveAt(frontier.IndexOfValue(next));
					}
					frontier.Add(newG + Vector3.Distance(next.transform.position, goal.transform.position), next);

					if (visitedFrom.ContainsKey(next))
					{
						visitedFrom.Remove(next);
					}
					visitedFrom.Add(next, current);

					if (g.ContainsKey(next))
						g.Remove(next);
					g.Add(next, newG);
				}
			}
		}

		// Return the path to the goal
		List<Waypoint> path = new List<Waypoint>();
		Waypoint w = goal;
		path.Add(goal);
		while (visitedFrom[w] != null)
		{
			path.Add(visitedFrom[w]);
			w = visitedFrom[w];
		}
		path.Reverse();
		return path;
	}
}
