using System.IO;
using UnityEngine;

public class PersistenceService
{
    private static string PathFor(string key) =>
        Path.Combine(Application.persistentDataPath, key + ".json");

    public void Save<T>(string key, T data)
    {
        var json = JsonUtility.ToJson(data, true);
        File.WriteAllText(PathFor(key), json);
    }

    public T Load<T>(string key) where T : class
    {
        var path = PathFor(key);
        if (!File.Exists(path))
            return null;
        var json = File.ReadAllText(path);
        return JsonUtility.FromJson<T>(json);
    }
}
