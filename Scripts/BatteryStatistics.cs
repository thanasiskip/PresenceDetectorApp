using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class BatteryStatisticsUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI batteryPercentText;   // Inside main circle
    [SerializeField] private TextMeshProUGUI batteryUsedText;      // Next to battery icon
    [SerializeField] private TextMeshProUGUI timeLeftText;         // Next to timeLeft icon
    [SerializeField] private TextMeshProUGUI sessionDurationText;  // NEW: plain session duration text
    [SerializeField] private Image batteryFillImage;               // Main circular fill

    [Header("Smoothing Settings")]
    [SerializeField] private float timeSmoothingSpeed = 0.1f;
    [SerializeField] private float fillSmoothingSpeed = 3f;

    private float smoothedSecondsLeft = -1f;
    private float currentFillLevel = -1f;

    void Start()
    {
        currentFillLevel = Mathf.Clamp01(SystemInfo.batteryLevel);
    }

    void Update()
    {
        float actualBatteryLevel = Mathf.Clamp01(SystemInfo.batteryLevel);

        //  Smooth battery fill animation
        if (batteryFillImage)
        {
            currentFillLevel = Mathf.Lerp(currentFillLevel, actualBatteryLevel, Time.deltaTime * fillSmoothingSpeed);
            batteryFillImage.fillAmount = currentFillLevel;
        }

        //  Inside main circle
        if (batteryPercentText)
            batteryPercentText.text = Mathf.RoundToInt(actualBatteryLevel * 100) + "%";

        //  Show Battery Used
        ShowBatteryUsed(actualBatteryLevel);

        //  Estimated remaining time
        EstimateBatteryTimeLeft(actualBatteryLevel);

        //  Show Session Duration
        ShowSessionDuration();
    }

    void ShowBatteryUsed(float currentLevel)
    {
        if (!BatteryUsageManager.Instance) return;

        float initialBattery = BatteryUsageManager.Instance.initialBatteryLevel;
        float batteryUsed = Mathf.Max(0f, (initialBattery - currentLevel) * 100f);

        if (batteryUsedText)
            batteryUsedText.text = $"Battery Used: {batteryUsed:F1}%";
    }

    void ShowSessionDuration()
    {
        if (!BatteryUsageManager.Instance || !sessionDurationText) return;

        // Global session time from BatteryUsageManager
        float sessionSeconds = BatteryUsageManager.Instance.GetSessionDuration();

        int hours = Mathf.FloorToInt(sessionSeconds / 3600f);
        int minutes = Mathf.FloorToInt((sessionSeconds % 3600f) / 60f);
        int seconds = Mathf.FloorToInt(sessionSeconds % 60f);

        sessionDurationText.text = $"Total App Duration: {hours:D2}h {minutes:D2}m {seconds:D2}s";
    }

    void EstimateBatteryTimeLeft(float currentLevel)
    {
        bool isCharging = SystemInfo.batteryStatus == BatteryStatus.Charging ||
                          SystemInfo.batteryStatus == BatteryStatus.Full;

        //  If charging - always show Charging
        if (timeLeftText && isCharging)
        {
            timeLeftText.text = "Charging...";
            smoothedSecondsLeft = -1f;
            return;
        }

        float sessionDuration = BatteryUsageManager.Instance.GetSessionDuration(); // seconds
        float initialBattery = BatteryUsageManager.Instance.initialBatteryLevel;
        float batteryUsed = Mathf.Max(0f, (initialBattery - currentLevel));

        //  If we already have measurable drain - calculate real estimate
        if (batteryUsed > 0f)
        {
            float drainRatePerSecond = batteryUsed / sessionDuration;

            if (drainRatePerSecond > 0)
            {
                float rawSecondsLeft = currentLevel / drainRatePerSecond;

                // Smooth transition
                if (smoothedSecondsLeft < 0f) smoothedSecondsLeft = rawSecondsLeft;
                smoothedSecondsLeft = Mathf.Lerp(smoothedSecondsLeft, rawSecondsLeft, timeSmoothingSpeed * Time.deltaTime);

                timeLeftText.text = FormatTimeDetailed(smoothedSecondsLeft);
                return;
            }
        }

        //  No measurable drain yet - fallback guess based on typical 10 hours full usage
        float hoursLeftGuess = currentLevel * 10f;  // 100% = 10h, 50% = 5h, etc.
        float secondsLeftGuess = hoursLeftGuess * 3600f;

        timeLeftText.text = $"Time Left: {FormatTimeDetailed(secondsLeftGuess)} (Estimated)";
    }


    string FormatTimeDetailed(float seconds)
    {
        int hours = Mathf.FloorToInt(seconds / 3600f);
        int minutes = Mathf.FloorToInt((seconds % 3600f) / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);

        return $"{hours:D2}h {minutes:D2}m {secs:D2}s";
    }
}
