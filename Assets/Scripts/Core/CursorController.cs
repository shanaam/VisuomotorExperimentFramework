using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;
using UXF;

public class CursorController : MonoBehaviour
{
    // The visible representation of the cursor. A blue sphere
    public GameObject Model;

    // References to the left and right hand positions
    public GameObject LeftHand, RightHand;
    private GameObject leftHandModel, rightHandModel;
    private GameObject leftHandCollider, rightHandCollider;

    public bool CursorVisible { get; private set; }
    public bool LeftHandVisible { get; private set; }
    public bool RightHandVisible { get; private set; }

    // Which hand is involved in the current task
    public string CurrentTaskHand { get; private set; }

    public InputDevice LeftHandDevice { get; private set; }
    public InputDevice RightHandDevice { get; private set; }

    // Used to track hold time
    private Vector3 previousPosition;
    public float PauseTime { get; private set; }

    public float DistanceFromHome { get; private set; }

    private float[] average = new float[10];
    private int counter = 0;

    public enum MovementType
    {
        aligned,
        rotated,
        clamped
    }

    public MovementType MoveType { get; private set; }

    void Start()
    {
        // For oculus
        leftHandModel = LeftHand.transform.Find("left_touch_controller_model_skel").gameObject;
        rightHandModel = RightHand.transform.Find("right_touch_controller_model_skel").gameObject;

        leftHandCollider = LeftHand.transform.Find("LeftHandCollider").gameObject;
        rightHandCollider = RightHand.transform.Find("RightHandCollider").gameObject;

        List<InputDevice> devices = new List<InputDevice>();
        InputDevices.GetDevices(devices);

        foreach (InputDevice device in devices)
        {
            switch (device.characteristics)
            {
                case InputDeviceCharacteristics.Left:
                    LeftHandDevice = device;
                    break;
                case InputDeviceCharacteristics.Right:
                    RightHandDevice = device;
                    break;
            }
        }

        MoveType = MovementType.aligned;
    }

    public bool IsTriggerDown()
    {
        return IsTriggerDown(CurrentTaskHand);
    }

    public bool IsTriggerDown(String hand)
    {
        if (hand == "l")
        {
            return LeftHandDevice == null
                ? false
                : LeftHandDevice.TryGetFeatureValue(CommonUsages.triggerButton, out bool val);
        }
        else
        {
            return RightHandDevice == null
                ? false
                : RightHandDevice.TryGetFeatureValue(CommonUsages.triggerButton, out bool val);
        }
    }

    /// <summary>
    /// Returns the gameobject that represents the hand involved in the current trial
    /// </summary>
    /// <returns></returns>
    public GameObject CurrentHand()
    {
        return CurrentTaskHand == "l" ? LeftHand : RightHand;
    }

    /// <summary>
    /// Sets up all properties pertaining to the cursor and hand.
    /// Run by the OnTrialBegin event by UXF
    /// </summary>
    public void SetupHand(Trial trial)
    {
        switch (trial.settings.GetString("per_block_hand"))
        {
            case "r":
            case "l":
                CurrentTaskHand = trial.settings.GetString("per_block_hand");
                break;
            default:
                Debug.LogWarning("\"per_block_hand\" is not 'l' or 'r'. Check the JSON.");
                break;
        }

        // Default movement type to aligned
        MoveType = MovementType.aligned;
    }

    public void SetMovementType(MovementType moveType)
    {
        MoveType = moveType;
    }

    /// <summary>
    /// Sets the visibility of the cursor
    /// </summary>
    public void SetCursorVisibility(bool visible)
    {
        Model.GetComponent<MeshRenderer>().enabled = CursorVisible = visible;
    }

    /// <summary>
    /// Sets the visibility of the real hands
    /// </summary>
    public void SetHandVisibility(bool visible)
    {
        leftHandModel.SetActive(visible);
        rightHandModel.SetActive(visible);
        LeftHandVisible = RightHandVisible = visible;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (ExperimentController.Instance().CurrentTask == null) return;

        Vector3 realHandPosition = CurrentTaskHand == "l"
            ? leftHandCollider.transform.position
            : rightHandCollider.transform.position;

        // Update the position of the cursor depending on which hand is involved
        transform.position = ConvertPosition(realHandPosition);

        if ((previousPosition - realHandPosition).magnitude > 0.001f)
            PauseTime = 0f;
        else
            PauseTime += Time.deltaTime;

        previousPosition = realHandPosition;
        DistanceFromHome = (transform.position - ExperimentController.Instance().CurrentTask.Home.transform.position).magnitude;
    }

    /// <summary>
    /// Converts the user's hand location into the transformed cursor location
    /// </summary>
    /// <returns></returns>
    private Vector3 ConvertPosition(Vector3 position)
    {
        switch (MoveType)
        {
            case MovementType.aligned:
                return position;
            case MovementType.rotated:
                float angle = ExperimentController.Instance().Session.CurrentTrial.settings
                    .GetFloat("per_block_rotation");

                return Quaternion.Euler(0, -angle, 0) * position;
            case MovementType.clamped:
                // Get vector between home position and target
                Vector3 home = ExperimentController.Instance().CurrentTask.Home.transform.position;
                Vector3 target = ExperimentController.Instance().CurrentTask.Target.transform.position;

                Vector3 normal = target - home;

                // Rotate vector by 90 degrees to get plane parallel to the vector
                normal = Quaternion.Euler(0f, -90f, 0f) * normal;

                //  o   < target
                //  |
                // -|   < normal
                //  |
                //  x   < dock / center of experiment

                // Project position using this new vector as the plane normal
                return Vector3.ProjectOnPlane(position - home, normal) + home;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
