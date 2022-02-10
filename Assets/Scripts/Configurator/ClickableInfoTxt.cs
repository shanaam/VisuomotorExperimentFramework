using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.Events;

public class ClickableInfoTxt : MonoBehaviour, IPointerClickHandler
{
    public GameObject InfoText;
    //public BlockPanel bp;

    public UnityEvent<int> OnClick = new UnityEvent<int>();

    public void OnPointerClick(PointerEventData eventData)
    {
        var text = InfoText.GetComponent<TextMeshProUGUI>();
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            int linkIndex = TMP_TextUtilities.FindIntersectingLink(text, Input.mousePosition, null);
            if (linkIndex > -1)
            {
                var linkInfo = text.textInfo.linkInfo[linkIndex];
                var linkId = linkInfo.GetLinkID();


                OnClick.Invoke(int.Parse(linkId));
                //bp.OnClickOption(int.Parse(linkId));
            }
        }
        
    }
}
