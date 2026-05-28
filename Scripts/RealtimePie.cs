using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using TMPro;

public class RealtimePie : MonoBehaviour
{
    [System.Serializable]
    public class PieSlice
    {
        public Image image;              // UI Image slice
        public TextMeshProUGUI label;    // Label
    }

    public List<PieSlice> slices;       // List of slice+label pairs
    public string questionKey = "Q1-Status";
    public float fillSpeed = 2f;        // Animation speed
    public float labelRadius = 280f;    // Distance of label from center

    private DatabaseReference dbRef;

    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                dbRef = FirebaseDatabase.DefaultInstance.GetReference("users");
                dbRef.ValueChanged += OnDataChanged;
            }
            else
            {
                Debug.LogError("Firebase error: " + task.Result);
            }
        });
    }

    void OnDataChanged(object sender, ValueChangedEventArgs e)
    {
        if (e.DatabaseError != null)
        {
            Debug.LogError("DB Error: " + e.DatabaseError.Message);
            return;
        }

        Dictionary<string, int> answerCounts = new Dictionary<string, int>();

        foreach (var user in e.Snapshot.Children)
        {
            /*string location = user.Child("User Location").Value?.ToString();

            // Only include users who are present on campus
            if (location != "PRESENT on Campus")
                continue;
            */
            string answer = user.Child(questionKey).Value?.ToString();
            if (!string.IsNullOrEmpty(answer))
            {
                if (!answerCounts.ContainsKey(answer))
                    answerCounts[answer] = 0;
                answerCounts[answer]++;
            }
        }

        DrawPie(answerCounts);
    }

    void DrawPie(Dictionary<string, int> counts)
    {
        float total = counts.Values.Sum();
        float zRotation = 0f;
        int index = 0;

        foreach (var pair in counts)
        {
            if (pair.Value == 0) continue;
            if (index >= slices.Count) break;

            float percent = pair.Value / total;
            PieSlice slice = slices[index];

            // Set color
            slice.image.color = GetColorForIndex(index);

            // Rotate slice to start
            slice.image.transform.localRotation = Quaternion.Euler(0, 0, -zRotation);

            // Animate fill
            StartCoroutine(AnimateFill(slice.image, percent));

            // Label text
            slice.label.text = $"{pair.Key}\n{Mathf.RoundToInt(percent * 100)}%";
            slice.label.gameObject.SetActive(true);

            // Label angle: middle of this slice
            float midAngle = zRotation + (percent * 360f) / 2f;

            float radius = slice.image.rectTransform.sizeDelta.x * 0.25f;
            Vector2 labelPos = new Vector2(
                radius * Mathf.Sin(midAngle * Mathf.Deg2Rad),
                radius * Mathf.Cos(midAngle * Mathf.Deg2Rad)
            );
            slice.label.rectTransform.anchoredPosition = labelPos;
            slice.label.rectTransform.localRotation = Quaternion.identity;

            zRotation += percent * 360f;
            index++;
        }

        // Hide unused slices
        for (int i = index; i < slices.Count; i++)
        {
            slices[i].image.fillAmount = 0f;
            slices[i].label.text = "";
            slices[i].label.gameObject.SetActive(false);
        }
    }




    IEnumerator AnimateFill(Image slice, float target)
    {
        float start = slice.fillAmount;
        float time = 0f;

        while (Mathf.Abs(slice.fillAmount - target) > 0.01f)
        {
            time += Time.deltaTime * fillSpeed;
            slice.fillAmount = Mathf.Lerp(start, target, time);
            yield return null;
        }

        slice.fillAmount = target;
    }

    // Optional: distinct color per slice
    Color GetColorForIndex(int index)
    {
        Color[] colors = new Color[]
        {
        new Color(0.2f, 1f, 1f),      // Cyan
        new Color(0.2f, 0.6f, 0.2f), // Green
        new Color(0.2f, 0.4f, 0.8f), // Blue
        new Color(0.8f, 0.2f, 0.6f), // Pink
        new Color(0.6f, 0.4f, 0.2f), // Brown
        new Color(0.5f, 0.5f, 0.5f), // Gray
        new Color(1f, 0.2f, 0.2f),   // Red
        new Color(1f, 0.5f, 0f),     // Orange
        new Color(0.9f, 0.9f, 0.2f), // Yellow
        new Color(0.4f, 0.2f, 0.8f), // Purple
        new Color(0.1f, 0.7f, 0.7f), // Teal
        new Color(0.7f, 0.3f, 0.3f), // Brick Red
        new Color(0.3f, 0.7f, 0.3f), // Light Green
        new Color(0.3f, 0.3f, 0.7f), // Navy Blue
        new Color(1f, 0.7f, 0.2f),   // Gold
        new Color(0.7f, 0.7f, 0.7f)  // Silver
        };

        Color baseColor = colors[index % colors.Length];
        return Color.Lerp(baseColor, Color.white, 0.5f); // 50% lighter
    }



}

