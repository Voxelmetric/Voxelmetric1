using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using UnityEngine;

public class Utils {

    public static void DebugDeserialize(SerializationInfo info, StreamingContext context) {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("info.FullTypeName=" + info.FullTypeName);
        sb.AppendLine("info.MemberCount=" + info.MemberCount);
        var it = info.GetEnumerator();
        while (it.MoveNext()) {
            sb.AppendLine(it.Name + ": " + it.ObjectType.FullName + " = " + it.Value);
        }
        Debug.Log(sb.ToString());
    }

    public static bool HasValue(SerializationInfo info, string name) {
        var it = info.GetEnumerator();
        while (it.MoveNext()) {
            if (it.Name == name)
                return true;
        }
        return false;
    }
}
