using UnityEngine;
using UnityEngine.UI;

public class FeatureGraphSelector : MonoBehaviour
{
    [System.Serializable]
    public class ChannelGraphPair
    {
        [Tooltip("The Radio Button (Toggle) for this channel")]
        public Toggle channelToggle;
        
        [Tooltip("The Graph GameObject that should be shown when this channel is selected")]
        public GameObject graphObject;
    }

    [Header("Channel Settings")]
    public ChannelGraphPair[] channelGraphs;

    void Start()
    {
        foreach (var pair in channelGraphs)
        {
            if (pair.channelToggle != null)
            {
                // Listen for when the radio button is clicked
                pair.channelToggle.onValueChanged.AddListener((isOn) => 
                {
                    if (isOn && pair.graphObject != null)
                    {
                        ShowGraph(pair.graphObject);
                    }
                });

                // Ensure the correct graph is shown when the game starts
                if (pair.channelToggle.isOn && pair.graphObject != null)
                {
                    ShowGraph(pair.graphObject);
                }
            }
        }
    }

    void ShowGraph(GameObject graphToShow)
    {
        foreach (var pair in channelGraphs)
        {
            if (pair.graphObject != null)
            {
                // Enable only the selected graph, disable the rest
                pair.graphObject.SetActive(pair.graphObject == graphToShow);
            }
        }
    }
}
