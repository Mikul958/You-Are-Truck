using UnityEngine;

public class Destroy : MonoBehaviour
{
    public void destroy()
    {
        Debug.Log(gameObject.name + " destroyed");

        Destroy(gameObject);
    }

    public void destroyExplode()
    {
        Debug.Log(gameObject.name + " exploded");

        // TODO spawn explosion and coroutine
        Destroy(gameObject);
    }

    public void destroySquish()
    {
        Debug.Log(gameObject.name + " squished");

        // TODO squish, spawn explosion, and coroutine
        Destroy(gameObject);
    }
}
