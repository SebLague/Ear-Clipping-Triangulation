using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Polygon {

    public readonly Vector2[] points;
    public readonly int numHullPoints;
    public readonly int numHolePoints;
    public readonly int numPoints;


    public Polygon(Vector2[] hullPoints, Vector2[] holePoints)
	{
        numHullPoints = hullPoints.Length;
        numHolePoints = holePoints.Length;
        numPoints = numHullPoints + numHolePoints;
		points = new Vector2[numPoints];

        // TODO: enforce cw vertices, ccw holeVertices


        for (int i = 0; i < numHullPoints; i++)
        {
            points[i] = hullPoints[i];
        }
        for (int i = 0; i < numHolePoints; i++)
        {
            points[i + numHullPoints] = holePoints[i];
        }

    }


}

