using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "EEG Toolkit/Experiment Config")]
public class ExperimentConfig : ScriptableObject
{
    public string baselineSceneName;
    public float baselineDuration = 180f;
    public string breakSceneName = ""; // optional, leave empty for default
    public float breakDuration = 30f;  
    public bool autoMode = false;      
    public bool autoBreak = false; // ← yeni
    public List<StimulusEntry> stimuli = new List<StimulusEntry>();
}

[System.Serializable]
public class StimulusEntry
{
    public string label;
    public string sceneName;
    public float duration = 60f; 
}