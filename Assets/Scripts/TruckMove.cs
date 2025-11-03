using System;
using UnityEngine;

[DefaultExecutionOrder(1)]
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
    public float neutralDecelMultiplier;   // Speed multiplier applied when the player is not holding forward or backwards (multiplier/tick)
    public float externalBodyDecel;        // Decelerationapplied to external velocity when the body is on the ground (m/s/s)
    public float externalWheelDecel;       // Deceleration applied to external velocity when the wheels are on the ground (overrides body decel, m/s/s)
    public float externalDecelMultiplier;  // Deceleration applied to external velocity each update (multiplier/tick)

    [Header("Handling Rotation")]
    public float minRotationSpeed;       // Lower rotation speed bound (deg/sec)
    public float maxRotationSpeed;       // Upper rotation speed bound (deg/sec)
    public float minTurnThreshold;       // The minimum moving speed for the truck to be able to turn (m/s)
    public float maxTurnThreshold;       // The moving speed at which the rotation speed reaches its max (m/s)

    [Header("Airtime Effects")]
    public float airtimeThreshold;       // After airtime crosses this threshold, jumps are ignored and handling is significantly reduced (s)
    public float airtimeTurnMultiplier;  // Handling multiplier applied when the vehicle is in the air (multiplier)

    [Header("Timer Lengths")]
    public float appliedBoostTime;
    public float appliedOilTime;
    public float appliedNailTime;

    // Instance variables

    // Current velocity values
    private Vector3 currentFacingDirection;
    private Vector3 currentEngineVelocity;
    private float currentEngineSpeed;           // Signed scalar, positive = forwards, negative = backwards
    private Vector3 currentExternalVelocity;    // TODO implement handling for external velocity including vector decomposition
    private float currentExternalSpeed;         // Unsigned scalar, magnitude of currentExternalVelocity

    // Important vehicle signs
    private int forwardInputSign;       // 0 = neutral1 = forwards, -1 = backwards
    private int sidewaysInputSign;      // 0 = neutral, -1 = left, 1 = right
    private int speedSign;              // 0 = engineSpeed near zero, otherwise matches sign of engineSpeed

    // Vehicle state
    private float currentSpeedCap;      // Current effective speed cap (m/s)
    private bool wheelsGrounded;        // Whether or not wheels are touching the floor, used to determine how much external velocity to dampen
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
        rigidBody.sleepThreshold = 0f;
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
        updateTimersAndEngineCap();
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
        currentExternalSpeed = currentExternalVelocity.magnitude;

        speedSign = 0;
        if (Math.Abs(currentEngineSpeed) > 0.001)
            speedSign = currentEngineSpeed > 0 ? 1 : -1;

        // Debug.Log("Current Engine Speed = " + currentEngineSpeed + ", Current Airtime = " + airtime + ", Boost Timer = " + boostTimer);
        // Debug.Log("Current Global Speed = " + rigidBody.linearVelocity.magnitude);
        // Debug.Log("Current vertical speed = " + Vector3.Dot(rigidBody.linearVelocity, Vector3.up));

        // Apply velocity updates
        calculateSpeedUpdates();
        calculateHandlingUpdates();
        applyGlobalSpeedCap();
    }

    private void calculateSpeedUpdates()
    {
        if (airtime <= Time.fixedDeltaTime && Math.Abs(currentEngineSpeed) <= currentSpeedCap)
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
        dampenExternalVelocity();
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
            float engineDecay = currentEngineSpeed * (1 - neutralDecelMultiplier);
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
        if (Math.Abs(currentEngineSpeed) > currentSpeedCap + brakeDecel * Time.fixedDeltaTime)
            rigidBody.linearVelocity -= brakeDecel * currentFacingDirection * speedSign * Time.fixedDeltaTime;
        else
            rigidBody.linearVelocity -= (currentEngineSpeed - speedSign * currentSpeedCap) * currentFacingDirection;
    }

    private void dampenExternalVelocity()
    {
        // Calculate base exponential decay and additional body/wheel decel if necessary
        float externalVelocityDecay = currentExternalSpeed * (1 - externalDecelMultiplier);
        if (airtime == 0)
        {
            externalVelocityDecay += externalBodyDecel * Time.fixedDeltaTime;
            if (wheelsGrounded)
                externalVelocityDecay += (externalWheelDecel - externalBodyDecel) * Time.fixedDeltaTime;
        }

        // If decay exceeds the current speed, zero out external velocity
        if (externalVelocityDecay > currentExternalSpeed)
            externalVelocityDecay = currentExternalSpeed;

        rigidBody.linearVelocity -= externalVelocityDecay * Vector3.Normalize(currentExternalVelocity);
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

        // Apply rotation to rigidBody and its linear velocity
        Quaternion turnOffset = Quaternion.Euler(0f, turnAngle, 0f);
        rigidBody.MoveRotation(rigidBody.rotation * turnOffset);

        // Apply rotation to engine velocity, TODO reork this so systems update velocity members rather than having to derive again.
        Vector3 updatedEngineVelocity = Vector3.Project(rigidBody.linearVelocity, currentFacingDirection);
        rigidBody.linearVelocity -= updatedEngineVelocity;
        rigidBody.linearVelocity += turnOffset * updatedEngineVelocity;
    }

    private void applyGlobalSpeedCap()
    {
        float currentGlobalSpeed = rigidBody.linearVelocity.magnitude;
        if (currentGlobalSpeed > globalSpeedCap)
            rigidBody.linearVelocity *= globalSpeedCap / currentGlobalSpeed;
    }

    private void updateTimersAndEngineCap()
    {
        // Update timers
        airtime += Time.fixedDeltaTime;
        boostTimer -= Time.fixedDeltaTime;
        if (boostTimer < 0)
            boostTimer = 0;
        oilTimer -= Time.fixedDeltaTime;
        if (oilTimer < 0)
            oilTimer = 0;
        nailTimer -= Time.fixedDeltaTime;
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

    public void applyBoost()
    {
        boostTimer = appliedBoostTime;
    }
}
