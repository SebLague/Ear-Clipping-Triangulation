using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Geometry {


	public static float PseudoDistanceFromPointToLine(Vector2 a, Vector2 b, Vector2 c)
	{
		return Mathf.Abs((c.x - a.x) * (-b.y + a.y) + (c.y - a.y) * (b.x - a.x));
	}

	public static int SideOfLine(Vector2 a, Vector2 b, Vector2 c)
	{
        return (int)Mathf.Sign((c.x - a.x) * (-b.y + a.y) + (c.y - a.y) * (b.x - a.x));
	}

	public static int SideOfLine(float ax, float ay, float bx, float by, float cx, float cy)
	{
		return (int)Mathf.Sign((cx - ax) * (-by + ay) + (cy - ay) * (bx - ax));
	}

	public static bool PointInTriangle(Vector2 a, Vector2 b, Vector2 c, Vector2 p)
	{
		float s1 = c.y - a.y;
		float s2 = c.x - a.x;
		float s3 = b.y - a.y;
		float s4 = p.y - a.y;

		float w1 = (a.x * s1 + s4 * s2 - p.x * s1) / (s3 * s2 - (b.x-a.x) * s1);
		float w2 = (s4- w1 * s3) / s1;
		return w1 >= 0 && w2 >= 0 && (w1 + w2) <= 1;
	}

    public static bool RayLineSegmentIntersect(Vector2 r0, Vector2 dir, Vector2 p0, Vector2 p1)
    {
        Vector2 a = dir;
        Vector2 b = p1 - p0;

        float t1 = (b.y * (p0.x - r0.x) + b.x * (r0.y - p0.y)) / (a.x * b.y - b.x * a.y);
        float t2 = (r0.y - p0.y + a.y * t1) / b.y;
        return t1 >= 0 && t2 >= 0 && t2 <= 1;
    }
}
