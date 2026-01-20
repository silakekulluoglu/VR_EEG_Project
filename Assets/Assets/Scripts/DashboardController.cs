using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // for TextMeshPro
using Looxid.Link;

public class DashboardController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject startScreen; // starting panel
    public CanvasGroup experimentCanvasGroup; // splitted experiment view

    [Header("UI Elements")]
    public Button startButton;
    public TextMeshProUGUI statusText;

    private bool uiUpdateNeeded = false; 
    private bool newSensorStatus = false;

    void Start()
    {
        // start the looxidlink without waiting for 2dvisualizer
        if (Looxid.Link.LooxidLinkManager.Instance != null)
        {
            Looxid.Link.LooxidLinkManager.Instance.SetDebug(true); 
            bool isInitialized = Looxid.Link.LooxidLinkManager.Instance.Initialize();

            if (isInitialized)
            {
                Debug.Log("✅ LOOXID System is initialized successfully!");
            }
            else
            {
                Debug.LogError("❌ LOOXID System could not be initialized! (Error)");
            }
        }

        // default start mode settings
        startScreen.SetActive(true);
        experimentCanvasGroup.alpha = 0; // we are making the experiment view invisible
        experimentCanvasGroup.interactable = false;
        experimentCanvasGroup.blocksRaycasts = false;

        // what happens when the button is clicked
        startButton.onClick.AddListener(OnStartClicked);

        // if the sensor is already connected when the game starts
        if (LooxidLinkManager.Instance != null &&
            LooxidLinkManager.Instance.isLinkHubConnected)
        {
            SetSensorReadyUI(true);
        }
        else
        {
            SetSensorReadyUI(false);
        }
    }

    // enable: when the game starts
    void OnEnable()
    {
        // subscribe to NetworkManager
        NetworkManager.OnLinkHubConnected += HandleSensorConnected;
        NetworkManager.OnLinkHubDisconnected += HandleSensorDisconnected;
    }

    // disable: when the game ends
    void OnDisable()
    {
        // unsubscribe from NetworkManager
        NetworkManager.OnLinkHubConnected -= HandleSensorConnected;
        NetworkManager.OnLinkHubDisconnected -= HandleSensorDisconnected;
    }

    void HandleSensorConnected()
    {
        newSensorStatus = true;
        uiUpdateNeeded = true;
    }

    void HandleSensorDisconnected()
    {
        newSensorStatus = false;
        uiUpdateNeeded = true;
    }

    // runs in every frame 
    void Update()
    {
        if (uiUpdateNeeded)
        {
            SetSensorReadyUI(newSensorStatus); // update UI
            uiUpdateNeeded = false;           
        }

        // if (NoiseSignalPanel != null) NoiseSignalPanel.alpha = noiseSingalWindowAlpha;
        // if (SensorOffPanel != null) SensorOffPanel.alpha = sensorOffWindowAlpha;
    }

    // function for updating the UI
    void SetSensorReadyUI(bool isReady) {
        if (isReady)
        {
            statusText.text = "Sensor is connected - ready";
            statusText.color = Color.green;
            startButton.interactable = true;
        }
        else 
        {
            statusText.text = "Sensor is disconnected - waiting...";
            statusText.color = Color.red;
            startButton.interactable = false;
        }
    }

    void OnStartClicked()
    {
        Debug.Log("Experiment starting...");

        // change the screen
        startScreen.SetActive(false);

        // we make experiment view visible
        experimentCanvasGroup.alpha = 1;
        experimentCanvasGroup.interactable = true;
        experimentCanvasGroup.blocksRaycasts = true;
    }
}
