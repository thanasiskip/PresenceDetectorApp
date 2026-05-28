using UnityEngine;
using UnityEngine.UI;

public class BatteryUI : MonoBehaviour
{
    [SerializeField] private Slider batterySlider;
    [SerializeField] private Text batteryText;
    [SerializeField] private Image statusImage;
    [SerializeField] private Image fillImage;

    [Header("Status Sprites")]
    [SerializeField] private Sprite chargingSprite;
    [SerializeField] private Sprite lowBatterySprite;
    [SerializeField] private Sprite fullSprite;

    [Header("Fill Sprites")]
    [SerializeField] private Sprite greenFillSprite;
    [SerializeField] private Sprite redFillSprite;

    void Update()
    {
        float level = Mathf.Clamp01(SystemInfo.batteryLevel);
        batterySlider.value = level;

        if (batteryText != null)
        {
            batteryText.text = Mathf.RoundToInt(level * 100f) + "%";
        }

        BatteryStatus status = SystemInfo.batteryStatus;

        // Fill sprite color and Text color according to battery level
        if (level < 0.2f)
        {
            fillImage.sprite = redFillSprite;
            batteryText.color = Color.red;
        }
        else
        {
            fillImage.sprite = greenFillSprite;
            batteryText.color = Color.green;
        }

        // Status Logic
        if (status == BatteryStatus.Charging)
        {
            statusImage.sprite = chargingSprite;
            statusImage.enabled = true;
        }
        else if (status == BatteryStatus.Full)
        {
            statusImage.sprite = fullSprite;
            statusImage.enabled = true;
        }
        else if (level < 0.2f)
        {
            statusImage.sprite = lowBatterySprite;
            statusImage.enabled = true;
        }
        else
        {
            statusImage.enabled = false;
        }
    }
}




