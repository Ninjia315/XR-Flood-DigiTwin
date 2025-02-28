using UnityEngine;

public class AffectedBuildingCollider : MonoBehaviour
{
    public Color TouchingFloodColor = Color.red;
    public Color NotTouchingFloodColor = Color.white;

    Renderer renderer;
    float lastEnteredTime;

    private void Start()
    {
        renderer = GetComponent<Renderer>();
    }

    private void OnTriggerEnter(Collider other)
    {
        renderer.material.color = TouchingFloodColor;
    }

    private void OnTriggerExit(Collider other)
    {
        renderer.material.color = NotTouchingFloodColor;
    }

    private void OnTriggerStay(Collider other)
    {
        renderer.material.color = TouchingFloodColor;
        lastEnteredTime = Time.time;
    }

    private void Update()
    {
        if((Time.time - lastEnteredTime) > 0.1f)
            renderer.material.color = NotTouchingFloodColor;
    }
}
