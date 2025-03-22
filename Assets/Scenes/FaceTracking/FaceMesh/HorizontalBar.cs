using UnityEngine;
using UnityEngine.UI;

public class HorizontalBar : MonoBehaviour
{
    public GameObject barPrefab; // Prefab for the bar (contains background and fill)
    public Transform panel; // Parent panel for the bars
    public float[] values; // Values for each bar (normalized between 0 and 1)
    public float barHeight = 30f; // Height of each bar
    public float spacing = 10f; // Spacing between bars

    void Start()
    {
        CreateBars();
    }

    void CreateBars()
    {
        float panelWidth = panel.GetComponent<RectTransform>().rect.width; // Width of the panel
        float startY = 100; // Start position for the first bar

        for (int i = 0; i < values.Length; i++)
        {
            // Instantiate a bar
            GameObject bar = Instantiate(barPrefab, panel);
            RectTransform barRect = bar.GetComponent<RectTransform>();

            // Set anchor to stretch horizontally and anchor to the bottom
            barRect.anchorMin = new Vector2(0, 0);
            barRect.anchorMax = new Vector2(1, 0);
            barRect.pivot = new Vector2(0, 0);

            // Position and size the bar
            barRect.sizeDelta = new Vector2(-150, barHeight); // Height is fixed, width will stretch
            barRect.anchoredPosition = new Vector2(100, startY + i * (barHeight + spacing));

            // Get the fill element
            Transform fill = bar.transform.Find("BarFill");
            if (fill != null)
            {
                RectTransform fillRect = fill.GetComponent<RectTransform>();
                fillRect.sizeDelta = new Vector2(barRect.rect.width * values[i], barHeight); // Scale fill width
            }
        }
    }
}