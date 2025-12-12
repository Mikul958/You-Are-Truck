using UnityEngine;

public class Destroy : MonoBehaviour
{
    public GameObject explosionPrefab;
    
    public void destroy()
    {
        Destroy(gameObject);
    }

    public void destroyExplode()
    {
        Instantiate(explosionPrefab, transform.position, transform.rotation);
        Destroy(gameObject);
    }

    public void destroySquish()
    {
        Instantiate(explosionPrefab, transform.position, transform.rotation);
        Destroy(gameObject);
    }
}
