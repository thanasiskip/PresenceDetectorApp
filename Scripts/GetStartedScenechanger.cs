using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;



public class GetStartedScenechanger : MonoBehaviour
{
    public string sceneToLoad; // Assign the scene name in the Inspector
    public float delayBeforeLoad = 2f; // Time in seconds before loading
    public Text warningMessage;
    public Text LoginTextMessage;

    void Start()
    {
        Invoke("LoadSceneWithDelay", delayBeforeLoad);
    }

    void LoadSceneWithDelay()
    {
        float value1 = CoordsValues.Lat;
        float value2 = CoordsValues.Long;

        if (value1 != 0 && value2 != 0)
        {
            LoginTextMessage.text = "You are now logged in to Presence Detector!";
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            warningMessage.text = "No Internet connection!Make sure your mobile Data or Wifi is connected to your device!";
            
        }
     
    }
}