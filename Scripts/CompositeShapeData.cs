using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Sebastian.Geometry
{
    public partial class CompositeShape
    {

        /*
         * Holds data for each shape needed when calculating composite shapes.
         */

        public class CompositeShapeData
        {
            public readonly Vector2[] points;
            public readonly Polygon polygon;
            public readonly int[] triangles;

            public List<CompositeShapeData> parents = new List<CompositeShapeData>();
            public List<CompositeShapeData> holes = new List<CompositeShapeData>();
            public bool IsValidShape { get; private set; }

            public CompositeShapeData(Vector3[] points)
            {
                this.points = points.Select(v => v.ToXZ()).ToArray();
                IsValidShape = points.Length >= 3 && !IntersectsWithSelf();

                if (IsValidShape)
                {
                    polygon = new Polygon(this.points);
                    Triangulator t = new Triangulator(polygon);
                    triangles = t.Triangulate();
                }
            }

            // Removes any holes which overlap with another hole
            public void ValidateHoles()
            {
                for (int i = 0; i < holes.Count; i++)
                {
                    for (int j = i + 1; j < holes.Count; j++)
                    {
                        bool overlap = holes[i].OverlapsPartially(holes[j]);

                        if (overlap)
                        {
                            holes[i].IsValidShape = false;
                            break;
                        }
                    }
                }

                for (int i = holes.Count - 1; i >= 0; i--)
                {
                    if (!holes[i].IsValidShape)
                    {
                        holes.RemoveAt(i);
                    }
                }
            }

            // A parent is a shape which fully contains another shape
            public bool IsParentOf(CompositeShapeData otherShape)
            {
                if (otherShape.parents.Contains(this))
                {
                    return true;
                }
                if (parents.Contains(otherShape))
                {
                    return false;
                }

                // check if first point in otherShape is inside this shape. If not, parent test fails.
                // if yes, then continue to line seg intersection test between the two shapes

                // (this point test is important because without it, if all line seg intersection tests fail,
                // we wouldn't know if otherShape is entirely inside or entirely outside of this shape)
                bool pointInsideShape = false;
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    if (Maths2D.PointInTriangle(polygon.points[triangles[i]], polygon.points[triangles[i + 1]], polygon.points[triangles[i + 2]], otherShape.points[0]))
                    {
                        pointInsideShape = true;
                        break;
                    }
                }

                if (!pointInsideShape)
                {
                    return false;
                }

                // Check for intersections between line segs of this shape and otherShape (any intersections will fail the parent test)
                for (int i = 0; i < points.Length; i++)
                {
                    LineSegment parentSeg = new LineSegment(points[i], points[(i + 1) % points.Length]);
                    for (int j = 0; j < otherShape.points.Length; j++)
                    {
                        LineSegment childSeg = new LineSegment(otherShape.points[j], otherShape.points[(j + 1) % otherShape.points.Length]);
                        if (Maths2D.LineSegmentsIntersect(parentSeg.a, parentSeg.b, childSeg.a, childSeg.b))
                        {
                            return false;
                        }
                    }
                }
                return true;
            }

            // Test if the shapes overlap partially (test will fail if one shape entirely contains other shape, i.e. one is parent of the other).
            public bool OverlapsPartially(CompositeShapeData otherShape)
            {

                // Check for intersections between line segs of this shape and otherShape (any intersection will validate the overlap test)
                for (int i = 0; i < points.Length; i++)
                {
                    LineSegment segA = new LineSegment(points[i], points[(i + 1) % points.Length]);
                    for (int j = 0; j < otherShape.points.Length; j++)
                    {
                        LineSegment segB = new LineSegment(otherShape.points[j], otherShape.points[(j + 1) % otherShape.points.Length]);
                        if (Maths2D.LineSegmentsIntersect(segA.a, segA.b, segB.a, segB.b))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }

            // Checks if any of the line segments making up this shape intersect
            public bool IntersectsWithSelf()
            {

                for (int i = 0; i < points.Length; i++)
                {
                    LineSegment segA = new LineSegment(points[i], points[(i + 1) % points.Length]);
                    for (int j = i + 2; j < points.Length; j++)
                    {
                        if ((j + 1) % points.Length == i)
                        {
                            continue;
                        }
                        LineSegment segB = new LineSegment(points[j], points[(j + 1) % points.Length]);
                        if (Maths2D.LineSegmentsIntersect(segA.a, segA.b, segB.a, segB.b))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }

            public struct LineSegment
            {
                public readonly Vector2 a;
                public readonly Vector2 b;

                public LineSegment(Vector2 a, Vector2 b)
                {
                    this.a = a;
                    this.b = b;
                }
            }
        }
    }
}