using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Triangulator {

    Polygon polygon;
    LinkedList<Vertex> verticesInReducedPolygon;
    LinkedList<Vertex> convexVerts;
    LinkedList<Vertex> reflexVerts;

    public Triangulator(Polygon polygon)
    {
        this.polygon = polygon;
	
        verticesInReducedPolygon = new LinkedList<Vertex>();
        LinkedListNode<Vertex> previousNode = new LinkedListNode<Vertex>(new Vertex(polygon.points[0],0));
        verticesInReducedPolygon.AddFirst(previousNode);


        for (int i = 1; i < polygon.points.Length; i++)
        {
            previousNode = verticesInReducedPolygon.AddAfter(previousNode, new Vertex(polygon.points[i], i));
        }

        Triangulate();
    }

    public List<Vector2> linePairs = new List<Vector2>();

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

    bool IsEar(Vector2 v1, Vector2 v2, Vector2 v3)
    {
        bool isConvex = Geometry.SideOfLine(v1,v3,v2) == -1;

        if (isConvex)
        {
            LinkedListNode<Vertex> prevNode = verticesInReducedPolygon.First;
            for (int i = 0; i < verticesInReducedPolygon.Count; i++)
            {
                if (prevNode.Value.position != v1 && prevNode.Value.position != v2 && prevNode.Value.position != v3)
                {
                    if (Geometry.PointInTriangle(v1, v2, v3, prevNode.Value.position))
                    {
                        return false;
                    }
                }
                prevNode = prevNode.Next;
            }
        }

        return isConvex;
    }


    struct Vertex
    {
        public readonly Vector2 position;
        public readonly int index;

        public Vertex(Vector2 position, int index)
        {
            this.position = position;
            this.index = index;
        }
    }

}

