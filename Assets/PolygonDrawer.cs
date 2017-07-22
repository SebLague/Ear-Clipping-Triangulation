using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;

public class PolygonDrawer : MonoBehaviour {

    public bool drawMesh;
    public bool showTris;
    public bool useGiz = false;
    public Transform holeHolder;

    Triangulator triangulator;
	List<Vector2> draw = new List<Vector2>();

    bool inputDown;

    private void Start()
    {
		
        //int[] tris = triangulator.Triangulate();
        StartCoroutine(Draw());
	}

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            inputDown = true;
        }

        if (drawMesh)
        {
            Triangulator tt = new Triangulator(GetPolygon());
            Mesh mesh = new Mesh();
            mesh.vertices = GetPolygon().points.Select(v2=>(Vector3)v2).ToArray();
            mesh.triangles = tt.Triangulate().Reverse().ToArray();
           // mesh.normals = mesh.vertices.Select(v => Vector3.forward).ToArray();
            GetComponent<MeshFilter>().mesh = mesh;
        }
    }

    IEnumerator Draw()
    {
        yield return new WaitUntil(() => inputDown);
		Polygon poly = GetPolygon();
		triangulator = new Triangulator(poly);
        LinkedListNode<Vertex> v = triangulator.vertsInClippedPolygon.First;
        while (true)
        {
            //yield return null;
           
            print("next : " + v.Value.index );
            inputDown = false;
            draw.Add(v.Value.position);
            if (v.Next == null)
            {
                break;
            }
            v = v.Next;
			yield return new WaitUntil(() => inputDown);
        }
		print("done");
    }

    public Polygon GetPolygon()
    {
        IEnumerable<Vector2> points = transform.GetComponentsInChildren<Transform>().Where(t => t != transform).Select(t => (Vector2)t.position);
        IEnumerable<Vector2> holePoints = holeHolder.GetComponentsInChildren<Transform>().Where(t => t != holeHolder).Select(t => (Vector2)t.position);
        return new Polygon(points.ToArray(), holePoints.ToArray());
    }

    private void OnDrawGizmos()
    {
        if (!useGiz)
        {
            return;
        }

        Gizmos.color = Color.red;
        foreach (Vector2 v in draw)
        {
            Gizmos.DrawSphere(v, .5f);
        }
     
		Polygon poly = GetPolygon();

        if (showTris)
        {
            // draw tris
            Gizmos.color = Color.cyan;

            Triangulator triangulator = new Triangulator(poly);
            //WikiTri triangulator = new WikiTri(poly.vertices.Select(v => v.position).ToArray());
            int[] tris = triangulator.Triangulate();
            for (int i = 0; i < tris.Length; i += 3)
            {

                Gizmos.DrawLine(poly.points[tris[i]], poly.points[tris[i + 1]]);
                Gizmos.DrawLine(poly.points[tris[i]], poly.points[tris[i + 2]]);
                Gizmos.DrawLine(poly.points[tris[i + 1]], poly.points[tris[i + 2]]);
            }
        }

        // draw outline
		Gizmos.color = Color.white;
		Vector3 prev = poly.points[0];

        for (int i = 0; i < poly.numHullPoints; i++)
		{
            
            Vector3 next = poly.points[(i+1) % poly.numHullPoints];
			Gizmos.DrawLine(prev, next);
			prev = next;

			Gizmos.DrawSphere(poly.points[i], .2f);
           // Handles.Label(poly.points[i] - Vector2.right * .3f, "" + poly.points[i].index);
		}


        // draw holes
        Gizmos.color = Color.red;
        prev = poly.points[poly.numHullPoints];

        for (int i = 0; i < poly.numHolePoints; i++)
		{
            Vector3 next = poly.points[poly.numHullPoints + (( i + 1) % poly.numHolePoints)];
			Gizmos.DrawLine(prev, next);
			prev = next;

            Gizmos.DrawSphere(poly.points[poly.numHullPoints + i], .1f);
		}

	
    }
}
