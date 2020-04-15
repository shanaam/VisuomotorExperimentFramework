using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UXF;

public class CursorController : MonoBehaviour
{
    // The visible representation of the cursor. A blue sphere
    public GameObject Model;

    // References to the left and right hand positions
    public GameObject LeftHand, RightHand;
    private GameObject leftHandModel, rightHandModel;

    private ExperimentController ctrler;

    public bool CursorVisible { get; private set; }
    public bool LeftHandVisible { get; private set; }
    public bool RightHandVisible { get; private set; }

    // Which hand is involved in the current task
    public string CurrentTaskHand { get; private set; }

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
        MoveType = MovementType.aligned;

        ctrler = ExperimentController.Instance();
    }

    public void SetHand(Trial trial)
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
        // Update the position of the cursor depending on which hand is involved
        transform.position = ConvertPosition(CurrentTaskHand == "l" ? 
            LeftHand.transform.position : RightHand.transform.position);
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
                return Quaternion.Euler(0, -angle, 0) * (position - ctrler.transform.position);
            case MovementType.clamped:
                // Get vector between home position and target
                Vector3 home = ctrler.CurrentTask.Home.transform.position;
                Vector3 target = ctrler.CurrentTask.Target.transform.position;

                Vector3 direction = target - home;
                return Vector3.ProjectOnPlane(position, Vector3.up);
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
