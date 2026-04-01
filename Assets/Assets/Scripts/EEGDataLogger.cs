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

        featureDataLogger = new StreamWriter(
            Path.Combine(basePath, "eegFeatureData_" + timestamp + ".csv"), true);
        featureDataLogger.AutoFlush = true;
        featureDataLogger.WriteLine("timestamp,scene_label,delta,theta,alpha,beta,gamma");

        mindIndexLogger = new StreamWriter(
            Path.Combine(basePath, "mindIndex_" + timestamp + ".csv"), true);
        mindIndexLogger.AutoFlush = true;
        mindIndexLogger.WriteLine("timestamp,scene_label,attention,relaxation,asymmetry,leftActivity,rightActivity");

        // subscribe to NetworkManager events
        NetworkManager.OnNetworkReceiveEEGRawSignals += OnRawSignals;
        NetworkManager.OnNetworkReceiveEEGFeatureIndexes += OnFeatureIndexes;
        NetworkManager.OnNetworkReceiveMindIndexes += OnMindIndex;
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
        string label = ExperimentController.CurrentSceneLabel;
        string delta = "", theta = "", alpha = "", beta = "", gamma = "";
        foreach (EEGSensorID sensorID in Enum.GetValues(typeof(EEGSensorID)))
        {
            delta += featureIndex.Delta(sensorID) + ";";
            theta += featureIndex.Theta(sensorID) + ";";
            alpha += featureIndex.Alpha(sensorID) + ";";
            beta  += featureIndex.Beta(sensorID)  + ";";
            gamma += featureIndex.Gamma(sensorID) + ";";
        }
        featureDataLogger.WriteLine(
            $"{FormatTimestamp(featureIndex.timestamp)},{label},{delta},{theta},{alpha},{beta},{gamma}");
    }

    void OnMindIndex(MindIndex mindIndex)
    {
        string label = ExperimentController.CurrentSceneLabel;
        mindIndexLogger.WriteLine(
            $"{FormatTimestamp(mindIndex.timestamp)}," +
            $"{label}," +
            $"{mindIndex.attention}," +
            $"{mindIndex.relaxation}," +
            $"{mindIndex.asymmetry}," +
            $"{mindIndex.leftActivity}," +
            $"{mindIndex.rightActivity}");
    }

    void OnDisable()
    {
        // unsubscribe when done
        NetworkManager.OnNetworkReceiveEEGRawSignals -= OnRawSignals;
        NetworkManager.OnNetworkReceiveEEGFeatureIndexes -= OnFeatureIndexes;
        NetworkManager.OnNetworkReceiveMindIndexes -= OnMindIndex;
    }

    void OnDestroy()
    {
        // close all files safely
        if (rawDataLogger != null) rawDataLogger.Close();
        if (featureDataLogger != null) featureDataLogger.Close();
        if (mindIndexLogger != null) mindIndexLogger.Close();
    }

    string FormatTimestamp(double unixTimestamp)
    {
        DateTimeOffset dt = DateTimeOffset.FromUnixTimeMilliseconds((long)(unixTimestamp * 1000));
        return dt.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");
    }
}