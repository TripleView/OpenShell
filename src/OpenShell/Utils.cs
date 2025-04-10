using System.Collections.Generic;
using Newtonsoft.Json;

namespace OpenShell;

public static class Utils
{
    public static T Clone<T>(this T t)
    {
       return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(t));
    }

    public static string StringJoin(this List<string> list,char separator=',')
    {
        return string.Join(separator, list);
    }
}