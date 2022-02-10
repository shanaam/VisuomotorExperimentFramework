using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class PropertyPanel : MonoBehaviour
{
    public string PropertyName, PropertyValue;

    public ConfigurationUIManager uiManager;

    public GameObject PropertyInfoText, PropertySelectionDropdown;

    public InputField NameInput, ValueInput;

    // Start is called before the first frame update
    void Start()
    {


    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnEnable()
    {
        Populate();
    }

    public void SaveProperty()
    {
        PropertyName = NameInput.text;
        PropertyValue = ValueInput.text;


        //uiManager.ExpContainer.Data[PropertyName] = PropertyValue;

        
        List<string> tempList = new List<string>();
        tempList.AddRange(PropertyValue.Split(','));

        List<object> newList = new List<object>();

        foreach (string value in tempList)
        {
            float v;
            if (!float.TryParse(value, out v))
            {
                newList.Add(value);
            }
            else
            {
                newList.Add(v);
            }

            //converttocorrectype
        }




        if (uiManager.ExpContainer.Data.ContainsKey(PropertyName))
        {
           
            uiManager.ExpContainer.Data[PropertyName] = newList;
        }
        else
        {

            uiManager.ExpContainer.Data.Add(PropertyName, newList);
 
        }

        uiManager.Dirty = true;

        Populate();
    }

    public void Populate()
    {
        PropertyInfoText.GetComponent<TextMeshProUGUI>().text = "\nProperties:\n\n";
        int i = 0;
        List<string> options = new List<string>();
        foreach (KeyValuePair<string, object> kp in uiManager.ExpContainer.Data)
        {
            if (!kp.Key.StartsWith("per_block") && !kp.Key.Equals("experiment_mode") && i < uiManager.ExpContainer.Data.Count - 4)
            {
                PropertyInfoText.GetComponent<TextMeshProUGUI>().text += "<u><link=\"" + i + "\">" + kp.Key + "</u> : " + string.Join(",",(kp.Value as List<object>)) + "</link>\n";
                options.Add(kp.Key);
                i++;
            }
        }
        PropertyInfoText.GetComponent<TextMeshProUGUI>().text += "\n\n";

        PropertySelectionDropdown.GetComponent<Dropdown>().ClearOptions();
        PropertySelectionDropdown.GetComponent<Dropdown>().AddOptions(options);
    }

    public void OnClickOption(int option)
    {
        string selectedParameter = PropertySelectionDropdown.GetComponent<Dropdown>().options[option].text;
        Debug.Log(selectedParameter);
        NameInput.text = selectedParameter;
        ValueInput.text = string.Join(",", uiManager.ExpContainer.Data[selectedParameter] as List<object>);
    }

    public void DeleteProperty()
    {
        PropertyName = NameInput.text;
        PropertyValue = ValueInput.text;

        if (uiManager.ExpContainer.Data.ContainsKey(PropertyName))
        {

            uiManager.ExpContainer.Data.Remove(PropertyName);
            uiManager.Dirty = true;

            Populate();
            PropertyName = "";
            PropertyValue = "";
        }
    }
}
