using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PolygonGenerator {

    public static Polygon current;

    public static void Create(int n)
    {
        float r = 20;
        float angleIncrement = 360f / n;
        Vertex[] vertices = new Vertex[n];
        for (int i = 0; i < n; i++)
        {
            float angle = angleIncrement * i * Mathf.Deg2Rad;
            float randR = r/2f + Random.value * (r / 2f);
            Vector2 onCircle = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * randR;
            vertices[i] = new Vertex(onCircle,i);
        }

        current = new Polygon(vertices);
    }



}
