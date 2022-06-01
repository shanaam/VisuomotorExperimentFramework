using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UXF;
using MovementType = CursorController.MovementType;
using TMPro;

public class ReachTrack : ReachToTargetTask
{
    protected GameObject field;
    protected GameObject goal;
    protected Vector3 mousePoint;
    protected GameObject baseObject;
    protected GameObject upArrow;
    protected GameObject downArrow;
    protected GameObject doubleUpArrow;
    protected GameObject doubleDownArrow;
    protected GameObject thumbsUp;

    Vector3 vel = new Vector3();
    Vector3 prev = new Vector3();
    Vector3 cur = new Vector3();
    float dist;
    float angle;
    Vector3 rotationAxis = new Vector3();
    float maxVel = 0;
    Vector3 newPos;
    Vector3 prevPos;
    float curVel;
    TextMeshPro text;
    float idealVel;
    float idealUpperBound;
    float idealLowerBound;
    float maxUpperVel;
    float minLowerVel;
    bool idealReached = false;
    // Start is called before the first frame update
    public override void Setup()
    {
        ctrler = ExperimentController.Instance();
        trial = ctrler.Session.CurrentTrial;
        d = null;

        Cursor.visible = false;

        reachPrefab = Instantiate(ctrler.GetPrefab("ReachTrack"));
        reachPrefab.transform.SetParent(ctrler.transform);
        reachPrefab.transform.localPosition = new Vector3(0, -0.8f, 0);

        reachCam = GameObject.Find("ReachCamera");
        reachSurface = GameObject.Find("Surface");
        timerIndicator = GameObject.Find("TimerIndicator").GetComponent<TimerIndicator>();
        scoreboard = GameObject.Find("Scoreboard").GetComponent<Scoreboard>();
        tint = GameObject.Find("Tint");
        field = GameObject.Find("Field");
        goal = field.transform.GetChild(0).transform.GetChild(0).gameObject;
        baseObject = GameObject.Find("BaseObject");
        text = baseObject.transform.GetChild(0).GetComponent<TextMeshPro>();
        text.transform.parent = reachPrefab.transform;
        doubleUpArrow = text.gameObject.transform.GetChild(0).gameObject;
        upArrow = text.gameObject.transform.GetChild(1).gameObject;
        doubleDownArrow = text.gameObject.transform.GetChild(2).gameObject;
        downArrow = text.gameObject.transform.GetChild(3).gameObject;
        thumbsUp = text.gameObject.transform.GetChild(4).gameObject;

        doubleUpArrow.SetActive(false);
        upArrow.SetActive(false);
        doubleDownArrow.SetActive(false);
        downArrow.SetActive(false);
        thumbsUp.SetActive(false);

        newPos = base.transform.position;
        prevPos = base.transform.position;

        idealVel = 0.4f;
        idealUpperBound = idealVel + 0.05f;
        idealLowerBound = idealVel - 0.05f;
        maxUpperVel = idealVel + 0.2f;
        minLowerVel = idealVel - 0.2f;

        field.SetActive(false);


        SetSetup();

        field.transform.position = targets[1].transform.position;

        ctrler.CursorController.Model.GetComponent<MeshRenderer>().enabled = false;

        field.transform.rotation = Quaternion.Euler(
            0f, -targetAngle + 90f, 0f);

        Vector3 pos = new Vector3();

        pos = targets[1].transform.localPosition +
                                        targets[2].transform.forward.normalized *
                                        (trial.settings.GetFloat("per_block_distance") / 100f);

        goal.transform.position =
        new Vector3(targets[2].transform.position.x,
        targets[2].transform.position.y - 0.005f, targets[2].transform.position.z);
        reachSurface.SetActive(true);


    }

    // Update is called once per frame
    void Update()
    {
        if (currentStep > 1)
        {
            field.SetActive(true);
            //VelocityTrack();
        }
        mousePoint = GetMousePoint(baseObject.transform);
        base.Update();
        ctrler.CursorController.Model.transform.position = new Vector3(ctrler.CursorController.Model.transform.position.x, mousePoint.y, ctrler.CursorController.Model.transform.position.z);
        baseObject.transform.position = new Vector3(ctrler.CursorController.Model.transform.position.x, mousePoint.y, ctrler.CursorController.Model.transform.position.z);

        cur = baseObject.transform.localPosition;
        vel = (cur - prev) / Time.deltaTime;
        dist = vel.magnitude;
        angle = dist * (180f / Mathf.PI);
        prev = cur;
        rotationAxis = Vector3.Cross(Vector3.up, vel).normalized;
        //if (dist > 0.0000000001f)
        //{
        baseObject.transform.localRotation = Quaternion.Euler(rotationAxis * angle) * baseObject.transform.localRotation;
        //}
        VelocityTrack();
    }

    void VelocityTrack(){
        newPos = baseObject.transform.position;
        curVel = ((newPos - prevPos) / Time.deltaTime).magnitude;
        prevPos = newPos;       
        text.gameObject.transform.position = baseObject.transform.position + new Vector3(0, 0.04f, 0);
        Debug.Log(curVel);
        if(currentStep>1){
            if(maxVel<curVel){
                maxVel = curVel;
            }    
            if(idealReached){
                doubleUpArrow.SetActive(false);
                upArrow.SetActive(false);
                doubleDownArrow.SetActive(false);
                downArrow.SetActive(false);
                thumbsUp.SetActive(true);
            }    
            else if(curVel > maxUpperVel){
                doubleUpArrow.SetActive(false);
                upArrow.SetActive(false);
                doubleDownArrow.SetActive(true);
                downArrow.SetActive(false);
                thumbsUp.SetActive(false);
            }
            else if(curVel>idealUpperBound && curVel<maxUpperVel){
                doubleUpArrow.SetActive(false);
                upArrow.SetActive(false);
                doubleDownArrow.SetActive(false);
                downArrow.SetActive(true);
                thumbsUp.SetActive(false);
            }
            else if (curVel < idealUpperBound && curVel > idealLowerBound){
                doubleUpArrow.SetActive(false);
                upArrow.SetActive(false);
                doubleDownArrow.SetActive(false);
                downArrow.SetActive(false);
                thumbsUp.SetActive(true);
                idealReached = true;
            }
            else if(curVel<idealLowerBound && curVel>minLowerVel){
                doubleUpArrow.SetActive(false);
                upArrow.SetActive(true);
                doubleDownArrow.SetActive(false);
                downArrow.SetActive(false);
                thumbsUp.SetActive(false);
            }
            else if(curVel<minLowerVel){
                doubleUpArrow.SetActive(true);
                upArrow.SetActive(false);
                doubleDownArrow.SetActive(false);
                downArrow.SetActive(false);
                thumbsUp.SetActive(false);
            }
            text.text = ("Current Vel: "+curVel.ToString() +"\nMax Vel: "+maxVel.ToString());
        }
        else{
            text.text = ("Current Vel: "+curVel.ToString() +"\nMax Vel: to be calculated after home step");
        }
    }

    protected virtual Vector3 GetMousePoint(Transform ball)
    {
        //ToFix: can the below two be one function called point to planepoint?

        Vector3 ctrl = new Vector3(ctrler.CursorController.GetHandPosition().x, 3, ctrler.CursorController.GetHandPosition().z);
        return ctrler.CursorController.ControllerToPlanePoint(
                            reachSurface.transform.up * ball.position.y,
                            ball.position,
                            ctrl);


    }
}
