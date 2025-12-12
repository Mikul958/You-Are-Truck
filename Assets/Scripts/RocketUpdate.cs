using System.Threading;
using Unity.VisualScripting;
using UnityEngine;

public class RocketUpdate : MonoBehaviour
{
    // Referenced Game Objects and Components
    private GameObject bossTruck;
    private GameObject playerTruck;
    public Destroy destroy;

    // Constants, set in game engine
    public float waitDuration;
    public float initialFireDuration;
    public float initialFireSpeed;
    public float travelSpeed;
    public float rotationSpeed;
    public float maxTravelTime;

    public Vector3 startOffset;
    public Vector3 startAngleOffset;
    
    // Instance variables
    private int state;
    private float time;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        bossTruck = GameObject.FindWithTag("Boss");
        if (bossTruck == null)
            Destroy(gameObject);
        playerTruck = GameObject.FindWithTag("Player");
    }

    // Update is called once per frame
    void Update()
    {
        if (state == 0)
            updateWait();
        else if (state == 1)
            updateInitialFire();
        else
            updateTravel();
        time += Time.deltaTime;
    }

    private void updateWait()
    {
        if (bossTruck == null)
        {
            state = 1;
            time = 0f;
            return;
        }
        
        // Update position and rotation to sit in boss truck launcher
        transform.position = bossTruck.transform.position + bossTruck.transform.rotation * startOffset;
        transform.rotation = bossTruck.transform.rotation * Quaternion.Euler(startAngleOffset);
        if (time >= waitDuration)
        {
            state = 1;
            time = 0f;
        }
    }

    private void updateInitialFire()
    {
        // Travel forward at initial fire speed
        transform.position += transform.forward * initialFireSpeed * Time.deltaTime;
        if (time >= initialFireDuration)
        {
            state = 2;
            time = 0f;
        }
    }

    private void updateTravel()
    {
        // Get difference between rocket and player
        Vector3 playerDiff = transform.forward;
        if (playerTruck != null)
            playerDiff = playerTruck.transform.position - transform.position;
        
        // Update rotation to look toward player and travel forward at travel speed
        Quaternion targetRotation = Quaternion.LookRotation(playerDiff);
        float angleDiff = Quaternion.Angle(transform.rotation, targetRotation);
        if (angleDiff > 0f)
        {
            float angleRatio = rotationSpeed * Time.deltaTime / angleDiff;
            if (angleRatio > 1f)
                angleRatio = 1f;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, angleRatio);
        }
        transform.position += transform.forward * travelSpeed * Time.deltaTime;

        if (time >= maxTravelTime)
            destroy.destroyExplode();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (isCollisionCounted(collision))
        {
            transform.position = collision.GetContact(0).point;  // Workaround to generate explosion at point of collision instead of making it huge
            destroy.destroyExplode();
        }
    }

    private bool isCollisionCounted(Collision collision)
    {
        if (collision.collider.gameObject.layer == LayerMask.NameToLayer("Rocket"))
            return false;
        if (state < 2 && (collision.collider.gameObject.layer == LayerMask.NameToLayer("BossTruck") || collision.collider.gameObject.layer == LayerMask.NameToLayer("BossWeakSpot")))
            return false;
        return true;
    }
}
