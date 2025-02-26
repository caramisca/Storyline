using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;  // Using UnityEngine for JsonUtility

public static class MiniJson
{
    public static object Deserialize(string json)
    {
        if (string.IsNullOrEmpty(json)) return null;

        try
        {
            return JsonParser.Parse(json);
        }
        catch (Exception e)
        {
            Debug.LogError("JSON Deserialization Error: " + e.Message);
            return null;
        }
    }

    public static string Serialize(object obj)
    {
        try
        {
            return JsonSerializer.Serialize(obj);
        }
        catch (Exception e)
        {
            Debug.LogError("JSON Serialization Error: " + e.Message);
            return "";
        }
    }

    sealed class JsonParser
    {
        public static object Parse(string json)
        {
            try
            {
                return JsonUtility.FromJson<Dictionary<string, object>>(json);
            }
            catch (Exception)
            {
                return JsonUtility.FromJson<List<object>>(json);
            }
        }
    }

    sealed class JsonSerializer
    {
        public static string Serialize(object obj)
        {
            return JsonUtility.ToJson(obj);
        }
    }
}
