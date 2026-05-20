using UnityEngine;
using UnityEngine.XR;
using VoltstroStudios.UnityWebBrowser.Core;
using VoltstroStudios.UnityWebBrowser;

public class VRWebController : MonoBehaviour
{
    [HideInInspector] public WebBrowserUIBasic webBrowserUIBasic; 
    
    private bool wasTrackpadPressed = false;
    private bool wasTriggerPressed = false;

    void Update()
    {
        // 1. OTOMATİK BAĞLANTI
        if (webBrowserUIBasic == null)
        {
            webBrowserUIBasic = FindObjectOfType<WebBrowserUIBasic>();
        }

        InputDevice rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (rightHand.isValid)
        {
            // 2. TRACKPAD KONTROLÜ (Güçlendirilmiş Space Komutu)
            rightHand.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out bool isTrackpadPressed);
            if (isTrackpadPressed && !wasTrackpadPressed)
            {
                if (webBrowserUIBasic != null && webBrowserUIBasic.browserClient.IsConnected)
                {
                    // PsyToolkit için özel hazırlanmış, keyCode içeren ve bas-çek (keydown+keyup) yapan komut
                    string spaceJs = "var d = new KeyboardEvent('keydown', {key: ' ', code: 'Space', keyCode: 32, which: 32, bubbles: true}); " +
                                    "var u = new KeyboardEvent('keyup', {key: ' ', code: 'Space', keyCode: 32, which: 32, bubbles: true}); " +
                                    "document.dispatchEvent(d); window.dispatchEvent(d); " +
                                    "document.dispatchEvent(u); window.dispatchEvent(u);";
                                    
                    webBrowserUIBasic.browserClient.ExecuteJs(spaceJs); 
                    Debug.Log("Trackpad tetiklendi: PsyToolkit için güçlendirilmiş Space gönderildi!");
                }
            }
            wasTrackpadPressed = isTrackpadPressed;

            // 3. TETİK (TRIGGER) KONTROLÜ (Gelişmiş Buton Avcısı)
            rightHand.TryGetFeatureValue(CommonUsages.triggerButton, out bool isTriggerPressed);
            if (isTriggerPressed && !wasTriggerPressed)
            {
                if (webBrowserUIBasic != null && webBrowserUIBasic.browserClient.IsConnected)
                {
                    // JavaScript listemiz: İçine istediğin kadar buton ID'si ekleyebilirsin
                    string jsCode = "['continuebutton', 'start-button'].forEach(id => { var btn = document.getElementById(id); if(btn) btn.click(); });";
                    
                    webBrowserUIBasic.browserClient.ExecuteJs(jsCode); 
                    Debug.Log("Tetik çekildi: Ekrandaki mevcut butona JS ile tıklandı!");
                }
            }
            wasTriggerPressed = isTriggerPressed;
        }
    }
}