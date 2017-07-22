using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Triangulator
{
    // notes:
	// num tris = numVerts-2
	// only convex verts can be ears
	// only reflex verts can be inside triangle
	// when removing ear: convex verts remain convex; but relfex verts may become convex

	Polygon polygon;
    public LinkedList<Vertex> vertsInClippedPolygon;
    int[] tris;
    int triIndex;

    public Triangulator(Polygon polygon)
    {
        this.polygon = polygon;
        tris = new int[(polygon.numPoints-2+2)*3];
        // create linked list containing all vertexnodes.
        // vertexNode is a class containing vertex + whether or not that vertex is convex

        // TODO: calculate hole insert index
        int holeInsertAfterIndex = 0;

        vertsInClippedPolygon = new LinkedList<Vertex>();
        LinkedListNode<Vertex> previousNode = null;

        for (int i = 0; i < polygon.numHullPoints; i++)
        {

            //int previousVertexIndex = previousNode?.Value.index ?? polygon.numHullPoints -1;
            int previousVertexIndex = (i - 1 + polygon.numHullPoints) % polygon.numHullPoints;
            int nextVertexIndex = (i + 1) % polygon.numHullPoints;

            bool vertexIsConvex = IsCCW(polygon.points[previousVertexIndex], polygon.points[i], polygon.points[nextVertexIndex]);
            Vertex currentHullVertex = new Vertex(polygon.points[i], i, vertexIsConvex);
           
            if (previousNode == null)
            {
                previousNode = vertsInClippedPolygon.AddFirst(currentHullVertex);
            }
            else
            {
                previousNode = vertsInClippedPolygon.AddAfter(previousNode, currentHullVertex);
            }
           // Debug.Log("Add hull vertex: " + previousNode.Value.index);
            // insert hole
            if (i == holeInsertAfterIndex)
            {
                Vertex firstHoleVertex = null;
                for (int j = 0; j < polygon.numHolePoints; j++)
                {
                    previousVertexIndex = previousNode.Value.index;
                    nextVertexIndex = polygon.numHullPoints + (j + 1) % polygon.numHolePoints;
                    vertexIsConvex = IsCCW(polygon.points[previousVertexIndex], polygon.points[polygon.numHullPoints+j], polygon.points[nextVertexIndex]);
                    Vertex holeVertex = new Vertex(polygon.points[j + polygon.numHullPoints], polygon.numHullPoints+j, vertexIsConvex);
                    previousNode = vertsInClippedPolygon.AddAfter(previousNode, holeVertex);
                    //Debug.Log("Add hole vertex: " + j + " (" + previousNode.Value.index + ")  " + previousNode.Value.position);
                    if (j == 0)
                    {
                        firstHoleVertex = holeVertex;
                    }
                }

                Vertex endHoleVertex = new Vertex(firstHoleVertex); // repeat first hole vertex
                previousNode = vertsInClippedPolygon.AddAfter(previousNode, endHoleVertex);
                //Debug.Log("Repeat first hole vertex: " + previousNode.Value.index);
                previousNode = vertsInClippedPolygon.AddAfter(previousNode, new Vertex(currentHullVertex)); // repeat first hull vertex before hole
                //Debug.Log("Repeat first hull vertex: " + previousNode.Value.index);
            }
        }
    }


    public int[] Triangulate()
    {

        while (vertsInClippedPolygon.Count > 3)
        {
            bool hasRemovedEarThisIteration = false;
            LinkedListNode<Vertex> vertexNode = vertsInClippedPolygon.First;
            for (int i = 0; i < vertsInClippedPolygon.Count; i++)
            {
                LinkedListNode<Vertex> prevVertexNode = vertexNode.Previous ?? vertsInClippedPolygon.Last;
                LinkedListNode<Vertex> nextVertexNode = vertexNode.Next ?? vertsInClippedPolygon.First;

                if (vertexNode.Value.isConvex)
                {
                    if (!TriangleContainsVertex(prevVertexNode.Value, vertexNode.Value, nextVertexNode.Value))
					{
                        // check if removal of ear makes prev/next vertex convex (if was previously reflex)
                        if (!prevVertexNode.Value.isConvex)
                        {
                            LinkedListNode<Vertex> prevOfPrev = prevVertexNode.Previous ?? vertsInClippedPolygon.Last;
                            prevVertexNode.Value.isConvex = IsCCW(prevOfPrev.Value.position, prevVertexNode.Value.position, nextVertexNode.Value.position);
                        }
                        if (!nextVertexNode.Value.isConvex)
                        {
                            LinkedListNode<Vertex> nextOfNext = nextVertexNode.Next ?? vertsInClippedPolygon.First;
                            nextVertexNode.Value.isConvex = IsCCW(prevVertexNode.Value.position, nextVertexNode.Value.position, nextOfNext.Value.position);
                        }

                    
                        // add triangle to tri array
                        tris[triIndex * 3] = prevVertexNode.Value.index;
                        tris[triIndex * 3 + 1] = vertexNode.Value.index;
                        tris[triIndex * 3 + 2] = nextVertexNode.Value.index;
                        triIndex++;

						// remove ear
						vertsInClippedPolygon.Remove(vertexNode);
                        hasRemovedEarThisIteration = true;
                        break;
					}
                }
              

                vertexNode = vertexNode.Next;
            }

            if (!hasRemovedEarThisIteration)
            {
                Debug.LogError("Error triangulating mesh. Aborted.");
                return null;
            }
        }

        return tris;
    }

    // check if triangle contains any verts (note, only necessary to check reflex verts).
    bool TriangleContainsVertex(Vertex v0, Vertex v1, Vertex v2)
	{
        LinkedListNode<Vertex> vertexNode = vertsInClippedPolygon.First;
		for (int i = 0; i < vertsInClippedPolygon.Count; i++)
		{
            if (!vertexNode.Value.isConvex) // convex verts will never be inside triangle
            {
                Vertex vertexToCheck = vertexNode.Value;

                if (vertexToCheck.index != v0.index && vertexToCheck.index != v1.index && vertexToCheck.index != v2.index) // dont check verts that make up triangle
                {
                    if (Geometry.PointInTriangle(v0.position, v1.position, v2.position, vertexToCheck.position))
                    {
                        return true;
                    }
                }

            }
            vertexNode = vertexNode.Next;
		}

        return false;
	}

    bool IsCCW(Vector2 v0, Vector2 v1, Vector2 v2)
    {
        //return Vector2.Angle(v2.position - v0.position, v1.position - v0.position) < 180;
        return Geometry.SideOfLine(v0, v2, v1) == -1;
    }


}

public class Vertex
{
	public readonly Vector2 position;
	public readonly int index;
	public bool isConvex;

	public Vertex(Vector2 position, int index, bool isConvex)
	{
		this.position = position;
		this.index = index;
		this.isConvex = isConvex;
	}

	public Vertex(Vertex vertex)
	{
		position = vertex.position;
		index = vertex.index;
		isConvex = vertex.isConvex;
	}
}
