using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UXF;
using MovementType = CursorController.MovementType;

public class ReachTrack : ReachToTargetTask
{
    protected GameObject field;
    protected GameObject goal;
    // Start is called before the first frame update
    public override void Setup()
    {
        ctrler = ExperimentController.Instance();
        trial = ctrler.Session.CurrentTrial;
        d = null;

        Cursor.visible = false;

        reachPrefab = Instantiate(ctrler.GetPrefab("ReachTrack"));
        reachPrefab.transform.SetParent(ctrler.transform);
        reachPrefab.transform.localPosition = new Vector3(0,-0.8f,0);

        reachCam = GameObject.Find("ReachCamera");
        reachSurface = GameObject.Find("Surface");
        timerIndicator = GameObject.Find("TimerIndicator").GetComponent<TimerIndicator>();
        scoreboard = GameObject.Find("Scoreboard").GetComponent<Scoreboard>();
        tint = GameObject.Find("Tint");
        field = GameObject.Find("Field");
        goal = field.transform.GetChild(0).transform.GetChild(0).gameObject;


        SetSetup();

        field.transform.position = targets[1].transform.position;

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
        base.Update();
    }
}
