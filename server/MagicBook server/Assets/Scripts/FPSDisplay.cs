using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FPSDisplay : MonoBehaviour
{
    public GameObject fpsTextObject; // Reference to the UI game object
    private TMP_Text fpsText; // Reference to the TextMeshPro component
    private float elapsedTime = 0f;
    private int frameCount = 0;
    private int fps = 0;
    

    void Start()
    {
        fpsText = fpsTextObject.GetComponent<TMP_Text>(); // Get the TextMeshProUGUI component
    }
    void Update()
    {
        frameCount++;
        elapsedTime += Time.deltaTime;

        // Update FPS every 30 seconds
        if (elapsedTime >= 1f)
        {
            fps = (int)(frameCount / elapsedTime); // Calculate FPS
            fpsText.text = $"FPS:{fps}"; 
            elapsedTime = 0f; // Reset timer
            frameCount = 0;  // Reset frame count
        }
    }
}
