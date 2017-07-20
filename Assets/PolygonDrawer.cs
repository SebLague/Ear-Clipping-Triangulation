using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class PolygonDrawer : MonoBehaviour {

    public bool useGiz = false;

    private void Start()
    {
		Polygon poly = GetPolygon();
		Triangulator triangulator = new Triangulator(poly);
		int[] tris = triangulator.Triangulate();
	}

    public Polygon GetPolygon()
    {
        int index = 0;
        return new Polygon(transform.GetComponentsInChildren<Transform>().Where(t => t!=transform).Select(t => new Vertex((Vector2)t.position, index++)).ToArray());
    }

    private void OnDrawGizmos()
    {
        if (!useGiz)
        {
            return;
        }

		Polygon poly = GetPolygon();

        // draw tris
        Gizmos.color = Color.cyan;
       
        Triangulator triangulator = new Triangulator(poly);
        int[] tris = triangulator.Triangulate();
        for (int i = 0; i < tris.Length; i+=3)
        {
         
            Gizmos.DrawLine(poly.vertices[tris[i]].position, poly.vertices[tris[i+1]].position);
            Gizmos.DrawLine(poly.vertices[tris[i]].position, poly.vertices[tris[i + 2]].position);
            Gizmos.DrawLine(poly.vertices[tris[i+1]].position, poly.vertices[tris[i + 2]].position);
        }

        // draw outline
		Gizmos.color = Color.white;
		Vector3 prev = poly.vertices[0].position;

        for (int i = 0; i < poly.numVerts; i++)
		{
            Vector3 next = poly.vertices[(i+1) % poly.numVerts].position;
			Gizmos.DrawLine(prev, next);
			prev = next;

			Gizmos.DrawSphere(poly.vertices[i].position, .2f);
		}

	
    }
}
