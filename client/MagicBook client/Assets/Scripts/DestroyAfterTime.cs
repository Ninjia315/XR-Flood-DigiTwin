using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyAfterTime : MonoBehaviour
{
    public float DestroyAfterSeconds;
    public bool InactiveInsteadOfDestroy;

    // Start is called before the first frame update
    void Start()
    {
        Invoke(nameof(DestroyMe), DestroyAfterSeconds);
    }

    void DestroyMe()
    {
        if (InactiveInsteadOfDestroy)
            GetComponent<Renderer>().enabled = false;
        else
            Destroy(gameObject);
    }
}
