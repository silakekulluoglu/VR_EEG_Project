using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System;

public class ExperimentManager : MonoBehaviour
{
    [Header("General Settings")]
    public string folderName = "eeg-analytics";
    public string currentLabel = "REST"; 
    
    public bool isLoggingActive = false; // Start Screen'de beklerken kayıt almaz

    [Header("Sensor Warning Panels (CanvasGroup)")]
    public CanvasGroup panelDisconnected;   
    public CanvasGroup panelSensorContact;  
    public CanvasGroup panelNoise;          

    [Header("Input Files")]
    public string featureSourceFile = "eegFeatureDataLogs.txt"; 
    public string mindSourceFile = "MindIndexDataLogs.txt";     

    [Header("Output File")]
    public string outputFileName = "Labeled_All_Data.txt"; 

    [Header("Cognitive Load Settings")]
    public float clTimeWindow = 5.0f; 

    [Header("UI References")]
    public Slider clBar;
    public TMP_Text clValueText;
    public TMP_Text statusText;
    public Image btnBaselineImage; 
    public Image btnStimuliImage;

    private string mainFolderPath;
    private List<float[]> clBuffer = new List<float[]>(); 
    private float latestCognitiveLoad = 0f; 
    private StreamWriter finalWriter;

    void Start()
    {
        mainFolderPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), folderName);
        if (!Directory.Exists(mainFolderPath)) Directory.CreateDirectory(mainFolderPath);

        string outputPath = Path.Combine(mainFolderPath, outputFileName);
        bool fileExists = File.Exists(outputPath);
        
        finalWriter = new StreamWriter(outputPath, true);
        
        // --- DEĞİŞİKLİK 1: BAŞLIK SIRALAMASI ---
        if (!fileExists) 
        { 
            // Label'ı Timestamp'ten hemen sonraya aldık
            finalWriter.WriteLine("Timestamp,Label,Attention,Relaxation,Asymmetry,LeftActivity,RightActivity,Cognitive_Load"); 
            finalWriter.Flush(); 
        }

        UpdateStatusUI();
        StartCoroutine(CalculateCognitiveLoadRoutine()); 
        StartCoroutine(ProcessAndLogRoutine()); 
    }

    public void StartExperimentLogging()
    {
        isLoggingActive = true;
        Debug.Log("Deney Başladı - Kayıt Aktif!");
    }

    public void ToggleBaseline()
    {
        if (currentLabel == "BASELINE") SetLabel("REST");
        else SetLabel("BASELINE");
    }

    public void ToggleStimuli()
    {
        if (currentLabel == "STIMULI") SetLabel("REST");
        else SetLabel("STIMULI");
    }

    void SetLabel(string newLabel)
    {
        currentLabel = newLabel;
        UpdateStatusUI();
    }

    void UpdateStatusUI()
    {
        if (statusText != null)
        {
            statusText.text = "State: " + currentLabel;
            if (currentLabel == "REST") statusText.color = Color.white;
            else if (currentLabel == "BASELINE") statusText.color = Color.yellow;
            else if (currentLabel == "STIMULI") statusText.color = Color.cyan;
        }
        if (btnBaselineImage != null) btnBaselineImage.color = (currentLabel == "BASELINE") ? Color.green : Color.white;
        if (btnStimuliImage != null) btnStimuliImage.color = (currentLabel == "STIMULI") ? Color.green : Color.white;
    }

    string GetCurrentEffectiveLabel()
    {
        if (panelDisconnected != null && panelDisconnected.alpha > 0.1f) return "DEVICE_DISCONNECTED"; 
        if (panelSensorContact != null && panelSensorContact.alpha > 0.1f) return "SENSOR_CONTACT_LOST";
        if (panelNoise != null && panelNoise.alpha > 0.1f) return "NOISY_DATA";
        return currentLabel;
    }

    IEnumerator ProcessAndLogRoutine()
    {
        string path = Path.Combine(mainFolderPath, mindSourceFile);
        while (!File.Exists(path)) yield return new WaitForSeconds(1f);
        FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        StreamReader reader = new StreamReader(fs);
        reader.BaseStream.Seek(0, SeekOrigin.End);
        while (true)
        {
            if (reader.BaseStream.Length > reader.BaseStream.Position)
            {
                string line = reader.ReadLine();
                if (!string.IsNullOrEmpty(line) && isLoggingActive)
                {
                    string[] parts = line.Split(',');
                    if (parts.Length > 1)
                    {
                        string correctTime = DateTime.Now.ToString("MM/dd/yyyy h:mm:ss tt", System.Globalization.CultureInfo.InvariantCulture);
                        
                        // Diğer veriler (Attention, Relaxation vb.)
                        string restOfData = string.Join(",", parts, 1, parts.Length - 1);
                        
                        string clString = latestCognitiveLoad.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                        string labelToWrite = GetCurrentEffectiveLabel();

                        // --- DEĞİŞİKLİK 2: VERİ YAZMA SIRASI ---
                        // Zaman, LABEL, Diğer Veriler, Cognitive Load
                        finalWriter.WriteLine($"{correctTime},{labelToWrite},{restOfData},{clString}");
                        finalWriter.Flush();
                    }
                }
            }
            yield return null;
        }
    }

    IEnumerator CalculateCognitiveLoadRoutine()
    {
        string path = Path.Combine(mainFolderPath, featureSourceFile);
        while (!File.Exists(path)) yield return new WaitForSeconds(1f);
        FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        StreamReader reader = new StreamReader(fs);
        reader.BaseStream.Seek(0, SeekOrigin.End);
        while (true)
        {
            if (reader.BaseStream.Length > reader.BaseStream.Position)
            {
                string line = reader.ReadLine();
                if (!string.IsNullOrEmpty(line))
                {
                    try
                    {
                        string[] parts = line.Split(',');
                        if (parts.Length >= 7)
                        {
                            float t = Time.time;
                            float theta = Mathf.Abs(float.Parse(parts[3], System.Globalization.CultureInfo.InvariantCulture));
                            float alpha = Mathf.Abs(float.Parse(parts[4], System.Globalization.CultureInfo.InvariantCulture));
                            clBuffer.Add(new float[] { t, theta, alpha });
                            while (clBuffer.Count > 0 && (t - clBuffer[0][0] > clTimeWindow)) clBuffer.RemoveAt(0);
                            float sumTheta = 0, sumAlpha = 0;
                            foreach (var d in clBuffer) { sumTheta += d[1]; sumAlpha += d[2]; }
                            if (sumAlpha < 0.0001f) sumAlpha = 0.0001f;
                            latestCognitiveLoad = sumTheta / sumAlpha;
                            UpdateUI();
                        }
                    } catch { }
                }
            }
            yield return null;
        }
    }

    void UpdateUI()
    {
        if (float.IsNaN(latestCognitiveLoad)) latestCognitiveLoad = 0;
        if (clBar != null) clBar.value = Mathf.Lerp(clBar.value, latestCognitiveLoad, Time.deltaTime * 5f);
        if (clValueText != null) clValueText.text = $"CL: {latestCognitiveLoad:F2}";
    }

    void OnDestroy()
    {
        if (finalWriter != null) { finalWriter.Flush(); finalWriter.Close(); }
    }
}