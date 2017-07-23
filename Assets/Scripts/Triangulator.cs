using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Triangulator
{
    public LinkedList<Vertex> vertsInClippedPolygon;
    int[] tris;
    int triIndex;

    LinkedList<Vertex> GenerateList(Polygon polygon)
    {
        LinkedList<Vertex> vertexList = new LinkedList<Vertex>();
        LinkedListNode<Vertex> currentNode = null;
	
        // Add all hull points to linkedlist<vertex>
		for (int i = 0; i < polygon.numHullPoints; i++)
		{
			int prevPointIndex = (i - 1 + polygon.numHullPoints) % polygon.numHullPoints;
			int nextPointIndex = (i + 1) % polygon.numHullPoints;

            bool vertexIsConvex = IsCCW(polygon.points[prevPointIndex], polygon.points[i], polygon.points[nextPointIndex]);
            Vertex currentHullVertex = new Vertex(polygon.points[i], i, vertexIsConvex);

			if (currentNode == null)
				currentNode = vertexList.AddFirst(currentHullVertex);
			else
				currentNode = vertexList.AddAfter(currentNode, currentHullVertex);
        }

		List<HoleData> sortedHoleData = new List<HoleData>(); // holds hole data; holes sorted by furthest vertex to the right (furthest first).

		// Loop through all holes
		for (int holeIndex = 0; holeIndex < polygon.numHoles; holeIndex++)
		{
			// Find index of rightmost point in hole. This 'bridge' point is where the hole will be connected to the hull.
			Vector2 holeBridgePoint = new Vector2(float.MinValue, 0);
			int holeBridgeIndex = 0;
			for (int i = 0; i < polygon.numPointsPerHole[holeIndex]; i++)
			{
				if (polygon.GetHolePoint(i, holeIndex).x > holeBridgePoint.x)
				{
					holeBridgePoint = polygon.GetHolePoint(i, holeIndex);
					holeBridgeIndex = i;

				}
			}
			sortedHoleData.Add(new HoleData(holeIndex, holeBridgeIndex, holeBridgePoint));
		}
		sortedHoleData.Sort((x, y) => (x.bridgePoint.x > y.bridgePoint.x) ? -1 : 1);

		foreach (HoleData holeData in sortedHoleData) {
            // Find first edge which intersects with ray from hole bridge point, pointing rightwards.
            Vector2 rayIntersectPoint = new Vector2(float.MaxValue, holeData.bridgePoint.y);
            List<LinkedListNode<Vertex>> hullNodesPotentiallyInBridgeTriangle = new List<LinkedListNode<Vertex>>();
            LinkedListNode<Vertex> initialBridgeNodeOnHull = null;
            currentNode = vertexList.First;
            while (currentNode != null)
            {
                Vector2 p0 = currentNode.Value.position;
                Vector2 p1 = (currentNode.Next == null) ? vertexList.First.Value.position : currentNode.Next.Value.position;

                // at least one point must be to right of holeData.bridgePoint for intersection with ray to be possible
                if (p0.x > holeData.bridgePoint.x || p1.x > holeData.bridgePoint.x)
                {
                    // one point is above, one point is below
                    if (p0.y > holeData.bridgePoint.y != p1.y > holeData.bridgePoint.y)
                    {
                        float rayIntersectX = p1.x; // only true if line p0,p1 is vertical
                        if (!Mathf.Approximately(p0.x, p1.x))
                        {
                            // float gradient = (p1.y - p0.y) / (p1.x - p0.x);
                            // rayIntersectX = p0.x + (p1.y - rayIntersectPoint.y) / gradient;
                            float intersectY = holeData.bridgePoint.y;
							float gradient = (p0.y - p1.y) / (p0.x - p1.x);
							float c = p1.y - gradient * p1.x;
							rayIntersectX = (intersectY - c) / gradient;
                        }

                        // if this is the closest ray intersection thus far, set bridge hull node to point in line having greater x pos (since def to right of hole).
                        if (rayIntersectX < rayIntersectPoint.x)
                        {
                            rayIntersectPoint.x = rayIntersectX;
                            initialBridgeNodeOnHull = (p0.x > p1.x) ? currentNode : currentNode.Next;
                        }

                    }
                }

                // Determine if current node might lie inside the triangle formed by hullBridgePoint, rayIntersection, and bridgeNodeOnHull
                // This is true only for reflex nodes which lie between holeData.bridgePoint and rayIntersect point on the x axis
                if (currentNode != initialBridgeNodeOnHull)
                {
                    if (!currentNode.Value.isConvex && p0.x > holeData.bridgePoint.x && p0.x < rayIntersectPoint.x)
                    {
                        hullNodesPotentiallyInBridgeTriangle.Add(currentNode);
                    }
                }

                currentNode = currentNode.Next;
            }

            // Check triangle formed by hullBridgePoint, rayIntersection, and bridgeNodeOnHull.
            // If this triangle contains any points, those points compete to become new bridgeNodeOnHull
            LinkedListNode<Vertex> validBridgeNodeOnHull = initialBridgeNodeOnHull;
            foreach (LinkedListNode<Vertex> nodePotentiallyInTriangle in hullNodesPotentiallyInBridgeTriangle)
            {
                // if there is a point inside triangle, this invalidates the current bridge node on hull.
                if (Geometry.PointInTriangle(holeData.bridgePoint, rayIntersectPoint, initialBridgeNodeOnHull.Value.position, nodePotentiallyInTriangle.Value.position))
                {
                    // if multiple nodes inside triangle, we want to choose the one with smallest angle from holeBridgeNode
                    float currentDstFromHoleBridgeY = Mathf.Abs(holeData.bridgePoint.y - validBridgeNodeOnHull.Value.position.y);
                    float pointInTriDstFromHoleBridgeY = Mathf.Abs(holeData.bridgePoint.y - nodePotentiallyInTriangle.Value.position.y);

                    if (pointInTriDstFromHoleBridgeY < currentDstFromHoleBridgeY)
                    {
                        validBridgeNodeOnHull = nodePotentiallyInTriangle;
                    }
                }
            }
            // Debug.Log("hole: " + holeData.holeIndex + "  index on hull: " + validBridgeNodeOnHull.Value.index);
            // if (holeData.holeIndex == 1)
            // {
            // Debug.DrawLine(rayIntersectPoint, holeData.bridgePoint, Color.green, 5);
            // }
            // Insert hole points (starting at holeBridgeNode) into vertex list at validBridgeNodeOnHull

            currentNode = validBridgeNodeOnHull;
            for (int i = holeData.bridgeIndex; i <= polygon.numPointsPerHole[holeData.holeIndex] + holeData.bridgeIndex; i++)
            {
                int previousIndex = currentNode.Value.index;
                int currentIndex = polygon.IndexOfPointInHole(i% polygon.numPointsPerHole[holeData.holeIndex], holeData.holeIndex);
                int nextIndex = polygon.IndexOfPointInHole((i + 1) % polygon.numPointsPerHole[holeData.holeIndex], holeData.holeIndex);

                if (i == polygon.numPointsPerHole[holeData.holeIndex] + holeData.bridgeIndex) // have come back to starting point
                {
                    nextIndex = validBridgeNodeOnHull.Value.index; // next point is back to the point on the hull
                }

                bool vertexIsConvex = IsCCW(polygon.points[previousIndex], polygon.points[currentIndex], polygon.points[nextIndex]);
                Vertex holeVertex = new Vertex(polygon.points[currentIndex], currentIndex, vertexIsConvex);
                currentNode = vertexList.AddAfter(currentNode, holeVertex);
            }

            bool isCCW = IsCCW(holeData.bridgePoint, validBridgeNodeOnHull.Value.position, currentNode.Next.Value.position);
            Vertex repeatStartHullVert = new Vertex(validBridgeNodeOnHull.Value.position, validBridgeNodeOnHull.Value.index, isCCW);
            vertexList.AddAfter(currentNode, repeatStartHullVert);
        }

        return vertexList;
    }

    public Triangulator(Polygon polygon)
    {
        tris = new int[(polygon.numPoints-2+2*polygon.numHoles)*3]; // +2 for extra hole verts

        vertsInClippedPolygon = GenerateList(polygon);
       
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

    public struct HoleData
    {
        public readonly int holeIndex;
        public readonly int bridgeIndex;
        public readonly Vector2 bridgePoint;

        public HoleData(int holeIndex, int bridgeIndex, Vector2 bridgePoint)
        {
            this.holeIndex = holeIndex;
            this.bridgeIndex = bridgeIndex;
            this.bridgePoint = bridgePoint;
        }
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
