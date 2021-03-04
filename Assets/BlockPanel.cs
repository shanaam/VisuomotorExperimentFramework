using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BlockPanel : MonoBehaviour
{
    public GameObject PropertySelectionDropdown;
    public GameObject BlockParameterText;
    public GameObject TextInputField;
    public GameObject DropdownInputField;
    public GameObject BlockParameterValue;
    public GameObject UIManager;

    private int index = -1;
    private string selectedParameter;

    private ConfigurationUIManager uiManager;

    void Start()
    {
        uiManager = UIManager.GetComponent<ConfigurationUIManager>();

        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
        }
    }

    public void Populate(int index)
    {
        this.index = index;

        List<string> options = new List<string>();
        foreach (KeyValuePair<string, object> kp in uiManager.ExpContainer.Data)
        {
            if (kp.Key.StartsWith("per_block"))
            {
                options.Add(kp.Key);
            }
        }
        PropertySelectionDropdown.GetComponent<Dropdown>().ClearOptions();
        PropertySelectionDropdown.GetComponent<Dropdown>().AddOptions(options);

        if (options.Count > 0)
        {
            OnClickOption(0);
        }

        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(true);
        }
    }

    public void OnClickOption(int option)
    {
        selectedParameter = PropertySelectionDropdown.GetComponent<Dropdown>().options[option].text;
        BlockParameterText.GetComponent<Text>().text = selectedParameter;
        BlockParameterValue.GetComponent<Text>().text = "Value: " +
                                                        (uiManager.ExpContainer.Data[selectedParameter] as List<object>)[index];

        if (uiManager.ExpContainer.GetDefaultValue(
            PropertySelectionDropdown.GetComponent<Dropdown>().options[option].text) is IList)
        {
            TextInputField.SetActive(false);
            DropdownInputField.SetActive(true);

            // Set up options for dropdown
            List<object> list = uiManager.ExpContainer.GetDefaultValue(
                PropertySelectionDropdown.GetComponent<Dropdown>().options[option].text) as List<object>;

            List<string> newList = new List<string>();

            // First option is blank
            newList.Add("");

            foreach (object o in list)
            {
                newList.Add((string)o);
            }

            DropdownInputField.GetComponent<Dropdown>().ClearOptions();
            DropdownInputField.GetComponent<Dropdown>().AddOptions(newList);
        }
        else
        {
            TextInputField.SetActive(true);
            DropdownInputField.SetActive(false);
        }
    }

    public void OnInputFinishEdit(string text)
    {
        if (index == -1 || text.Length == 0) return;

        object obj = uiManager.ExpContainer.ConvertToCorrectType(text);

        if (obj.GetType().IsInstanceOfType(uiManager.ExpContainer.GetDefaultValue(selectedParameter)))
        {
            BlockParameterValue.GetComponent<Text>().text = "Value: " + text;
            ((List<object>)uiManager.ExpContainer.Data[selectedParameter])[index] = obj;
            uiManager.Dirty = true;
        }
        else
        {
            uiManager.ConfirmationPopup.GetComponent<ConfirmationPopup>().ShowPopup(
                "The input type does not match the correct type for this property.", null);
        }
    }

    public void OnDropdownFinishEdit(int option)
    {
        // If user selected blank, don't edit the parameter
        if (option == 0) return;

        BlockParameterValue.GetComponent<Text>().text = "Value: " + 
            DropdownInputField.GetComponent<Dropdown>().options[option].text;

        ((List<object>) uiManager.ExpContainer.Data[selectedParameter])[index] =
            DropdownInputField.GetComponent<Dropdown>().options[option].text;

        uiManager.BlockView.GetComponent<ConfigurationBlockManager>().UpdateBlockText();

        uiManager.Dirty = true;
    }
}
