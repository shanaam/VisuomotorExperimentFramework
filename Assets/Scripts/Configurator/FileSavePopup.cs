using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FileSavePopup : MonoBehaviour
{
    public GameObject FileText;
    public GameObject Container;
    public GameObject UIManager;

    private string fileName;

    public delegate void SaveCallback(bool accept, string filename);
    public event SaveCallback Callback;

    public void OnConfirm()
    {
        if (fileName.Length > 1)
        {
            Callback?.Invoke(true, fileName);
            Container.SetActive(false);
        }
    }

    public void OnCancel()
    {
        Container.SetActive(false);
        Callback?.Invoke(false, fileName);
    }

    public void OnFinishEdit(string fileName)
    {
        this.fileName = fileName + ".json";
        FileText.GetComponent<Text>().text =
            "File will be saved as: " + this.fileName;
    }
}
