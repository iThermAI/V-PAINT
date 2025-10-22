using UnityEngine;

public class SetVideoPanelSize : MonoBehaviour
{
    public RectTransform videoPanel;

    void Start()
    {
        // تنظیم سایز
        videoPanel.sizeDelta = new Vector2(200, 150);

        // تنظیم موقعیت (مثلاً گوشه پایین-راست)
        videoPanel.anchorMin = new Vector2(1, 0);  // Bottom-Right
        videoPanel.anchorMax = new Vector2(1, 0);
        videoPanel.pivot = new Vector2(1, 0);      // گوشه پایین راست خودش
        videoPanel.anchoredPosition = new Vector2(-20, 20);  // فاصله از گوشه
    }
}
