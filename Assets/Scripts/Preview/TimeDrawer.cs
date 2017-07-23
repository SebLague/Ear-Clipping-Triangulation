using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class TimeDrawer : MonoBehaviour
{

    public float dst = .25f;
    public bool drawMesh = true;
    Vector2 lastDrawPoint;

    List<List<Vector2>> holes = new List<List<Vector2>>();
    List<Vector2> main = new List<Vector2>();

    void Update()
    {
        Vector2 mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            lastDrawPoint = mouse;
            main.Add(mouse);

        }
        if (Input.GetKey(KeyCode.Mouse0))
        {
            if (Vector2.Distance(mouse, lastDrawPoint) > dst)
            {
                lastDrawPoint = mouse;
                main.Add(mouse);
            }
        }

        if (Input.GetKeyDown(KeyCode.Mouse1))
		{
			lastDrawPoint = mouse;
            holes.Add(new List<Vector2>());
            holes[holes.Count - 1].Add(mouse);

		}
		if (Input.GetKey(KeyCode.Mouse1))
		{
			if (Vector2.Distance(mouse, lastDrawPoint) > dst)
			{
				lastDrawPoint = mouse;
				holes[holes.Count - 1].Add(mouse);
			}
		}

       // Debug.Log(main.Count);
        if (main.Count > 3 && holes.All(x=>x.Count>3))
        {
            if (drawMesh)
            {
                Vector2[][] holesArray = new Vector2[holes.Count][];
                for (int i = 0; i < holesArray.GetLength(0); i++)
                {
                    holesArray[i] = holes[i].ToArray();
                 
                    if (holesArray[i] == null || holesArray[i].Length < 4)
                    {
                        holesArray[i] = new Vector2[0];
                    }
                }
                Polygon p = new Polygon(main.ToArray(), holesArray);
                Triangulator tt = new Triangulator(p);
                Mesh mesh = new Mesh();
                mesh.vertices = p.points.Select(v2 => (Vector3)v2).ToArray();
                int[] tris = tt.Triangulate();
                if (tris != null)
                {
                    mesh.triangles = tt.Triangulate().Reverse().ToArray();
                    // mesh.normals = mesh.vertices.Select(v => Vector3.forward).ToArray();
                    GetComponent<MeshFilter>().mesh = mesh;
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        foreach (Vector2 v in main)
        {
            Gizmos.DrawSphere(v, .05f);
        }

        Gizmos.color = Color.red;
        foreach (List<Vector2> l in holes)
		{

            foreach (Vector2 v in l)
            {
                Gizmos.DrawSphere(v, .05f);
            }
		}
    }
}
