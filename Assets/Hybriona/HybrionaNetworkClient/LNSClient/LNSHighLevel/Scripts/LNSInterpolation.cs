using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LNSInterpolation
{


    public static InterpolationVariable CalcuateInterpolationVariables(Vector3 from, Vector3 to, float time)
    {
        InterpolationVariable o = new InterpolationVariable(from,to,time,(to-from)/time);
       
        return o;
    }

    public static Vector3 InterpolateVector3(InterpolationVariable variable)
    {
        return variable.from + (variable.to - variable.from) * Mathf.Clamp01( variable.timer /variable.timeLength);
    }

    public static Vector3 InterpolateEulers(Vector3 from, Vector3 to)
    {
        return to;
    }

}
[System.Serializable]
public struct InterpolationVariable
{
    public Vector3 from { get; private set; }
    public Vector3 to { get; private set; }
    public float timeLength { get; private set; }
    public Vector3 velocity { get; private set; }
    public float speed { get; private set; }
    public bool hasValue { get; private set; }

    public float timer { get; private set; }

    public InterpolationVariable(Vector3 from,Vector3 to,float timeLength,Vector3 velocity)
    {
        this.hasValue = true;
        this.from = from;
        this.to = to;
        this.timeLength = timeLength;
        this.velocity = velocity;
        this.speed = velocity.magnitude;
        this.timer = 0;
    }

    public void ResetTimer()
    {
        this.timer = 0;
    }

    public void UpdateTimer(float delta)
    {
        this.timer += delta;
    }

    public void UpdateTimer()
    {
        this.timer += Time.deltaTime;
    }
}
