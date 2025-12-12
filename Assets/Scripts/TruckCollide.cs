using UnityEngine;
using UnityEngine.Events;

[DefaultExecutionOrder(0)]
public class TruckCollide : MonoBehaviour
{
    // Referenced Game Objects and Components
    public TruckMove truckMove;
    public Destroy truckDestroy;
    private AudioManager audioManager;
    private AudioSource collisionAudio;

    // Events to propagate for UI / camera
    [HideInInspector]
    public UnityEvent onGoalEntered;
    [HideInInspector]
    public UnityEvent onTruckDeath;
    [HideInInspector]
    public UnityEvent<bool> onCameraTrigger;

    // Instance Variables
    private Vector3 workingFloorNormal;
    private bool floorTouched;
    private Vector3 workingPlatformVelocity;

    private int truckBodyMask;
    private int truckWheelMask;

    private int roadMask;
    private int stickyRoadMask;
    private int boostPanelMask;
    private int wallMask;
    private int killMask;
    private int killExplodeMask;
    private int killSquishMask;
    private int oilMask;
    private int nailMask;
    private int goalMask;
    private int cameraTriggerMask;

    private float collisionSoundCooldown = 0.5f;
    private float screechTimer;
    private float bonkTimer;

    void Start()
    {
        GameObject audioManagerObject = GameObject.FindGameObjectWithTag("AudioManager");
        if (audioManagerObject != null)
            audioManager = audioManagerObject.GetComponent<AudioManager>();
        collisionAudio = gameObject.AddComponent<AudioSource>();
        collisionAudio.playOnAwake = false;
        collisionAudio.spatialBlend = 1f;
        collisionAudio.rolloffMode = AudioRolloffMode.Logarithmic;
        
        workingFloorNormal = Vector3.zero;
        floorTouched = false;
        workingPlatformVelocity = Vector3.zero;

        truckBodyMask = 1 << LayerMask.NameToLayer("TruckBody");
        truckWheelMask = 1 << LayerMask.NameToLayer("TruckWheel");

        roadMask = 1 << LayerMask.NameToLayer("Road");
        stickyRoadMask = 1 << LayerMask.NameToLayer("StickyRoad");
        boostPanelMask = 1 << LayerMask.NameToLayer("BoostPanel");
        wallMask = 1 << LayerMask.NameToLayer("Wall");
        killMask = 1 << LayerMask.NameToLayer("Kill");
        killExplodeMask = 1 << LayerMask.NameToLayer("KillExplode");
        killSquishMask = 1 << LayerMask.NameToLayer("KillSquish");
        oilMask = 1 << LayerMask.NameToLayer("Oil");
        nailMask = 1 << LayerMask.NameToLayer("Nail");
        goalMask = 1 << LayerMask.NameToLayer("Goal");
        cameraTriggerMask = 1 << LayerMask.NameToLayer("CameraTrigger");

        screechTimer = collisionSoundCooldown;
        bonkTimer = collisionSoundCooldown;
    }

    void FixedUpdate()
    {
        // Check if the truck is below the global death plane
        if (transform.position.y < -200f)
        {
            onTruckDeath.Invoke();
            truckDestroy.kill();
            return;
        }
        
        // Apply floor normal and velocity updates
        if (floorTouched)
        {
            truckMove.updateFloorNormal(workingFloorNormal.normalized);
            truckMove.updatePlatformVelocity(workingPlatformVelocity);  // Only update target moving platform velocity when grounded, updates with 0 if no moving floor touched
        }

        // Clear floor normal and working velocities
        workingFloorNormal = Vector3.zero;
        floorTouched = false;
        workingPlatformVelocity = Vector3.zero;

        // Decrement timers
        screechTimer -= Time.fixedDeltaTime;
        if (screechTimer < 0f)
            screechTimer = 0f;
        bonkTimer -= Time.fixedDeltaTime;
        if (bonkTimer < 0f)
            bonkTimer = 0f;
    }

    // Solid collision checks
    void OnCollisionStay(Collision collision)
    {
        bool touchedDrivable = checkForDrivable(collision);
        if (touchedDrivable)
        {
            checkForStickyRoad(collision);
            checkForBoostPanel(collision);
        }
        checkForSolidOutOfBounds(collision);
        checkForWall(collision);
    }

    private bool checkForDrivable(Collision collision)
    {
        // Check if the given surface is on a drivable layer, exit if not
        int surfaceLayerMask = 1 << collision.collider.gameObject.layer;  // Note: Important to use collider.gameObject here because just gameObject returns the parent
        if ((surfaceLayerMask & (roadMask | stickyRoadMask | boostPanelMask)) == 0)
            return false;

        // Update airtime and wheels touching logic in TruckMove using layer of this collider
        int thisLayerMask = 1 << collision.GetContact(0).thisCollider.gameObject.layer;
        if ((thisLayerMask & truckWheelMask) > 0)
        {
            if (screechTimer <= 0 && truckMove.getAirtime() > 0 && (truckMove.getTotalVelocity().magnitude > truckMove.globalSpeedCap / 10))
            {
                audioManager.updateLocalizedAudioSource(collisionAudio, "TireScreech");
                collisionAudio.Play();
                screechTimer = collisionSoundCooldown;
            }
            truckMove.resetAirtimeWheels();
        }
        else
        {
            if (bonkTimer <= 0 && truckMove.getAirtime() > 0 && (truckMove.getTotalVelocity().magnitude > truckMove.globalSpeedCap / 10))
            {
                audioManager.updateLocalizedAudioSource(collisionAudio, "Wallhit");
                collisionAudio.Play();
                bonkTimer = collisionSoundCooldown;
            }
            truckMove.resetAirtime();
        }

        // Get the surface normal for each contact point and add it to total for this collision
        int contactPoints = 0;
        Vector3 combinedNormal = Vector3.zero;
        ContactPoint[] contacts = new ContactPoint[collision.contactCount];
        collision.GetContacts(contacts);
        foreach (ContactPoint contactPoint in contacts)
        {
            Ray ray = new Ray(contactPoint.point + contactPoint.normal * 0.1f, -contactPoint.normal);
            if (Physics.Raycast(ray, out RaycastHit hit, 0.1f, (roadMask | stickyRoadMask | boostPanelMask)))
            {
                Vector3 surfaceNormal = hit.normal;  // This is the “true” surface normal
                contactPoints++;
                combinedNormal += surfaceNormal;
            }
        }

        // Add averaged surface normal to working total and signal floor was touched on this tick
        if (contactPoints > 0)
        {
            workingFloorNormal += combinedNormal.normalized;
            floorTouched = true;
        }
        
        // Update working platform velocity              TODO this is not perfect yet
        if (collision.collider.gameObject.CompareTag("CranePlatform"))
        {
            workingPlatformVelocity = collision.collider.gameObject.GetComponentInParent<CraneUpdate>().rigidBody.linearVelocity;
        }

        // Indicate some drivable type has been touched, more specialized checks will only run if drivable collision has been touched
        return true;
    }

    private void checkForStickyRoad(Collision collision)
    {
        int surfaceLayerMask = 1 << collision.collider.gameObject.layer;
        if ((surfaceLayerMask & stickyRoadMask) > 0)
            truckMove.applyStickyRoad();
    }

    private void checkForBoostPanel(Collision collision)
    {
        int surfaceLayerMask = 1 << collision.collider.gameObject.layer;
        if ((surfaceLayerMask & boostPanelMask) > 0)
            truckMove.applyBoost();
    }

    private void checkForSolidOutOfBounds(Collision collision)
    {
        int surfaceLayerMask = 1 << collision.collider.gameObject.layer;
        if ((surfaceLayerMask & killExplodeMask) > 0)
        {
            onTruckDeath.Invoke();
            truckDestroy.destroyExplode();
        }
        else if ((surfaceLayerMask & killSquishMask) > 0)
        {
            onTruckDeath.Invoke();
            truckDestroy.destroySquish();
        }
    }

    private void checkForWall(Collision collision)
    {
        int surfaceLayerMask = 1 << collision.collider.gameObject.layer;
        if ((surfaceLayerMask & wallMask) > 0 && bonkTimer <= 0 && (truckMove.getTotalVelocity().magnitude > truckMove.globalSpeedCap / 10))
        {
            audioManager.updateLocalizedAudioSource(collisionAudio, "Wallhit");
            collisionAudio.Play();
            bonkTimer = collisionSoundCooldown;

            Debug.Log("Hit Wall");
        }
    }

    // Non-solid collision checks
    void OnTriggerEnter(Collider trigger)
    {
        int layerMask = 1 << trigger.gameObject.layer;
        checkForGoal(layerMask);
        checkForOutOfBounds(layerMask);
        checkForCameraTrigger(layerMask, true);
    }

    void OnTriggerStay(Collider trigger)
    {
        int layerMask = 1 << trigger.gameObject.layer;
        checkForOil(layerMask);
        checkForNail(layerMask);
    }

    void OnTriggerExit(Collider trigger)
    {
        int layerMask = 1 << trigger.gameObject.layer;
        checkForCameraTrigger(layerMask, false);
    }

    private void checkForGoal(int collisionLayer)
    {
        if ((collisionLayer & goalMask) > 0)
            onGoalEntered.Invoke();
    }
    
    private void checkForOutOfBounds(int collisionLayer)
    {
        if ((collisionLayer & killMask) > 0)
        {
            onTruckDeath.Invoke();
            truckDestroy.kill();
        }
        else if ((collisionLayer & killExplodeMask) > 0)
        {
            onTruckDeath.Invoke();
            truckDestroy.destroyExplode();
        }
    }

    private void checkForOil(int collisionLayer)
    {
        if ((collisionLayer & oilMask) > 0)
            truckMove.applyOil();
    }

    private void checkForNail(int collisionLayer)
    {
        if ((collisionLayer & nailMask) > 0)
            truckMove.applyNail();
    }

    private void checkForCameraTrigger(int collisionLayer, bool isEntering)
    {
        if ((collisionLayer & cameraTriggerMask) > 0)
            onCameraTrigger.Invoke(isEntering);
    }
}
