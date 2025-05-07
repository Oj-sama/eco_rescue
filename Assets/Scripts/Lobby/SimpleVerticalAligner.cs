using UnityEngine;
using UnityEngine.UI;

public class SimpleVerticalAligner : MonoBehaviour
{
    public float spacing = 10f; // Space between items (adjustable in the Inspector)

    private RectTransform parentRectTransform;

    private void Start()
    {
        parentRectTransform = GetComponent<RectTransform>();
        AlignChildren();
    }

    // Method to align the child elements vertically
    public void AlignChildren()
    {
        float yOffset = 0f;  // This will track the position of the next item
        float totalHeight = 0f;  // This will track the total height of all the items

        // Loop through all children of the parent
        foreach (Transform child in transform)
        {
            RectTransform childRect = child.GetComponent<RectTransform>();
            if (childRect != null)
            {
                // If it's not the first item, adjust its position by adding the offset and spacing
                if (child != transform.GetChild(0))
                {
                    childRect.anchoredPosition = new Vector2(childRect.anchoredPosition.x, -yOffset);
                    yOffset += childRect.rect.height + spacing;  // Move yOffset by height of item + spacing
                }
                totalHeight += childRect.rect.height + spacing;  // Track the total height
            }
        }

        // Adjust the size of the parent container to fit all the items without overflow
        parentRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, totalHeight);

        // Optional: if the content exceeds the panel size, ensure the ScrollRect will allow scrolling
        parentRectTransform.GetComponentInParent<ScrollRect>().verticalNormalizedPosition = 0f;
    }
}
