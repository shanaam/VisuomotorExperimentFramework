using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UXF;

public class LocalizationTask : BaseTask
{

    private GameObject[] targets = new GameObject[2];
    private GameObject arc;

    private Trial trial;

    private bool expand;

    public void Init(Trial trial)
    {
        maxSteps = 3;
        this.trial = trial;
        Setup();
    }

    public void LateUpdate()
    {
        if (currentStep == 1 && 
            ExperimentController.Instance().CursorController.PauseTime > 0.5f)
        {

        }
    }

    public override bool IncrementStep()
    {
        switch (currentStep)
        {
            case 0: // Enter home
                Home.SetActive(false);

                ExperimentController.Instance().OnEnterHome();

                foreach (GameObject g in Trackers)
                    g.GetComponent<PositionRotationTracker>().StartRecording();

                break;
            case 1: // Pause in arc
                break;
            case 2: // Select the spot they think their real hand is
                break;
        }

        base.IncrementStep();
        return finished;
    }

    protected override void Setup()
    {
        ExperimentController ctrler = ExperimentController.Instance();

        ctrler.CursorController.SetHandVisibility(false);

        // Set up the dock position
        Home = Instantiate(ctrler.TargetPrefab);
        Home.transform.position = ctrler.TargetContainer.transform.position;
        Home.name = "Dock";

        var targetAngles = ctrler.Session.settings.GetFloatList(
            trial.settings.GetString("per_block_targetListToUse")
        );

        Target = new GameObject("Arc");
        Target.transform.rotation = Quaternion.Euler(
            0f,
            -targetAngles[Random.Range(0, targetAngles.Count - 1)] + 90f,
            0f);

        Target.transform.position = targets[1].transform.position +
                                    targets[2].transform.forward.normalized *
                                    (trial.settings.GetFloat("per_block_distance") / 100f);

        Target.AddComponent<MeshFilter>();
        Target.AddComponent<MeshRenderer>();

        Target.GetComponent<MeshFilter>().mesh = GenerateArc();

        Target.SetActive(false);
    }

    protected override void OnDestroy()
    {
        foreach (GameObject g in targets)
            Destroy(g);

        base.OnDestroy();
    }


    private Mesh GenerateArc()
    {
        Mesh mesh = new Mesh();


        // TODO: Copy old mesh code into here

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();

        return mesh;
    }
}
