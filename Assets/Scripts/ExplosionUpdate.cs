using UnityEngine;

public class ExplosionUpdate : MonoBehaviour
{
    // Referenced components
    public SphereCollider collision;
    
    // Constants, set in game engine
    public float collisionDuration;
    public float visualDuration;

    // Instance variables
    private float timer = 0;

    void Update()
    {
        timer += Time.fixedDeltaTime;
        if (timer > collisionDuration)
            collision.enabled = false;
        if (timer > visualDuration)
            Destroy(gameObject);
    }
}
