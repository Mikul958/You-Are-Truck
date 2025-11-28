using System.Threading;
using UnityEngine;

public class CraneUpdate : MonoBehaviour
{
    // Referenced components
    public Rigidbody rigidBody;

    // Constants, set in editor
    public Vector3 amplitude;      // End distance platform will move in either direction
    public bool reverseDirection;  // Move towards endpoint first instead of startpoint if enabled
    public float moveSpeed;        // Speed the platform moves between each point
    public float waitTime;         // Time the platform will wait at each end of its path

    // Instance variables
    private Vector3[] endpoints;   // Derived endpoints from placed position and amplitude
    private int currentEndpoint;   // Index representing the current destination
    private Vector3 moveDirection;
    private bool isMoving;
    private float waitTimer;

    void Start()
    {
        endpoints = new Vector3[2] { rigidBody.position - amplitude, rigidBody.position + amplitude };
        currentEndpoint = 0;
        moveDirection = (endpoints[0] - endpoints[1]).normalized;
        if (reverseDirection)
        {
            currentEndpoint = 1;
            moveDirection *= -1;
        }
        
        isMoving = true;
        waitTimer = 0;
    }

    void FixedUpdate()
    {
        if (isMoving)
            calculateMove();
        else
            calculateWait();
    }

    private void calculateMove()
    {        
        Vector3 currentPosition = rigidBody.position;
        float distanceLeft = (endpoints[currentEndpoint] - currentPosition).magnitude;

        // If remaining distance is less than speed, update to destination, flip destination/direction, and trigger wait
        if (distanceLeft <= moveSpeed * Time.fixedDeltaTime)
        {
            rigidBody.MovePosition(endpoints[currentEndpoint]);
            currentEndpoint = (currentEndpoint + 1) % 2;
            moveDirection *= -1;

            isMoving = false;
            waitTimer = waitTime;
            return;
        }

        // Otherwise, move platform by moveSpeed
        Vector3 newPosition = currentPosition + moveDirection * moveSpeed * Time.fixedDeltaTime;
        rigidBody.MovePosition(newPosition);
    }

    private void calculateWait()
    {
        // Decrement waitTimer and enter moving state if expired
        waitTimer -= Time.fixedDeltaTime;
        if (waitTimer <= 0f)
            isMoving = true;
    }
}
