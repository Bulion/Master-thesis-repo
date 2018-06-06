using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;

public class gear_button_manager : MonoBehaviour, IInputClickHandler, IInputHandler, IControllerInputHandler {

    public void OnInputClicked(InputClickedEventData eventData)
    {
        Debug.LogErrorFormat("Button clicked");
    }
    public void OnInputPositionChanged(InputPositionEventData eventData)
    {
        Debug.LogErrorFormat("Input position changed x: {0}, y: {1}", eventData.Position.x, eventData.Position.y);
    }
    public void OnInputDown(InputEventData eventData)
    {
        Debug.LogErrorFormat("Button down");
    }
    public void OnInputUp(InputEventData eventData)
    {
        Debug.LogErrorFormat("Button up");
    }
}
