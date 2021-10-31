using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class BlockInfoTxt : MonoBehaviour, IPointerClickHandler
{
    public GameObject BlockInfoText;
    public BlockPanel bp;

    public void OnPointerClick(PointerEventData eventData)
    {
        var text = BlockInfoText.GetComponent<TextMeshProUGUI>();
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            int linkIndex = TMP_TextUtilities.FindIntersectingLink(text, Input.mousePosition, null);
            if (linkIndex > -1)
            {
                var linkInfo = text.textInfo.linkInfo[linkIndex];
                var linkId = linkInfo.GetLinkID();

                bp.OnClickOption(int.Parse(linkId));
            }
        }
        
    }
}
