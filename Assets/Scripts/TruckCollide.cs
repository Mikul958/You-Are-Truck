using UnityEngine;

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
        truckMove.resetAirtime();
    }
}
