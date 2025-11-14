using System;
using UnityEngine;

[DefaultExecutionOrder(1)]
public class TruckMove : MonoBehaviour
{
    // Referenced Game Objects and Components
    public Rigidbody rigidBody;

    // Truck constants, set in editor
    [Header("Speed Caps")]
    public float topEngineSpeed;           // Default maximum engine speed (m/s)
    public float boostSpeedCapMultiplier;  // Maximum engine speed multiplier when boost is active (multiplier)
    public float nailSpeedCapMultiplier;   // Maximum engine speed multiplier when nail penalty is active (multiplier)
    public float globalSpeedCap;           // Maximum speed cap in all directions (m/s)

    [Header("Acceleration")]
    public float baseAccel;                // Acceleration applied when the player is holding in the same direction they are moving (m/s/s)
    public float boostAccel;               // Acceleration applied when the player has an active boost timer -- always applied when boost is active (m/s/s)
    public float brakeDecel;               // Deceleration when the player is holding in the opposite direction they are moving (m/s/s)
    public float neutralDecelMultiplier;   // Speed multiplier applied when the player is not holding forward or backwards (multiplier/tick)
    public float externalBodyDecel;        // Decelerationapplied to external velocity when the body is on the ground (m/s/s)
    public float externalWheelDecel;       // Deceleration applied to external velocity when the wheels are on the ground (overrides body decel, m/s/s)
    public float externalDecelMultiplier;  // Deceleration applied to external velocity each update (multiplier/tick)
    public float platformAccel;            // Rate at which platformVelocity changes to meet moving road velocity (m/s/s)

    [Header("Handling Rotation")]
    public float minRotationSpeed;  // Lower rotation speed bound (deg/sec)
    public float maxRotationSpeed;  // Upper rotation speed bound (deg/sec)
    public float minTurnThreshold;  // The minimum moving speed for the truck to be able to turn (m/s)
    public float maxTurnThreshold;  // The moving speed at which the rotation speed reaches its max (m/s)

    [Header("Miscellaneous Physics")]
    public float groundAlignmentSpeed;  // How fast the vehicle attempts to realign itself while touching the ground (deg/sec)

    [Header("Airtime Effects")]
    public float airtimeThreshold;       // After airtime crosses this threshold, jumps are ignored and handling is significantly reduced (s)
    public float airtimeTurnMultiplier;  // Handling multiplier applied when the vehicle is in the air (multiplier)

    [Header("Effect Durations")]
    public float boostDuration;  // Duration of boost applied when a boost panel is touched
    public float oilDuration;    // Duration of slipperiness applied when an oil slick is touched
    public float nailDuration;   // Duration of nail penalty applied when a nail pile is touched

    // Instance variables

    // Current velocity values
    private Vector3 facingDirection;   // Facing direction of the vehicle at any time
    private Vector3 floorNormal;       // Cumulative normal of all "floor type" colliders being touched by wheels
    private Vector3 engineDirection;   // Direction of engine velocity, only updated when on the ground
    private float engineSpeed;         // Signed scalar, positive = forwards, negative = backwards
    private Vector3 externalVelocity;  // Sum of all external velocity sources
    private float externalSpeed;       // Unsigned scalar, magnitude of externalVelocity
    private Vector3 platformVelocity;  // Currently velocity applied by moving platforms
    private Vector3 platformVelocityTarget;  // Target value, updated by TruckCollide
    private Vector3 appliedVelocity;   // Total velocity applied on this tick, used to derive physics delta on next tick
    private Vector3 physicsDelta;      // Velocity applied by Unity's physics engine, incorporated into other vectors each tick

    // Important vehicle signs
    private int forwardInputSign;   // 0 = neutral, 1 = forwards, -1 = backwards
    private int sidewaysInputSign;  // 0 = neutral, -1 = left, 1 = right
    private int speedSign;          // 0 = engineSpeed near zero, otherwise matches sign of engineSpeed

    // Vehicle state
    private float currentEngineCap; // Current effective speed cap (m/s)
    private float airtime;          // Time since the ground was last touched by any hitbox
    private float airtimeWheels;    // Time since the ground was last touched by a wheel
    private float boostTimer;
    private float oilTimer;
    private float nailTimer;

    // Additional private constants
    private const float zeroEngineSpeed = 0.001f;
    private const float zeroThreshold = 1e-6f;

    // LIFECYCLE FUNCTIONS

    // Initialization
    void Start()
    {
        facingDirection = rigidBody.rotation * Vector3.forward;
        floorNormal = rigidBody.rotation * Vector3.up;
        engineDirection = facingDirection;
        engineSpeed = 0;
        externalVelocity = Vector3.zero;
        externalSpeed = 0;
        appliedVelocity = Vector3.zero;
        physicsDelta = Vector3.zero;

        forwardInputSign = 0;
        sidewaysInputSign = 0;
        speedSign = 0;

        currentEngineCap = topEngineSpeed;
        airtime = 0;
        airtimeWheels = 0;
        boostTimer = 0;
        oilTimer = 0;
        nailTimer = 0;

        rigidBody.linearVelocity = appliedVelocity;
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
        processPhysicsDeltas();
        runVelocityUpdates();
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

    private void processPhysicsDeltas()
    {
        // TODO need to figure out how to apply angular dampening on floor normal axis and flip vehicle roll to match normal
        
        // Isolate velocity applied by Unity's physics on this tick
        physicsDelta = rigidBody.linearVelocity - appliedVelocity;

        // Add portion of delta facing along engine direction to engine speed, disallowing any velocity additions over cap
        float engineSpeedDelta = Vector3.Dot(physicsDelta, engineDirection);
        if (Math.Abs(engineSpeed + engineSpeedDelta) <= currentEngineCap)
            engineSpeed += engineSpeedDelta;
        else if (Math.Abs(engineSpeed) < currentEngineCap)
            engineSpeed += speedSign * currentEngineCap - engineSpeed;
        
        speedSign = engineSpeed > 0 ? 1 : -1;
        if (Math.Abs(engineSpeed) < zeroEngineSpeed)
            speedSign = 0;

        // Add portion of delta orthogonal to engine direction to external velocity and update cached speed
        Vector3 externalVelocityDelta = physicsDelta - Vector3.Project(physicsDelta, engineDirection);
        externalVelocity += externalVelocityDelta;
        externalSpeed = externalVelocity.magnitude;

        // Safeguard, these values should not be used again until velocity updates are complete
        appliedVelocity = Vector3.zero;
        physicsDelta = Vector3.zero;

        // Update facing directions and roll using rigidBody rotation and floor normal
        facingDirection = rigidBody.rotation * Vector3.forward;
        if (airtime == 0)
        {
            // Update engine direction to match facing direction along plane of floor normal
            Vector3 targetEngineDirection = Vector3.ProjectOnPlane(facingDirection, floorNormal).normalized;
            if (targetEngineDirection.magnitude > zeroThreshold)
                engineDirection = targetEngineDirection;
            
            // TODO may be a good idea to ease using lerp/slerp
        }

        // Update rigidBody rotation towards current engineDirection and floorNormal at floorAlignmentSpeed
        Quaternion targetRotation = Quaternion.LookRotation(engineDirection, floorNormal);  // TODO may have to use targetEngineDirection instead? double check
        float angleOffset = Quaternion.Angle(rigidBody.rotation, targetRotation);
        if (angleOffset > zeroThreshold)
        {
            float angleRatio = Mathf.Clamp01(groundAlignmentSpeed / angleOffset * Time.fixedDeltaTime);
            rigidBody.MoveRotation(Quaternion.Slerp(rigidBody.rotation, targetRotation, angleRatio));
        }

        // TODO ensure horizontal external velocity does not flip if truck drives upside-down
    }

    private void runVelocityUpdates()
    {
        calculateSpeedUpdates();
        calculateHandlingUpdates();
        updatePlatformVelocity();
        applyCappedVelocityUpdates();
    }

    private void calculateSpeedUpdates()
    {
        if (airtime < airtimeThreshold && Math.Abs(engineSpeed) <= currentEngineCap)
        {
            if (boostTimer > 0)
                updateEngineSpeedBoost();
            else if (airtime == 0)
                updateEngineSpeed();
        }
        else if (Math.Abs(engineSpeed) > currentEngineCap)
        {
            softCapEngineSpeed();
        }
        dampenExternalVelocity();
    }

    private void updateEngineSpeedBoost()
    {
        // Add boost velocity in current forward direction, disallowing addition over cap
        engineSpeed += boostAccel * Time.fixedDeltaTime;
        if (engineSpeed > currentEngineCap)
            engineSpeed = currentEngineCap;
    }

    private void updateEngineSpeed()
    {
        // If holding neutral, apply neutral decel and exit
        if (forwardInputSign == 0)
        {
            engineSpeed *= neutralDecelMultiplier;
            return;
        }

        // Else, apply vehicle accel / brake decel in appropriate direction up to cap
        if (speedSign == 0 || forwardInputSign == speedSign)
            engineSpeed += baseAccel * forwardInputSign * Time.fixedDeltaTime;
        else
            engineSpeed -= brakeDecel * speedSign * Time.fixedDeltaTime;

        // Apply hard cap to speed in engine direction
        if (Math.Abs(engineSpeed) > currentEngineCap)
            engineSpeed = speedSign * currentEngineCap;
    }

    private void softCapEngineSpeed()
    {
        if (Math.Abs(engineSpeed) > currentEngineCap + brakeDecel * Time.fixedDeltaTime)
            engineSpeed -= brakeDecel * speedSign * Time.fixedDeltaTime;
        else
            engineSpeed -= engineSpeed - speedSign * currentEngineCap;
    }

    private void dampenExternalVelocity()
    {
        // Calculate base exponential decay and additional body/wheel decel if necessary
        float externalVelocityDecay = externalSpeed * (1 - externalDecelMultiplier);
        if (airtime == 0)
        {
            if (airtimeWheels == 0)
                externalVelocityDecay += externalWheelDecel * Time.fixedDeltaTime;
            else
                externalVelocityDecay += externalBodyDecel * Time.fixedDeltaTime;
        }

        // If decay exceeds the current speed, zero out external velocity
        if (externalVelocityDecay > externalSpeed)
            externalVelocityDecay = externalSpeed;
        externalVelocity -= externalVelocityDecay * Vector3.Normalize(externalVelocity);
    }

    private void calculateHandlingUpdates()
    {
        // Don't turn if sideways input is neutral or the current engine speed is less than the threshold
        if (sidewaysInputSign == 0 || Math.Abs(engineSpeed) < minTurnThreshold)
            return;

        // Calculate turn angle based on engine speed
        float turnAngle;
        if (Math.Abs(engineSpeed) >= maxTurnThreshold)
            turnAngle = maxRotationSpeed * sidewaysInputSign * speedSign * Time.fixedDeltaTime;
        else
            turnAngle = ((Math.Abs(engineSpeed) - minTurnThreshold) * (maxRotationSpeed - minRotationSpeed) / (maxTurnThreshold - minTurnThreshold) + minRotationSpeed)
                * sidewaysInputSign * speedSign * Time.fixedDeltaTime;

        // Apply airtime turn multiplier if player is in the air
        if (airtime > airtimeThreshold)
            turnAngle *= airtimeTurnMultiplier;

        // Apply rotation to rigidBody based on its local up vector
        Quaternion vehicleRotationOffset = Quaternion.Euler(0f, turnAngle, 0f);
        rigidBody.MoveRotation(rigidBody.rotation * vehicleRotationOffset);

        // Apply rotation to engine velocity based on calculated floor normal
        Quaternion engineRotationOffset = Quaternion.AngleAxis(turnAngle, floorNormal);
        engineDirection = engineRotationOffset * engineDirection;

        // Apply rotation around global up vector to horizontal component only of external velocity
        Quaternion externalRotationOffset = Quaternion.AngleAxis(turnAngle, Vector3.up);
        Vector3 verticalExternalVelocity = Vector3.Project(externalVelocity, Vector3.up);
        Vector3 horizontalExternalVelocity = externalVelocity - verticalExternalVelocity;

        horizontalExternalVelocity = externalRotationOffset * horizontalExternalVelocity;
        externalVelocity = verticalExternalVelocity + horizontalExternalVelocity;
    }

    private void updatePlatformVelocity()
    {
        platformVelocity = Vector3.MoveTowards(platformVelocity, platformVelocityTarget, platformAccel * Time.fixedDeltaTime);
    }

    private void applyCappedVelocityUpdates()
    {
        // Calculate applied velocity for this tick and cap if needed
        appliedVelocity = engineSpeed * engineDirection + externalVelocity + platformVelocity;
        float newSpeed = appliedVelocity.magnitude;
        if (newSpeed > globalSpeedCap)
            appliedVelocity *= globalSpeedCap / newSpeed;

        // Set velocity of rigidBody to appliedVelocity (physics updates have already been incorporated into component vectors)
        rigidBody.linearVelocity = appliedVelocity;
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
        currentEngineCap = topEngineSpeed;
        if (boostTimer > 0)
            currentEngineCap *= boostSpeedCapMultiplier;
        if (nailTimer > 0)
            currentEngineCap *= nailSpeedCapMultiplier;
    }

    // CALLBACK FUNCTIONS -- Functions intended to be called by external Components or Game Objects
    public void updateFloorNormal(Vector3 normal)
    {
        floorNormal = normal;
    }
    public void resetAirtime()
    {
        airtime = 0;
    }

    public void resetAirtimeWheels()
    {
        airtime = 0;
        airtimeWheels = 0;
    }

    public void applyBoost()
    {
        boostTimer = boostDuration;
    }

    public void applyOil()
    {
        oilTimer = oilDuration;
    }

    public void applyNail()
    {
        nailTimer = nailDuration;
    }

    public void updatePlatformVelocity(Vector3 newVelocity)
    {
        platformVelocityTarget = newVelocity;
    }
}
