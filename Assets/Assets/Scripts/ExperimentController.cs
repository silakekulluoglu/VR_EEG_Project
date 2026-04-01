using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class ExperimentController : MonoBehaviour
{
    [Header("Welcome Environment")]
    public GameObject welcomeEnvironment;

    [Header("Config Asset")]
    public ExperimentConfig config;

    [Header("Controls")]
    public TMP_Dropdown sceneDropdown;
    public Button showSceneButton;
    public Button breakButton;

    [Header("Logging")]
    public string logFileName = "experiment_log";

    public static string CurrentSceneLabel = "NONE";
    private string logPath = "";
    private string currentlyLoadedScene = "";

    void Start()
    {
        CurrentSceneLabel = "NONE";
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

    void SetSceneLayer(string sceneName)
    {
        Scene scene = SceneManager.GetSceneByName(sceneName);
        foreach (GameObject obj in scene.GetRootGameObjects())
            SetLayerRecursively(obj, LayerMask.NameToLayer("VROnly"));
    }

    void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    IEnumerator SwitchScene(string newSceneName, string label)
    {
        if (welcomeEnvironment != null)
            welcomeEnvironment.SetActive(false);

        if (!string.IsNullOrEmpty(currentlyLoadedScene))
        {
            LogEvent(GetCurrentLabel() + "_END");
            CurrentSceneLabel = "TRANSITION"; // ← add this
            yield return SceneManager.UnloadSceneAsync(currentlyLoadedScene);
        }

        yield return SceneManager.LoadSceneAsync(newSceneName, LoadSceneMode.Additive);
        currentlyLoadedScene = newSceneName;

        CenterSceneAroundPlayer(newSceneName);
        SetSceneLayer(newSceneName);
        DisableExtraAudioListeners(newSceneName);

        CurrentSceneLabel = label; // ← add this (e.g. "BASELINE", "STIMULI_1")
        LogEvent(label + "_START");
    }
    
    void CenterSceneAroundPlayer(string sceneName)
    {
        Scene scene = SceneManager.GetSceneByName(sceneName);
        GameObject[] rootObjects = scene.GetRootGameObjects();

        // first, find the camera in the loaded scene
        Vector3 offset = Vector3.zero;
        foreach (GameObject obj in rootObjects)
        {
            Camera cam = obj.GetComponentInChildren<Camera>();
            if (cam != null)
            {
                // offset is the negative of that camera's position
                offset = -cam.transform.position;
                break;
            }
        }

        // then move all root objects by that offset
        foreach (GameObject obj in rootObjects)
        {
            if (obj.GetComponent<Camera>() != null) continue;
            if (obj.GetComponent<Light>() != null) continue;

            obj.transform.position += offset;
        }
    }

    void OnBreakClicked()
    {
        StartCoroutine(HandleBreak("BREAK"));
    }

    IEnumerator HandleBreak(string label)
    {
        if (!string.IsNullOrEmpty(currentlyLoadedScene))
        {
            LogEvent(GetCurrentLabel() + "_END");
            CurrentSceneLabel = label; // ← add this (e.g. "BREAK_1")
            yield return SceneManager.UnloadSceneAsync(currentlyLoadedScene);
            currentlyLoadedScene = "";
        }

        if (!string.IsNullOrEmpty(config.breakSceneName))
        {
            yield return SceneManager.LoadSceneAsync(config.breakSceneName, LoadSceneMode.Additive);
            currentlyLoadedScene = config.breakSceneName;
            CenterSceneAroundPlayer(config.breakSceneName);
            SetSceneLayer(config.breakSceneName);
        }
        else
        {
            if (welcomeEnvironment != null)
                welcomeEnvironment.SetActive(true);
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

    void DisableExtraAudioListeners(string sceneName)
    {
        Scene scene = SceneManager.GetSceneByName(sceneName);
        foreach (GameObject obj in scene.GetRootGameObjects())
        {
            // find and disable any audio listeners in the loaded scene
            AudioListener[] listeners = obj.GetComponentsInChildren<AudioListener>();
            foreach (AudioListener listener in listeners)
                listener.enabled = false;
        }
    }
}

