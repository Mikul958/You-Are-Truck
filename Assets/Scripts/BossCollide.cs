using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class BossCollide : MonoBehaviour
{    
    // Referenced game objects and components
    public Destroy bossDestroy;
    
    // Event to broadcast level win
    [HideInInspector]
    public UnityEvent onBossDeath;

    // Constants, set in game engine
    public int bossHealth;
    public float bossHitCooldown;

    // Instance variables
    private int playerBodyMask;
    private int playerWheelMask;
    private int weakSpotMask;

    private int currentHealth;
    private float hitTimer;

    void Start()
    {
        playerBodyMask = 1 << LayerMask.NameToLayer("TruckBody");
        playerWheelMask = 1 << LayerMask.NameToLayer("TruckWheel");
        weakSpotMask = 1 << LayerMask.NameToLayer("BossWeakSpot");

        currentHealth = bossHealth;
        hitTimer = bossHitCooldown;
    }

    void FixedUpdate()
    {
        hitTimer -= Time.fixedDeltaTime;
        if (hitTimer < 0f)
            hitTimer = 0f;
    }
    
    void OnCollisionEnter(Collision collision)
    {
        int thisLayerMask = 1 << collision.GetContact(0).thisCollider.gameObject.layer;
        int otherLayerMask = 1 << collision.collider.gameObject.layer;
        if ((thisLayerMask & weakSpotMask) > 0 && hitTimer <= 0 && (otherLayerMask & (playerBodyMask | playerWheelMask)) > 0)
            reduceBossHealth();
    }

    private void reduceBossHealth()
    {
        currentHealth--;
        hitTimer = bossHitCooldown;

        if (currentHealth <= 0)
            StartCoroutine(startBossDeath(0.5f));
    }

    private IEnumerator startBossDeath(float seconds)
    {
        yield return new WaitForSeconds(seconds);

        onBossDeath.Invoke();
        bossDestroy.destroyExplode();
    }
}
