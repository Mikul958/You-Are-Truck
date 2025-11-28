using UnityEngine;

public class CameraMove : MonoBehaviour
{
    // Referenced components
    private GameObject playerTruck;

    // Constants, set in game editor
    public float targetDistance;
    public float verticalOffset;
    public Vector3 targetAngleOffset;
    
    public float minSpeed;
    public float maxSpeed;

    public float minMoveSmooth;
    public float maxMoveSmooth;
    public float minRotSmooth;
    public float maxRotSmooth;


    // Instance variables
    private bool canMove;
    
    void Start()
    {
        playerTruck = GameObject.FindWithTag("Player");
        if (playerTruck == null)
        {
            transform.rotation = Quaternion.Euler(new Vector3(90, 0, 0));
            transform.position = new Vector3(0, 10, 0);
            canMove = false;
        }
        else
        {
            transform.rotation = Quaternion.LookRotation(playerTruck.GetComponent<TruckMove>().getEngineDirection()) * Quaternion.Euler(targetAngleOffset);
            transform.position = playerTruck.transform.position - transform.forward * targetDistance + transform.up * verticalOffset;
            canMove = true;
        }
    }
    void Update()
    {
        if (!canMove || playerTruck == null)
            return;
        
        // Get smoothing values based on current speed
        float speed = playerTruck.GetComponent<Rigidbody>().linearVelocity.magnitude;
        float moveSmooth = Mathf.Lerp(minMoveSmooth, maxMoveSmooth, speed / maxSpeed);
        float rotSmooth  = Mathf.Lerp(minRotSmooth, maxRotSmooth, speed / maxSpeed);

        // Apply smoothed rotation change to camera
        Quaternion targetRotation = Quaternion.LookRotation(playerTruck.GetComponent<TruckMove>().getEngineDirection()) * Quaternion.Euler(targetAngleOffset);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotSmooth * Time.deltaTime);

        // Apply smoothed position change to camera
        Vector3 targetPosition = playerTruck.transform.position - targetRotation * Vector3.forward * targetDistance + targetRotation * Vector3.up * verticalOffset;
        transform.position = Vector3.Lerp(transform.position, targetPosition, moveSmooth * Time.deltaTime);
    }
}