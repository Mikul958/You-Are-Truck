using System;
using UnityEngine;

public class TruckMove : MonoBehaviour
{
    // Referenced Game Objects and Components
    public Rigidbody rigidBody;
    
    // Truck constants, set in editor
    [Header("Speed Caps")]
    public float topBaseSpeed;             // Maximum forward base speed.
    public float boostSpeedCapMultiplier;  // Maximum forward speed when boost is active.
    public float nailSpeedCapMultiplier;   // Maximum forward speed when nail penalty is active.

    [Header("Acceleration")]
    public float baseAccel;             // Acceleration (constant) applied when the player is holding in the same direction they are moving
    public float boostAccel;            // Acceleration (constant) applied when the player has an active boost timer -- always applied when boost is active
    public float baseDecelMultiplier;   // Deceleration (multiplier) applied when the player is not holding forward or backwards
    public float brakeDecel;            // Deceleration (constant) when the player is holding in the opposite direction they are moving

    [Header("Handling Rotation")]
    public float minTurnSpeed;           // Lower rotation speed bound.
    public float maxTurnSpeed;           // Upper rotation speed bound.
    public float minTurnThreshold;       // The minimum moving speed for the truck to be able to turn.
    public float maxTurnThreshold;       // The moving speed at which the rotation speed reaches its max.

    [Header("Airtime Effects")]
    public float airtimeThreshold;       // After airtime crosses this threshold, speed inputs and jumps are ignored and handling is significantly reduced
    public float airtimeTurnMultiplier;  // Handling multiplier applied when the vehicle is in the air.

    // Instance variables
    private float currentSpeedCap;      // Tracks the current effective speed cap
    private float airtime;
    private float boostTimer;
    private float oilTimer;
    private float nailTimer;

    private int forwardInputSign;       // 1 = forwards, -1 = backwards, 0 = neutral
    private int sidewaysInputSign;      // -1 = left, 1 = right, 0 = neutral

    private Vector3 currentFacingDirection;
    private Vector3 currentEngineVelocity;
    private Vector3 currentExternalVelocity;

    // LIFECYCLE FUNCTIONS
    void Start()
    {
        currentSpeedCap = topBaseSpeed;
        airtime = 0;
        boostTimer = 0;
        oilTimer = 0;
        nailTimer = 0;

        forwardInputSign = 0;
        sidewaysInputSign = 0;

        rigidBody.linearVelocity = Vector3.zero;
        rigidBody.linearDamping = 0;
        rigidBody.sleepThreshold = 0f;  // Make it so Rigid Body doesn't fall asleep
    }

    void Update()
    {
        getPlayerInputs();
    }

    void FixedUpdate()
    {
        calculateVelocityUpdates();
        updateTimersAndCap();
    }

    private void getPlayerInputs()
    {        
        forwardInputSign = 0;
        if (Input.GetKey(KeyCode.W))
            forwardInputSign++;
        if (Input.GetKey(KeyCode.S))
            forwardInputSign--;
        
        sidewaysInputSign = 0;
        if (Input.GetKey(KeyCode.A))
            sidewaysInputSign--;
        if (Input.GetKey(KeyCode.D))
            sidewaysInputSign++;
    }

    private void calculateVelocityUpdates()
    {
        // Read current direction and engine speed from the RigidBody
        currentFacingDirection = rigidBody.rotation * Vector3.forward;
        currentEngineVelocity = Vector3.Project(rigidBody.linearVelocity, currentFacingDirection);
        currentExternalVelocity = rigidBody.linearVelocity - currentEngineVelocity;

        // Apply velocity updates
        calculateSpeedUpdates();
        Debug.Log("Speed = " + rigidBody.linearVelocity);
        // calculateDirectionUpdates();
        
    }

    private void calculateSpeedUpdates()
    {
        int speedSign = 0;
        if (Math.Abs(currentEngineVelocity.magnitude) > 0.001)
            speedSign = currentEngineVelocity.magnitude > 0 ? 1 : -1;
        if (airtime < airtimeThreshold && Math.Abs(currentEngineVelocity.magnitude) <= currentSpeedCap)
        {
            if (boostTimer > 0)
                updateSpeedBoost();
            else
                updateSpeedBase(speedSign);
        }
        else if (Math.Abs(currentEngineVelocity.magnitude) > currentSpeedCap)
        {
            softCapEngineSpeed(speedSign);
        }
    }

    private void updateSpeedBoost()
    {
        // Add boost velocity in current forward direction
        rigidBody.linearVelocity += boostAccel * currentFacingDirection * Time.fixedDeltaTime;

        // Read in updated forward velocity and cap it
        float updatedSpeed = Vector3.Dot(rigidBody.linearVelocity, currentFacingDirection);
        if (updatedSpeed > currentSpeedCap)
            rigidBody.linearVelocity -= (updatedSpeed - currentSpeedCap) * currentFacingDirection;
    }

    private void updateSpeedBase(int speedSign)
    {
        // If holding neutral, apply neutral decel and exit
        if (forwardInputSign == 0)
        {
            float engineDecay = currentEngineVelocity.magnitude * (1 - baseDecelMultiplier) * Time.fixedDeltaTime;
            rigidBody.linearVelocity -= engineDecay * currentFacingDirection;
            Debug.Log("Neutral, speed = " + rigidBody.linearVelocity);
            return;
        }

        // Else, apply vehicle accel / brake decel in appropriate direction up to cap
        if (speedSign == 0 || forwardInputSign == speedSign)
        {
            rigidBody.linearVelocity += baseAccel * currentFacingDirection * forwardInputSign * Time.fixedDeltaTime;
            Debug.Log("Accelerating, speed = " + Vector3.Dot(rigidBody.linearVelocity, currentFacingDirection));
        }
        else
        {
            rigidBody.linearVelocity -= brakeDecel * currentFacingDirection * speedSign * Time.fixedDeltaTime;
            Debug.Log("Applying brake, speed = " + Vector3.Dot(rigidBody.linearVelocity, currentFacingDirection));
        }

        // Apply hard cap to speed in engine direction
        float updatedSpeed = Vector3.Dot(rigidBody.linearVelocity, currentFacingDirection);
        if (Math.Abs(updatedSpeed) > currentSpeedCap)
        {
            Debug.Log("Speed cap hit, current speed: " + updatedSpeed);
            rigidBody.linearVelocity -= (updatedSpeed - speedSign * currentSpeedCap) * currentFacingDirection;
        }
    }
    
    private void softCapEngineSpeed(int speedSign)
    {
        if (currentEngineVelocity.magnitude + brakeDecel > currentSpeedCap)
            rigidBody.linearVelocity -= brakeDecel * currentFacingDirection * speedSign * Time.fixedDeltaTime;
        else
            rigidBody.linearVelocity -= (currentEngineVelocity.magnitude - currentSpeedCap) * currentFacingDirection * speedSign * Time.fixedDeltaTime;
    }

    private void calculateDirectionUpdates()
    {
        // TODO
    }

    private void updateTimersAndCap()
    {
        airtime += Time.deltaTime;
        boostTimer -= Time.deltaTime;
        if (boostTimer < 0)
            boostTimer = 0;
        oilTimer -= Time.deltaTime;
        if (oilTimer < 0)
            oilTimer = 0;
        nailTimer -= Time.deltaTime;
        if (nailTimer < 0)
            nailTimer = 0;

        currentSpeedCap = topBaseSpeed;
        if (boostTimer > 0)
            currentSpeedCap *= boostSpeedCapMultiplier;
        if (nailTimer > 0)
            currentSpeedCap *= nailSpeedCapMultiplier;
    }

    // CALLBACK FUNCTIONS -- Functions intended to be called by external Components or Game Objects

    public void resetAirtime()
    {
        airtime = 0;
    }
}
