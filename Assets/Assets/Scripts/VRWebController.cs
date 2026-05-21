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
            // 2. TRACKPAD KONTROLÜ (Buton Tıklama)
            rightHand.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out bool isTrackpadPressed);
            if (isTrackpadPressed && !wasTrackpadPressed)
            {
                if (webBrowserUIBasic != null && webBrowserUIBasic.browserClient.IsConnected)
                {
                    string jsCode = "['continuebutton', 'start-button'].forEach(id => { var btn = document.getElementById(id); if(btn) btn.click(); });";
                    
                    webBrowserUIBasic.browserClient.ExecuteJs(jsCode); 
                    Debug.Log("Trackpad tetiklendi: Butona tıklandı!");
                }
            }
            wasTrackpadPressed = isTrackpadPressed;

            // 3. TETİK (TRIGGER) KONTROLÜ (Space Komutu)
            rightHand.TryGetFeatureValue(CommonUsages.triggerButton, out bool isTriggerPressed);
            if (isTriggerPressed && !wasTriggerPressed)
            {
                if (webBrowserUIBasic != null && webBrowserUIBasic.browserClient.IsConnected)
                {
                    string spaceJs = "var d = new KeyboardEvent('keydown', {key: ' ', code: 'Space', keyCode: 32, which: 32, bubbles: true}); " +
                                    "var u = new KeyboardEvent('keyup', {key: ' ', code: 'Space', keyCode: 32, which: 32, bubbles: true}); " +
                                    "document.dispatchEvent(d); window.dispatchEvent(d); " +
                                    "document.dispatchEvent(u); window.dispatchEvent(u);";
                                    
                    webBrowserUIBasic.browserClient.ExecuteJs(spaceJs); 
                    Debug.Log("Tetik çekildi: Space gönderildi!");
                }
            }
            wasTriggerPressed = isTriggerPressed;
        }
    }
}