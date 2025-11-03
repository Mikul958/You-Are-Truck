using System;
using UnityEngine;

public class TruckMove : MonoBehaviour
{
    // Referenced Game Objects and Components
    public Rigidbody rigidBody;
    
    // Truck constants, set in editor
    [Header("Speed Caps")]
    public float topEngineSpeed;           // Maximum forward base speed (m/s)
    public float boostSpeedCapMultiplier;  // Maximum forward speed when boost is active (m/s)
    public float nailSpeedCapMultiplier;   // Maximum forward speed when nail penalty is active (m/s)
    public float globalSpeedCap;           // Maximum speed cap in all directions (m/s)

    [Header("Acceleration")]
    public float baseAccel;                // Acceleration applied when the player is holding in the same direction they are moving (m/s/s)
    public float boostAccel;               // Acceleration applied when the player has an active boost timer -- always applied when boost is active (m/s/s)
    public float brakeDecel;               // Deceleration when the player is holding in the opposite direction they are moving (m/s/s)
    public float engineDecelMultiplier;    // Speed multiplier applied when the player is not holding forward or backwards (multiplier/tick)
    public float externalBodyDecel;        // Decelerationapplied to external velocity when the body is on the ground (m/s/s)
    public float externalWheelDecel;       // Deceleration applied to external velocity when the wheels are on the ground (overrides body decel, m/s/s)
    public float externalDecelMultiplier;  // Deceleration applied to external velocity each update (multiplier/tick)

    [Header("Handling Rotation")]
    public float minRotationSpeed;       // Lower rotation speed bound (deg/sec)
    public float maxRotationSpeed;       // Upper rotation speed bound (deg/sec)
    public float minTurnThreshold;       // The minimum moving speed for the truck to be able to turn (m/s)
    public float maxTurnThreshold;       // The moving speed at which the rotation speed reaches its max (m/s)

    [Header("Airtime Effects")]
    public float airtimeThreshold;       // After airtime crosses this threshold, speed inputs and jumps are ignored and handling is significantly reduced (s)
    public float airtimeTurnMultiplier;  // Handling multiplier applied when the vehicle is in the air (multiplier)

    // Instance variables
    
    // Current velocity values
    private Vector3 currentFacingDirection;
    private Vector3 currentEngineVelocity;
    private float currentEngineSpeed;
    private Vector3 currentExternalVelocity;
    
    // Important vehicle signs
    private int forwardInputSign;       // 0 = neutral1 = forwards, -1 = backwards
    private int sidewaysInputSign;      // 0 = neutral, -1 = left, 1 = right
    private int speedSign;              // 0 = engineSpeed near zero, otherwise matches sign of engineSpeed

    // Vehicle state
    private float currentSpeedCap;      // Current effective speed cap (m/s)
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

        speedSign = 0;
        if (Math.Abs(currentEngineSpeed) > 0.001)
            speedSign = currentEngineSpeed > 0 ? 1 : -1;

        // Apply velocity updates
        calculateSpeedUpdates();
        calculateHandlingUpdates();
        
    }

    private void calculateSpeedUpdates()
    {
        if (airtime < airtimeThreshold && Math.Abs(currentEngineSpeed) <= currentSpeedCap)
        {
            if (boostTimer > 0)
                updateSpeedBoost();
            else
                updateSpeedBase();
        }
        else if (Math.Abs(currentEngineSpeed) > currentSpeedCap)
        {
            softCapEngineSpeed();
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

    private void updateSpeedBase()
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
    
    private void softCapEngineSpeed()
    {
        if (Math.Abs(currentEngineSpeed) > currentSpeedCap + brakeDecel)
            rigidBody.linearVelocity -= brakeDecel * currentFacingDirection * speedSign * Time.fixedDeltaTime;
        else
            rigidBody.linearVelocity -= (currentEngineSpeed - speedSign * currentSpeedCap) * currentFacingDirection * Time.fixedDeltaTime;
    }

    private void calculateHandlingUpdates()
    {
        // Don't turn if sideways input is neutral or the current engine speed is less than the threshold
        if (sidewaysInputSign == 0 || Math.Abs(currentEngineSpeed) < minTurnThreshold)
            return;

        // Calculate turn angle based on engine speed
        float turnAngle;
        if (Math.Abs(currentEngineSpeed) >= maxTurnThreshold)
            turnAngle = maxRotationSpeed * sidewaysInputSign * speedSign * Time.fixedDeltaTime;
        else
            turnAngle = ((Math.Abs(currentEngineSpeed) - minTurnThreshold) * (maxRotationSpeed - minRotationSpeed) / (maxTurnThreshold - minTurnThreshold) + minRotationSpeed)
                * sidewaysInputSign * speedSign * Time.fixedDeltaTime;

        // Apply airtime turn multiplier if player is in the air
        if (airtime > airtimeThreshold)
            turnAngle *= airtimeTurnMultiplier;

        // Apply rotation to rigidBody
        Quaternion turnOffset = Quaternion.Euler(0f, turnAngle, 0f);
        rigidBody.MoveRotation(rigidBody.rotation * turnOffset);
    }

    private void updateTimersAndCap()
    {
        // Update timers
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

        // Check state and update engine speed cap
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
