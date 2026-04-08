using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CognitiveLoadBar : MonoBehaviour
{
    [Header("UI References")]
    public Image barFill;
    public TextMeshProUGUI valueText;

    [Header("Bar Settings")]
    public float minValue = 0f;
    public float maxValue = 3f;

    [Header("Colors")]
    public Color lowColor  = Color.green;
    public Color midColor  = Color.yellow;
    public Color highColor = Color.red;

    void Update()
    {
        float raw = EEGDataLogger.CurrentCognitiveLoad;
        float normalized = Mathf.Clamp01((raw - minValue) / (maxValue - minValue));

        barFill.fillAmount = normalized;

        if (normalized < 0.5f)
            barFill.color = Color.Lerp(lowColor, midColor, normalized * 2f);
        else
            barFill.color = Color.Lerp(midColor, highColor, (normalized - 0.5f) * 2f);

        valueText.text = raw.ToString("F2");
    }
}