using System.Text.Json;

namespace LocalMimu.Models;

public static class Deser
{
    public static T? DeserJson<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return default;
        }
        return JsonSerializer.Deserialize<T>(json);
    }
    public static string SerJson<T>(T? text)
    {
        if(text != null)
        {
           return JsonSerializer.Serialize(text); 
        }
        else
        {
            return "";
        }
        
    }
}