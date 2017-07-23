using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
public class Polygon {

    public readonly Vector2[] points;
	public readonly int numPoints;

    public readonly int numHullPoints;

    public readonly int[] numPointsPerHole;
    public readonly int numHoles;

    readonly int[] holeStartIndices;

    public Polygon(Vector2[] hull, Vector2[][] holes)
	{
        numHullPoints = hull.Length;
        numHoles = holes.GetLength(0);

        numPointsPerHole = new int[numHoles];
        holeStartIndices = new int[numHoles];
        int numHolePointsSum = 0;

        for (int i = 0; i < holes.GetLength(0); i++)
        {
            numPointsPerHole[i] = holes[i].Length;

            holeStartIndices[i] = numHullPoints + numHolePointsSum;
            numHolePointsSum += numPointsPerHole[i];
        }

        numPoints = numHullPoints + numHolePointsSum;
		points = new Vector2[numPoints];

        // TODO: enforce cw vertices, ccw holeVertices

        // add hull points
        for (int i = 0; i < numHullPoints; i++)
        {
            points[i] = hull[i];
        }
        // add hole points
        for (int i = 0; i < numHoles; i++)
        {
            for (int j = 0; j < holes[i].Length; j++)
            {
                points[IndexOfPointInHole(j,i)] = holes[i][j];
            }
        }

    }

    public int IndexOfFirstPointInHole(int holeIndex)
    {
		return holeStartIndices[holeIndex];
    }

	public int IndexOfPointInHole(int index, int holeIndex)
	{
        return holeStartIndices[holeIndex] + index;
	}

    public Vector2 GetHolePoint(int index, int holeIndex)
    {
        return points[holeStartIndices[holeIndex] + index];
    }


}

