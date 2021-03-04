using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Application = UnityEngine.Application;

public class ConfigurationUIManager : MonoBehaviour
{
    // List of all files currently in the StreamingAssets directory
    private FileInfo[] files;

    // File Information
    public ExperimentContainer ExpContainer;

    // String representation of the file currently being edited
    private string currentFile;

    // GameObjects for the various UI components
    public GameObject ConfirmationPopup, FileDropdown, BlockView, DirtyText, FileSaveDialog;

    public GameObject BlockTab, PropertyTab;

    // When true, the user has made a modification to the JSON without saving.
    // We use this to let the user know in the UI they have unsaved changes.
    private bool dirty;

    // Reserved filename for JSON file that contains information for type checking
    public const string MASTER_JSON_FILENAME = "experiment_parameters.json";

    /// <summary>
    /// Public accessor for "dirty" variable. When the variable is modified
    /// We also let the user know via updating the UI
    /// </summary>
    public bool Dirty
    {
        get => dirty;
        set
        {
            dirty = value;
            UpdateDirtyText();
        }
    }

    // Key-value map containing all the default values for each parameter type currently supported.
    // This can be modified via the UI or in the json file
    private Dictionary<string, object> masterParameters;

    // The zero based index representing which block is currently selected by the user to modify
    public int CurrentSelectedBlock;

    // Determines if the block editor or property editor should be displayed
    private bool enableBlockPanel = true;

    // Property Panel Objects
    public Text PropertyNameText, PropertyValueText;
    public InputField PropertyNameInput, PropertyValueInput;
    public GameObject BlockPanel, PropertyPanel;
    public GameObject BlockPropertiesButton;
    public Dropdown PropertyDropdown;

    private int selectedProperty;
    
    // Start is called before the first frame update
    void Start()
    {
        // Assign callback for file save dialog prompt
        FileSaveDialog.GetComponent<FileSavePopup>().Callback += SaveAs;

        GetFiles();

        string path = Application.dataPath + "/StreamingAssets/experiment_parameters.json";
        if (File.Exists(path))
        {
            masterParameters = (Dictionary<string, object>)MiniJSON.Json.Deserialize(File.ReadAllText(
                path));
        }
        else
        {
            Debug.LogWarning("Master JSON does not exist.");
        }
    }

    /// <summary>
    /// Updates the list of files in the StreamingAssets folder
    /// </summary>
    void GetFiles()
    {
        DirectoryInfo d = new DirectoryInfo(Application.dataPath + "/StreamingAssets");
        files = d.GetFiles("*.json").Where(
            file => file.Name != MASTER_JSON_FILENAME).ToArray();

        List<Dropdown.OptionData> fileOptions = new List<Dropdown.OptionData>();
        foreach (FileInfo f in files)
        {
            fileOptions.Add(new Dropdown.OptionData(f.Name));
        }

        FileDropdown.GetComponent<Dropdown>().options.AddRange(fileOptions);
    }

    // Update is called once per frame
    void Update()
    {
        /*
        if (Input.GetKeyDown(KeyCode.A))
        {
            ConfirmationPopup.GetComponent<ConfirmationPopup>().ShowPopup(
                "Testing popup thingy", OnConfirmationPopup);
        }
        */
        //DirtyText.SetActive(Dirty);
    }

    /// <summary>
    /// Callback method for when the user interacts with the popup
    /// </summary>
    /// <param name="accept"></param>
    void OnConfirmationPopup(bool accept)
    {
        // Unsubscribe
        ConfirmationPopup.GetComponent<ConfirmationPopup>().ConfirmCallback -= OnConfirmationPopup;
    }

    void SaveFile(string fileName)
    {
        string json = MiniJSON.Json.Serialize(ExpContainer.Data);
        File.WriteAllText(fileName, json);
        currentFile = fileName;
    }

    /// <summary>
    /// Executed when the user presses "Save" on the UI
    /// </summary>
    void Save()
    {
        if (dirty)
        {
            SaveFile(currentFile);
            Dirty = false;
        }
    }

    /// <summary>
    /// Executed when the user presses confirm in the "Save As" popup.
    /// The "Save As" popup allows the user to enter their own file name before saving.
    /// </summary>
    /// <param name="accept"></param>
    void SaveAs(bool accept, string fileName)
    {
        if (accept)
        {
            SaveFile(Application.dataPath + "/StreamingAssets/" + fileName);
            GetFiles();

            FileDropdown.GetComponent<Dropdown>().value = FileDropdown.GetComponent<Dropdown>().options.FindIndex(
                o => o.text == fileName
            );

            Dirty = false;
        }
    }

    /// <summary>
    /// Executes when the user presses "Save As" in the UI
    /// </summary>
    void PromptSaveDialog()
    {
        if (Dirty)
        {
            FileSaveDialog.GetComponent<FileSavePopup>().Container.SetActive(true);
        }
    }

    /// <summary>
    /// Executes when the user selects a file from the dropdown. The file is loaded, parsed, then
    /// the UI is populated with all the parameters provided by the JSON.
    /// </summary>
    /// <param name="index"></param>
    public void OpenFile(int index)
    {
        currentFile = files[index].FullName;

        if (dirty)
        {
            ConfirmationPopup.GetComponent<ConfirmationPopup>().ShowPopup(
                "You have unsaved changes. Are you sure you want to continue?", OnOpenFileConfirm);
        }
        else
        {
            OpenFile(currentFile);
        }

    }

    private void OpenFile(string fileName)
    {
        // Attempt to open file
        Dictionary<string, object> fileParameters = 
            (Dictionary<string, object>)MiniJSON.Json.Deserialize(File.ReadAllText(fileName));

        ExpContainer = new ExperimentContainer(fileParameters, masterParameters);

        // Default to show the block tab
        PropertyTab.SetActive(false);
        BlockTab.SetActive(true);

        BlockView.GetComponent<ConfigurationBlockManager>().InitializeBlockPrefabs(this, ExpContainer);
        
        // TODO: Set up property editor
        /*
        // Set up property editor
        PropertyDropdown.GetComponent<Dropdown>().ClearOptions();

        List<string> options = new List<string>();
        foreach (KeyValuePair<string, object> kp in fileParameters)
        {
            if (!kp.Key.StartsWith("per_block"))
            {
                options.Add(kp.Key);
            }
        }

        PropertyDropdown.GetComponent<Dropdown>().AddOptions(options);
        */
    }

    public void OnOpenFileConfirm(bool accept)
    {
        if (accept)
        {
            Dirty = false;
            OpenFile(currentFile);
        }
    }

    /// <summary>
    /// Generates a new file with one block and all default values
    /// </summary>
    private void NewFile()
    {
        if (dirty)
        {
            ConfirmationPopup.GetComponent<ConfirmationPopup>().ShowPopup(
                "You have unsaved changes. Are you sure you want to continue?", OnNewFileConfirm);
        }
        else
        {
            ExpContainer = new ExperimentContainer(new Dictionary<string, object>(), masterParameters);

            // Initialize dictionary with 1 block and default values
            foreach (KeyValuePair<string, object> kp in masterParameters)
            {
                List<object> list = kp.Value as List<object>;
                ExpContainer.Data[kp.Key] = new List<object>();
                List<object> newList = ExpContainer.Data[kp.Key] as List<object>;
                if (list[0].GetType() == typeof(string))
                {
                    switch ((string) list[0])
                    {
                        case ExperimentContainer.STRING_PROPERTY_ID:
                            newList.Add("");
                            break;
                        case ExperimentContainer.BOOLEAN_PROPERTY_ID:
                            newList.Add(false);
                            break;
                        case ExperimentContainer.INTEGER_PROPERTY_ID:
                            newList.Add(0);
                            break;
                        default:
                            newList.Add(list[0]);
                            break;
                    }
                }
                else
                {
                    newList.Add(list[0]);
                }
            }

            Dirty = true;

            BlockView.GetComponent<ConfigurationBlockManager>().InitializeBlockPrefabs(this, ExpContainer);
            FileDropdown.GetComponent<Dropdown>().value = 0;
        }
    }

    /// <summary>
    /// Callback function if the user decides to create a new file without saving
    /// </summary>
    void OnNewFileConfirm(bool accept)
    {
        if (accept)
        {
            Dirty = false;
            NewFile();
        }
    }

    /// <summary>
    /// Executes when the user clicks on the GameObject representing an individual block.
    /// This populates the UI with input fields that allow the user to modify the "per_block_" parameters for
    /// a particular block.
    /// </summary>
    public void OnClickBlock(GameObject btn)
    {
        if (!BlockView.GetComponent<ConfigurationBlockManager>().Dragged && 
            !Input.GetKeyDown(KeyCode.LeftShift))
        {
            BlockPanel.GetComponent<BlockPanel>().Populate(btn.GetComponent<BlockComponent>().BlockID);
            CurrentSelectedBlock = btn.GetComponent<BlockComponent>().BlockID;
        }
    }

    /// <summary>
    /// Updates the text shown on the screen depending on the value of dirty
    /// </summary>
    private void UpdateDirtyText()
    {
        DirtyText.SetActive(dirty);
    }


    /// <summary>
    /// Switches between showing the editor for blocks and custom parameters
    /// </summary>
    void SwapPanel()
    {
        enableBlockPanel = !enableBlockPanel;

        BlockPanel.SetActive(enableBlockPanel);
        PropertyPanel.SetActive(!enableBlockPanel);
    }

    // Property Editor Panel
    void OnSelectProperty(int index)
    {
        PropertyNameText.gameObject.SetActive(true);
        PropertyNameText.text =
            "Property Name: " + PropertyDropdown.options[index].text;

        PropertyValueText.gameObject.SetActive(true);
        PropertyValueText.text =
            "Property Value: " + string.Join(", ", ExpContainer.Data[
                PropertyDropdown.options[index].text
            ]);

        //PropertyNameInput.
    }

    /// <summary>
    /// Executes when the user modifies a value in the property
    /// </summary>
    /// <param name="newValue"></param>
    void OnEndPropertyValueEdit(string newValue)
    {
        // Converts the comma separated input into a list of the correct type
        string[] values = newValue.Split(',');

        List<object> newList = new List<object>();
        foreach (string val in values)
        {
            newList.Add(ExpContainer.ConvertToCorrectType(val));
        }

        // Replace the old list
        ExpContainer.Data[PropertyDropdown.options[PropertyDropdown.value].text] = newList;
    }

    /// <summary>
    /// Executes when the user modifies the name of the property
    /// </summary>
    /// <param name="newName"></param>
    void OnEndPropertyNameEdit(string newName)
    {
        // Move old list to new key and delete old key
        ExpContainer.Data[newName] =
            ExpContainer.Data[PropertyDropdown.options[PropertyDropdown.value].text];

        ExpContainer.Data.Remove(PropertyDropdown.options[PropertyDropdown.value].text);
    }
}
