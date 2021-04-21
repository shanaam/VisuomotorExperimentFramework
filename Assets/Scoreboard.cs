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

    private int numTrials;

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
        }
        else
        {
            CameraSpaceCanvas.SetActive(true);
            WorldSpaceCanvas.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (WorldSpace)
        {
            WorldSpaceText.text = "Score: " + ExperimentController.Instance().Score;
        }
        else
        {
            CamSpaceText.text = "Score: " + ExperimentController.Instance().Score;
        }
    }
}
