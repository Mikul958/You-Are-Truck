using UnityEngine;

public class ExplosionUpdate : MonoBehaviour
{
    // Constants, set in game engine
    public float explosionRadius;
    public float explosionDuration;

    // Instance variables
    private float timer;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        timer = explosionDuration;
    }

    // Update is called once per frame
    void Update()
    {
        timer -= Time.fixedDeltaTime;
        if (timer <= 0f)
            Destroy(gameObject);
    }
}
