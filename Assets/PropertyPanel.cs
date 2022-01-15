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

    // Start is called before the first frame update
    void Start()
    {


    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void PropertyNameInput(string input)
    {
        if (input.Length == 0)
            return;

        PropertyName = input;
    }

    public void PropertyValueInput(string input)
    {
        if (input.Length == 0)
            return;

        PropertyValue = input;
    }

    public void SaveProperty()
    {
        //uiManager.ExpContainer.Data[PropertyName] = PropertyValue;


        if (uiManager.ExpContainer.Data.ContainsKey(PropertyName))
        {
           
            uiManager.ExpContainer.Data[PropertyName] = PropertyValue;
            Debug.Log(uiManager.ExpContainer.Data.ContainsKey(PropertyName));
        }
        else
        {
            Debug.Log(uiManager.ExpContainer.Data.ContainsKey(PropertyName));

            uiManager.ExpContainer.Data.Add(PropertyName, PropertyValue);


            
        }


        uiManager.Dirty = true;
    }

    public void Populate()
    {


    }

    public void UpdatePropertyText()
    {

    }
}
