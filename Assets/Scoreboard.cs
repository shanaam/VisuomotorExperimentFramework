using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Scoreboard : MonoBehaviour
{
    public bool WorldSpace = true;

    public GameObject CameraSpaceCanvas, WorldSpaceCanvas;
    public Text CamSpaceText, WorldSpaceText;

    // Start is called before the first frame update
    void Start()
    {
        if (WorldSpace)
        {
            CameraSpaceCanvas.SetActive(false);
            WorldSpaceCanvas.SetActive(true);
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
