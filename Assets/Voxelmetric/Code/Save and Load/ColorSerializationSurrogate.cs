using System.Runtime.Serialization;
using UnityEngine;

public class ColorSerializationSurrogate : ISerializationSurrogate
{

    public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
    {
        Color color = (Color)obj;
        info.AddValue("r", color.r);
        info.AddValue("g", color.g);
        info.AddValue("b", color.b);
        info.AddValue("a", color.a);
    }

    public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
    {
        Color color = (Color)obj;
        color.r = info.GetSingle("r");
        color.g = info.GetSingle("g");
        color.b = info.GetSingle("b");
        color.a = info.GetSingle("a");
        return null;
    }
}
