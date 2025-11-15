using UnityEngine;

[DefaultExecutionOrder(0)]
public class TruckCollide : MonoBehaviour
{
    // Referenced Game Objects and Components
    public TruckMove truckMove;
    public Destroy truckDestroy;

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
    private int outOfBoundsMask;
    private int oilMask;
    private int nailMask;
    private int goalMask;

    void Start()
    {
        workingFloorNormal = Vector3.zero;
        floorTouched = false;
        workingPlatformVelocity = Vector3.zero;

        truckBodyMask = 1 << LayerMask.NameToLayer("TruckBody");
        truckWheelMask = 1 << LayerMask.NameToLayer("TruckWheel");

        roadMask = 1 << LayerMask.NameToLayer("Road");
        stickyRoadMask = 1 << LayerMask.NameToLayer("StickyRoad");
        boostPanelMask = 1 << LayerMask.NameToLayer("BoostPanel");
        wallMask = 1 << LayerMask.NameToLayer("Wall");
        outOfBoundsMask = 1 << LayerMask.NameToLayer("OutOfBounds");
        oilMask = 1 << LayerMask.NameToLayer("Oil");
        nailMask = 1 << LayerMask.NameToLayer("Nail");
        goalMask = 1 << LayerMask.NameToLayer("Goal");
    }

    void FixedUpdate()
    {
        // Check if the truck is below the global death plane
        if (transform.position.y < 0)
        {
            
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
    }

    void OnCollisionStay(Collision collision)
    {
        bool touchedDrivable = checkForDrivable(collision);
        if (touchedDrivable)
        {
            checkForStickyRoad(collision);
            checkForBoostPanel(collision);
        }
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
            truckMove.resetAirtimeWheels();
        else
            truckMove.resetAirtime();

        // Get the surface normal for each contact point and add it to total for this collision
        int contactPoints = 0;
        Vector3 combinedNormal = Vector3.zero;
        foreach (ContactPoint contactPoint in collision.contacts)  // TODO move contacts to GetContacts
        {
            Ray ray = new Ray(contactPoint.point + contactPoint.normal * 0.01f, -contactPoint.normal);
            if (Physics.Raycast(ray, out RaycastHit hit, 0.1f))
            {
                Vector3 surfaceNormal = hit.normal;  // This is the “true” surface normal
                contactPoints++;
                combinedNormal += surfaceNormal;
            }
        }

        // Add averaged surface normal to working total and signal floor was touched on this tick
        if (contactPoints > 0)
        {
            workingFloorNormal = (workingFloorNormal + combinedNormal).normalized;
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
        // TODO
    }

    private void checkForBoostPanel(Collision collision)
    {
        int surfaceLayerMask = 1 << collision.collider.gameObject.layer;
        if ((surfaceLayerMask & boostPanelMask) > 0)
            truckMove.applyBoost();
    }

    void OnTriggerEnter(Collider trigger)
    {
        int layerMask = 1 << trigger.gameObject.layer;
        checkForGoal(layerMask);
        checkForOutOfBounds(layerMask);
        checkForOil(layerMask);
        checkForNail(layerMask);
    }

    private void checkForGoal(int collisionLayer)
    {
        if ((collisionLayer & goalMask) > 0)
        {
            Debug.Log("Level Complete!");
            // TODO send to level manager
        }
    }
    
    private void checkForOutOfBounds(int collisionLayer)
    {
        if ((collisionLayer & outOfBoundsMask) > 0)
        {
            Debug.Log("Explode");
            // TODO send kill to Destroy component
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
}
