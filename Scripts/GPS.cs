using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Android;

public class GPS : MonoBehaviour
{
    //public Text gpsOutLong;
    //public Text gpsOutLat;
    public Text warningMessage;
    public bool isUpdating;
    private void Update()
    {
        if (!isUpdating)
        {
            StartCoroutine(GetLocation());
            isUpdating = !isUpdating;
        }
    }
    IEnumerator GetLocation()
    {
        //float Lat;
        //float Long;

        //Lat = CoordsValues.Lat = 300;
        //Long = CoordsValues.Long = 400;
        //Debug.Log(CoordsValues.Lat);
        //Debug.Log(CoordsValues.Long);
  

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

            // Access granted and location value could be retrieved
            //print("Location: " + Input.location.lastData.latitude + " " + Input.location.lastData.longitude + " " + Input.location.lastData.altitude + " " + Input.location.lastData.horizontalAccuracy + " " + Input.location.lastData.timestamp);
        }
        
        // Stop service if there is no need to query location updates continuously
        isUpdating = !isUpdating;
        Input.location.Stop();
    }
    
}