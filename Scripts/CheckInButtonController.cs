using UnityEngine;
using UnityEngine.UI;

public class CheckInButtonController : MonoBehaviour
{
    public Button checkInButton;      // Reference to the Check In button
    public Text arriveText;           // The text that triggers the button
    public Text holdTightText;        // The "Hold Tight" text to hide when button is shown

    public float breatheSpeed = 1f;   // Breathing speed
    public float breatheScale = 1.1f; // Max scale factor for breathing

    private Vector3 originalScale;
    private bool shouldBreathe = false;

    // The two allowed trigger messages
    //private string message1 = ""; // For testing ""
    //private string message2 = ""; // For testing ""
    private string message1 = "You are now at Department of Electrical and Computer Engineering!Please, check in...";
    private string message2 = "You are now at Campus!Please, check in...";

    void Start()
    {
        // Save the button's original scale
        originalScale = checkInButton.transform.localScale;

        // Initially show HoldTightText and hide the CheckIn button
        holdTightText.gameObject.SetActive(true);
        checkInButton.gameObject.SetActive(false);
    }

    void Update()
    {
        // Check if arriveText matches exactly one of the allowed values
        bool showButton = arriveText.text == message1 || arriveText.text == message2;

        if (showButton)
        {
            // Show button and hide HoldTightText
            if (!checkInButton.gameObject.activeSelf)
            {
                checkInButton.gameObject.SetActive(true);
                holdTightText.gameObject.SetActive(false);
                shouldBreathe = true;
            }
        }
        else
        {
            // Hide button and show HoldTightText
            if (checkInButton.gameObject.activeSelf)
            {
                checkInButton.gameObject.SetActive(false);
                holdTightText.gameObject.SetActive(true);
                shouldBreathe = false;

                // Reset scale
                checkInButton.transform.localScale = originalScale;
            }
        }

        // Breathing animation
        if (shouldBreathe)
        {
            float scaleFactor = 1f + (Mathf.Sin(Time.time * breatheSpeed) * 0.5f + 0.5f) * (breatheScale - 1f);
            checkInButton.transform.localScale = originalScale * scaleFactor;
        }
    }
}
