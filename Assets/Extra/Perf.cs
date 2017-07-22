using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System.Linq;

public class Perf : MonoBehaviour {

    public int it = 10;
    public int numPoints = 100;
    public bool use;

	void Start () {
        if (use)
        {
            PolygonGenerator.Create(numPoints);
            Polygon testPoly = PolygonGenerator.current;
            Stopwatch sw = new Stopwatch();

            sw.Start();
            for (int i = 0; i < it; i++)
            {
                Triangulator c = new Triangulator(testPoly);
                c.Triangulate();
            }
            sw.Stop();

            print("time ms: " + sw.ElapsedMilliseconds);
        }

	}
}
