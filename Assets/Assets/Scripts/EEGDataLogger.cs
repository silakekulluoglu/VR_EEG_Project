using System;
using System.IO;
using UnityEngine;
using Looxid.Link;

public class EEGDataLogger : MonoBehaviour
{
    private StreamWriter rawDataLogger;
    private StreamWriter featureDataLogger;
    private StreamWriter mindIndexLogger;

    private string basePath;

    private float cachedTheta = 0f;
    private float cachedAlpha = 0f;

    public string MindIndexPath { get; private set; }
    public string FeatureDataPath { get; private set; }

    void Start()
    {
        // save to Documents/eeg-analytics
        basePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Personal),
            "eeg-analytics"
        );

        if (!Directory.Exists(basePath))
            Directory.CreateDirectory(basePath);

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

        // open log files
        rawDataLogger = new StreamWriter(
            Path.Combine(basePath, "eegRawData_" + timestamp + ".csv"), true);
        rawDataLogger.AutoFlush = true;
        rawDataLogger.WriteLine("timestamp,scene_label,seq_num,ch_data");

        FeatureDataPath = Path.Combine(basePath, "eegFeatureData_" + timestamp + ".csv");
        featureDataLogger = new StreamWriter(FeatureDataPath, true);
        featureDataLogger.AutoFlush = true;
        featureDataLogger.WriteLine("timestamp,scene_label,delta,theta,alpha,beta,gamma");

        MindIndexPath = Path.Combine(basePath, "mindIndex_" + timestamp + ".csv");
        mindIndexLogger = new StreamWriter(MindIndexPath, true);
        mindIndexLogger.AutoFlush = true;
        mindIndexLogger.WriteLine("timestamp,scene_label,attention,relaxation,asymmetry,leftActivity,rightActivity, cognitive_load");

        // subscribe to NetworkManager events
        NetworkManager.OnNetworkReceiveEEGRawSignals += OnRawSignals;
        LooxidLinkData.OnReceiveEEGFeatureIndexes += OnFeatureIndexes;
        NetworkManager.OnNetworkReceiveMindIndexes += OnMindIndex;
    }

    public static float CurrentCognitiveLoad { get; private set; } = 0f;

    float ComputeCognitiveLoad()
    {
        if (cachedAlpha <= 0.0001f) return 0f; // sıfıra çok yakınsa böl
        return cachedTheta / cachedAlpha;
    }

    void OnRawSignals(EEGRawSignal signal)
    {
        string label = ExperimentController.CurrentSceneLabel;
        foreach (var data in signal.rawSignal)
        {
            string chData = string.Join(";", data.ch_data);
            rawDataLogger.WriteLine(
                $"{FormatTimestamp(data.timestamp)},{label},{data.seq_num},{chData}");
        }
    }

    void OnFeatureIndexes(EEGFeatureIndex featureIndex)
    {
        Debug.Log("OnFeatureIndexes called");
        string delta = "";
        string theta = "";
        string alpha = "";
        string beta  = "";
        string gamma = "";

        foreach (EEGSensorID sensorID in Enum.GetValues(typeof(EEGSensorID)))
        {
            delta += featureIndex.Delta(sensorID) + ";";
            theta += featureIndex.Theta(sensorID) + ";";
            alpha += featureIndex.Alpha(sensorID) + ";";
            beta  += featureIndex.Beta(sensorID)  + ";";
            gamma += featureIndex.Gamma(sensorID) + ";";
        }

        featureDataLogger.WriteLine(
            $"{FormatTimestamp(featureIndex.timestamp)},{ExperimentController.CurrentSceneLabel},{delta},{theta},{alpha},{beta},{gamma}");

        // cognitive load için cache güncelle
        EEGSensorID[] channels = {
        EEGSensorID.AF3, EEGSensorID.AF4,
        EEGSensorID.Fp1, EEGSensorID.Fp2,
        EEGSensorID.AF7, EEGSensorID.AF8
    };

    float t = 0f, a = 0f;
    int validCount = 0;

    foreach (var ch in channels)
    {
        double tVal = featureIndex.Theta(ch);
        double aVal = featureIndex.Alpha(ch);

        Debug.Log($"{ch} Theta={tVal}, Alpha={aVal}");

        if (!double.IsNaN(tVal) && !double.IsInfinity(tVal) &&
            !double.IsNaN(aVal) && !double.IsInfinity(aVal))
        {
            t += (float)tVal;
            a += (float)aVal;
            validCount++;
        }
    }

    Debug.Log($"validCount={validCount}, cachedTheta={t}, cachedAlpha={a}");

    if (validCount > 0)
    {
        cachedTheta = Mathf.Abs(t / validCount);
        cachedAlpha = Mathf.Abs(a / validCount);
    }
    }

    void OnMindIndex(MindIndex mindIndex)
    {
        float cognitiveLoad = ComputeCognitiveLoad(); 
        CurrentCognitiveLoad = cognitiveLoad;

        mindIndexLogger.WriteLine(
            $"{FormatTimestamp(mindIndex.timestamp)}," +
            $"{ExperimentController.CurrentSceneLabel}," +
            $"{mindIndex.attention}," +
            $"{mindIndex.relaxation}," +
            $"{mindIndex.asymmetry}," +
            $"{mindIndex.leftActivity}," +
            $"{mindIndex.rightActivity}," +
            $"{cognitiveLoad}");
    }
    void OnDisable()
    {
        // unsubscribe when done
        NetworkManager.OnNetworkReceiveEEGRawSignals -= OnRawSignals;
         LooxidLinkData.OnReceiveEEGFeatureIndexes -= OnFeatureIndexes;
        NetworkManager.OnNetworkReceiveMindIndexes -= OnMindIndex;
    }

    void OnDestroy()
    {
        if (rawDataLogger != null)   rawDataLogger.Close();
        if (featureDataLogger != null) featureDataLogger.Close();
        if (mindIndexLogger != null) mindIndexLogger.Close();
    }

    string FormatTimestamp(double unixTimestamp)
    {
        DateTimeOffset dt = DateTimeOffset.FromUnixTimeMilliseconds((long)(unixTimestamp * 1000));
        return dt.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");
    }

    public void StopLogging()
    {
        NetworkManager.OnNetworkReceiveEEGRawSignals -= OnRawSignals;
        LooxidLinkData.OnReceiveEEGFeatureIndexes -= OnFeatureIndexes;
        NetworkManager.OnNetworkReceiveMindIndexes -= OnMindIndex;

        if (rawDataLogger != null)   { rawDataLogger.Close();   rawDataLogger = null; }
        if (featureDataLogger != null) { featureDataLogger.Close(); featureDataLogger = null; }
        if (mindIndexLogger != null) { mindIndexLogger.Close(); mindIndexLogger = null; }

        Debug.Log("Logging stopped.");
    }
}