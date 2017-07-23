using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;

public class PolygonDrawer : MonoBehaviour {

    public bool drawMesh;
    public bool showTris;
    public bool useGiz = false;
    public Transform[] holeHolder;

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
        List<Vector2[]> allHoles = new List<Vector2[]>();
        foreach (Transform t in holeHolder)
        {
            allHoles.Add(t.GetComponentsInChildren<Transform>().Where(c => c != t).Select(h => (Vector2)h.position).ToArray());
        }
        return new Polygon(points.ToArray(), allHoles.ToArray());
    }

    private void OnDrawGizmos()
    {
        if (!useGiz)
        {
            return;
        }

        Gizmos.color = Color.red;
        List<Vector2> alreadyDone = new List<Vector2>();
        foreach (Vector2 v in draw)
        {
            int num = alreadyDone.Count(x => x == v);
            Gizmos.DrawSphere(v, .25f + .25f * num);
            alreadyDone.Add(v);
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
      
        for (int i = 0; i < poly.numHoles; i++)
        {
            prev = poly.points[poly.IndexOfPointInHole(0,i)];

			for (int j = 0; j < poly.numPointsPerHole[i]; j++)
			{
                Vector3 next = poly.points[poly.IndexOfPointInHole((j+1)%poly.numPointsPerHole[i],i)];
				Gizmos.DrawLine(prev, next);
				prev = next;

				Gizmos.DrawSphere(poly.points[poly.numHullPoints + j], .1f);
			}
        }
   

	
    }
}
