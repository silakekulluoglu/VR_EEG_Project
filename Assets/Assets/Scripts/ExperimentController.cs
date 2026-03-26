using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class ExperimentController : MonoBehaviour
{
    [Header("Config Asset")]
    public ExperimentConfig config;

    [Header("Controls")]
    public TMP_Dropdown sceneDropdown;
    public Button showSceneButton;
    public Button breakButton;

    [Header("Logging")]
    public string logFileName = "experiment_log";

    private string logPath = "";
    private int breakCount = 0;
    private string currentlyLoadedScene = "";

    void Start()
    {
        logPath = Application.persistentDataPath + "/" + logFileName + "_" +
                  System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".csv";
        System.IO.File.WriteAllText(logPath, "timestamp,event\n");

        BuildDropdown();

        showSceneButton.onClick.AddListener(OnShowSceneClicked);
        breakButton.onClick.AddListener(OnBreakClicked);
    }

    void BuildDropdown()
    {
        sceneDropdown.ClearOptions();
        List<string> options = new List<string>();
        options.Add("Baseline - " + config.baselineSceneName);
        foreach (var stimulus in config.stimuli)
            options.Add(stimulus.label + " - " + stimulus.sceneName);
        sceneDropdown.AddOptions(options);
    }

    void OnShowSceneClicked()
    {
        int index = sceneDropdown.value;
        string sceneName;
        string label;

        if (index == 0)
        {
            sceneName = config.baselineSceneName;
            label = "BASELINE";
        }
        else
        {
            var stimulus = config.stimuli[index - 1];
            sceneName = stimulus.sceneName;
            label = "STIMULI_" + index;
        }

        StartCoroutine(SwitchScene(sceneName, label));
    }

    IEnumerator SwitchScene(string newSceneName, string label)
    {
        // unload current scene if one is loaded
        if (!string.IsNullOrEmpty(currentlyLoadedScene))
        {
            LogEvent(GetCurrentLabel() + "_END");
            yield return SceneManager.UnloadSceneAsync(currentlyLoadedScene);
        }

        // load the new scene on top of main scene
        yield return SceneManager.LoadSceneAsync(newSceneName, LoadSceneMode.Additive);
        currentlyLoadedScene = newSceneName;

        LogEvent(label + "_START");
        Debug.Log("Switched to: " + newSceneName);
    }

    void OnBreakClicked()
    {
        breakCount++;
        string label = "BREAK_" + breakCount;

        if (!string.IsNullOrEmpty(currentlyLoadedScene))
        {
            LogEvent(GetCurrentLabel() + "_END");
            StartCoroutine(UnloadCurrent());
        }

        LogEvent(label);
        Debug.Log("Break: " + label);
    }

    IEnumerator UnloadCurrent()
    {
        yield return SceneManager.UnloadSceneAsync(currentlyLoadedScene);
        currentlyLoadedScene = "";
    }

    string GetCurrentLabel()
    {
        if (currentlyLoadedScene == config.baselineSceneName)
            return "BASELINE";
        for (int i = 0; i < config.stimuli.Count; i++)
            if (config.stimuli[i].sceneName == currentlyLoadedScene)
                return "STIMULI_" + (i + 1);
        return "UNKNOWN";
    }

    void LogEvent(string label)
    {
        string line = System.DateTime.Now.ToString("HH:mm:ss.fff") + "," + label;
        System.IO.File.AppendAllText(logPath, line + "\n");
        Debug.Log("Logged: " + line);
    }
}
