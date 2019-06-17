using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node {

    public Vector3 position;
    public Node parent;
    public float g = float.MaxValue; // Cost of getting from the start node to this node
    public float f = float.MaxValue; // Cost of getting from the start node to the goal by passing by this node

    public Node(Vector3 position, Node parent)
    {
        this.position = position;
        this.parent = parent;
    }

    public static Node nodeWithLowestf(List<Node> set)
    {
        float lowestF = float.MaxValue;
        // Find lowest f in the set
        foreach (Node n in set)
            if (n.f < lowestF)
                lowestF = n.f;
        // Return the node with that value of f
        return set.Find(n => n.f == lowestF);
    }

}
