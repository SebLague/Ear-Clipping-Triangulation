using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PolygonDrawer))]
public class PolygonEditor : Editor {

	private void OnEnable()
	{
		Tools.hidden = true;
	}

	private void OnDisable()
	{
		Tools.hidden = false;
	}

	private void OnSceneGUI()
	{

        PolygonDrawer poly = (PolygonDrawer)target;

        Handles.color = Color.green;
        foreach (Transform t in poly.transform)
        {
			t.position = Handles.FreeMoveHandle(t.position, Quaternion.identity, .2f, Vector3.zero, Handles.SphereHandleCap);
        }

        Handles.color = Color.red;
        foreach (Transform p in poly.holeHolder)
		{
            foreach (Transform t in p)
            {
                //t.position = Handles.DoPositionHandle(t.position, Quaternion.identity);
                t.position = Handles.FreeMoveHandle(t.position, Quaternion.identity, .2f, Vector3.zero, Handles.SphereHandleCap);
            }
		}
		
	}
}
