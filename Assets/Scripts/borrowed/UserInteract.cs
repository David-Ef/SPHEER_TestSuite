using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class UserInteract : MonoBehaviour
{
    public InputActionReference Trigger;
    public InputActionReference SideButton;

    public static UserInteract instance { private set; get; }

    private void Awake()
    {
        instance = this;

        Trigger.action.Enable();
        SideButton.action.Enable();
    }

    public bool userInteracted => releasedTrigger || releasedSpaceKey;

    // Underside trigger button
    private bool _pressedTrigger => Trigger.action.WasPressedThisFrame();
    public bool pressedTrigger => _pressedTrigger;

    private bool _pressingTrigger => Trigger.action.IsInProgress() || Keyboard.current.spaceKey.IsPressed();
    public bool pressingTrigger => _pressingTrigger;

    private bool _releasedTrigger => Trigger.action.WasReleasedThisFrame();
    public bool releasedTrigger => _releasedTrigger;

    // Side grip buttons
    private bool _pressingGrip => SideButton.action.IsInProgress();
    public bool pressingGrip => _pressingGrip;

    private bool _releasedGrip => SideButton.action.WasReleasedThisFrame();
    public bool releasedGrip => _releasedGrip;

    // Keyboard substitutes
    private bool _releasedSpaceKey => Keyboard.current.spaceKey.wasReleasedThisFrame;
    public bool releasedSpaceKey => _releasedSpaceKey;
    private bool _releasedReturnKey => Keyboard.current.enterKey.wasReleasedThisFrame || Keyboard.current.numpadEnterKey.wasReleasedThisFrame;
    public bool releasedReturnKey => _releasedReturnKey;

    // Body part tracking
    public Transform Torso;
    public Transform Leg;
    public Transform Hand;
}
