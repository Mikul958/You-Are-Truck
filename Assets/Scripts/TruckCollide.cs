using Unity.VisualScripting;
using UnityEngine;

[DefaultExecutionOrder(0)]
public class TruckCollide : MonoBehaviour
{
    // Referenced Game Objects and Components
    public TruckMove truckMove;


    void Start()
    {

    }

    void Update()
    {

    }

    void OnCollisionStay(Collision collision)
    {
        // TODO need to figure out how to only do this with wheels touching, need to overhaul collision checks entirely before progressing
        truckMove.resetAirtime();

        int contactPoints = 0;
        Vector3 combinedNormal = Vector3.zero;
        foreach (ContactPoint contactPoint in collision.contacts)
        {
            contactPoints++;
            combinedNormal += contactPoint.normal;
        }

        if (contactPoints > 0)
            truckMove.updateFloorNormal(combinedNormal.normalized);


        if (collision.collider.gameObject.CompareTag("boost"))
        {
            Debug.Log("Found boost panel");
            truckMove.applyBoost();
        }
    }
}
