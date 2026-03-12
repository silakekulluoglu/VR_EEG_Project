using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class ExperimenterMouseInput : MonoBehaviour
{
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            GraphicRaycaster[] raycasters = FindObjectsOfType<GraphicRaycaster>();
            EventSystem eventSystem = FindObjectOfType<EventSystem>();

            foreach (GraphicRaycaster raycaster in raycasters)
            {
                PointerEventData pointerData = new PointerEventData(eventSystem);
                pointerData.position = Input.mousePosition;

                List<RaycastResult> results = new List<RaycastResult>();
                raycaster.Raycast(pointerData, results);

                foreach (var result in results)
                {
                    Button button = result.gameObject.GetComponent<Button>();
                    if (button != null)
                    {
                        button.onClick.Invoke();
                        return;
                    }
                }
            }
        }
    }
}