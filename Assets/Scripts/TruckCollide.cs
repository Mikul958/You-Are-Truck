using Unity.VisualScripting;
using UnityEngine;

[DefaultExecutionOrder(0)]
public class TruckCollide : MonoBehaviour
{
    // Referenced Game Objects and Components
    public TruckMove truckMove;

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
        goalMask = 1 << LayerMask.NameToLayer("Goal");
    }

    void FixedUpdate()
    {
        if (floorTouched)
            truckMove.updateFloorNormal(workingFloorNormal);
        floorTouched = false;
        truckMove.updatePlatformVelocity(workingPlatformVelocity);
    }

    void OnCollisionStay(Collision collision)
    {
        checkForDrivable(collision);
    }

    private void checkForDrivable(Collision collision)
    {
        // Check if the given surface is on a drivable layer, exit if not
        int surfaceLayerMask = 1 << collision.collider.gameObject.layer;  // Note: Important to use collider.gameObject here because just gameObject returns the parent
        if ((surfaceLayerMask & (roadMask | stickyRoadMask | boostPanelMask)) == 0)
            return;

        // Update airtime and wheels touching logic in TruckMove using layer of this collider
        int thisLayerMask = 1 << collision.GetContact(0).thisCollider.gameObject.layer;
        if ((thisLayerMask & truckWheelMask) > 0)
            truckMove.resetAirtimeWheels();
        else
            truckMove.resetAirtime();

        // Get the surface normal for this collision and add it to total
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

                Debug.Log("Factored in normal: " + surfaceNormal + " from layermask: " + surfaceLayerMask);
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
    }

    private void checkForStickyRoad()
    {

    }

    private void checkForBoostPanel()
    {

    }

    void OnTriggerEnter(Collider trigger)
    {
        int layerMask = 1 << trigger.gameObject.layer;
        checkForGoal(layerMask);
        checkForOutOfBounds(layerMask);
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
}
