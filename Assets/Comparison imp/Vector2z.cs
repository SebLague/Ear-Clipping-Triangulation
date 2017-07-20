using UnityEngine;
using System.Collections;

[System.Serializable]
public class Vector2z
{
    
    
    public static Vector2z zero
    {
        get{return new Vector2z(0,0);}
    }
    
    public static Vector2z Lerp(Vector2z a, Vector2z b, float val)
    {
        Vector2z lerped = new Vector2z();
        lerped.vector2 = Vector2.Lerp(a.vector2, b.vector2, val);
        return lerped;
    }
    
    
    
    public float x = 0;
    public float z = 0;
    
    public Vector2z()
    {
        //nothing
    }
    
    public Vector2z(float _x, float _z)
    {
        x = _x;
        z = _z;
    }
    
    public Vector2z(Vector2 v2)
    {
        x = v2.x;
        z = v2.y;
    }
    
    public Vector2z(Vector3 v3)
    {
        x = v3.x;
        z = v3.z;
    }
    
    public Vector3 vector3
    {
        get{return new Vector3(x,0,z);}
        
        set{
            x = value.x;
            z = value.z;
        }
    }
    
    public Vector2 vector2
    {
        get{return new Vector3(x,z);}
        
        set{
            x = value.x;
            z = value.y;
        }
    }
    
    public float y
    {
        get{return z;}
        set{z = value;}
    }
    
    public static Vector2z operator +(Vector2z a, Vector2z b)
    {
        return new Vector2z(a.x+b.x,a.z+b.z);
    }
    
    public static Vector2z operator -(Vector2z a, Vector2z b)
    {
        return new Vector2z(a.x-b.x,a.z-b.z);
    }
    
    public static Vector2z operator *(Vector2z a, Vector2z b)
    {
        return new Vector2z(a.x*b.x,a.z*b.z);
    }
    
    public static Vector2z operator /(Vector2z a, Vector2z b)
    {
        return new Vector2z(a.x/b.x,a.z/b.z);
    }
}