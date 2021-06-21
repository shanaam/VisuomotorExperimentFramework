using UnityEngine;
using UnityEngine.UI;

public class ConfirmationPopup : MonoBehaviour
{
    public delegate void ConfirmationCallback(bool accept);
    public event ConfirmationCallback ConfirmCallback;

    public GameObject Container;
    public GameObject MessageContainer;

    // Default position of the "Confirm" button
    private Vector3 ConfirmPosition;

    public GameObject ConfirmButton, CancelButton;

    void Start()
    {
        ConfirmPosition = ConfirmButton.GetComponent<RectTransform>().position;
    }

    public void ShowPopup(string message, ConfirmationCallback callback)
    {
        if (!Container.activeSelf)
        {
            Container.SetActive(true);
            MessageContainer.GetComponent<Text>().text = message;

            // If callback is null, then just show the accept button
            if (callback != null)
            {
                ConfirmCallback += callback;
                
                // Re-enable cancel button and move confirm button back to it's correct position
                CancelButton.SetActive(true);
                ConfirmButton.transform.position = ConfirmPosition;

            }
            else
            {
                // Disable cancel button and move confirm to center
                CancelButton.SetActive(false);
                ConfirmButton.GetComponent<RectTransform>().position = new Vector3(
                    ConfirmPosition.x + 45f,
                    ConfirmButton.transform.position.y,
                    ConfirmButton.transform.position.z
                    );
            }
        }
        else
        {
            Debug.Log("There is already a popup");
        }
    }

    public void AcceptDialog()
    {
        ConfirmCallback?.Invoke(true);
        Container.SetActive(false);
    }

    public void DeclineDialog()
    {
        ConfirmCallback?.Invoke(false);
        Container.SetActive(false);
    }
}
