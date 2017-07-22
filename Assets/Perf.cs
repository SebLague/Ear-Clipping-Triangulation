using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System.Linq;

public class Perf : MonoBehaviour {

    public int it = 10;
    public int numPoints = 100;

	void Start () {
        
        PolygonGenerator.Create(numPoints);
        Polygon testPoly = PolygonGenerator.current;
        Vector2[] pointsArray = testPoly.points.Select(v => v).ToArray();
	    Stopwatch sw = new Stopwatch();

        sw.Start();
        for (int i = 0; i < it; i++)
        {
            Triangulator c = new Triangulator(testPoly);
            c.Triangulate();
        }
        sw.Stop();

        print("my: " + sw.ElapsedMilliseconds);

        sw.Reset();
		sw.Start();
		for (int i = 0; i < it; i++)
		{
			//EarClipper.Triangulate(pointsArray);
            WikiTri c = new WikiTri(pointsArray);
			c.Triangulate();
		}
		sw.Stop();

		print("their: " + sw.ElapsedMilliseconds);

	}



	void Update () {
		
	}
}
