using UnityEngine;
using UnityEngine.UI;
using UXF;
using CommonUsages = UnityEngine.XR.CommonUsages;
using InputDevice = UnityEngine.XR.InputDevice;

public class InstructionTask : BaseTask
{
  /// <summary>
  /// this is an instruction script:
  /// will get the required instructions from the json File
  /// starts the time
  /// </summary>

  private ExperimentController ctrler;

  private string ins;
  private double timeRemaining = 3f;

  private GameObject instructionPanel;
  private GameObject vrInstructions;
  private GameObject instruction;
  private GameObject timer;
  private GameObject done;

  // private float vrInstructionOffset = -217.25f;

  private GameObject pinballDummy;


  private GameObject tempMainCamera;

  // offsets
  private float FWD_OFFSET = 5f;
  private float UP_OFFSET = 0f;

  public override void Setup()
  {
    ctrler = ExperimentController.Instance();

    //pinballDummy = Instantiate(ctrler.GetPrefab("PinballPrefabDummy"));
    //pinballDummy.transform.SetParent(ctrler.gameObject.transform);
    //pinballDummy.transform.localPosition = new Vector3(0, 0, 0);

    string per_block_ins = ctrler.Session.CurrentTrial.settings.GetString("per_block_instruction");
    ins = ctrler.Session.CurrentTrial.settings.GetString(per_block_ins);

    // Temporarily disable VR Camera
    // TODO: This needs to be changed when we implement instruction task for VR
    //ctrler.CursorController.SetVRCamera(false);

    //Task GameObjects
    instructionPanel = Instantiate(ctrler.GetPrefab("InstructionPanel"), this.transform);

    instruction = GameObject.Find("Instruction");
    timer = GameObject.Find("Timer");
    done = GameObject.Find("Done");
    vrInstructions = GameObject.Find("VRInstructions");
    tempMainCamera = GameObject.Find("Main Camera");

    // move the vrInstructions to be in fromt of the Main Camera
    vrInstructions.transform.position = tempMainCamera.transform.position + transform.forward * FWD_OFFSET + transform.up * UP_OFFSET;

    instruction.GetComponent<Text>().text = ins;
    vrInstructions.GetComponent<TextMesh>().text = ins;

    //countdown Timer start
    timer.GetComponent<Text>().text = System.Math.Round(timeRemaining, 0).ToString();

    //add event listener to done button
    done.GetComponent<Button>().onClick.AddListener(() => End());
  }

  private void Update()
  {
    if (timeRemaining > 0)
    {
      timeRemaining = timeRemaining - Time.deltaTime;
      timer.GetComponent<Text>().text = System.Math.Round(timeRemaining, 0).ToString();
    }
    else
    {
      //Enable Done Button
      done.GetComponent<Button>().interactable = true;
    }
    //checks to see if left hand controller pressed the joystick button to skip the menu
    if (ctrler.CursorController.MenuSkip())
    {
      End();
    }
    {

    }
  }

  void End()
  {
    ctrler.EndAndPrepare();
  }

  public override void Disable()
  {
    instructionPanel.SetActive(false);

    // Turn VR Camera back on
    // TODO: See Setup()
    ctrler.CursorController.SetVRCamera(true);
  }

  // No implementation. Overriden only because LogParameters is abstract
  public override void LogParameters() { }

  protected override void OnDestroy()
  {
    Destroy(instructionPanel);
    //pinballDummy.SetActive(false);
  }
}
