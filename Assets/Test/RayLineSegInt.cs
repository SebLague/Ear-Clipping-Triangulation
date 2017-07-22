using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayLineSegInt : MonoBehaviour {

    public Transform r0;
	public Transform r1;
	public Transform p0;
	public Transform p1;
	
	void Update () {
        Vector2 dir = r1.position - r0.position;
        bool intersect = Geometry.RayLineSegmentIntersect(r0.position, dir, p0.position, p1.position);
        Debug.DrawRay(r0.position, dir * 10, (intersect)?Color.green:Color.red);
        Debug.DrawLine(p0.position, p1.position, Color.white);

	}
}
