using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StimulusRowUI : MonoBehaviour
{
    public TMP_Text labelText;
    public TMP_InputField sceneNameInput;
    public TMP_InputField durationInput;
    public Button removeButton;

    private Action<StimulusRowUI> onRemove;

    public float GetDuration()
    {
        float result;
        return float.TryParse(durationInput.text, out result) ? result : 0f;
    }

    public void Init(string label, string sceneName, float duration, Action<StimulusRowUI> removeCallback)
    {
        labelText.text = label;
        sceneNameInput.text = sceneName;
        durationInput.text = duration.ToString();
        onRemove = removeCallback;
        removeButton.onClick.RemoveAllListeners();
        removeButton.onClick.AddListener(() => onRemove?.Invoke(this));
    }

    public void SetLabel(string label) => labelText.text = label;
    public string GetLabel() => labelText.text;
    public string GetSceneName() => sceneNameInput.text.Trim();
}