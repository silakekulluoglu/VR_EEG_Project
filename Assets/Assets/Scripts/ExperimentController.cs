using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using System.IO;


public class ExperimentController : MonoBehaviour
{
    [Header("VR Player")]
    public Transform xrOrigin;

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

    [Header("Auto Mode")]
    public Button startCycleButton; // ← yeni

    [Header("Auto Mode UI")]
    public TextMeshProUGUI timerText;

    [Header("Finish")]
    public Button finishButton;

    private string sessionTimestamp;
    private string mindIndexCsvPath;

    public static string CurrentSceneLabel = "NONE";
    private string logPath = "";
    private string currentlyLoadedScene = "";


    private bool isSwitching = false;
    private Coroutine autoCoroutine;
    private bool continuePressed = false;

    void Start()
    {
        CurrentSceneLabel = "NONE";

        if (welcomeEnvironment != null)
            welcomeEnvironment.SetActive(true);

        if (timerText != null)
            timerText.gameObject.SetActive(config.autoMode);

        continueButton.gameObject.SetActive(false);
        if (finishButton != null)
        {
            finishButton.gameObject.SetActive(!config.autoMode);
        }

        // start cycle butonu sadece auto modda görünsün
        if (startCycleButton != null)
        {
            startCycleButton.gameObject.SetActive(config.autoMode);
            startCycleButton.onClick.RemoveAllListeners();
            startCycleButton.onClick.AddListener(OnStartCycleClicked);
        }

        logPath = Application.persistentDataPath + "/" + logFileName + "_" +
                  System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".csv";
        System.IO.File.WriteAllText(logPath, "timestamp,event\n");

        BuildDropdown();

        finishButton.onClick.RemoveAllListeners();
        finishButton.onClick.AddListener(OnFinishClicked);

        showSceneButton.onClick.RemoveAllListeners();
        showSceneButton.onClick.AddListener(OnShowSceneClicked);

        breakButton.onClick.RemoveAllListeners();
        breakButton.onClick.AddListener(OnBreakClicked);

        continueButton.onClick.RemoveAllListeners();
        continueButton.onClick.AddListener(OnContinueAutoClicked);

        sessionTimestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        Debug.Log("ExperimentController Start() called — frame: " + Time.frameCount);

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
        if (sceneDropdown != null) sceneDropdown.value = 0;
        yield return StartCoroutine(SwitchScene(config.baselineSceneName, "BASELINE"));
        yield return StartCoroutine(CountDown(config.baselineDuration, "Baseline"));

        for (int i = 0; i < config.stimuli.Count; i++)
        {
            // break
            yield return StartCoroutine(HandleBreak("BREAK"));

            if (config.autoBreak)
            {
                // otomatik break — süre dolar, devam eder
                yield return StartCoroutine(CountDown(config.breakDuration, "Break"));
            }
            else
            {
                // manuel break — experimenter continue a basana kadar bekle
                continuePressed = false;
                continueButton.gameObject.SetActive(true);
                if (timerText != null) timerText.text = "Break — press Continue when ready";
                yield return new WaitUntil(() => continuePressed);
                continueButton.gameObject.SetActive(false);
            }

            var stimulus = config.stimuli[i];
            if (sceneDropdown != null) sceneDropdown.value = i + 1;
            yield return StartCoroutine(SwitchScene(stimulus.sceneName, stimulus.sceneName));
            yield return StartCoroutine(CountDown(stimulus.duration, "Stimuli " + (i + 1)));
        }

        // sekans bitti
        if (!string.IsNullOrEmpty(currentlyLoadedScene))
        {
            LogEvent(GetCurrentLabel() + "_END");
            CurrentSceneLabel = "NONE";
            yield return SceneManager.UnloadSceneAsync(currentlyLoadedScene);
            currentlyLoadedScene = "";
        }

        if (welcomeEnvironment != null)
            welcomeEnvironment.SetActive(true);

        if (timerText != null) timerText.text = "Sequence complete";
        if (finishButton != null) finishButton.gameObject.SetActive(true);
    }

    IEnumerator CountDown(float duration, string prefix)
    {
        float remaining = duration;
        while (remaining > 0f)
        {
            if (timerText != null)
            {
                int minutes = Mathf.FloorToInt(remaining / 60f);
                int seconds = Mathf.FloorToInt(remaining % 60f);
                timerText.text = $"{prefix}: {minutes:00}:{seconds:00} remaining";
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
            label = config.stimuli[index - 1].sceneName;
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

        PositionPlayerAtSpawnPoint(newSceneName);
        SetSceneLayer(newSceneName);
        DisableExtraSceneComponents(newSceneName);

        CurrentSceneLabel = label;
        LogEvent(label + "_START");

        isSwitching = false;
        showSceneButton.interactable = true; // ← butonu aç
        breakButton.interactable = true;
    }

    
     void PositionPlayerAtSpawnPoint(string sceneName)
    {
        if (xrOrigin == null)
        {
            Debug.LogWarning("XR Origin atanmadı!");
            return;
        }

        Scene scene = SceneManager.GetSceneByName(sceneName);
        GameObject spawnPoint = null;

        foreach (GameObject obj in scene.GetRootGameObjects())
        {
            if (obj.name == "PlayerSpawnPoint")
            {
                spawnPoint = obj;
                break;
            }
        }

        if (spawnPoint != null)
        {
            xrOrigin.position = spawnPoint.transform.position;
            xrOrigin.rotation = spawnPoint.transform.rotation;
            Debug.Log("Oyuncu spawn noktasına taşındı: " + spawnPoint.transform.position);
        }
        else
        {
            // SpawnPoint yoksa ortada tut, sessize alma
            Debug.LogWarning(sceneName + " sahnesinde PlayerSpawnPoint bulunamadı! Pozisyon sıfırlandı.");
            xrOrigin.position = Vector3.zero;
            xrOrigin.rotation = Quaternion.identity;
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
            if (welcomeEnvironment != null)
                welcomeEnvironment.SetActive(false);

            yield return SceneManager.LoadSceneAsync(config.breakSceneName, LoadSceneMode.Additive);
            currentlyLoadedScene = config.breakSceneName;
            PositionPlayerAtSpawnPoint(config.breakSceneName);
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
                return config.stimuli[i].sceneName;
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

        if (startCycleButton != null)
            startCycleButton.gameObject.SetActive(true);
    }

    void OnFinishClicked()
    {
        EEGDataLogger logger = FindObjectOfType<EEGDataLogger>();
        if (logger != null)
            logger.StopLogging();

        if (logger == null)
        {
            Debug.LogError("EEGDataLogger bulunamadı.");
            return;
        }

        string basePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Personal),
            "eeg-analytics"
        );

        var analyzer = new SessionAnalyzer();
        analyzer.Analyze(logger.MindIndexPath);
        analyzer.AnalyzeFeatures(logger.FeatureDataPath);

        var generator = new ReportGenerator();
        string reportPath = generator.Generate(analyzer, basePath, sessionTimestamp);

        Debug.Log("Rapor oluşturuldu: " + reportPath);

        // tarayıcıda aç
        Application.OpenURL("file:///" + reportPath.Replace("\\", "/"));
    }

    public void OnStartCycleClicked()
    {
        if (startCycleButton != null)
            startCycleButton.gameObject.SetActive(false);

        if (welcomeEnvironment != null)
            welcomeEnvironment.SetActive(false);

        autoCoroutine = StartCoroutine(AutoSequence());
    }
}

