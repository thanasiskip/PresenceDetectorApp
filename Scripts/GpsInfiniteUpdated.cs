using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Android;
using UnityEngine.SceneManagement;
using System;
using System.Threading.Tasks;
using Firebase;
using Firebase.Database;
using Firebase.Auth;
using System.Collections.Generic;

public class GpsInfiniteUpdated : MonoBehaviour
{
    public float delayBeforeLoad = 2f;
    private string uId;
    DatabaseReference dbRef;

    public Text warningMessage;
    public Text arriveMessage;
    public bool isUpdating;

    // Barometer plugin
    private AndroidJavaClass unityPlayer;
    private AndroidJavaObject currentActivity;
    private AndroidJavaClass barometerBridge;

    private float basePressure = -1f;
    private float currentPressure = 0f;
    private int currentFloor = 0;

    private const float METERS_PER_FLOOR = 3.0f;
    private const float METERS_PER_HPA = 8.3f;

    // Fixed reference altitude for floor 0
    private const float GROUND_ALTITUDE_BASE = 120.0f; // Adjust according to the building

    // Smoothing queues
    private Queue<float> altitudeHistory = new Queue<float>();
    private int altitudeSmoothingWindow = 5; // smooth over last 5 altitude readings

    private Queue<int> floorHistory = new Queue<int>();
    private int floorSmoothingWindow = 5; // smooth over last 5 floor readings

    // Accelerometer hints
    private Vector3 lastAccel;
    private float verticalAccelThreshold = 0.25f;

    void Awake()
    {
        var currentUser = FirebaseAuth.DefaultInstance.CurrentUser;
        uId = currentUser == null ? "NULL" : SystemInfo.deviceUniqueIdentifier;
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
    }

    void Start()
    {
        // Initialize barometer plugin
        unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        barometerBridge = new AndroidJavaClass("com.yourcompany.barometerplugin.UnityBarometerBridge");

        if (barometerBridge != null)
        {
            barometerBridge.CallStatic("init", currentActivity);
            Debug.Log("Barometer initialized");
        }
        else
        {
            Debug.LogWarning("Barometer plugin not found!");
        }

        lastAccel = Input.acceleration;
    }

    private void Update()
    {
        if (!isUpdating)
        {
            StartCoroutine(GetLocationAndFloor());
            isUpdating = !isUpdating;
        }
    }

    // Smooth altitude values
    private float SmoothAltitude(float newAltitude)
    {
        altitudeHistory.Enqueue(newAltitude);
        while (altitudeHistory.Count > altitudeSmoothingWindow)
            altitudeHistory.Dequeue();

        float sum = 0f;
        foreach (float a in altitudeHistory) sum += a;
        return sum / altitudeHistory.Count;
    }

    // Smooth floor values
    private int SmoothFloor(int newFloor)
    {
        floorHistory.Enqueue(newFloor);
        while (floorHistory.Count > floorSmoothingWindow)
            floorHistory.Dequeue();

        float sum = 0;
        foreach (int f in floorHistory) sum += f;
        return Mathf.RoundToInt(sum / floorHistory.Count);
    }

    IEnumerator GetLocationAndFloor()
    {
        // Request permissions
        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            Permission.RequestUserPermission(Permission.FineLocation);
            Permission.RequestUserPermission(Permission.CoarseLocation);
        }

        if (!Input.location.isEnabledByUser)
            yield return new WaitForSeconds(10);

        Input.location.Start();

        int maxWait = 2;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        if (maxWait == 1)
        {
            warningMessage.text = "Timed out";
            Debug.Log("GPS Timed out");
            yield break;
        }

        if (Input.location.status == LocationServiceStatus.Failed)
        {
            warningMessage.text = "Unable to determine device location";
            Debug.Log("Unable to determine device location");
            yield break;
        }
        else
        {
            // Get raw GPS data
            CoordsValues.Lat = Input.location.lastData.latitude;
            CoordsValues.Long = Input.location.lastData.longitude;
            float rawAlt = Input.location.lastData.altitude;

            // First smooth the altitude
            float smoothedAlt = SmoothAltitude(rawAlt);
            CoordsValues.Alt = smoothedAlt;

            // Try Barometer first
            currentPressure = barometerBridge.CallStatic<float>("getPressure");

            if (currentPressure > 0f)
            {
                // Barometer available - use pressure difference
                if (basePressure < 0f)
                    basePressure = currentPressure;

                float altitudeDiff = (basePressure - currentPressure) * METERS_PER_HPA;
                currentFloor = Mathf.RoundToInt(altitudeDiff / METERS_PER_FLOOR);

                Debug.Log("Barometer detected! Pressure=" + currentPressure.ToString("F2") + " hPa -> Floor=" + currentFloor);
            }
            else
            {
                // No barometer - fallback to smoothed GPS altitude
                float gpsAltitudeDiff = smoothedAlt - GROUND_ALTITUDE_BASE;
                int gpsFloorRaw = Mathf.RoundToInt(gpsAltitudeDiff / METERS_PER_FLOOR);

                // Then smooth the floor values as well
                currentFloor = SmoothFloor(gpsFloorRaw);

                // Detect vertical movement via accelerometer
                Vector3 accel = Input.acceleration;
                float verticalDelta = accel.z - lastAccel.z;
                lastAccel = accel;

                if (Mathf.Abs(verticalDelta) > verticalAccelThreshold)
                {
                    Debug.Log("Vertical movement detected - possible stairs/elevator");
                }

                Debug.Log(
                    "Fallback GPS Alt(Smoothed)=" + smoothedAlt.ToString("F1") +
                    "m vs baseline " + GROUND_ALTITUDE_BASE.ToString("F1") +
                    "m (Delta=" + gpsAltitudeDiff.ToString("F1") + "m) -> Floor(Smoothed)=" + currentFloor
                );
            }

            //Writing GPS coords to database only when user is inside campus for privacy safety!
            //if (CoordsValues.Lat < 41.140688 && CoordsValues.Lat > 41.1369726 && CoordsValues.Long < 24.920689 && CoordsValues.Long > 24.910631)
            //{
            dbRef.Child("users").Child(uId).Child("Latitude").SetValueAsync(CoordsValues.Lat);
            dbRef.Child("users").Child(uId).Child("Longitude").SetValueAsync(CoordsValues.Long);
            dbRef.Child("users").Child(uId).Child("Altitude_meters").SetValueAsync(CoordsValues.Alt);
            dbRef.Child("users").Child(uId).Child("BarometerPressure_hPa").SetValueAsync(currentPressure);
            Task<DataSnapshot> floorCheckTask = dbRef.Child("users").Child(uId).Child("CheckinQ1-Floor").GetValueAsync();
            yield return new WaitUntil(() => floorCheckTask.IsCompleted);

            if (floorCheckTask.Exception == null)
            {
                DataSnapshot floorSnapshot = floorCheckTask.Result;

                if (floorSnapshot == null || floorSnapshot.Value == null)
                {
                    dbRef.Child("users").Child(uId).Child("CheckinQ1-Floor").SetValueAsync(currentFloor);
                    Debug.Log("CheckinQ1-Floor written to Firebase: " + currentFloor);
                }
                else
                {
                    Debug.Log("CheckinQ1-Floor already exists in Firebase, skipping write.");
                }
            }
            else
            {
                Debug.LogWarning("Failed to check CheckinQ1-Floor in Firebase: " + floorCheckTask.Exception);
            }
            //}
        }

        isUpdating = !isUpdating;

        // Campus detection unchanged
        if (CoordsValues.Lat < 41.140688 && CoordsValues.Lat > 41.1369726 &&
            CoordsValues.Long < 24.920689 && CoordsValues.Long > 24.910631)
        {
            if (CoordsValues.Lat < 41.1402953 && CoordsValues.Lat > 41.1389712 &&
                CoordsValues.Long < 24.9150664 && CoordsValues.Long > 24.9129843)
            {
                dbRef.Child("users").Child(uId)
                    .Child("User Location")
                    .SetValueAsync("PRESENT on Department of Electrical and Computer Engineering");
                arriveMessage.text = "You are now at Department of Electrical and Computer Engineering!Please, check in...";
            }
            else
            {
                dbRef.Child("users").Child(uId)
                    .Child("User Location")
                    .SetValueAsync("PRESENT on Campus");
                arriveMessage.text = "You are now at Campus!Please, check in...";
            }

            Task<DataSnapshot> dataTask = dbRef.Child("users").Child(uId).GetValueAsync();
            yield return new WaitUntil(() => dataTask.IsCompleted);

            if (dataTask.Exception == null)
            {
                DataSnapshot snapshot = dataTask.Result;
                string q4 = snapshot.Child("Q4-Transport").Value != null ? snapshot.Child("Q4-Transport").Value.ToString() : "";
                bool hasAnswered = !string.IsNullOrEmpty(q4);

                if (!hasAnswered)
                {
                    StartCoroutine(DelayedSceneLoad("Question4"));
                }
                else
                {
                    long lastWriteTimeMs = snapshot.Child("Q4-Transport-Timestamp").Value != null
                        ? Convert.ToInt64(snapshot.Child("Q4-Transport-Timestamp").Value)
                        : 0;

                    if (lastWriteTimeMs == 0)
                    {
                        StartCoroutine(DelayedSceneLoad("Question4"));
                    }
                    else
                    {
                        DateTime lastWriteTime = DateTimeOffset.FromUnixTimeMilliseconds(lastWriteTimeMs).UtcDateTime;
                        TimeSpan diff = DateTime.UtcNow - lastWriteTime;
                        if (diff.TotalHours > 12)
                        {
                            StartCoroutine(DelayedSceneLoad("Question4"));
                        }
                    }
                }
            }
        }
        else
        {
            dbRef.Child("users").Child(uId).Child("User Location").SetValueAsync("ABSENT from Campus");
        }

        if (CoordsValues.Lat < 41.140688 && CoordsValues.Lat > 41.1369726 &&
            CoordsValues.Long < 24.920689 && CoordsValues.Long > 24.910631)
        {
            Input.location.Stop();
        }
    }

    private IEnumerator DelayedSceneLoad(string sceneName)
    {
        yield return new WaitForSeconds(delayBeforeLoad);
        SceneManager.LoadScene(sceneName);
    }
}

