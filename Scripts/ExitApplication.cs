using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;



public class QuitApplication : MonoBehaviour
{
    public float delayBeforeLoad = 2f; // Time in seconds before loading

    void Start()
    {
        Invoke("LoadSceneWithDelay", delayBeforeLoad);
    }

    void LoadSceneWithDelay()
    {
        Application.Quit();

    }
}