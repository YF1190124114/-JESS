using System;
using System.IO;
using UnityEngine;

[Serializable]
public class JessSecretsData
{
    public string aliyunAppKey;
    public string aliyunToken;
    public string qwenApiKey;
}

public static class JessSecrets
{
    private static JessSecretsData data;

    public static JessSecretsData Data
    {
        get
        {
            if (data == null)
                Load();

            return data;
        }
    }

    private static void Load()
    {
        string path;

#if UNITY_EDITOR
path = Path.Combine(
    Directory.GetParent(Application.dataPath).FullName,
    "LocalSecrets",
    "JESS_Secrets.json"
);
#else
path = Path.Combine(
    Application.persistentDataPath,
    "JESS_Secrets.json"
);
#endif

        if (!File.Exists(path))
        {
            Debug.LogError(
                "[Secrets] Missing local secrets file: " + path
            );
            data = new JessSecretsData();
            return;
        }

        string json = File.ReadAllText(path);
        data = JsonUtility.FromJson<JessSecretsData>(json);
    }
}
