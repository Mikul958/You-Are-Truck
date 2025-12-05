using UnityEngine;

public class Destroy : MonoBehaviour
{
    public GameObject explosionPrefab;
    
    public void kill()
    {
        Debug.Log(gameObject.name + " killed");
        // TODO prompt death
    }
    
    public void destroy()
    {
        Debug.Log(gameObject.name + " destroyed");

        Destroy(gameObject);
    }

    public void destroyExplode()
    {
        Debug.Log(gameObject.name + " exploded");

        Instantiate(explosionPrefab, transform.position, transform.rotation);
        Destroy(gameObject);
    }

    public void destroySquish()
    {
        Debug.Log(gameObject.name + " squished");

        // TODO squish effect and coroutine
        Instantiate(explosionPrefab, transform.position, transform.rotation);
        Destroy(gameObject);
    }
}
