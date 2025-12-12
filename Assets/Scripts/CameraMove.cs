using UnityEngine;

[DefaultExecutionOrder(2)]
public class CameraMove : MonoBehaviour
{
    // Referenced components
    private GameObject playerTruck;
    private TruckMove truckMove;
    private TruckCollide truckCollide;

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
    private bool disabled;
    private bool truckAlive;
    private bool useUpDirection;  // Whether or not to factor in an up direction, enabled with triggers encapsulating e.g. inversions
    private Vector3 targetPosition = Vector3.zero;
    private Quaternion targetRotation = Quaternion.identity;
    
    void Start()
    {
        playerTruck = GameObject.FindWithTag("Player");
        if (playerTruck != null)
        {
            truckMove = playerTruck.GetComponent<TruckMove>();
            truckCollide = playerTruck.GetComponent<TruckCollide>();
            truckCollide.onTruckDeath.AddListener(this.handleTruckDeath);
            
            targetRotation = Quaternion.LookRotation(truckMove.getEngineDirection(), truckMove.getFloorNormal()) * Quaternion.Euler(targetAngleOffset);
            transform.rotation = targetRotation;
            targetPosition = playerTruck.transform.position - transform.forward * targetDistance + transform.up * verticalOffset;
            transform.position = targetPosition;

            disabled = false;
            truckAlive = true;
            useUpDirection = false;
        }
        else
        {
            targetRotation = Quaternion.Euler(new Vector3(90, 0, 0));
            transform.rotation = targetRotation;
            targetPosition = new Vector3(0, 10, 0);
            transform.position = targetPosition;

            disabled = true;
            truckAlive = false;
        }
    }
    void Update()
    {
        if (disabled)
            return;
        
        if (truckAlive)
            updateTruckAlive();
        else
            updateTruckDead();
    }

    private void updateTruckAlive()
    {
        // Get smoothing values based on current speed
        float speed = playerTruck.GetComponent<Rigidbody>().linearVelocity.magnitude;
        float moveSmooth = Mathf.Lerp(minMoveSmooth, maxMoveSmooth, speed / maxSpeed);
        float rotSmooth  = Mathf.Lerp(minRotSmooth, maxRotSmooth, speed / maxSpeed);

        // Apply smoothed rotation change to camera
        if (useUpDirection)
            targetRotation = Quaternion.LookRotation(truckMove.getEngineDirection(), truckMove.getFloorNormal()) * Quaternion.Euler(targetAngleOffset);
        else
            targetRotation = Quaternion.LookRotation(truckMove.getEngineDirection()) * Quaternion.Euler(targetAngleOffset);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotSmooth * Time.deltaTime);

        // Apply smoothed position change to camera
        targetPosition = playerTruck.transform.position - targetRotation * Vector3.forward * targetDistance + targetRotation * Vector3.up * verticalOffset;
        transform.position = Vector3.Lerp(transform.position, targetPosition, moveSmooth * Time.deltaTime);
    }

    private void updateTruckDead()
    {
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 0.05f * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, targetPosition, 0.05f * Time.deltaTime);
    }

    private void handleTruckDeath()
    {
        truckAlive = false;
    }

    private void setUseUpDirection(bool enabled)
    {
        useUpDirection = enabled;
    }
}