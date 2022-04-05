using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LNSInterpolation
{


    public static InterpolationVariable CalcuateInterpolationVariables(Vector3 from, Vector3 to, float time)
    {
        InterpolationVariable o = new InterpolationVariable();
        o.from = from;
        o.to = to;
        o.timeLength = time;
  
        o.velocity = (to - from) / time;
        o.timer = 0;
        return o;
    }
    public static Vector3 InterpolateVector3(Vector3 from, Vector3 to, float speed)
    {
        return from + (to - from) * speed;
    }

    public static Vector3 InterpolateEulers(Vector3 from, Vector3 to)
    {
        return to;
    }

}
[System.Serializable]
public struct InterpolationVariable
{
    public Vector3 from;
    public Vector3 to;
    public float timeLength;
    public Vector3 velocity;
    public float timer;
}
