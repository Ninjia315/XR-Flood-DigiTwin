using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class TouchInstantiate : MonoBehaviour
{
    [SerializeField] private GameObject prefabToInstantiate; // Prefab to spawn
    [SerializeField] private Transform spawnParent;          // Parent for the spawned prefab (optional)
    [SerializeField] private LayerMask interactionLayerMask;
    static GameObject instance;

    private void OnTriggerEnter(Collider other)
    {
        //Debug.Log("TEST");

        // Check if the collider belongs to an XR hand
        //if (other.GetComponent<XRDirectInteractor>())
        if((interactionLayerMask.value & (1 << other.gameObject.layer)) != 0)
        {

            if (other.Raycast(new Ray(transform.position, transform.forward), out RaycastHit hitInfo, 10f))
            {
                if (other.TryGetComponent(out FloodMeshInteractable floodMesh))
                {
                    floodMesh.OnTapped(hitInfo);
                }
                else if(other.TryGetComponent(out GeometryInteractable geom))
                {
                    geom.OnTapped(hitInfo);
                }
                else if(other.TryGetComponent(out BasicReticlePointerInteraction basic))
                {
                    basic.OnTapped(hitInfo);
                }
            }

            if (prefabToInstantiate != null)
            {
                // Get the contact point
                var contactPoint = other.ClosestPoint(transform.position);

                if (instance == null)
                {
                    // Instantiate the prefab at the contact point
                    instance = Instantiate(prefabToInstantiate, contactPoint, Quaternion.identity, spawnParent);
                    //instance.transform.localScale = Vector3.one * 0.1f;
                }
                else
                {
                    instance.transform.position = contactPoint;
                }
            }
        }
    }
}
