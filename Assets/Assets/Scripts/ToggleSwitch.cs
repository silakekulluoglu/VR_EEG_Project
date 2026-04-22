using UnityEngine;
using UnityEngine.UI;

public class ToggleSwitch : MonoBehaviour
{
    public Image background;
    public RectTransform knob;
    public Toggle toggle;

    void Start()
    {
        // başlangıç durumunu ayarla
        OnToggleChanged(toggle.isOn);
    }

    public void OnToggleChanged(bool isOn)
    {
        background.color = isOn
            ? new Color(0.416f, 0.051f, 0.678f)
            : new Color(0.2f, 0.2f, 0.2f);

        knob.anchoredPosition = isOn
            ? new Vector2(8, 0)
            : new Vector2(-20, 0);
    }
}