using UnityEngine;

public class BarrelCollide : MonoBehaviour
{
    // Referenced components
    public Rigidbody rigidBody;
    public Destroy barrelDestroy;

    // Constants, set in game editor
    public float criticalSpeed;
    
    void FixedUpdate()
    {
        if (rigidBody.position.y < 0)
            barrelDestroy.destroy();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.relativeVelocity.magnitude >= criticalSpeed)
            barrelDestroy.destroyExplode();
        if (collision.collider.gameObject.layer == LayerMask.NameToLayer("KillExplode") || collision.collider.gameObject.layer == LayerMask.NameToLayer("KillSquish"))
            barrelDestroy.destroyExplode();
    }
}
