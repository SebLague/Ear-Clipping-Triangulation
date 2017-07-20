using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class PolygonDrawer : MonoBehaviour {

    bool useGiz = false;

    public Polygon GetPolygon()
    {
        return new Polygon(transform.GetComponentsInChildren<Transform>().Where(t => t!=transform).Select(t => (Vector2)t.position).ToArray());
    }

    private void OnDrawGizmos()
    {
        if (!useGiz)
        {
            return;
        }

		Gizmos.color = Color.white;
        Vector3 prev = transform.GetChild(0).position;
       
        for (int i = 1; i <= transform.childCount; i++)
        {
            Vector3 next = transform.GetChild(i % transform.childCount).position;
            Gizmos.DrawLine(prev, next);
            prev = next;
        }

        Gizmos.color = Color.cyan;
        Triangulator triangulator = new Triangulator(GetPolygon());
        for (int i = 0; i < triangulator.linePairs.Count; i+=2)
        {
            Gizmos.DrawLine(triangulator.linePairs[i], triangulator.linePairs[i + 1]);
        }


    }
}
