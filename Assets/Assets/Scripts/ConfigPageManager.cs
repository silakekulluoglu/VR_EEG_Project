using System.Collections.Generic;
using UnityEngine;
using TMPro;

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

    void SpawnRow(string label, string sceneName)
    {
        GameObject go = Instantiate(stimulusRowPrefab, stimuliContainer);
        StimulusRowUI row = go.GetComponent<StimulusRowUI>();
        row.Init(label, sceneName, OnRemoveRow);
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
        config.baselineSceneName = baselineSceneInput.text.Trim();
        config.baselineDuration = float.Parse(baselineDurationInput.text);
        config.breakSceneName = breakSceneInput.text.Trim();
        
        config.stimuli.Clear();
        foreach (var row in rows)
            config.stimuli.Add(new StimulusEntry
            {
                label = row.GetLabel(),
                sceneName = row.GetSceneName()
            });

        configPanel.SetActive(false);
        experimentView.SetActive(true);
    }
}