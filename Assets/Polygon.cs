using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Polygon {

    public readonly Vertex[] vertices;
    public readonly int numVerts;

    public Polygon(Vertex[] vertices)
    {
        this.vertices = vertices;
        numVerts = vertices.Length;
    }
}

public struct Vertex
{
	public readonly Vector2 position;
	public readonly int index;

	public Vertex(Vector2 position, int index)
	{
		this.position = position;
		this.index = index;
	}
}