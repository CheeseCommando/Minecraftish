using System.Collections.Generic;
using UnityEngine;

public class PathFinder : MonoBehaviour
{
	public static void PathTo(Entity entity, Vector3 destination)
	{
		new PathFinderWorker(entity, entity.position + new Vector3(1f, 0, 1f), destination);
	}
}
public class PathFinderWorker
{
	public const int MOVE_DIAGONAL_COST = 14;
	public const int MOVE_STRAIGHT_COST = 10;
	private readonly List<PathNode> blocked = new List<PathNode>();
	private readonly List<PathNode> toSearch = new List<PathNode>();
	const int maxNodes = 400;
	private readonly Entity entity1;
	public PathFinderWorker(Entity entity, Vector3 from, Vector3 to)
	{
		entity1 = entity;
		entity.path = Path(Vector3Int.FloorToInt(from), Vector3Int.FloorToInt(to));
	}

	int CalculateDistanceCost(Vector3Int pos1, Vector3Int pos2)
	{
		int xDist = Mathf.Abs(pos1.x - pos2.x);
		int yDist = Mathf.Abs(pos1.y - pos2.y);
		int zDist = Mathf.Abs(pos1.z - pos2.z);
		int remaining = Mathf.Abs(xDist - yDist - zDist);
		return MOVE_DIAGONAL_COST * Mathf.Min(xDist, yDist, zDist) + MOVE_STRAIGHT_COST * remaining;
	}
	private PathNode[] GetNeighbours(PathNode node)
	{
		List<PathNode> neighbours = new List<PathNode>();
		for (int p = 0; p < 6; p++)
		{
			BlockState neighbour = node.block.GetChunk().data.GetBlock(node.block.position + VoxelData.faceChecks[p]);
			if (isWalkable(neighbour))
			{
				PathNode node1 = new PathNode(neighbour)
				{
					x = neighbour.position.x,
					y = neighbour.position.y,
					z = neighbour.position.z
				};
				neighbours.Add(node1);
			}
		}
		return neighbours.ToArray();
	}
	private bool isWalkable(BlockState state)
	{
		if (state == null)
		{
			return false;
		}

		if (state.properties.isSolid)
		{
			return false;
		}

		BlockState b = state.GetChunk().data.GetBlock(state.position + Vector3Int.up);
		if (b != null && b.properties.isSolid)
		{
			return false;
		}

		bool walkable = false;
		for (int y = -1; y > -3; y--)
		{
			BlockState a = state.GetChunk().data.GetBlock(state.position + new Vector3Int(0, y, 0));
			if (a != null && a.properties.isSolid)
			{
				walkable = true;
			}
			if (a == null || a.properties.damage > 0)
			{
				walkable = false;
			}
		}

		return walkable;
	}
	private Vector3[] Path(Vector3Int from, Vector3Int to)
	{
		BlockState start = World.instance.GetBlockState(from);
		BlockState end = World.instance.GetBlockState(to);
		if (start != null && end != null)
		{
			PathNode startNode = new PathNode(start, start.position);
			PathNode endNode = new PathNode(end, end.position);
			toSearch.Add(startNode);

			startNode.gCost = 0;
			startNode.hCost = CalculateDistanceCost(from, to);
			startNode.CalculateFCost();

			int searchedNodes = 0;

			PathNode current = null;

			while (searchedNodes < maxNodes && toSearch.Count > 0)
			{
				searchedNodes++;

				current = LowestFNode(toSearch, to);

				if (blocked.Contains(current) || current.closed)
				{
					toSearch.Remove(current);
					continue;
				}

				if (current.block == endNode.block)
				{
					return Reverse(current);
				}

				toSearch.Remove(current);
				blocked.Add(current);

				PathNode[] neigh = GetNeighbours(current);

				for (int i = 0; i < neigh.Length; i++)
				{

					PathNode n = neigh[i];

					if (blocked.Contains(n))
					{
						continue;
					}

					int tentativeCost = current.gCost + CalculateDistanceCost(current.block.globalPosition, n.block.globalPosition);

					if (tentativeCost < n.gCost)
					{
						n.comeFrom = current;
						n.gCost = tentativeCost;
						n.hCost = CalculateDistanceCost(n.block.globalPosition, to);
						n.CalculateFCost();
					}
					if (!toSearch.Contains(n))
					{
						toSearch.Add(n);
					}
				}

			}
		}
		return null;
	}
	PathNode LowestFNode(List<PathNode> array, Vector3 to)
	{
		PathNode lowest = array[0];
		for (int i = 0; i < array.Count; i++)
		{
			if (Vector3.Distance(array[i].block.globalPosition, to) < Vector3.Distance(lowest.block.globalPosition, to))
			{
				lowest = array[i];
			}
		}
		return lowest;
	}
	Vector3[] Reverse(PathNode node)
	{
		List<Vector3> path = new List<Vector3>();
		int searchs = 0;
		while (node.comeFrom != null && searchs < 1500)
		{
			searchs++;
			path.Add(node.block.globalPosition - new Vector3(.5f, 0, .5f));
			node = node.comeFrom;
		}
		return path.ToArray();
	}

}
public class PathNode
{
	public int x, y, z;
	public int gCost, fCost, hCost;
	public PathNode comeFrom;
	public BlockState block;
	public bool closed, closed2;
	public PathNode(BlockState blockState)
	{
		gCost = int.MaxValue;
		block = blockState;
		CalculateFCost();
	}
	public PathNode(BlockState blockState, Vector3Int position)
	{
		x = position.x;
		y = position.y;
		z = position.z;
		gCost = int.MaxValue;
		block = blockState;
		CalculateFCost();
	}

	public void CalculateFCost()
	{
		fCost = gCost + hCost;
	}
}
public class VoxelPath
{
	public Vector3[] path;
}
