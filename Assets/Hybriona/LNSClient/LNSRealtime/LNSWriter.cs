using UnityEngine;
using LiteNetLib.Utils;
public class LNSWriter : NetDataWriter
{

    public void Put(Vector2 vector2)
    {
        base.Put(vector2.x);
        base.Put(vector2.y);
    }

    public void Put(Vector3 vector3)
    {
        base.Put(vector3.x);
        base.Put(vector3.y);
        base.Put(vector3.z);
    }

    public void Put(Vector4 vector4)
    {
        base.Put(vector4.x);
        base.Put(vector4.y);
        base.Put(vector4.z);
        base.Put(vector4.w);
    }

    public void Put(Quaternion quaternion)
    {
        base.Put(quaternion.x);
        base.Put(quaternion.y);
        base.Put(quaternion.z);
        base.Put(quaternion.w);
    }

    public void Put(Color color)
    {
        base.Put(color.r);
        base.Put(color.g);
        base.Put(color.b);
        base.Put(color.a);
    }

   
}
