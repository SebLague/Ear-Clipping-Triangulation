using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Sebastian.Geometry
{
    /*
     * Handles triangulation of given polygon using the 'ear-clipping' algorithm.
     * The implementation is based on the following paper:
     * https://arxiv.org/ftp/arxiv/papers/1212/1212.6038.pdf
     * or alternatively
     * https://moscow.sci-hub.st/4540/5a573ec8f2082c9b48a4be206306c977/mei2012.pdf
     */

    public class TriangulatorBasic : ITriangulator
    {
        private const float MinAngle = 60;

        private List<Triangle> _formedTriangles;
        private int _triIndex;
        private int[] _tris;

        private LinkedList<Vertex> vertsInClippedPolygon;

        private void Init(Polygon polygon)
        {
            int numHoleToHullConnectionVerts = 2 * polygon.numHoles; // 2 verts are added when connecting a hole to the hull.
            int totalNumVerts = polygon.numPoints + numHoleToHullConnectionVerts;
            _tris = new int[(totalNumVerts - 2) * 3];
            vertsInClippedPolygon = GenerateVertexList(polygon);
        }

        // v1 is considered a convex vertex if v0-v1-v2 are wound in a counter-clockwise order.
        public static float GetAngle(Vector2 v0, Vector2 v1, Vector2 v2)
        {
            return Vector2.Angle(v1 - v0, v1 - v2);
        }

        public int[] Triangulate(Polygon polygon)
        {
            Init(polygon);

            _formedTriangles = new List<Triangle>();

            foreach (var vertexNode in vertsInClippedPolygon.GetNodes())
            {
                UpdateVertex(vertexNode);
            }

            while (vertsInClippedPolygon.Count >= 3)
            {
                bool hasRemovedEarThisIteration = false;
                var vertexNodes = GetSmallestConvexEar();
                foreach (var vertexNode in vertexNodes)
                {
                    LinkedListNode<Vertex> prevVertexNode = GetPreviousVertexNode(vertexNode);
                    LinkedListNode<Vertex> nextVertexNode = GetNextVertexNode(vertexNode);

                    Triangle triangle = new Triangle(prevVertexNode.Value, vertexNode.Value, nextVertexNode.Value);

                    _formedTriangles.Add(triangle);

                    hasRemovedEarThisIteration = true;
                    vertsInClippedPolygon.Remove(vertexNode);

                    // check if removal of ear makes prev/next vertex convex
                    UpdateVertex(prevVertexNode);
                    UpdateVertex(nextVertexNode);

                    break;
                }

                if (!hasRemovedEarThisIteration)
                {
                    Debug.LogError("Error triangulating mesh. Aborted.");
                    return null;
                }
            }

            // swapedges
            bool tryToSwapEdges = true;
            while (tryToSwapEdges)
            {
                tryToSwapEdges = false;

                foreach (var triangle in _formedTriangles.OrderBy(k => k.GetSmallestAngle()))
                {
                    if (NeedSwapEdge(triangle))
                    {
                        if (tryToSwapEdges = SwapEdges(triangle))
                            break;
                    }
                }
            }

            // add triangle to tri array
            foreach (var triangle in _formedTriangles)
            {
                _tris[_triIndex * 3 + 2] = triangle.A.index;
                _tris[_triIndex * 3 + 1] = triangle.B.index;
                _tris[_triIndex * 3] = triangle.C.index;
                _triIndex++;
            }

            return _tris;
        }

        // Creates a linked list of all vertices in the polygon, with the hole vertices joined to
        // the hull at optimal points.
        private LinkedList<Vertex> GenerateVertexList(Polygon polygon)
        {
            LinkedList<Vertex> vertexList = new LinkedList<Vertex>();
            LinkedListNode<Vertex> currentNode = null;

            // Add all hull points to the linked list
            for (int i = 0; i < polygon.numHullPoints; i++)
            {
                int prevPointIndex = (i - 1 + polygon.numHullPoints) % polygon.numHullPoints;
                int nextPointIndex = (i + 1) % polygon.numHullPoints;

                bool vertexIsConvex = IsConvex(polygon.points[prevPointIndex], polygon.points[i], polygon.points[nextPointIndex]);
                Vertex currentHullVertex = new Vertex(polygon.points[i], i, vertexIsConvex);

                if (currentNode == null)
                    currentNode = vertexList.AddFirst(currentHullVertex);
                else
                    currentNode = vertexList.AddAfter(currentNode, currentHullVertex);
            }

            // Process holes:
            List<HoleData> sortedHoleData = new List<HoleData>();

            for (int holeIndex = 0; holeIndex < polygon.numHoles; holeIndex++)
            {
                // Find index of rightmost point in hole. This 'bridge' point is where the hole will
                // be connected to the hull.
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
            // Sort hole data so that holes furthest to the right are first
            sortedHoleData.Sort((x, y) => (x.bridgePoint.x > y.bridgePoint.x) ? -1 : 1);

            foreach (HoleData holeData in sortedHoleData)
            {
                // Find first edge which intersects with rightwards ray originating at the hole
                // bridge point.
                Vector2 rayIntersectPoint = new Vector2(float.MaxValue, holeData.bridgePoint.y);
                List<LinkedListNode<Vertex>> hullNodesPotentiallyInBridgeTriangle = new List<LinkedListNode<Vertex>>();
                LinkedListNode<Vertex> initialBridgeNodeOnHull = null;
                currentNode = vertexList.First;
                while (currentNode != null)
                {
                    LinkedListNode<Vertex> nextNode = (currentNode.Next == null) ? vertexList.First : currentNode.Next;
                    Vector2 p0 = currentNode.Value.position;
                    Vector2 p1 = nextNode.Value.position;

                    // at least one point must be to right of holeData.bridgePoint for intersection
                    // with ray to be possible
                    if (p0.x > holeData.bridgePoint.x || p1.x > holeData.bridgePoint.x)
                    {
                        // one point is above, one point is below
                        if (p0.y > holeData.bridgePoint.y != p1.y > holeData.bridgePoint.y)
                        {
                            float rayIntersectX = p1.x; // only true if line p0,p1 is vertical
                            if (!Mathf.Approximately(p0.x, p1.x))
                            {
                                float intersectY = holeData.bridgePoint.y;
                                float gradient = (p0.y - p1.y) / (p0.x - p1.x);
                                float c = p1.y - gradient * p1.x;
                                rayIntersectX = (intersectY - c) / gradient;
                            }

                            // intersection must be to right of bridge point
                            if (rayIntersectX > holeData.bridgePoint.x)
                            {
                                LinkedListNode<Vertex> potentialNewBridgeNode = (p0.x > p1.x) ? currentNode : nextNode;
                                // if two intersections occur at same x position this means is
                                // duplicate edge duplicate edges occur where a hole has been joined
                                // to the outer polygon
                                bool isDuplicateEdge = Mathf.Approximately(rayIntersectX, rayIntersectPoint.x);

                                // connect to duplicate edge (the one that leads away from the
                                // other, already connected hole, and back to the original hull) if
                                // the current hole's bridge point is higher up than the bridge
                                // point of the other hole (so that the new bridge connection
                                // doesn't intersect).
                                bool connectToThisDuplicateEdge = holeData.bridgePoint.y > potentialNewBridgeNode.Previous.Value.position.y;

                                if (!isDuplicateEdge || connectToThisDuplicateEdge)
                                {
                                    // if this is the closest ray intersection thus far, set bridge
                                    // hull node to point in line having greater x pos (since def to
                                    // right of hole).
                                    if (rayIntersectX < rayIntersectPoint.x || isDuplicateEdge)
                                    {
                                        rayIntersectPoint.x = rayIntersectX;
                                        initialBridgeNodeOnHull = potentialNewBridgeNode;
                                    }
                                }
                            }
                        }
                    }

                    // Determine if current node might lie inside the triangle formed by
                    // holeBridgePoint, rayIntersection, and bridgeNodeOnHull We only need consider
                    // those which are reflex, since only these will be candidates for visibility
                    // from holeBridgePoint. A list of these nodes is kept so that in next step it
                    // is not necessary to iterate over all nodes again.
                    if (currentNode != initialBridgeNodeOnHull)
                    {
                        if (!currentNode.Value.isConvex && p0.x > holeData.bridgePoint.x)
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
                    if (nodePotentiallyInTriangle.Value.index == initialBridgeNodeOnHull.Value.index)
                    {
                        continue;
                    }
                    // if there is a point inside triangle, this invalidates the current bridge node
                    // on hull.
                    if (Maths2D.PointInTriangle(holeData.bridgePoint, rayIntersectPoint, initialBridgeNodeOnHull.Value.position, nodePotentiallyInTriangle.Value.position))
                    {
                        // Duplicate points occur at hole and hull bridge points.
                        bool isDuplicatePoint = validBridgeNodeOnHull.Value.position == nodePotentiallyInTriangle.Value.position;

                        // if multiple nodes inside triangle, we want to choose the one with
                        // smallest angle from holeBridgeNode. if is a duplicate point, then use the
                        // one occurring later in the list
                        float currentDstFromHoleBridgeY = Mathf.Abs(holeData.bridgePoint.y - validBridgeNodeOnHull.Value.position.y);
                        float pointInTriDstFromHoleBridgeY = Mathf.Abs(holeData.bridgePoint.y - nodePotentiallyInTriangle.Value.position.y);

                        if (pointInTriDstFromHoleBridgeY < currentDstFromHoleBridgeY || isDuplicatePoint)
                        {
                            validBridgeNodeOnHull = nodePotentiallyInTriangle;
                        }
                    }
                }

                // Insert hole points (starting at holeBridgeNode) into vertex list at validBridgeNodeOnHull
                currentNode = validBridgeNodeOnHull;
                for (int i = holeData.bridgeIndex; i <= polygon.numPointsPerHole[holeData.holeIndex] + holeData.bridgeIndex; i++)
                {
                    int previousIndex = currentNode.Value.index;
                    int currentIndex = polygon.IndexOfPointInHole(i % polygon.numPointsPerHole[holeData.holeIndex], holeData.holeIndex);
                    int nextIndex = polygon.IndexOfPointInHole((i + 1) % polygon.numPointsPerHole[holeData.holeIndex], holeData.holeIndex);

                    if (i == polygon.numPointsPerHole[holeData.holeIndex] + holeData.bridgeIndex) // have come back to starting point
                    {
                        nextIndex = validBridgeNodeOnHull.Value.index; // next point is back to the point on the hull
                    }

                    bool vertexIsConvex = IsConvex(polygon.points[previousIndex], polygon.points[currentIndex], polygon.points[nextIndex]);
                    Vertex holeVertex = new Vertex(polygon.points[currentIndex], currentIndex, vertexIsConvex);
                    currentNode = vertexList.AddAfter(currentNode, holeVertex);
                }

                // Add duplicate hull bridge vert now that we've come all the way around. Also set
                // its concavity
                Vector2 nextVertexPos = (currentNode.Next == null) ? vertexList.First.Value.position : currentNode.Next.Value.position;
                bool isConvex = IsConvex(holeData.bridgePoint, validBridgeNodeOnHull.Value.position, nextVertexPos);
                Vertex repeatStartHullVert = new Vertex(validBridgeNodeOnHull.Value.position, validBridgeNodeOnHull.Value.index, isConvex);
                vertexList.AddAfter(currentNode, repeatStartHullVert);

                //Set concavity of initial hull bridge vert, since it may have changed now that it leads to hole vert
                LinkedListNode<Vertex> nodeBeforeStartBridgeNodeOnHull = (validBridgeNodeOnHull.Previous == null) ? vertexList.Last : validBridgeNodeOnHull.Previous;
                LinkedListNode<Vertex> nodeAfterStartBridgeNodeOnHull = (validBridgeNodeOnHull.Next == null) ? vertexList.First : validBridgeNodeOnHull.Next;
                validBridgeNodeOnHull.Value.isConvex = IsConvex(nodeBeforeStartBridgeNodeOnHull.Value.position, validBridgeNodeOnHull.Value.position, nodeAfterStartBridgeNodeOnHull.Value.position);
            }
            return vertexList;
        }

        private LinkedListNode<Vertex> GetNextVertexNode(LinkedListNode<Vertex> vertexNode)
        {
            return vertexNode.Next ?? vertsInClippedPolygon.First;
        }

        private LinkedListNode<Vertex> GetPreviousVertexNode(LinkedListNode<Vertex> vertexNode)
        {
            return vertexNode.Previous ?? vertsInClippedPolygon.Last;
        }

        private IEnumerable<LinkedListNode<Vertex>> GetSmallestConvexEar()
        {
            return from vertexNode in vertsInClippedPolygon.GetNodes()
                   where vertexNode.Value.isConvex && !vertexNode.Value.triangleContainsVertex
                   orderby vertexNode.Value.earAngle
                   select vertexNode;
        }

        // angle of v1v0 ^ v1v2
        private bool IsConvex(Vector2 v0, Vector2 v1, Vector2 v2)
        {
            return Maths2D.SideOfLine(v0, v2, v1) == -1;
        }

        private bool NeedSwapEdge(Triangle triangle)
        {
            return triangle.GetSmallestAngle() < MinAngle;
        }

        private bool SwapEdges(Triangle triangle)
        {
            float edge_ab = Vector2.Distance(triangle.A.position, triangle.B.position);
            float edge_ac = Vector2.Distance(triangle.A.position, triangle.C.position);
            float edge_bc = Vector2.Distance(triangle.B.position, triangle.C.position);

            Triangle GetOtherTriangleWithEdge(Vertex a, Vertex b, Triangle except)
            {
                foreach (Triangle other in _formedTriangles)
                {
                    if (other == except)
                        continue;

                    if (other.HaveEdge(a, b))
                        return other;
                }

                return null;
            }

            bool ShouldSwapEdge(Triangle first, Triangle second)
            {
                float firstSmallestAngle = first.GetSmallestAngle();
                float secondSmallestAngle = second.GetSmallestAngle();

                first.C = second.C;
                second.A = first.B;

                float firstSmallestAngle2 = first.GetSmallestAngle();
                float secondSmallestAngle2 = second.GetSmallestAngle();

                return Mathf.Min(firstSmallestAngle2, secondSmallestAngle2) >
                       Mathf.Min(firstSmallestAngle, secondSmallestAngle);
            }

            Triangle otherTriangle;

            if (edge_ab >= edge_ac && edge_ab >= edge_bc)
            {
                otherTriangle = GetOtherTriangleWithEdge(triangle.A, triangle.B, triangle);
                if (otherTriangle == null)
                    return false;

                var first = new Triangle(triangle.B, triangle.C, triangle.A);
                var second = new Triangle(first.A, first.C, otherTriangle.Opposite(first.A, first.C));

                if (ShouldSwapEdge(first, second))
                {
                    triangle.A = first.A;
                    triangle.B = first.B;
                    triangle.C = first.C;

                    otherTriangle.A = second.A;
                    otherTriangle.B = second.B;
                    otherTriangle.C = second.C;

                    return true;
                }
            }
            else if (edge_ac >= edge_ab && edge_ac >= edge_bc)
            {
                otherTriangle = GetOtherTriangleWithEdge(triangle.A, triangle.C, triangle);
                if (otherTriangle == null)
                    return false;

                var first = new Triangle(triangle.A, triangle.B, triangle.C);
                var second = new Triangle(first.A, first.C, otherTriangle.Opposite(first.A, first.C));

                if (ShouldSwapEdge(first, second))
                {
                    triangle.A = first.A;
                    triangle.B = first.B;
                    triangle.C = first.C;

                    otherTriangle.A = second.A;
                    otherTriangle.B = second.B;
                    otherTriangle.C = second.C;

                    return true;
                }
            }
            else
            {
                otherTriangle = GetOtherTriangleWithEdge(triangle.B, triangle.C, triangle);
                if (otherTriangle == null)
                    return false;

                var first = new Triangle(triangle.C, triangle.A, triangle.B);
                var second = new Triangle(first.A, first.C, otherTriangle.Opposite(first.A, first.C));

                if (ShouldSwapEdge(first, second))
                {
                    triangle.A = first.A;
                    triangle.B = first.B;
                    triangle.C = first.C;

                    otherTriangle.A = second.A;
                    otherTriangle.B = second.B;
                    otherTriangle.C = second.C;

                    return true;
                }
            }

            return false;
        }

        // check if triangle contains any verts (note, only necessary to check reflex verts).
        private bool TriangleContainsVertex(Vertex v0, Vertex v1, Vertex v2)
        {
            LinkedListNode<Vertex> vertexNode = vertsInClippedPolygon.First;
            for (int i = 0; i < vertsInClippedPolygon.Count; i++)
            {
                if (!vertexNode.Value.isConvex) // convex verts will never be inside triangle
                {
                    Vertex vertexToCheck = vertexNode.Value;
                    if (vertexToCheck.index != v0.index && vertexToCheck.index != v1.index && vertexToCheck.index != v2.index) // dont check verts that make up triangle
                    {
                        if (Maths2D.PointInTriangle(v0.position, v1.position, v2.position, vertexToCheck.position))
                        {
                            return true;
                        }
                    }
                }
                vertexNode = vertexNode.Next;
            }

            return false;
        }

        private void UpdateVertex(LinkedListNode<Vertex> vertexNode)
        {
            var prevVertexNode = GetPreviousVertexNode(vertexNode);
            var nextVertexNode = GetNextVertexNode(vertexNode);

            vertexNode.Value.isConvex = IsConvex(prevVertexNode.Value.position, vertexNode.Value.position, nextVertexNode.Value.position);

            // update other info only if is convex
            if (vertexNode.Value.isConvex)
            {
                vertexNode.Value.earAngle = GetAngle(prevVertexNode.Value.position, vertexNode.Value.position, nextVertexNode.Value.position);
                vertexNode.Value.triangleContainsVertex = TriangleContainsVertex(prevVertexNode.Value, vertexNode.Value, nextVertexNode.Value);
            }
            else
            {
                // just reset these data
                vertexNode.Value.earAngle = 0;
                vertexNode.Value.triangleContainsVertex = false;
            }
        }

        public struct HoleData
        {
            public readonly int bridgeIndex;
            public readonly Vector2 bridgePoint;
            public readonly int holeIndex;

            public HoleData(int holeIndex, int bridgeIndex, Vector2 bridgePoint)
            {
                this.holeIndex = holeIndex;
                this.bridgeIndex = bridgeIndex;
                this.bridgePoint = bridgePoint;
            }
        }

        public class Triangle
        {
            public Vertex A;
            public Vertex B;
            public Vertex C;

            public Triangle(Vertex a, Vertex b, Vertex c)
            {
                A = a;
                B = b;
                C = c;
            }

            public float GetSmallestAngle()
            {
                var angles = new float[] {
                            GetAngle(A.position, B.position, C.position),
                            GetAngle(B.position, C.position, A.position),
                            GetAngle(C.position, A.position, B.position)};

                return angles.Min();
            }

            public bool HaveEdge(Vertex a, Vertex b)
            {
                if (A == a && B == b)
                    return true;

                if (A == b && B == a)
                    return true;

                if (A == a && C == b)
                    return true;

                if (A == b && C == a)
                    return true;

                if (C == a && B == b)
                    return true;

                if (C == b && B == a)
                    return true;

                return false;
            }

            internal Vertex Opposite(Vertex v1, Vertex v2)
            {
                if (v1 != A && v1 != B && v1 != C)
                    return null;

                if (v2 != A && v2 != B && v2 != C)
                    return null;

                if (A != v1 && A != v2)
                    return A;

                if (B != v1 && B != v2)
                    return B;

                return C;
            }
        }

        public class Vertex
        {
            public readonly int index;
            public readonly Vector2 position;

            public float earAngle;
            public bool isConvex;
            public bool triangleContainsVertex;

            public Vertex(Vector2 position, int index, bool isConvex)
            {
                this.position = position;
                this.index = index;
                this.isConvex = isConvex;
            }
        }
    }
}