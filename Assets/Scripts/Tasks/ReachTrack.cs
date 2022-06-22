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
    protected GameObject symbols;
    protected int velResult;
    protected AudioSource sound;
    protected GameObject ray;
    protected List<UnityEngine.XR.InputDevice> devices = new List<UnityEngine.XR.InputDevice>();
    GameObject speedometer;

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
    bool hasPlayed = false;
    float originalDist;
    float curDist;
    float fieldLength;
    bool hasRotated = false;
    int scoreTrack;

    // Start is called before the first frame update
    public override void Setup()
    {
        maxSteps = 4;
        ctrler = ExperimentController.Instance();
        trial = ctrler.Session.CurrentTrial;
        d = null;

        Cursor.visible = false;

        reachPrefab = Instantiate(ctrler.GetPrefab("ReachTrack"));
        reachPrefab.transform.SetParent(ctrler.transform);
        reachPrefab.transform.localPosition = new Vector3(0, -0.8f, 0);

        reachCam = GameObject.Find("ReachCamera");
        reachSurface = GameObject.Find("Surface");
        scoreboard = GameObject.Find("Scoreboard").GetComponent<Scoreboard>();
        tint = GameObject.Find("Tint");
        field = GameObject.Find("soccer");
        goal = field.transform.GetChild(0).gameObject;
        baseObject = GameObject.Find("BaseObject");
        text = baseObject.transform.GetChild(0).GetComponent<TextMeshPro>();
        text.transform.parent = reachPrefab.transform;
        sound = baseObject.GetComponent<AudioSource>();
        ray = GameObject.Find("Ray");
        speedometer = GameObject.Find("speedometer");
        speedometer.transform.parent = field.transform;


        newPos = base.transform.position;
        prevPos = base.transform.position;

        idealVel = 0.4f;
        idealUpperBound = idealVel + 0.05f;
        idealLowerBound = idealVel - 0.05f;
        maxUpperVel = idealVel + 0.2f;
        minLowerVel = idealVel - 0.2f;

        field.SetActive(false);

        SetSetup();

        field.transform.position = new Vector3(targets[1].transform.position.x, field.transform.position.y, targets[1].transform.position.z) ;

        ctrler.CursorController.Model.GetComponent<MeshRenderer>().enabled = false;

        field.transform.rotation = Quaternion.Euler(
            0f, -targetAngle - 90f, 0f);

        
         if (ctrler.Session.settings.GetString("camera") != "vr"){
            speedometer.transform.rotation = Quaternion.Euler(90, 0, 0);
         }
        speedometer.transform.parent = reachPrefab.transform;
        speedometer.transform.localScale = new Vector3(0.04f, 0.04f, 0.04f);
        speedometer.SetActive(false);

        originalDist = goal.transform.position.magnitude - field.transform.position.magnitude;

        goal.transform.position =
        new Vector3(targets[2].transform.position.x,
        targets[2].transform.position.y - 0.005f, targets[2].transform.position.z);
        reachSurface.SetActive(true);

        curDist = goal.transform.position.magnitude - field.transform.position.magnitude;
        fieldLength = (((curDist*100)/originalDist)*0.01f)+0.2f;
        goal.transform.parent = null;
        field.transform.localScale = new Vector3(field.transform.localScale.x, field.transform.localScale.y, field.transform.localScale.z * fieldLength);

        goal.transform.parent = field.transform;

        float width = ctrler.Session.CurrentBlock.settings.GetFloat("per_block_width");
        field.transform.localScale = new Vector3(field.transform.localScale.x * width, field.transform.localScale.y, field.transform.localScale.z);
        field.GetComponent<Renderer>().material.mainTextureScale = new Vector2(width * 4 , fieldLength * 18);

        targets[2].GetComponent<BaseTarget>().CollisionModeOnly = true;

        
        
        //speedometer.SetActive(false);



    }

    // Update is called once per frame
    void Update()
    {
        UnityEngine.XR.InputDevices.GetDevicesWithRole(UnityEngine.XR.InputDeviceRole.RightHanded, devices);
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

        if(currentStep == 2){

            ray.transform.position = baseObject.transform.position;
            if(Physics.Raycast(ray.transform.position, -ray.transform.up, out RaycastHit hit, 0.1f)){
                if(hit.collider.gameObject.name == "Surface" && !hasPlayed){
                    baseObject.GetComponent<Renderer>().material.color = Color.gray;
                    sound.clip = ctrler.AudioClips["incorrect"];
                    sound.Play();
                    hasPlayed = true;
                    VibrateController(0, 0.34f, 0.15f, devices);
                }
                else if(hit.collider.gameObject.name == "soccer"){
                    baseObject.GetComponent<Renderer>().material.color = Color.white;
                    hasPlayed = false;
                }
            }
            if (currentStep == 2 &&
            ctrler.CursorController.PauseTime > 0.3f &&
            Mathf.Abs(targets[2].transform.localPosition.magnitude - baseObject.transform.localPosition.magnitude) < 0.001f){
                sound.clip = ctrler.AudioClips["correct"];
                sound.Play();
                if(trackScore){
                    ctrler.Score += scoreTrack;
                }
                IncrementStep();
            }
                
        }

        if(currentStep > 2 && !hasRotated){
            text.text = ("Max Vel: "+maxVel.ToString());
            speedometer.transform.position = goal.transform.position + new Vector3(0, 0.02f, 0);           
            speedometer.SetActive(true);
            speedometer.transform.GetChild(0).transform.Rotate (0, velResult, 0);
            hasRotated = true;

            if(trackScore){
                speedometer.GetComponentInChildren<TextMeshPro>().text = "+" + scoreTrack.ToString();
            }
            else {
                speedometer.GetComponentInChildren<TextMeshPro>().text = "";
            }
            
            //symbols.GetComponent<Animator>().SetTrigger("rot");
            StartCoroutine(Wait());
        }
    }

    IEnumerator Wait(){
        yield return new WaitForSeconds(3f);
        base.IncrementStep();
    }

    void VelocityTrack(){
        newPos = baseObject.transform.position;
        curVel = ((newPos - prevPos) / Time.deltaTime).magnitude;
        prevPos = newPos;       
        text.gameObject.transform.position = baseObject.transform.position + new Vector3(0, 0.04f, 0);

        if(currentStep==2){
            if(maxVel<curVel){
                maxVel = curVel;               
            }    
            if(idealReached){
                velResult = 0;               
            }    
            else if(maxVel > maxUpperVel){
                velResult = 75;
                scoreTrack = 1;
            }
            else if(maxVel>idealUpperBound && maxVel<maxUpperVel){
                velResult = 35;
                scoreTrack = 2;
            }
            else if (maxVel < idealUpperBound && maxVel > idealLowerBound){
                idealReached = true;
                scoreTrack = 5;
            }
            else if(maxVel<idealLowerBound && maxVel>minLowerVel){
                velResult = -35;
                scoreTrack = 2;            
            }
            else if(maxVel<minLowerVel){
                velResult = -75;
                scoreTrack = 1;
            }
            text.text = ("Current Vel: "+curVel.ToString() +"\nMax Vel: "+maxVel.ToString());
        }  
        
        else if(currentStep<2){
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
