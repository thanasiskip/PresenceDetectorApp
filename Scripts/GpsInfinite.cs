using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Android;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System;
using Firebase;
using Firebase.Database;
using Firebase.Auth;
using System.Threading.Tasks;

public class GpsInfinite : MonoBehaviour
{
    public float delayBeforeLoad = 2f; // Time in seconds before loading
    private string uId;
    //private string userEmail;
    DatabaseReference dbRef;

    public Text warningMessage;
    public Text arriveMessage;
    public bool isUpdating;

    public void Awake()
    {
        var currentUser = FirebaseAuth.DefaultInstance.CurrentUser;

        if (currentUser == null)
        {
            uId = "NULL";
        }
        //uId = currentUser.UserId;
        uId = SystemInfo.deviceUniqueIdentifier;
        //userEmail = currentUser.Email;
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
    }
    private void Update()
    {
        if (!isUpdating)
        {
            StartCoroutine(GetLocationInfinite());
            isUpdating = !isUpdating;
        }

    }
    IEnumerator GetLocationInfinite()
    {
        //float Lat;
        //float Long;

        //Lat = CoordsValues.Lat = 300;
        //Long = CoordsValues.Long = 400;
        //Debug.Log(CoordsValues.Lat);
        //Debug.Log(CoordsValues.Long);
        //Debug.Log(warningMessage.text);

        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            Permission.RequestUserPermission(Permission.FineLocation);
            Permission.RequestUserPermission(Permission.CoarseLocation);
        }
        // First, check if user has location service enabled
        if (!Input.location.isEnabledByUser)
            yield return new WaitForSeconds(10);

        // Start service before querying location
        Input.location.Start();

        // Wait until service initializes
        int maxWait = 2;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        // Service didn't initialize in 20 seconds
        if (maxWait == 1)
        {
            warningMessage.text = "Timed out";
            print("Timed out");
            yield break;
        }

        // Connection has failed
        if (Input.location.status == LocationServiceStatus.Failed)
        {
            warningMessage.text = "Unable to determine device location";
            print("Unable to determine device location");
            yield break;
        }
        else
        {

            //gpsOut.text = Input.location.lastData.latitude + " " + Input.location.lastData.longitude + " " + Input.location.lastData.altitude+100f + " " + Input.location.lastData.horizontalAccuracy + " " + Input.location.lastData.timestamp;
            CoordsValues.Lat = Input.location.lastData.latitude;
            CoordsValues.Long = Input.location.lastData.longitude;
            CoordsValues.Alt = Input.location.lastData.altitude + 100f;

            // Access granted and location value could be retrieved
            //print("Location: " + Input.location.lastData.latitude + " " + Input.location.lastData.longitude + " " + Input.location.lastData.altitude + " " + Input.location.lastData.horizontalAccuracy + " " + Input.location.lastData.timestamp);
        }

        // Stop service if there is no need to query location updates continuously
        isUpdating = !isUpdating;
        //warningMessage.text = "Location:" + CoordsValues.Lat.ToString() + " " + CoordsValues.Long.ToString();

        //Writing GPS coords to database only when user is inside campus for privacy safety!
        if (CoordsValues.Lat < 41.140688 && CoordsValues.Lat > 41.1369726 && CoordsValues.Long < 24.920689 && CoordsValues.Long > 24.910631)
        { 
            dbRef.Child("users")
                .Child(uId)
                .Child("Latitude")
                .SetValueAsync(CoordsValues.Lat);
            dbRef.Child("users")
                .Child(uId)
                .Child("Longitude")
                .SetValueAsync(CoordsValues.Long);
        }

        //Checking if user is on campus and writing data to database
        if (CoordsValues.Lat < 41.140688 && CoordsValues.Lat > 41.1369726 && CoordsValues.Long < 24.920689 && CoordsValues.Long > 24.910631)
        {
            if (CoordsValues.Lat < 41.1402953 && CoordsValues.Lat > 41.1389712 && CoordsValues.Long < 24.9150664 && CoordsValues.Long > 24.9129843)
            {
                dbRef.Child("users")
                    .Child(uId)
                    .Child("User Location")
                    .SetValueAsync("PRESENT on Department of Electrical and Computer Engineering");

                arriveMessage.text = "You are now at Department of Electrical and Computer Engineering!";
            }
            else
            {
                dbRef.Child("users")
                    .Child(uId)
                    .Child("User Location")
                    .SetValueAsync("PRESENT on Campus");

                arriveMessage.text = "You are now at Campus!";
            }

            //Checking if there's any Q4-Transport answer in Database and loading the question if there's not
            Task<DataSnapshot> dataTask = dbRef.Child("users").Child(uId).GetValueAsync();
            yield return new WaitUntil(() => dataTask.IsCompleted);

            if (dataTask.Exception != null)
            {
                // Cannot read Data
                Debug.LogError("Database read failed: " + dataTask.Exception);
                yield break;
            }

            DataSnapshot snapshot = dataTask.Result;

            if (!snapshot.Exists)
            {
                StartCoroutine(DelayedSceneLoad("Question1"));
                yield break;
            }

            //Extract Q4
            string q4 = snapshot.Child("Q4-Transport").Value != null ? snapshot.Child("Q4-Transport").Value.ToString() : "";

            //Check if Database got any Q4 answers . If there's no answers load question4 scene. If there's answer , check when user answered the question.
            //If answer exists for 12hours dont load it again!
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
                    Debug.Log("No timestamp found - treat as old - load Question4");
                    StartCoroutine(DelayedSceneLoad("Question4"));
                }
                else
                {
                    DateTime lastWriteTime = DateTimeOffset.FromUnixTimeMilliseconds(lastWriteTimeMs).UtcDateTime;
                    TimeSpan diff = DateTime.UtcNow - lastWriteTime;

                    if (diff.TotalHours > 12)
                    {
                        Debug.Log("Q4 answer is older than 12h - load Question4 again");
                        StartCoroutine(DelayedSceneLoad("Question4"));
                    }
                    else
                    {
                        Debug.Log($"Q4 was answered {diff.TotalHours:F1} hours ago - still valid");
                    }
                }
            }

        }
        else
        {
            dbRef.Child("users")
                .Child(uId)
                .Child("User Location")
                .SetValueAsync("ABSENT from Campus");

        }
        if (CoordsValues.Lat < 41.140688 && CoordsValues.Lat > 41.1369726 && CoordsValues.Long < 24.920689 && CoordsValues.Long > 24.910631)
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
