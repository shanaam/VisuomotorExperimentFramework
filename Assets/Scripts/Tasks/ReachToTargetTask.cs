using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UXF;
using MovementType = CursorController.MovementType;

public class ReachToTargetTask : BaseTask
{
    // The current steps are as follows:
    // 1. User goes to DOCK position (the starting position)
    // 2. User moves FORWARD to HOME position (aligned)
    // 3. User moves to TARGET with reachType[1]

    MovementType[] reachType;  // Reach type for current step
    Trial trial;
    private GameObject[] targets = new GameObject[3];

    /// <summary>
    /// Initializes a task where you move from a starting position to
    /// a target in space
    /// </summary>
    /// <param name="reachType">Reach type from HOME to TARGET.</param>
    public void Init(Trial trial, MovementType reachType)
    {
        this.reachType = new MovementType[3];
        this.reachType[2] = reachType;
        this.trial = trial;
        maxSteps = 3;

        Setup();
    }

    public void Update()
    {
        if (Input.GetKeyDown("n"))
        {
            IncrementStep();
        }

        if (Finished)
        {
            ExperimentController.Instance().EndAndPrepare();
        }
    }

    public override bool IncrementStep()
    {
        targets[currentStep].SetActive(false);

        base.IncrementStep();

        if (!finished)
            targets[currentStep].SetActive(true);

        return finished;
    }

    protected override void Setup()
    {
        ExperimentController ctrler = ExperimentController.Instance();

        // Set up the dock position
        targets[0] = Instantiate(ctrler.TargetPrefab);
        targets[0].transform.position = ctrler.TargetContainer.transform.position;
        targets[0].name = "Dock";

        // Set up the home position
        targets[1] = Instantiate(ctrler.TargetPrefab);
        targets[1].transform.position = ctrler.TargetContainer.transform.position + ctrler.transform.forward * 0.05f;
        targets[1].SetActive(false);
        targets[1].name = "Home";
        Home = targets[1];

        Debug.Log(trial.settings.GetFloat("per_block_rotation"));
        
        // Set up the target
        targets[2] = Instantiate(ctrler.TargetPrefab);
        targets[2].transform.rotation = Quaternion.Euler(0f, trial.settings.GetFloat("per_block_rotation"), 0f);

        targets[2].transform.position = targets[1].transform.position + 
            targets[2].transform.forward.normalized * (trial.settings.GetFloat("per_block_distance") / 100f);

        Debug.Log(Vector3.Distance(targets[1].transform.position, targets[2].transform.position));

        targets[2].SetActive(false);
        targets[2].name = "Target";
        Target = targets[2];

        foreach (GameObject g in targets)
            g.transform.SetParent(ctrler.TargetContainer.transform);
    }

    void OnDestroy()
    {
        // When the trial ends, we need to delete all the objects this task spawned
        foreach (GameObject g in targets)
            Destroy(g);
    }
}
