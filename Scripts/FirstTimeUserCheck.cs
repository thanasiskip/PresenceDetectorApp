using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using TMPro;
using System.Threading.Tasks;


public class FirstTimeUserCheck : MonoBehaviour
{
    public float delayBeforeLoad = 2f; // Time in seconds before loading
    public Text warningMessage;
    public Text LoginTextMessage;

    private string uId;
    //private string userEmail;
    DatabaseReference dbRef;

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
        StartCoroutine(CheckUserAnswers(uId));
    }

    private IEnumerator CheckUserAnswers(string userId)
    {
        //  Check for internet BEFORE hitting Firebase
        if (!HasInternetConnection())
        {
            warningMessage.text = "No internet connection. Please check your network.";
            yield break; // stop here, don't call Firebase
        }

        DatabaseReference dbRef = FirebaseDatabase.DefaultInstance.RootReference;

        Task<DataSnapshot> dataTask = dbRef.Child("users").Child(userId).GetValueAsync();
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

        //Extract Q1, Q2, Q3
        string q1 = snapshot.Child("Q1-Status").Value != null ? snapshot.Child("Q1-Status").Value.ToString() : "";
        string q2 = snapshot.Child("Q2-Years").Value != null ? snapshot.Child("Q2-Years").Value.ToString() : "";
        string q3 = snapshot.Child("Q3-Student Dormitories").Value != null ? snapshot.Child("Q3-Student Dormitories").Value.ToString() : "";

        Debug.Log($"Q1: {q1}, Q2: {q2}, Q3: {q3}");

        //Check if they have answers
        bool hasAnswered = !string.IsNullOrEmpty(q1) && !string.IsNullOrEmpty(q2) && !string.IsNullOrEmpty(q3);

        if (hasAnswered)
        {
            StartCoroutine(DelayedSceneLoad("InfiniteGpsCheck"));
        }
        else
        {
            StartCoroutine(DelayedSceneLoad("Question1"));
        }
    }
    private IEnumerator DelayedSceneLoad(string sceneName)
    {
        LoginTextMessage.text = "You are now logged in to Presence Detector!";
        yield return new WaitForSeconds(delayBeforeLoad);
        SceneManager.LoadScene(sceneName);
    }

    private bool HasInternetConnection()
    {
        return Application.internetReachability != NetworkReachability.NotReachable;
    }

}


