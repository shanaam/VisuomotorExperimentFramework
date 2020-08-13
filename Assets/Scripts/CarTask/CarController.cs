using System;
using UnityEngine;
using UnityEngine.UI;

public class CarController : MonoBehaviour
{
    private float turnAngle;
    public float TurnSpeed = 45f;
    private const float MAX_ANGLE = 35f;

    private float turnResetTimer;
    private float accelResetTimer;

    private float accelerator;
    public float MaxTorque = 850f;

    public WheelCollider WCFLeft, WCFRight, WCRLeft, WCRRight;
    public Transform WFLeft, WFRight, WRLeft, WRRight;

    public Text speedtext;
    private Vector3 prevPosition = Vector3.zero;

    public GameObject FollowObject;

    // Update is called once per frame
    void Update()
    {
        int accel = 0;
        if (Input.GetKey(KeyCode.W))
            accel++;
        else if (Input.GetKey(KeyCode.S))
            accel--;

        if (accel == 0)
        {
            accelResetTimer += Time.deltaTime;
            accelerator = Mathf.Lerp(accelerator, 0f, accelResetTimer);
        }
        else
        {
            accelResetTimer = 0f;
            accelerator += accel * Time.deltaTime * 5f;
        }

        accelerator = Mathf.Clamp(accelerator, -1f, 1f);

        Debug.Log(accelerator);

        int direction = 0;

        if (Input.GetKey(KeyCode.A))
            direction--;

        if (Input.GetKey(KeyCode.D))
            direction++;

        // Reset wheel angle to center if no input is read
        if (direction == 0)
        {
            turnResetTimer += 2f * Time.deltaTime;
            turnAngle = Mathf.Lerp(turnAngle, 0f, turnResetTimer);
        }
        else
        {
            turnResetTimer = 0f;
            turnAngle += direction * TurnSpeed * Time.deltaTime;
        }

        turnAngle = Mathf.Clamp(turnAngle, -MAX_ANGLE, MAX_ANGLE);

        // Set turning angle for wheels
        WCFLeft.steerAngle = WCFRight.steerAngle = turnAngle;

        // Set torque for wheels
        WCFLeft.motorTorque = WCFRight.motorTorque = 
            WCRLeft.motorTorque = WCRRight.motorTorque = 
                accelerator * MaxTorque;


        if (Input.GetKey(KeyCode.S))
            WCRLeft.brakeTorque = WCRRight.brakeTorque = WCFLeft.brakeTorque = WCFRight.brakeTorque = 2350f;
        else
            WCRLeft.brakeTorque = WCRRight.brakeTorque = WCFLeft.brakeTorque = WCFRight.brakeTorque = 0f;
        
        WheelFrictionCurve curve = WCRRight.sidewaysFriction;

        if (Input.GetKey(KeyCode.Space))
        {
            WCRLeft.brakeTorque = WCRRight.brakeTorque = 2350f;
            WCRLeft.motorTorque = WCRRight.motorTorque = 0f;

            //WCFLeft.motorTorque = WCFRight.motorTorque = accelerator * MaxTorque * 2f;
            
            curve.extremumValue = 0.8f;
            WCRLeft.sidewaysFriction = WCRRight.sidewaysFriction = curve;
        }
        else
        {
            curve.extremumValue = 1.0f;
            WCRLeft.sidewaysFriction = WCRRight.sidewaysFriction = curve;
        }

        // Update wheel positions
        UpdateWheel(WCFLeft, WFLeft);
        UpdateWheel(WCFRight, WFRight);
        UpdateWheel(WCRLeft, WRLeft);
        UpdateWheel(WCRRight, WRRight);

        WRRight.Rotate(0f, 180f, 0f);
        WFRight.Rotate(0f, 180f, 0f);


    }

    void FixedUpdate()
    {
        float speed = Vector3.Distance(prevPosition, transform.position) / Time.fixedDeltaTime * 3.6f;
        speedtext.text = "Speed: " + speed.ToString("F2") + " KM/H";

        prevPosition = transform.position;

        float interp = 5f * Time.deltaTime;

        Camera.main.transform.position =
            Vector3.Lerp(Camera.main.transform.position, FollowObject.transform.position, interp);

        Camera.main.transform.rotation = transform.rotation;
    }

    void LateUpdate()
    {

    }

    private void UpdateWheel(WheelCollider collider, Transform transform)
    {
        collider.GetWorldPose(out Vector3 pos, out Quaternion rot);

        transform.position = pos;
        transform.rotation = rot;
    }
}
