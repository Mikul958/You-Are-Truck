using System;
using Unity.VisualScripting;
using UnityEngine;

public class TruckMove : MonoBehaviour
{
    // Referenced Game Objects and Components
    public Rigidbody rigidBody;
    
    // Truck constants, set in editor
    [Header("Speed Caps")]
    public float topEngineSpeed;           // Maximum forward base speed
    public float boostSpeedCapMultiplier;  // Maximum forward speed when boost is active
    public float nailSpeedCapMultiplier;   // Maximum forward speed when nail penalty is active
    public float globalSpeedCap;           // Maximum speed cap in all directions

    [Header("Acceleration")]
    public float baseAccel;                // Acceleration (constant, per-second) applied when the player is holding in the same direction they are moving
    public float boostAccel;               // Acceleration (constant, per-second) applied when the player has an active boost timer -- always applied when boost is active
    public float brakeDecel;               // Deceleration (constant, per-second) when the player is holding in the opposite direction they are moving
    public float engineDecelMultiplier;    // Deceleration (multiplier, per-tick) applied when the player is not holding forward or backwards
    public float externalBodyDecel;        // Deceleration (constant, per-second) applied to external velocity when the body is on the ground
    public float externalWheelDecel;       // Deceleration (constant, per-second) applied to external velocity when the wheels are on the ground (overrides body decel)
    public float externalDecelMultiplier;  // Deceleration (multiplier, per-tick) applied to external velocity each update

    [Header("Handling Rotation")]
    public float minTurnSpeed;           // Lower rotation speed bound.
    public float maxTurnSpeed;           // Upper rotation speed bound.
    public float minTurnThreshold;       // The minimum moving speed for the truck to be able to turn.
    public float maxTurnThreshold;       // The moving speed at which the rotation speed reaches its max.

    [Header("Airtime Effects")]
    public float airtimeThreshold;       // After airtime crosses this threshold, speed inputs and jumps are ignored and handling is significantly reduced
    public float airtimeTurnMultiplier;  // Handling multiplier applied when the vehicle is in the air.

    // Instance variables
    
    // Current velocity values
    private Vector3 currentFacingDirection;
    private Vector3 currentEngineVelocity;
    private float currentEngineSpeed;
    private Vector3 currentExternalVelocity;
    
    // Current input values
    private int forwardInputSign;       // 1 = forwards, -1 = backwards, 0 = neutral
    private int sidewaysInputSign;      // -1 = left, 1 = right, 0 = neutral

    // Vehicle state
    private float currentSpeedCap;      // Tracks the current effective speed cap
    private float airtime;
    private float boostTimer;
    private float oilTimer;
    private float nailTimer;

    // LIFECYCLE FUNCTIONS
    void Start()
    {
        currentSpeedCap = topEngineSpeed;
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

    // Updates that occur every drawn frame
    void Update()
    {
        readPlayerInputs();
    }

    // Updates that occur each time the physics engine ticks
    void FixedUpdate()
    {
        calculateVelocityUpdates();
        updateTimersAndCap();
    }

    private void readPlayerInputs()
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
        currentEngineSpeed = Vector3.Dot(rigidBody.linearVelocity, currentFacingDirection);
        currentExternalVelocity = rigidBody.linearVelocity - currentEngineVelocity;

        // Apply velocity updates
        calculateSpeedUpdates();
        // calculateDirectionUpdates();
        
    }

    private void calculateSpeedUpdates()
    {
        int speedSign = 0;
        if (Math.Abs(currentEngineSpeed) > 0.001)
            speedSign = currentEngineSpeed > 0 ? 1 : -1;
        if (airtime < airtimeThreshold && Math.Abs(currentEngineSpeed) <= currentSpeedCap)
        {
            if (boostTimer > 0)
                updateSpeedBoost();
            else
                updateSpeedBase(speedSign);
        }
        else if (Math.Abs(currentEngineSpeed) > currentSpeedCap)
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
            float engineDecay = currentEngineSpeed * (1 - engineDecelMultiplier);
            rigidBody.linearVelocity -= engineDecay * currentFacingDirection;
            return;
        }

        // Else, apply vehicle accel / brake decel in appropriate direction up to cap
        if (speedSign == 0 || forwardInputSign == speedSign)
            rigidBody.linearVelocity += baseAccel * currentFacingDirection * forwardInputSign * Time.fixedDeltaTime;
        else
            rigidBody.linearVelocity -= brakeDecel * currentFacingDirection * speedSign * Time.fixedDeltaTime;

        // Apply hard cap to speed in engine direction
        float updatedSpeed = Vector3.Dot(rigidBody.linearVelocity, currentFacingDirection);
        if (Math.Abs(updatedSpeed) > currentSpeedCap)
            rigidBody.linearVelocity -= (updatedSpeed - speedSign * currentSpeedCap) * currentFacingDirection;
    }
    
    private void softCapEngineSpeed(int speedSign)
    {
        if (Math.Abs(currentEngineSpeed) > currentSpeedCap + brakeDecel)
            rigidBody.linearVelocity -= brakeDecel * currentFacingDirection * speedSign * Time.fixedDeltaTime;
        else
            rigidBody.linearVelocity -= (currentEngineSpeed - speedSign * currentSpeedCap) * currentFacingDirection * Time.fixedDeltaTime;
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

        currentSpeedCap = topEngineSpeed;
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
