using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LNSInterpolation
{


    public static InterpolationVariable CalcuateInterpolationVariables(Vector3 from, Vector3 to, float timeLength)
    {
        InterpolationVariable o = new InterpolationVariable(from,to, timeLength);
       
        return o;
    }

    public static Vector3 InterpolateVector3(InterpolationVariable variable)
    {
        
        return Vector3.Lerp(variable.from, variable.to, Mathf.Clamp01(variable.timer / variable.timeLength));
    }

  

        

    public static Quaternion InterpolateQuaternion(InterpolationVariable variable)
    {
        return Quaternion.Lerp(Quaternion.Euler(variable.from), Quaternion.Euler(variable.to), Mathf.Clamp01(variable.timer / variable.timeLength));
    }

}
[System.Serializable]
public struct InterpolationVariable
{
    public Vector3 from { get; private set; }
    public Vector3 to { get; private set; }
    public float timeLength { get; private set; }
  
    public bool hasValue { get; private set; }

    public float timer { get; private set; }
    public Vector3 velocity { get; set; }
    public InterpolationVariable(Vector3 from,Vector3 to,float timeLength)
    {
        this.hasValue = true;
        this.from = from;
        this.to = to;
        this.timeLength = timeLength;
        this.timer = 0;
        this.velocity = Vector3.zero;
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

