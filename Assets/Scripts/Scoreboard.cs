using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Scoreboard : MonoBehaviour
{
    public bool WorldSpace = true;
    public bool TrackTrials = false;

    public GameObject CameraSpaceCanvas, WorldSpaceCanvas;
    public Text CamSpaceText, WorldSpaceText, TrialTrackText;

    public String ManualScoreText = "";

    /// <summary>
    /// When true, the score will not update based on the ExperimentController's score
    /// </summary>
    public bool AllowManualSet = false;

    /// <summary>
    /// When true, the score text will be prefaced with "Score: "
    /// </summary>
    public bool ScorePrefix = true;

    // Start is called before the first frame update
    void Start()
    {
        if (WorldSpace)
        {
            CameraSpaceCanvas.SetActive(false);
            WorldSpaceCanvas.SetActive(true);

            TrialTrackText.gameObject.SetActive(TrackTrials);

            TrialTrackText.text = "Trials Remaining: " +
                                  (ExperimentController.Instance().Session.Trials.Count() -
                                  ExperimentController.Instance().Session.currentTrialNum + 1);

            ManualScoreText = ExperimentController.Instance().Score.ToString();
        }
        else
        {
            CameraSpaceCanvas.SetActive(true);
            WorldSpaceCanvas.SetActive(false);

            ManualScoreText = ExperimentController.Instance().Score.ToString();
        }
    }

    // Update is called once per frame
    void Update()
    {
        Text target = WorldSpace ? WorldSpaceText : CamSpaceText;

        if (AllowManualSet)
        {
            if (ScorePrefix) target.text = "Score: " + ManualScoreText;
            else target.text = ManualScoreText;
        }
        else
        {
            if (ScorePrefix) target.text = "Score: " + ExperimentController.Instance().Score;
            else target.text = "" + ExperimentController.Instance().Score;
        }
    }
}