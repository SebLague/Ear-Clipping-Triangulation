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

        float holeVertexMaxX = float.MinValue;
        int holeIndexMaxX = 0;
        for (int i = 0; i < polygon.numHolePoints; i++)
        {
            if (polygon.points[i + polygon.numHullPoints].x > holeVertexMaxX)
            {
                holeVertexMaxX = polygon.points[i + polygon.numHullPoints].x;
                holeIndexMaxX = i + polygon.numHullPoints;
            }
        }

        vertsInClippedPolygon = new LinkedList<Vertex>();
        LinkedListNode<Vertex> previousNode = null;
		LinkedListNode<Vertex> nodeOnClosestLineToHoleMaxX = null;
        float minXDstFromHullToHoleMaxX = float.MaxValue;
        List<LinkedListNode<Vertex>> pointsToCheckHole = new List<LinkedListNode<Vertex>>();

        for (int i = 0; i < polygon.numHullPoints; i++)
        {

            //int previousVertexIndex = previousNode?.Value.index ?? polygon.numHullPoints -1;
            Vector2 previousPoint = polygon.points[(i - 1 + polygon.numHullPoints) % polygon.numHullPoints];
            Vector2 nextPoint = polygon.points[(i + 1) % polygon.numHullPoints];
            Vector2 currentPoint = polygon.points[i];

            bool vertexIsConvex = IsCCW(previousPoint, currentPoint, nextPoint);
            Vertex currentHullVertex = new Vertex(currentPoint, i, vertexIsConvex);
           
            if (previousNode == null)
            {
                previousNode = vertsInClippedPolygon.AddFirst(currentHullVertex);
            }
            else
            {
                previousNode = vertsInClippedPolygon.AddAfter(previousNode, currentHullVertex);
            }

            // test if horizontal ray to right of max x hole vertex intersects with line segment formed by previous-current vertex
            if (previousPoint.x > holeVertexMaxX || currentPoint.x > holeVertexMaxX) // at least one point of line seg to right of hole
            {
                if (previousPoint.y > polygon.points[holeIndexMaxX].y != currentPoint.y > polygon.points[holeIndexMaxX].y) // prev/current points lie on either side of hole point
                {
                    
                    float intersectX = currentPoint.x; // true only if line is vertical

                    if (!Mathf.Approximately(currentPoint.x, previousPoint.x)) // if not vertical line, then calculate intersectX
                    {
                        float intersectY = polygon.points[holeIndexMaxX].y;
                        float gradient = (previousPoint.y - currentPoint.y) / (previousPoint.x - currentPoint.x);
                        float c = currentPoint.y - gradient * currentPoint.x;
                        intersectX = (intersectY - c) / gradient;
                    }
                    float dstX = intersectX - holeVertexMaxX;
                    if (dstX < minXDstFromHullToHoleMaxX && dstX > 0) // dont allow points to the left
                    {
                        minXDstFromHullToHoleMaxX = dstX;
                        nodeOnClosestLineToHoleMaxX = (previousPoint.x > currentPoint.x)?previousNode.Previous:previousNode; // chose node on line with max x since other may be behind maxHoleX
                    }
                }
            }

            // point needs to be tested for collision with hole triangle if reflex, and between holeMaxX and closestPointOnHullX, and not on that line
            if (!vertexIsConvex && currentPoint.x <= holeVertexMaxX + minXDstFromHullToHoleMaxX && currentPoint.x >= holeVertexMaxX && (nodeOnClosestLineToHoleMaxX == null || i != nodeOnClosestLineToHoleMaxX.Value.index))
            {
                pointsToCheckHole.Add(previousNode);
            }

        }

        // find best connection point for hole
        Vector2 closestVisibleHolePointOnHull = new Vector2(minXDstFromHullToHoleMaxX + holeVertexMaxX, polygon.points[holeIndexMaxX].y);
        LinkedListNode<Vertex> holeConnectionNode = nodeOnClosestLineToHoleMaxX;

        foreach (LinkedListNode<Vertex> v in pointsToCheckHole)
        {
            
            if (Geometry.PointInTriangle(polygon.points[holeIndexMaxX], closestVisibleHolePointOnHull, nodeOnClosestLineToHoleMaxX.Value.position, v.Value.position))
            {
                // choose point which minimizes deltaAngle from holeMaxXPoint (i.e that with greatest y val). Use min x for tiebreak.
                if (v.Value.position.y > holeConnectionNode.Value.position.y || (Mathf.Approximately(v.Value.position.y, holeConnectionNode.Value.position.y) && v.Value.position.x < holeConnectionNode.Value.position.x))
                {
                    holeConnectionNode = v;
                }
            }
        }

        // insert hole vertices into list
        previousNode = holeConnectionNode;
        for (int i = holeIndexMaxX; i <= polygon.numHolePoints + holeIndexMaxX; i++) // loop through all hole points and back to first one
        {
			int previousVertexIndex = previousNode.Value.index;
            int nextVertexIndex = polygon.numHullPoints + ((i + 1) % polygon.numHolePoints);
            if (i == polygon.numHolePoints + holeIndexMaxX) // repeated first node may have different ccw than actual first
            {
                nextVertexIndex = holeConnectionNode.Value.index;
            }
            int currentVertexIndex = polygon.numHullPoints + (i % polygon.numHolePoints);
			bool vertexIsConvex = IsCCW(polygon.points[previousVertexIndex], polygon.points[currentVertexIndex], polygon.points[nextVertexIndex]);
            Vertex newHoleVert = new Vertex(polygon.points[currentVertexIndex], currentVertexIndex, vertexIsConvex);
            previousNode = vertsInClippedPolygon.AddAfter(previousNode, newHoleVert);
        }

        bool isCCW = IsCCW(previousNode.Value.position, holeConnectionNode.Value.position, previousNode.Next.Value.position);
        Vertex repeatStartHoleHullVert = new Vertex(holeConnectionNode.Value.position, holeConnectionNode.Value.index, isCCW);
        vertsInClippedPolygon.AddAfter(previousNode, repeatStartHoleHullVert); // repeat first hull node before hole (note must have own ccw calc)

    }


    public int[] Triangulate()
    {

        while (vertsInClippedPolygon.Count >= 3)
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
