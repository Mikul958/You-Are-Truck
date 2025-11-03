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
        truckMove.resetAirtime(); // TODO has to collide with floor collision, do wheel decay stuff

        if (collision.collider.gameObject.CompareTag("boost"))
        {
            Debug.Log("Found boost panel");
            truckMove.applyBoost();
        }
    }
}
