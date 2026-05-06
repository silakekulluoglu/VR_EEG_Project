using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SceneStats
{
    public string sceneName;
    public List<float> attention    = new List<float>();
    public List<float> relaxation   = new List<float>();
    public List<float> cognitiveLoad = new List<float>();

    public float AvgAttention     => Average(attention);
    public float AvgRelaxation    => Average(relaxation);
    public float AvgCognitiveLoad => Average(cognitiveLoad);

    float Average(List<float> list)
    {
        if (list.Count == 0) return 0f;
        float sum = 0f;
        foreach (var v in list) sum += v;
        return sum / list.Count;
    }
}

public class SessionAnalyzer
{
    public List<SceneStats> sceneStatsList = new List<SceneStats>();
    public SceneStats baselineStats;

    public void Analyze(string mindIndexCsvPath)
    {
        if (!File.Exists(mindIndexCsvPath))
        {
            Debug.LogError("Mind index CSV not found: " + mindIndexCsvPath);
            return;
        }

        List<string> lines = new List<string>();
        using (var fs = new FileStream(mindIndexCsvPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var reader = new StreamReader(fs))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
                lines.Add(line);
        }
        var sceneMap = new Dictionary<string, SceneStats>();

        // header satırını atla
        for (int i = 1; i < lines.Count; i++)
        {
            var parts = lines[i].Split(',');
            if (parts.Length < 8) continue;

            string label = parts[1].Trim();
            if (label == "NONE" || label == "TRANSITION" ||
                label == "DISCONNECTED" || label == "SENSOR_OFF" || label == "NOISE_SIGNAL") continue;

            if (!sceneMap.ContainsKey(label))
                sceneMap[label] = new SceneStats { sceneName = label };

            var stats = sceneMap[label];

            if (float.TryParse(parts[2], out float att))  stats.attention.Add(att);
            if (float.TryParse(parts[3], out float rel))  stats.relaxation.Add(rel);
            if (float.TryParse(parts[7], out float cog))  stats.cognitiveLoad.Add(cog);
        }

        foreach (var kvp in sceneMap)
        {
            sceneStatsList.Add(kvp.Value);
            if (kvp.Key == "BASELINE")
                baselineStats = kvp.Value;
        }
    }
}