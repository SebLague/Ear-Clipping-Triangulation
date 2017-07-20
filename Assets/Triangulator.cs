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
    LinkedList<VertexNode> vertsInClippedPolygon;
    int[] tris;
    int triIndex;

    public Triangulator(Polygon polygon)
    {
        this.polygon = polygon;
        tris = new int[(polygon.numVerts-2)*3];
        // create linked list containing all vertexnodes.
        // vertexNode is a class containing vertex + whether or not that vertex is convex

        vertsInClippedPolygon = new LinkedList<VertexNode>();
        LinkedListNode<VertexNode> prevLinkedListNode = null;

        for (int i = 0; i < polygon.numVerts; i++)
        {
            Vertex currentVertex = polygon.vertices[i];
            Vertex previousVertex = polygon.vertices[(i - 1+polygon.numVerts) % polygon.numVerts];
            Vertex nextVertex = polygon.vertices[(i + 1) % polygon.numVerts];

            bool vertexIsConvex = IsCCW(previousVertex, currentVertex, nextVertex);
            VertexNode vertexNode = new VertexNode(currentVertex, vertexIsConvex);

            if (prevLinkedListNode == null)
            {
                prevLinkedListNode = vertsInClippedPolygon.AddFirst(vertexNode);
            }
            else
            {
                prevLinkedListNode = vertsInClippedPolygon.AddAfter(prevLinkedListNode, vertexNode);
            }
        }

    }


    public int[] Triangulate()
    {

        while (vertsInClippedPolygon.Count > 3)
        {
            LinkedListNode<VertexNode> vertexNode = vertsInClippedPolygon.First;
            for (int i = 0; i < vertsInClippedPolygon.Count; i++)
            {
                LinkedListNode<VertexNode> prevVertexNode = vertexNode.Previous ?? vertsInClippedPolygon.Last;
                LinkedListNode<VertexNode> nextVertexNode = vertexNode.Next ?? vertsInClippedPolygon.First;

                if (vertexNode.Value.isConvex)
                {
                    if (!TriangleContainsVertex(prevVertexNode.Value.vertex, vertexNode.Value.vertex, nextVertexNode.Value.vertex))
					{
                        // check if removal of ear makes prev/next vertex convex (if was previously reflex)
                        if (!prevVertexNode.Value.isConvex)
                        {
                            LinkedListNode<VertexNode> prevOfPrev = prevVertexNode.Previous ?? vertsInClippedPolygon.Last;
                            prevVertexNode.Value.isConvex = IsCCW(prevOfPrev.Value.vertex, prevVertexNode.Value.vertex, nextVertexNode.Value.vertex);
                        }
                        if (!nextVertexNode.Value.isConvex)
                        {
                            LinkedListNode<VertexNode> nextOfNext = nextVertexNode.Next ?? vertsInClippedPolygon.First;
                            nextVertexNode.Value.isConvex = IsCCW(prevVertexNode.Value.vertex, nextVertexNode.Value.vertex, nextOfNext.Value.vertex);
                        }

                    
                        // add triangle to tri array
                        tris[triIndex * 3] = prevVertexNode.Value.vertex.index;
                        tris[triIndex * 3 + 1] = vertexNode.Value.vertex.index;
                        tris[triIndex * 3 + 2] = nextVertexNode.Value.vertex.index;
                        triIndex++;

						// remove ear
						vertsInClippedPolygon.Remove(vertexNode);
                        break;
					}
                }
              

                vertexNode = vertexNode.Next;
            }
        }

        return tris;
    }

    // check if triangle contains any verts (note, only necessary to check reflex verts).
    bool TriangleContainsVertex(Vertex v0, Vertex v1, Vertex v2)
	{
        LinkedListNode<VertexNode> vertexNode = vertsInClippedPolygon.First;
		for (int i = 0; i < vertsInClippedPolygon.Count; i++)
		{
            if (!vertexNode.Value.isConvex) // convex verts will never be inside triangle
            {
                Vertex vertexToCheck = vertexNode.Value.vertex;

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

    bool IsCCW(Vertex v0, Vertex v1, Vertex v2)
    {
        return Geometry.SideOfLine(v0.position, v2.position, v1.position) == -1;
    }

    /*


   public void Triangulate()
   {


	   while (verticesInReducedPolygon.Count > 3)
	   {

		   LinkedListNode<Vertex> startNode = verticesInReducedPolygon.First;


		   for (int i = 1; i < verticesInReducedPolygon.Count; i++)
		   {
			   LinkedListNode<Vertex> midNode = startNode.Next ?? verticesInReducedPolygon.First;
			   LinkedListNode<Vertex>  endNode = midNode.Next ?? verticesInReducedPolygon.First;

			   if (IsEar(startNode.Value.position, midNode.Value.position, endNode.Value.position))
			   {
				   linePairs.Add(startNode.Value.position);
				   linePairs.Add(endNode.Value.position);
				   verticesInReducedPolygon.Remove(midNode);
				   break;
			   }

			   startNode = startNode.Next;
		   }

	   }


   }



  
*/

    public class VertexNode
    {
        public readonly Vertex vertex;
        public bool isConvex;

        public VertexNode(Vertex vertex, bool isConvex)
        {
            this.vertex = vertex;
            this.isConvex = isConvex;
        }
    }

}

