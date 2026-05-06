using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SceneStats
{
    public string sceneName;
    public List<float> attention     = new List<float>();
    public List<float> relaxation    = new List<float>();
    public List<float> cognitiveLoad = new List<float>();
    public List<float> asymmetry     = new List<float>();
    public List<float> leftActivity  = new List<float>();
    public List<float> rightActivity = new List<float>();

    // bandChannelData[bandIndex][channelIndex]: 0=delta,1=theta,2=alpha,3=beta,4=gamma
    // channels: AF3=0, AF4=1, Fp1=2, Fp2=3, AF7=4, AF8=5
    public List<float>[][] bandChannelData;

    public float AvgAttention     => Average(attention);
    public float AvgRelaxation    => Average(relaxation);
    public float AvgCognitiveLoad => Average(cognitiveLoad);
    public float AvgAsymmetry     => Average(asymmetry);
    public float AvgLeftActivity  => Average(leftActivity);
    public float AvgRightActivity => Average(rightActivity);

    public float[] AvgBandPerChannel(int band)
    {
        if (bandChannelData == null || bandChannelData[band] == null) return null;
        var result = new float[bandChannelData[band].Length];
        for (int i = 0; i < result.Length; i++)
            result[i] = Average(bandChannelData[band][i]);
        return result;
    }

    float Average(List<float> list)
    {
        if (list == null || list.Count == 0) return 0f;
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

        for (int i = 1; i < lines.Count; i++)
        {
            var parts = lines[i].Split(',');
            if (parts.Length < 8) continue;

            string label = parts[1].Trim();
            if (label == "NONE" || label == "TRANSITION") continue;

            if (!sceneMap.ContainsKey(label))
                sceneMap[label] = new SceneStats { sceneName = label };

            var stats = sceneMap[label];

            if (float.TryParse(parts[2], out float att))  stats.attention.Add(att);
            if (float.TryParse(parts[3], out float rel))  stats.relaxation.Add(rel);
            if (float.TryParse(parts[4], out float asym)) stats.asymmetry.Add(asym);
            if (float.TryParse(parts[5], out float left)) stats.leftActivity.Add(left);
            if (float.TryParse(parts[6], out float right)) stats.rightActivity.Add(right);
            if (float.TryParse(parts[7], out float cog))  stats.cognitiveLoad.Add(cog);
        }

        foreach (var kvp in sceneMap)
        {
            sceneStatsList.Add(kvp.Value);
            if (kvp.Key == "BASELINE")
                baselineStats = kvp.Value;
        }
    }

    public void AnalyzeFeatures(string featureCsvPath)
    {
        if (string.IsNullOrEmpty(featureCsvPath) || !File.Exists(featureCsvPath)) return;

        List<string> lines = new List<string>();
        using (var fs = new FileStream(featureCsvPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var reader = new StreamReader(fs))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
                lines.Add(line);
        }

        var sceneMap = new Dictionary<string, SceneStats>();
        foreach (var stats in sceneStatsList)
            sceneMap[stats.sceneName] = stats;

        for (int i = 1; i < lines.Count; i++)
        {
            var parts = lines[i].Split(',');
            if (parts.Length < 7) continue;

            string label = parts[1].Trim();
            if (label == "NONE" || label == "TRANSITION") continue;
            if (!sceneMap.ContainsKey(label)) continue;

            var stats = sceneMap[label];

            if (stats.bandChannelData == null)
            {
                stats.bandChannelData = new List<float>[5][];
                for (int b = 0; b < 5; b++)
                {
                    stats.bandChannelData[b] = new List<float>[6];
                    for (int c = 0; c < 6; c++)
                        stats.bandChannelData[b][c] = new List<float>();
                }
            }

            for (int b = 0; b < 5; b++)
            {
                if (2 + b >= parts.Length) continue;
                string[] chVals = parts[2 + b].Split(';');
                for (int c = 0; c < 6 && c < chVals.Length; c++)
                {
                    if (float.TryParse(chVals[c].Trim(),
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out float val)
                        && !float.IsNaN(val) && !float.IsInfinity(val))
                    {
                        stats.bandChannelData[b][c].Add(val);
                    }
                }
            }
        }
    }
}
