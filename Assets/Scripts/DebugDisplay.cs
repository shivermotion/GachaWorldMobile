using UnityEngine;
using TMPro;

public class DebugDisplay : MonoBehaviour
{
    // Assign this in the Inspector
    public TMP_Text debugText;

    // Reference to your GameManager (assign in the Inspector)
    public GameManager gameManager;

    // Update is called once per frame
    void Update()
    {
        if (debugText != null && gameManager != null)
        {
            // Instead of using JSON, call the custom ToString() method
            debugText.text = gameManager.GetGameData().ToString();
        }
    }
}
