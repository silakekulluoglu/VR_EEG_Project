using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ConfigPageManager : MonoBehaviour
{
    [Header("Config Asset")]
    public ExperimentConfig config;

    [Header("Baseline UI")]
    public TMP_InputField baselineSceneInput;
    public TMP_InputField baselineDurationInput;

    [Header("Stimuli UI")]
    public Transform stimuliContainer;
    public GameObject stimulusRowPrefab;

    [Header("Break UI")]
    public TMP_InputField breakSceneInput;

    [Header("Panel References")]
    public GameObject configPanel;
    public GameObject experimentView;

    [Header("Auto Mode")]
    public Toggle autoModeToggle;
    public Toggle autoBreakToggle;
    public TMP_InputField breakDurationInput;

    private List<StimulusRowUI> rows = new List<StimulusRowUI>();

    void Start()
    {
        baselineSceneInput.text = config.baselineSceneName;
        baselineDurationInput.text = config.baselineDuration.ToString();
        breakSceneInput.text = config.breakSceneName;
    }

    public void OnAddStimulusClicked()
    {
        string label = "Stimulus " + (rows.Count + 1);
        SpawnRow(label, "");
    }

    void SpawnRow(string label, string sceneName, float duration = 0f)
    {
        GameObject go = Instantiate(stimulusRowPrefab, stimuliContainer);
        StimulusRowUI row = go.GetComponent<StimulusRowUI>();
        row.Init(label, sceneName, duration, OnRemoveRow); // ← duration ve OnRemoveRow eklendi
        rows.Add(row);
    }

    void OnRemoveRow(StimulusRowUI row)
    {
        rows.Remove(row);
        Destroy(row.gameObject);
        for (int i = 0; i < rows.Count; i++)
            rows[i].SetLabel("Stimulus " + (i + 1));
    }

    public void OnContinueClicked()
    {
        bool isAutoMode = autoModeToggle.isOn;
        bool isAutoBreak = autoBreakToggle.isOn;
        
        // Geçişe izin veriyor muyuz? Başlangıçta evet diyoruz.
        bool canProceed = true; 
        string errorMessage = "";

        // --- KISITLAMA KONTROLLERİ ---
        if (isAutoMode)
        {
            // 1. Baseline Süresi Zorunlu
            if (string.IsNullOrWhiteSpace(baselineDurationInput.text) || !float.TryParse(baselineDurationInput.text, out _))
            {
                errorMessage += "- Baseline duration is missing or invalid!\n";
                canProceed = false;
            }

            // 2. Tüm Stimulilerin Süreleri Zorunlu
            foreach (var row in rows)
            {
                if (row.GetDuration() <= 0)
                {
                    errorMessage += $"- {row.GetLabel()} Duration is missing or invalid! \n";
                    canProceed = false;
                }
            }

            // 3. Eğer Auto Break de seçiliyse Break Süresi Zorunlu
            if (isAutoBreak)
            {
                if (string.IsNullOrWhiteSpace(breakDurationInput.text) || !float.TryParse(breakDurationInput.text, out _))
                {
                    errorMessage += "- Auto Break is chosen but the Break scene duration is missing or invalid!\n";
                    canProceed = false;
                }
            }
        }

        // --- KARAR AŞAMASI ---
        if (!canProceed)
        {
            // Eğer bir engel varsa, konsola yazdır ve FONKSİYONDAN ÇIK (Panel değişmez)
            Debug.LogError("Experiment could not started: \n" + errorMessage);
            // İstersen buraya UI'da kırmızı bir uyarı yazısı çıkartan kod ekleyebilirsin.
            return; 
        }

        // --- BURADAN AŞAĞISI SADECE HER ŞEY TAMAMSA ÇALIŞIR ---
        SaveConfigAndSwitch();
    }

    private void SaveConfigAndSwitch()
    {
        // 1. UI'daki tüm verileri Config asset'ine kaydet
        config.baselineSceneName = baselineSceneInput.text.Trim();
        float.TryParse(baselineDurationInput.text, out config.baselineDuration);

        config.breakSceneName = breakSceneInput.text.Trim();
        float.TryParse(breakDurationInput.text, out config.breakDuration);

        // EKSİK OLAN KRİTİK KISIM BURASIYDI:
        config.autoMode = autoModeToggle.isOn;
        config.autoBreak = autoBreakToggle.isOn;

        config.stimuli.Clear();
        foreach (var row in rows)
        {
            config.stimuli.Add(new StimulusEntry
            {
                label = row.GetLabel(),
                sceneName = row.GetSceneName(),
                duration = row.GetDuration()
            });
        }

        // 2. Panelleri Değiştir
        configPanel.SetActive(false);
        experimentView.SetActive(true);

        // 3. Auto Mode Aktifse Controller'ı Tetikle
        if (config.autoMode)
        {
            ExperimentController controller = experimentView.GetComponentInChildren<ExperimentController>();
            
            if (controller != null)
            {
                controller.StartAutoMode();
            }
            else
            {
                // Eğer Hierarchy'de bir sorun olursa konsolda görmeni sağlar
                Debug.LogError("ExperimentController bulunamadı! 'experimentView' altında olduğundan emin ol.");
            }
        }
    }
}