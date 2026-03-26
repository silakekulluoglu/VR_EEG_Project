using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StimulusRowUI : MonoBehaviour
{
    public TMP_Text labelText;
    public TMP_InputField sceneNameInput;
    public Button removeButton;

    private Action<StimulusRowUI> onRemove;

    public void Init(string label, string sceneName, Action<StimulusRowUI> removeCallback)
    {
        labelText.text = label;
        sceneNameInput.text = sceneName;
        onRemove = removeCallback;
        removeButton.onClick.AddListener(() => onRemove?.Invoke(this));
    }

    public void SetLabel(string label) => labelText.text = label;
    public string GetLabel() => labelText.text;
    public string GetSceneName() => sceneNameInput.text.Trim();
}