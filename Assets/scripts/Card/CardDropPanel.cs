using UnityEngine;
using UnityEngine.EventSystems;

public class CardDropPanel : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        GameObject dragged = eventData.pointerDrag;
        if (dragged != null)
        {
            dragged.transform.SetParent(transform);

            // Determine correct sibling index
            int newIndex = 0;
            for (int i = 0; i < transform.childCount; i++)
            {
                RectTransform child = transform.GetChild(i) as RectTransform;
                if (child == dragged.transform) continue;

                if (dragged.transform.position.x < child.position.x)
                {
                    newIndex = i;
                    break;
                }
                newIndex = i + 1;
            }

            dragged.transform.SetSiblingIndex(newIndex);
        }
    }
}
