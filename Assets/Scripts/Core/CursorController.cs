using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SpatialTracking;
using UnityEngine.XR;
using UXF;
using CommonUsages = UnityEngine.XR.CommonUsages;
using InputDevice = UnityEngine.XR.InputDevice;

public class CursorController : MonoBehaviour
{
  // The visible representation of the cursor. A blue sphere
  public GameObject Model;

  public bool triggerUp;
  // to check whether it's being pressed
  public bool IsPressed { get; private set; }

  // References to the left and right hand positions
  public GameObject LeftHand, RightHand;
  private GameObject leftHandModel, rightHandModel;
  private GameObject leftHandCollider, rightHandCollider;

  private GameObject vrCamera;

  public bool CursorVisible { get; private set; }
  public bool LeftHandVisible { get; private set; }
  public bool RightHandVisible { get; private set; }

  // Which hand is involved in the current task
  public string CurrentTaskHand { get; private set; }

  // Used to access the hardware controllers
  public InputDevice LeftHandDevice { get; private set; }
  public InputDevice RightHandDevice { get; private set; }

  // Used to track hold time
  private Vector3 previousPosition;
  public float PauseTime { get; private set; }

  // Returns the distance from the cursor to the home
  public float DistanceFromHome { get; private set; }

  // Bools for when the trigger is pressed
  private bool prevLeftTrigger, prevRightTrigger;

  // Prev controller velocity for the current controller
  public Vector3 prevVelocity;

  // Bool for whether or not to use VR controllers as input for the cursor position
  public bool UseVR;

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
    //InputDevices.GetDevices(devices);

    InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Right, devices);
    if (devices.Count > 0)
    {
      RightHandDevice = devices[0];
    }
    else
    {
      Debug.Log("No devices detected.");
    }
    Debug.Log("Detecting devices...");
    Debug.Log("Found Right Device: " + RightHandDevice);

    InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Left, devices);
    if (devices.Count > 0)
    {
      LeftHandDevice = devices[0];
    }
    else
    {
      Debug.Log("No devices detected.");
    }
    Debug.Log("Detecting devices...");
    Debug.Log("Found Left Device: " + LeftHandDevice);

    vrCamera = GameObject.Find("Main Camera");


    //foreach (InputDevice device in devices)
    //{
    //    switch (device.characteristics)
    //    {
    //        case InputDeviceCharacteristics.Left:
    //            LeftHandDevice = device;
    //            break;
    //        case InputDeviceCharacteristics.Right:
    //            RightHandDevice = device;
    //            break;
    //    }

    //    Debug.Log("Found Device: " + device.name);
    //}

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
          : LeftHandDevice.TryGetFeatureValue(CommonUsages.triggerButton, out bool val) && val;
    }
    else
    {

      return RightHandDevice == null
          ? false
          : RightHandDevice.TryGetFeatureValue(CommonUsages.triggerButton, out bool val) && val;
      // THE ABOVE CODE WORKS!
    }
  }

  // Returns true if the trigger was released on this frame
  public bool OnTriggerUp()
  {
    return OnTriggerUp(CurrentTaskHand);
  }

  public bool OnTriggerUp(String hand)
  {
    if (hand == "l")
    {
      return LeftHandDevice == null
          ? false
          : !LeftHandDevice.TryGetFeatureValue(CommonUsages.triggerButton, out bool val) && prevLeftTrigger;
    }
    else
    {
      return RightHandDevice == null
          ? false
          : !RightHandDevice.TryGetFeatureValue(CommonUsages.triggerButton, out bool val) && prevRightTrigger;
    }
  }

  public Vector3 GetVelocity()
  {
    return GetVelocity(CurrentTaskHand);
  }

  public Vector3 GetVelocity(String hand)
  {
    if (hand == "l")
    {
      Vector3 vel;
      LeftHandDevice.TryGetFeatureValue(CommonUsages.deviceVelocity, out vel);
      return vel;
    }
    else
    {
      Vector3 vel;
      RightHandDevice.TryGetFeatureValue(CommonUsages.deviceVelocity, out vel);
      return vel;
    }
  }
  //if left hand joystick is pressed then this send a true bool
  public bool MenuSkip()
  {
    LeftHandDevice.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out bool val);
    return val;
  }

  /// <summary>
  /// Returns the GameObject that represents the hand involved in the current trial
  /// </summary>
  public GameObject CurrentHand()
  {
    return CurrentTaskHand == "l" ? LeftHand : RightHand;
  }

  /// <summary>
  /// Returns the GameObject that represents the current task's hand collider
  /// </summary>
  /// <returns></returns>
  public GameObject CurrentCollider()
  {
    return CurrentTaskHand == "l" ? leftHandCollider : rightHandCollider;
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
        CurrentTaskHand = "r";
        break;
      case "l":
        CurrentTaskHand = "l";
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
    triggerUp = false;

    if (RightHandDevice.TryGetFeatureValue(CommonUsages.triggerButton, out bool val) && val)
    {
      // if start pressing, trigger event
      if (!IsPressed)
      {
        IsPressed = true;
        // Debug.Log("Trig down now");
      }
    }
    else if (IsPressed) // check for button release
    {
      IsPressed = false;
      triggerUp = true;
    }
    // Above code should work for OnTriggerUp (this means clean-up is required on current onTriggerUp method)

    if (ExperimentController.Instance().CurrentTask == null) return;

    Vector3 realHandPosition = GetHandPosition();

    // Update the position of the cursor depending on which hand is involved
    transform.position = ConvertPosition(realHandPosition);

    if ((previousPosition - realHandPosition).magnitude > 0.001f)
      PauseTime = 0f;
    else
      PauseTime += Time.deltaTime;

    previousPosition = realHandPosition;

    if (ExperimentController.Instance().CurrentTask.Home != null)
    {
      DistanceFromHome =
          (transform.position - ExperimentController.Instance().CurrentTask.Home.transform.position).magnitude;
    }
    else
    {
      DistanceFromHome = -1f;
    }

    prevLeftTrigger = IsTriggerDown("l");
    prevRightTrigger = IsTriggerDown("r");
  }

  //
  public Vector3 GetHandPosition()
  {
    if (UseVR)
    {
      return CurrentTaskHand == "l"
      ? leftHandCollider.transform.position
      : rightHandCollider.transform.position;
    }

    if (Camera.main == null)
    {
      Debug.LogWarning("make sure your camera is tagged as the Main camera");
    }

    Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    return new Vector3(mousePos.x, ExperimentController.Instance().transform.position.y, mousePos.z);
  }

  /// <summary>
  /// Converts the user's hand location into the transformed cursor location
  /// </summary>
  /// <returns></returns>
  private Vector3 ConvertPosition(Vector3 position)
  {
    ExperimentController ctrler = ExperimentController.Instance();

    // Get home position. Returns Vector3.zero when task doesn't use a home position
    Vector3 home = ctrler.CurrentTask.Home != null ?
        ctrler.CurrentTask.Home.transform.position : Vector3.zero;

    switch (MoveType)
    {
      case MovementType.aligned:
        return position;
      case MovementType.rotated:
        float angle = ctrler.Session.CurrentTrial.settings
            .GetFloat("per_block_rotation");

        return Quaternion.Euler(0, -angle, 0) * (position - home) + home;
      case MovementType.clamped:
        // Get vector between home position and target
        Vector3 target = ctrler.CurrentTask.Target.transform.position;
        Vector3 normal = target - home;

        // Rotate vector by 90 degrees to get plane parallel to the vector
        normal = Quaternion.Euler(0f, -90f, 0f) * normal;

        //  o   < target
        //  |
        // -|   < normal
        //  |
        //  x   < dock / center of experiment

        // Project position using this new vector as the plane normal
        return Vector3.ProjectOnPlane(position - home, normal.normalized) + home;
      default:
        throw new ArgumentOutOfRangeException();
    }
  }

  /// <summary>
  /// Maps the mouse cursor position to the plane's Y coordinate.
  /// A camera must be provided to determine the mouse position.
  /// </summary>
  public Vector3 MouseToPlanePoint(Vector3 planeNormal, Vector3 planePos, Camera camera)
  {
    // If the camera is orthographic, it is a top down view and thus x and y are just screen coordinates
    if (camera.orthographic)
    {
      Vector3 mouseCoords = camera.ScreenToWorldPoint(Input.mousePosition);
      return new Vector3(mouseCoords.x, planePos.y, mouseCoords.z);
    }

    Vector3 pos = camera.ScreenToWorldPoint(new Vector3(
        Input.mousePosition.x, Input.mousePosition.y, camera.nearClipPlane));

    Vector3 direction = (pos - camera.transform.position).normalized;

    Plane plane = new Plane(planeNormal.normalized, planePos);
    Ray r = new Ray(camera.transform.position, direction);

    return plane.Raycast(r, out float enter) ? r.GetPoint(enter) : Vector3.zero;
  }

  /// <summary>
  /// Enable or disable the VR Camera. Required for experiments with a specific camera
  /// </summary>
  /// <param name="state"></param>
  public void SetVRCamera(bool state)
  {
    if (state)
    {
      vrCamera.SetActive(true);
      vrCamera.GetComponent<TrackedPoseDriver>().enabled = true;
    }
    else
    {
      vrCamera.GetComponent<TrackedPoseDriver>().enabled = false;
      vrCamera.SetActive(false);
    }
  }

  /*
  private void OnDrawGizmos()
  {
      if (ExperimentController.Instance() == null) return;

      Vector3 mousepos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

      Gizmos.DrawSphere(new Vector3(mousepos.x, ExperimentController.Instance().transform.position.y, mousepos.z), .005f);
  }
  */
}
