using UnityEngine;

public class BatteryUsageManager : MonoBehaviour
{
    public static BatteryUsageManager Instance;

    public float initialBatteryLevel = -1f;   // Battery % when app opened
    public float appStartTime = -1f;          // Time when app started

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep alive across scenes
        }
        else
        {
            Destroy(gameObject); // Prevent duplicates
        }
    }

    void Start()
    {
        // Automatically capture initial battery level & time
        CaptureInitialBatteryLevel();
        CaptureAppStartTime();
    }

    public void CaptureInitialBatteryLevel()
    {
        if (initialBatteryLevel < 0f) // Only do it once
        {
            initialBatteryLevel = Mathf.Clamp01(SystemInfo.batteryLevel);
        }
    }

    public void CaptureAppStartTime()
    {
        if (appStartTime < 0f) // Only do it once
        {
            appStartTime = Time.realtimeSinceStartup; // In seconds since app launch
        }
    }

    public float GetSessionDuration()
    {
        if (appStartTime < 0f) return 0f;
        return Time.realtimeSinceStartup - appStartTime; // Always total time in app
    }
}
