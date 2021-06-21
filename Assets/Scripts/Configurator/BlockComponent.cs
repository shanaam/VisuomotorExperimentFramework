using UnityEngine;
using UnityEngine.EventSystems;

public class BlockComponent : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public ConfigurationBlockManager BlockController;
    public int BlockID;
    public GameObject Notch;
    public GameObject Block;

    public void OnBeginDrag(PointerEventData eventData)
    {
        BlockController.Dragged = true;
        BlockController.OnBlockBeginDrag(gameObject);
        //parent = transform.parent;
        //transform.SetParent(transform.parent.parent.parent.parent);
    }

    public void OnDrag(PointerEventData eventData)
    {
        //transform.position = eventData.position;
        BlockController.OnBlockDrag(gameObject, eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        //transform.SetParent(parent);
        BlockController.Dragged = false;
        BlockController.OnEndDrag(gameObject);
    }
}
