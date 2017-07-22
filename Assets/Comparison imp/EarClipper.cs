
using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class EarClipTriangle
{
    public Vector2 a;
    public Vector2 b;
    public Vector2 c;
    public Rect bounds;
    
    public EarClipTriangle(Vector2 a, Vector2 b, Vector2 c)
    {
        bounds = new Rect(a.x,a.y,0,0);
        Vector2[] points = new Vector2[]{a,b,c};
        for(int i=1; i<3; i++)
        {
            if(bounds.xMin < points[i].x)
                bounds.xMin = points[i].x;
            if(bounds.xMax < points[i].x)
                bounds.xMax = points[i].x;
            if(bounds.yMin < points[i].y)
                bounds.yMin = points[i].y;
            if(bounds.yMax < points[i].y)
                bounds.yMax = points[i].y;
        }
    }
}

public class EarClipper 
{

    public static int[] Triangulate( Vector2[] points)
    {
        int numberOfPoints = points.Length;
        List<int> usePoints = new List<int>();
    
        for(int p=0; p<numberOfPoints; p++)
            usePoints.Add(p);
        int numberOfUsablePoints = usePoints.Count;

        List<int> indices = new List<int>();
        
        if (numberOfPoints < 3)
            return indices.ToArray();
        
        int it = 100;
        while(numberOfUsablePoints > 3)
        {
            for(int i=0; i<numberOfUsablePoints; i++)
            {
                int a,b,c;
                
                a=usePoints[i];
                
                if(i>=numberOfUsablePoints-1)
                    b=usePoints[0];
                else
                    b=usePoints[i+1];
                
                if(i>=numberOfUsablePoints-2)
                    c=usePoints[(i+2)-numberOfUsablePoints];
                else
                    c=usePoints[i+2];
                
                Vector2 pA = points[b];
                Vector2 pB = points[a];
                Vector2 pC = points[c];
                
                float dA = Vector2.Distance(pA,pB);
                float dB = Vector2.Distance(pB,pC);
                float dC = Vector2.Distance(pC,pA);
                
                float angle = Mathf.Acos((Mathf.Pow(dB,2)-Mathf.Pow(dA,2)-Mathf.Pow(dC,2))/(2*dA*dC))*Mathf.Rad2Deg * Mathf.Sign(Sign(points[a],points[b],points[c]));
                if(angle < 0)
                {
                    continue;//angle is not reflex
                }
                
                bool freeOfIntersections = true;
                for(int p=0; p<numberOfUsablePoints; p++)
                {
                    int pu = usePoints[p];
                    if(pu==a || pu==b || pu==c)
                        continue;
                    
                    if(IntersectsTriangle2(points[a],points[b],points[c],points[pu]))
                    {
                        freeOfIntersections=false;
                        break;
                    }
                }
                
                if(freeOfIntersections)
                {
                    indices.Add(a);
                    indices.Add(b);
                    indices.Add(c);
                    usePoints.Remove(b);
                    it=100;
                    numberOfUsablePoints = usePoints.Count;
                    i--;
                    break;
                }
            }
            it--;
            //if(it<0)
                //break;
        }
        
        indices.Add(usePoints[0]);
        indices.Add(usePoints[1]);
        indices.Add(usePoints[2]);
        indices.Reverse();
        
        return indices.ToArray();
    }
    
    private static bool IntersectsTriangle(Vector2 A, Vector2 B, Vector2 C, Vector2 P)
    {
        bool b1, b2, b3;

        b1 = Sign(P, A, B) < 0.0f;
        b2 = Sign(P, B, C) < 0.0f;
        b3 = Sign(P, C, A) < 0.0f;
        
        return ((b1 == b2) && (b2 == b3));
    }
    
    private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
    }
                    
    private static bool IntersectsTriangle2(Vector2 A, Vector2 B, Vector2 C, Vector2 P)
    {
            float planeAB = (A.x-P.x)*(B.y-P.y)-(B.x-P.x)*(A.y-P.y);
            float planeBC = (B.x-P.x)*(C.y-P.y)-(C.x - P.x)*(B.y-P.y);
            float planeCA = (C.x-P.x)*(A.y-P.y)-(A.x - P.x)*(C.y-P.y);
            return Sign(planeAB)==Sign(planeBC) && Sign(planeBC)==Sign(planeCA);
    }
    
    private static int Sign(float n) 
    {
        return (int)(Mathf.Abs(n)/n);
    }
}