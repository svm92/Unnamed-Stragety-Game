using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathFinding {

    public static int distance;

    // Implementation of the A* search algorithm
    // It returns the distance from start to goal, taking obstacles into account
    public static IEnumerator findDistance(Vector3 start, Vector3 goal, Cursor cursor)
    {
        yield return null; // Wait a frame to keep the calculation from freezing the game's animations

        // Positions already evaluated
        List<Node> closedSet = new List<Node>();

        // Discovered positions (not yet evaluated)
        List<Node> openSet = new List<Node>();
        Node firstNode = new Node(start, null); // The first node has no parent
        firstNode.g = 0;
        firstNode.f = Vector3.Distance(start, goal);
        openSet.Add(firstNode);

        while (openSet.Count != 0) // While open set is not empty
        {
            Node current = Node.nodeWithLowestf(openSet);
            if (current.position == goal) // If the goal is reached
            {
                distance = reconstructPath(current);
                yield break;
            }

            openSet.Remove(current);
            closedSet.Add(current);

            Vector3[] neighboringPositions = new Vector3[] { Vector3.up, Vector3.down, Vector3.left, Vector3.right };
            foreach (Vector3 n in neighboringPositions)
            {
                Node neighbor = new Node(current.position + n, current);
                if (cursor.tileIsImpassable( (int)neighbor.position.x, (int)neighbor.position.y ))
                    continue; // Ignore impassable tiles
                
                if (setContains(closedSet, neighbor))
                    continue; // Ignore already evaluated neighbors

                if (!setContains(openSet, neighbor)) // New node discovered
                    openSet.Add(neighbor);

                float tentativeG = current.g + 1;
                if (tentativeG >= neighbor.g)
                    continue; // Not a better path

                // Otherwise this is the best path until now, so record it
                neighbor.parent = current;
                neighbor.g = tentativeG;
                neighbor.f = neighbor.g + Vector3.Distance(neighbor.position, goal);
            }
        }

        // If the code reaches this point, the goal cannot be reached
        distance = int.MaxValue;
    }

    static bool setContains(List<Node> set, Node nodeToLookFor)
    {
        foreach (Node n in set)
            if (n.position == nodeToLookFor.position)
                return true;
        return false;
    }

    static int reconstructPath(Node current)
    {
        int pathLength = 0;
        while (current.parent != null)
        {
            pathLength++;
            current = current.parent;
        }
        return pathLength;
    }

}
