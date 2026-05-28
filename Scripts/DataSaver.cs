using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using Firebase;
using Firebase.Database;
using Firebase.Auth;


public class DataSaver : MonoBehaviour
{
    public Text Answer1;
    public Text Answer2;
    public Text Answer3;
    public Text Answer4;

   
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
    }
    public void ButtonQuestion1()
    {
        string textComponent = Answer1.GetComponent<Text>().text;
        float LatValue = CoordsValues.Lat;
        float LongValue = CoordsValues.Long;

        //Checking if user is on campus and writing cords to database only when user is inside Campus
        if (CoordsValues.Lat < 41.140688 && CoordsValues.Lat > 41.1369726 && CoordsValues.Long < 24.920689 && CoordsValues.Long > 24.910631)
        {
            dbRef.Child("users")
                .Child(uId)
                .Child("Latitude")
                .SetValueAsync(LatValue);            
            dbRef.Child("users")
                .Child(uId)
                .Child("Longitude")
                .SetValueAsync(LongValue);
        }
        

        //Checking if user is on campus and writing ABSENT/PRESENT to database
        if (LatValue<41.140688 && LatValue>41.1369726 && LongValue<24.920689 && LongValue>24.910631)
        {
            if(LatValue < 41.1402953 && LatValue > 41.1389712 && LongValue < 24.9150664 && LongValue > 24.9129843)
            {
                dbRef.Child("users")
                    .Child(uId)
                    .Child("User Location")
                    .SetValueAsync("PRESENT on Department of Electrical and Computer Engineering");

            }
            else
            {
                dbRef.Child("users")
                    .Child(uId)
                    .Child("User Location")
                    .SetValueAsync("PRESENT on Campus");

            }

        }
        else
        {
            dbRef.Child("users")
                .Child(uId)
                .Child("User Location")
                .SetValueAsync("ABSENT from Campus");

        }
        dbRef.Child("users")
            .Child(uId)
            .Child("Q1-Status")
            .SetValueAsync(textComponent);

    }

    public void ButtonQuestion2()
    {
        string textComponent = Answer2.GetComponent<Text>().text;

        dbRef.Child("users")
            .Child(uId)
            .Child("Q2-Years")
            .SetValueAsync(textComponent);


    }
    public void ButtonQuestion3()
    {
        string textComponent = Answer3.GetComponent<Text>().text;


        dbRef.Child("users")
            .Child(uId)
            .Child("Q3-Student Dormitories")
            .SetValueAsync(textComponent);
    }


    /*public void ButtonQuestion4()
    {
        string textComponent = Answer4.GetComponent<Text>().text;
 

        dbRef.Child("users")
            .Child(uId)
            .Child("Q4-Transport")
            .SetValueAsync(textComponent);


    }
    */
    public void ButtonQuestion4()
    {
        string textComponent = Answer4.GetComponent<Text>().text;

        // Prepare both answer + timestamp
        var updates = new Dictionary<string, object>
        {
        { "Q4-Transport", textComponent },
        { "Q4-Transport-Timestamp", ServerValue.Timestamp }  // Firebase server time
        };

        // Save them together atomically
        dbRef.Child("users")
            .Child(uId)
            .UpdateChildrenAsync(updates)
            .ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("Failed to save Q4 transport & timestamp: " + task.Exception);
                }
                else
                {
                    Debug.Log("Q4 transport & timestamp saved successfully!");
                }
            });
    }

    public void CheckinQuestionFloor()
    {
        string textComponent = Answer1.GetComponent<Text>().text;


        dbRef.Child("users")
            .Child(uId)
            .Child("CheckinQ1-Floor")
            .SetValueAsync(textComponent);
    }

    public void CheckinQuestionRoom()
    {
        string textComponent = Answer2.GetComponent<Text>().text;


        dbRef.Child("users")
            .Child(uId)
            .Child("CheckinQ2-Room")
            .SetValueAsync(textComponent);
    }

}
