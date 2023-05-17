using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimateSurfaceTask : BaseTask
{
  private ExperimentController ctrler;

  // Task GameObjects
  private GameObject surface;
  private GameObject surfaceSpace;
  private GameObject home;
  private GameObject pinballCam;
  private GameObject scoreboard, pinballWall, rotateObject;
  // private GameObject lights;
  private float prevSurfaceTilt, nextSurfaceTilt, prevCameraTilt, nextCameraTilt;
  private GameObject XRRig, XRPosLock;
  private Vector3 ball_pos;
  private bool rotating = false;

  // Pinball Camera Offset
  private Vector3 PINBALL_CAM_OFFSET = new Vector3(0f, 0.725f, -0.535f);
  private const float PINBALL_CAM_ANGLE = 35f;
  private const float ROTATE_SPEED = 180f;


  public override void Setup()
  {
    maxSteps = 1;

    ctrler = ExperimentController.Instance();

    surfaceSpace = Instantiate(ctrler.GetPrefab("AnimateSurfacePrefab"));
    surface = GameObject.Find("Surface");
    home = GameObject.Find("PinballHome");
    scoreboard = GameObject.Find("Scoreboard");
    rotateObject = GameObject.Find("RotateObject");
    // lights = GameObject.Find("Lights");
    pinballCam = GameObject.Find("PinballCamera");
    pinballWall = GameObject.Find("PinballWall");
    XRRig = GameObject.Find("XR Rig");
    XRPosLock = GameObject.Find("XRPosLock");

    // unparent lights
    // lights.transform.SetParent(ctrler.transform);

    prevSurfaceTilt = 0f;
    nextSurfaceTilt = 0f;
    prevCameraTilt = -25f;
    nextCameraTilt = 25f;

    ball_pos = home.transform.position + Vector3.up * 0.025f;

    // SetTilt(surface, ball_pos, surfaceSpace, prevSurfaceTilt);

    Debug.Log("AnimateSurfaceTask set up");

    // Camera setup
    // Use static camera for non-vr version of pinball
    if (ctrler.Session.settings.GetString("experiment_mode") == "pinball")
    {
      // Setup Pinball Camera Offset
      pinballCam.transform.position = PINBALL_CAM_OFFSET;
      pinballCam.transform.rotation = Quaternion.Euler(PINBALL_CAM_ANGLE, 0f, 0f);

      ctrler.CursorController.SetVRCamera(false);
    }
    else // VR version
    {
      ctrler.CursorController.UseVR = true;

      pinballCam.SetActive(false);
      ctrler.CursorController.SetVRCamera(true);
      ctrler.CursorController.SetCursorVisibility(false);

      scoreboard.transform.position += Vector3.up * 0.33f;

      SetTilt(pinballCam, ball_pos, surfaceSpace, prevCameraTilt);
      SetTilt(pinballWall, ball_pos, surfaceSpace, prevCameraTilt);
      SetTilt(surfaceSpace, ball_pos, surfaceSpace, prevSurfaceTilt); //Tilt surface
      SetTilt(XRRig, ball_pos, surfaceSpace, prevCameraTilt + prevSurfaceTilt);
      XRRig.transform.position = XRPosLock.transform.position;
    }

    // Rotate the RotateObject back to its original position
    // unparent surface first
    surface.transform.SetParent(null);
    rotateObject.transform.localEulerAngles = new Vector3(0f, 0f, -25f);
    surface.transform.SetParent(rotateObject.transform);

    // Wait for 1 second, then rotate the rotateObject
    StartCoroutine(Wait());
  }

  void Update()
  {
    if (rotating)
    {
      if (ctrler.Session.settings.GetString("anim_type") == "half")
      {
        // Rotate the Surface by ROTATE_SPEED degrees
      surface.transform.RotateAround(ball_pos, rotateObject.transform.up, -1 * ROTATE_SPEED * Time.deltaTime);

      if (surface.transform.localEulerAngles.y <= 180f)
      {
        rotating = false;
        StartCoroutine(WaitAndEnd());
      }
      }
      else if (ctrler.Session.settings.GetString("anim_type") == "full")
      {
        // Rotate the Surface by 2 * ROTATE_SPEED degrees
      surface.transform.RotateAround(ball_pos, rotateObject.transform.up, -2 * ROTATE_SPEED * Time.deltaTime);
        if (surface.transform.localEulerAngles.y <= 360f)
        {
          rotating = false;
          StartCoroutine(WaitAndEnd());
        }
      }
      else if (ctrler.Session.settings.GetString("anim_type") == "wait")
      {
        rotating = false;
        StartCoroutine(WaitAndEnd(2f));
      }
      else 
      {
        Debug.LogError("Animation type not recognized. Please check spelling.");
      }
    }
  }

  public override void Disable()
  {
    // Realign XR Rig to non-tilted position
    if (ctrler.Session.settings.GetString("experiment_mode") == "pinball_vr")
    {
      //XRRig.transform.RotateAround(Home.transform.position + Vector3.up * 0.25f, pinballSpace.transform.forward,
      //    (cameraTilt + surfaceTilt) * -1);
      SetTilt(XRRig, ball_pos, surfaceSpace, (prevCameraTilt + prevSurfaceTilt) * -1);
    }
    surfaceSpace.SetActive(false);
  }

  public override void LogParameters()
  {
    ctrler.LogObjectPosition("home", ball_pos);
  }

  /// <summary>
  /// Rotates obj around axis by angle degrees
  /// </summary>
  /// <param name="obj">Object to be rotated</param>
  /// <param name="point">Point to rotate around</param>
  /// <param name="axis">Object to rotate around</param>
  /// <param name="angle">Angle in degrees of rotation</param>
  private void SetTilt(GameObject obj, Vector3 point, GameObject axis, float angle)
  {
    // Decouple object from parent
    Transform parent = obj.transform.parent;
    obj.transform.SetParent(null);

    obj.transform.RotateAround(point, axis.transform.forward, angle);

    // Reparent obj
    obj.transform.SetParent(parent);
  }

  /// <summary>
  /// Sets the material of the surface
  /// </summary>
  /// <param name="material">Material to set</param>
  private void SetSurfaceMaterial(Material material)
  {
    if (surface == null)
    {
      Debug.LogError("Surface was not found in the prefab. Please make sure it is added." +
                     " If Surface exists in the prefab, check if you ran base.Setup()");
    }

    if (material == null)
    {
      Debug.LogError("Material was not found. Check spelling.");
    }

    surface.GetComponent<MeshRenderer>().material = material;
  }

  /// <summary>
  /// Wait for 1 second
  /// </summary>
  /// <returns></returns>
  private IEnumerator Wait()
  {
    yield return new WaitForSeconds(1);
    rotating = true;
  }

  /// <summary>
  /// Wait 1 second then run EndAndPrepare
  /// </summary>
  /// <param name="waitTime">Time to wait in seconds</param>
  /// <returns></returns>
  private IEnumerator WaitAndEnd(float waitTime = 1f)
  {
    yield return new WaitForSeconds(waitTime);
    // re-parent lights
    // lights.transform.SetParent(surface.transform);
    ctrler.EndAndPrepare();
  }

}
