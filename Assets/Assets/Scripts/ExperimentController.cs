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
    public Button continueButton;

    [Header("Logging")]
    public string logFileName = "experiment_log";

    [Header("Auto Mode UI")]
    public TextMeshProUGUI timerText;

    public static string CurrentSceneLabel = "NONE";
    private string logPath = "";
    private string currentlyLoadedScene = "";

    private bool isSwitching = false;
    private Coroutine autoCoroutine;
    private bool continuePressed = false;

    void Start()
    {
        CurrentSceneLabel = "NONE";
        logPath = Application.persistentDataPath + "/" + logFileName + "_" +
                  System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".csv";
        System.IO.File.WriteAllText(logPath, "timestamp,event\n");

        BuildDropdown();

        showSceneButton.onClick.AddListener(OnShowSceneClicked);
        breakButton.onClick.AddListener(OnBreakClicked);
        continueButton.onClick.AddListener(OnContinueAutoClicked);

        continueButton.gameObject.SetActive(false);

        if (timerText != null)
            timerText.gameObject.SetActive(config.autoMode);

        if (config.autoMode)
            autoCoroutine = StartCoroutine(AutoSequence());
    }

    public void BuildDropdown()
    {
        sceneDropdown.ClearOptions();
        List<string> options = new List<string>();
        options.Add("Baseline - " + config.baselineSceneName);
        foreach (var stimulus in config.stimuli)
            options.Add(stimulus.label + " - " + stimulus.sceneName);
        sceneDropdown.AddOptions(options);
    }
    IEnumerator AutoSequence()
    {
        // Baseline
        yield return StartCoroutine(SwitchScene(config.baselineSceneName, "BASELINE"));
        yield return StartCoroutine(CountDown(config.baselineDuration));

        for (int i = 0; i < config.stimuli.Count; i++)
        {
            // süre doldu → break
            yield return StartCoroutine(HandleBreak("BREAK"));

            // experimenter continue a basana kadar bekle
            continuePressed = false;
            continueButton.gameObject.SetActive(true);
            if (timerText != null) timerText.text = "Waiting...";
            yield return new WaitUntil(() => continuePressed);
            continueButton.gameObject.SetActive(false);

            // sıradaki stimulus
            var stimulus = config.stimuli[i];
            yield return StartCoroutine(SwitchScene(stimulus.sceneName, "STIMULI_" + (i + 1)));
            yield return StartCoroutine(CountDown(stimulus.duration));
        }

        // son stimulustan sonra da break
        yield return StartCoroutine(HandleBreak("BREAK"));
        continueButton.gameObject.SetActive(false);

        if (timerText != null) timerText.text = "Sequence complete";
        Debug.Log("Auto sequence finished.");
    }

    IEnumerator CountDown(float duration)
    {
        float remaining = duration;
        while (remaining > 0f)
        {
            if (timerText != null)
            {
                int minutes = Mathf.FloorToInt(remaining / 60f);
                int seconds = Mathf.FloorToInt(remaining % 60f);
                timerText.text = $"{minutes:00}:{seconds:00} remaining";
            }
            yield return null;
            remaining -= Time.deltaTime;
        }
    }

    public void OnContinueAutoClicked()
    {
        continuePressed = true;
    }

    void OnShowSceneClicked()
    {
        if (isSwitching) return; // ← işlem varsa yeni isteği yoksay

        if (autoCoroutine != null)
        {
            StopCoroutine(autoCoroutine);
            autoCoroutine = null;
            continueButton.gameObject.SetActive(false);
            if (timerText != null) timerText.text = "Manual mode";
        }
        
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
        isSwitching = true;
        showSceneButton.interactable = false; // ← butonu kapat
        breakButton.interactable = false;

        if (welcomeEnvironment != null)
            welcomeEnvironment.SetActive(false);

        if (!string.IsNullOrEmpty(currentlyLoadedScene))
        {
            LogEvent(GetCurrentLabel() + "_END");
            CurrentSceneLabel = "TRANSITION";
            yield return SceneManager.UnloadSceneAsync(currentlyLoadedScene);
        }

        yield return SceneManager.LoadSceneAsync(newSceneName, LoadSceneMode.Additive);
        currentlyLoadedScene = newSceneName;

        CenterSceneAroundPlayer(newSceneName);
        SetSceneLayer(newSceneName);
        DisableExtraSceneComponents(newSceneName);

        CurrentSceneLabel = label;
        LogEvent(label + "_START");

        isSwitching = false;
        showSceneButton.interactable = true; // ← butonu aç
        breakButton.interactable = true;
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
        if (autoCoroutine != null)
        {
            StopCoroutine(autoCoroutine);
            autoCoroutine = null;
            continueButton.gameObject.SetActive(false);
            if (timerText != null) timerText.text = "Manual mode";
        }

        StartCoroutine(HandleBreak("BREAK"));
    }

    IEnumerator HandleBreak(string label)
    {
        isSwitching = true;
        showSceneButton.interactable = false;
        breakButton.interactable = false;

        if (!string.IsNullOrEmpty(currentlyLoadedScene))
        {
            LogEvent(GetCurrentLabel() + "_END");
            CurrentSceneLabel = label;
            yield return SceneManager.UnloadSceneAsync(currentlyLoadedScene);
            currentlyLoadedScene = "";
        }

        if (!string.IsNullOrEmpty(config.breakSceneName))
        {
            yield return SceneManager.LoadSceneAsync(config.breakSceneName, LoadSceneMode.Additive);
            currentlyLoadedScene = config.breakSceneName;
            CenterSceneAroundPlayer(config.breakSceneName);
            SetSceneLayer(config.breakSceneName);
            DisableExtraSceneComponents(config.breakSceneName);
        }
        else
        {
            if (welcomeEnvironment != null)
                welcomeEnvironment.SetActive(true);
        }

        LogEvent(label);
        Debug.Log("Break: " + label);

        isSwitching = false;
        showSceneButton.interactable = true;
        breakButton.interactable = true;
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

    void DisableExtraSceneComponents(string sceneName)
    {
        Scene scene = SceneManager.GetSceneByName(sceneName);
        foreach (GameObject obj in scene.GetRootGameObjects())
        {
            // disable extra cameras
            Camera[] cameras = obj.GetComponentsInChildren<Camera>();
            foreach (Camera cam in cameras)
                cam.enabled = false;

            // disable extra audio listeners
            AudioListener[] listeners = obj.GetComponentsInChildren<AudioListener>();
            foreach (AudioListener listener in listeners)
                listener.enabled = false;

            // disable extra lights
            Light[] lights = obj.GetComponentsInChildren<Light>();
            foreach (Light light in lights)
                light.enabled = false;
        }
    }

    public void StartAutoMode()
    {
        if (timerText != null)
            timerText.gameObject.SetActive(true);
            
        autoCoroutine = StartCoroutine(AutoSequence());
    }
}

