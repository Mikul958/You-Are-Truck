using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

[DefaultExecutionOrder(1)]
public class TruckMove : MonoBehaviour
{
    // Referenced Game Objects and Components
    public Rigidbody rigidBody;
    public InputManager inputManager;
    public WheelAnimator wheelAnimator;
    private List<GameObject> truckWheels;
    private AudioManager audioManager;
    private AudioSource idleAudio;
    private AudioSource engineAudio;

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
    public float minRotationSpeed;    // Lower rotation speed bound (deg/sec)
    public float maxRotationSpeed;    // Upper rotation speed bound (deg/sec)
    public float minTurnThreshold;    // The minimum moving speed for the truck to be able to turn (m/s)
    public float maxTurnThreshold;    // The moving speed at which the rotation speed reaches its max (m/s)
    public float maxSlipAngle;        // Max angle that engine velocity desyncs from facing angle when turning under slip effects
    public float slipDeviationSpeed;  // Speed at which engine direction deviates relative to facing direction when under slip effects

    [Header("Miscellaneous Physics")]
    public float jumpSpeed;             // Magnitude of initial velocity applied by jumps
    public float groundAlignmentSpeed;  // How fast the vehicle attempts to realign itself while touching the ground (deg/sec)
    public float maxStickyRoadSpeed;    // Downward speed applied when sticky road is active, applied instantaneously, not dependent on time
    public float stickyRoadDistance;    // Max distance for a sticky road raycast hit
    public float bossKnockbackStrength; // Knockback applied after hitting boss weak spot

    [Header("Airtime Effects")]
    public float airtimeThreshold;       // After airtime crosses this threshold, jumps are ignored and handling is significantly reduced (s)
    public float airtimeTurnMultiplier;  // Handling multiplier applied when the vehicle is in the air (multiplier)

    [Header("Effect Durations")]
    public float boostDuration;  // Duration of boost applied when a boost panel is touched
    public float oilDuration;    // Duration of slipperiness applied when an oil slick is touched
    public float nailDuration;   // Duration of nail penalty applied when a nail pile is touched

    // Instance variables

    // Current velocity values
    private Vector3 facingDirection;         // Facing direction of the vehicle at any time
    private Vector3 floorNormal;             // Cumulative normal of all "floor type" colliders being touched by wheels
    private Vector3 baseEngineDirection;     // Direction of engine velocity, calculated as facing direction along last touched floor plane(s)
    private Vector3 realEngineDirection;     // Engine direction with slip applied
    private float engineSpeed;               // Signed scalar, positive = forwards, negative = backwards
    private int speedSign;                   // Stored sign of engine speed to cut down on sign checks
    private float slipTurnOffset;            // Current offset of velocity from facing angle
    private Vector3 externalVelocity;        // Sum of all external velocity sources
    private float externalSpeed;             // Unsigned scalar, magnitude of externalVelocity
    private Vector3 stickyRoadAdjust;        // Instantaneously velocity applied by sticky road, does not persist across updates
    private Vector3 platformVelocity;        // Currently velocity applied by moving platforms
    private Vector3 platformVelocityTarget;  // Target value, updated by TruckCollide
    private Vector3 appliedVelocity;         // Total velocity applied on this tick, used to derive physics delta on next tick
    private Vector3 physicsDelta;            // Velocity applied by Unity's physics engine, incorporated into other vectors each tick

    // Vehicle state
    private float currentEngineCap; // Current effective speed cap (m/s)
    private bool canJump;           // Whether or not a jump is permitted by pressing the jump button
    private float airtime;          // Time since the ground was last touched by any hitbox
    private float airtimeWheels;    // Time since the ground was last touched by a wheel
    private bool stickyRoad;        // Applied after physically touching sticky road, removed after sticky road AoE is fully left
    private float boostTimer;
    private float oilTimer;
    private float nailTimer;
    private float engineRevTimer;

    // Additional private constants
    private const float zeroEngineSpeed = 0.001f;
    private const float zeroThreshold = 1e-6f;

    private int roadMask;
    private int stickyRoadMask;
    private int boostPanelMask;

    // LIFECYCLE FUNCTIONS

    // Initialization
    void Start()
    {
        truckWheels = new List<GameObject>();
        foreach (Transform childTransform in gameObject.transform)
        {
            if (childTransform.gameObject.layer == LayerMask.NameToLayer("TruckWheel"))
                truckWheels.Add(childTransform.gameObject);
        }
        GameObject audioManagerObject = GameObject.FindGameObjectWithTag("AudioManager");
        if (audioManagerObject != null)
            audioManager = audioManagerObject.GetComponent<AudioManager>();
        
        idleAudio = gameObject.AddComponent<AudioSource>();
        idleAudio.playOnAwake = false;
        idleAudio.spatialBlend = 1f;
        idleAudio.rolloffMode = AudioRolloffMode.Logarithmic;
        audioManager.updateLocalizedAudioSource(idleAudio, "EngineIdle");
        idleAudio.Play();

        engineAudio = gameObject.AddComponent<AudioSource>();
        engineAudio.playOnAwake = false;
        engineAudio.spatialBlend = 1f;
        engineAudio.rolloffMode = AudioRolloffMode.Logarithmic;
        audioManager.updateLocalizedAudioSource(engineAudio, "EngineRev");
        
        facingDirection = rigidBody.rotation * Vector3.forward;
        floorNormal = rigidBody.rotation * Vector3.up;
        baseEngineDirection = facingDirection;
        realEngineDirection = baseEngineDirection;
        engineSpeed = 0f;
        speedSign = 0;
        slipTurnOffset = 0f;
        externalVelocity = Vector3.zero;
        externalSpeed = 0f;
        appliedVelocity = Vector3.zero;
        physicsDelta = Vector3.zero;

        currentEngineCap = topEngineSpeed;
        canJump = false;
        airtime = airtimeThreshold;
        airtimeWheels = airtimeThreshold;
        stickyRoad = false;
        boostTimer = 0f;
        oilTimer = 0f;
        nailTimer = 0f;
        engineRevTimer = 0f;

        rigidBody.linearVelocity = appliedVelocity;
        rigidBody.sleepThreshold = 0f;

        roadMask = 1 << LayerMask.NameToLayer("Road");
        stickyRoadMask = 1 << LayerMask.NameToLayer("StickyRoad");
        boostPanelMask = 1 << LayerMask.NameToLayer("BoostPanel");
    }

    // Updates that occur each time the physics engine ticks
    void FixedUpdate()
    {
        processPhysicsDeltas();
        runVelocityUpdates();
        updateWheelAnimations();
        updateEngineAudio();
        updateTimersAndEngineCap();
    }

    private void processPhysicsDeltas()
    {
        // Isolate velocity applied by Unity's physics on this tick
        physicsDelta = rigidBody.linearVelocity - appliedVelocity;

        // Add portion of delta facing along engine direction to engine speed, disallowing any velocity additions over cap
        float engineSpeedDelta = Vector3.Dot(physicsDelta, baseEngineDirection);
        if (Math.Abs(engineSpeed + engineSpeedDelta) <= currentEngineCap)
            engineSpeed += engineSpeedDelta;
        else if (Math.Abs(engineSpeed) < currentEngineCap)
            engineSpeed += speedSign * currentEngineCap - engineSpeed;
        
        speedSign = engineSpeed > 0 ? 1 : -1;
        if (Math.Abs(engineSpeed) < zeroEngineSpeed)
            speedSign = 0;

        // Add portion of delta orthogonal to engine direction to external velocity and update cached speed
        Vector3 externalVelocityDelta = physicsDelta - Vector3.Project(physicsDelta, baseEngineDirection);
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
                baseEngineDirection = targetEngineDirection;
        }

        // Update rigidBody rotation towards current engineDirection and floorNormal at floorAlignmentSpeed
        Quaternion targetRotation = Quaternion.LookRotation(baseEngineDirection, floorNormal);
        float angleOffset = Quaternion.Angle(rigidBody.rotation, targetRotation);
        if (angleOffset > zeroThreshold)
        {
            float angleRatio = Mathf.Clamp01(groundAlignmentSpeed / angleOffset * Time.fixedDeltaTime);
            rigidBody.MoveRotation(Quaternion.Slerp(rigidBody.rotation, targetRotation, angleRatio));
        }
    }

    private void runVelocityUpdates()
    {
        calculateSpeedUpdates();
        calculateHandlingUpdates();
        calculateJump();
        calculateStickyRoad();
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
        if (inputManager.getForwardInputSign() == 0)
        {
            engineSpeed *= neutralDecelMultiplier;
            return;
        }

        // Else, apply vehicle accel / brake decel in appropriate direction up to cap
        if (speedSign == 0 || inputManager.getForwardInputSign() == speedSign)
            engineSpeed += baseAccel * inputManager.getForwardInputSign() * Time.fixedDeltaTime;
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
        // Calculate turn angle based on engine speed
        float targetTurnAngle;
        if (Math.Abs(engineSpeed) < minTurnThreshold)
            targetTurnAngle = 0;
        else if (Math.Abs(engineSpeed) >= maxTurnThreshold)
            targetTurnAngle = maxRotationSpeed * inputManager.getSidewaysInputSign() * speedSign * Time.fixedDeltaTime;
        else
            targetTurnAngle = ((Math.Abs(engineSpeed) - minTurnThreshold) * (maxRotationSpeed - minRotationSpeed) / (maxTurnThreshold - minTurnThreshold) + minRotationSpeed)
                * inputManager.getSidewaysInputSign() * speedSign * Time.fixedDeltaTime;

        // Apply airtime turn multiplier if player is in the air
        if (airtime > airtimeThreshold)
            targetTurnAngle *= airtimeTurnMultiplier;

        // Apply rotation to rigidBody
        Quaternion vehicleRotationOffset = Quaternion.Euler(0f, targetTurnAngle, 0f);
        rigidBody.MoveRotation(rigidBody.rotation * vehicleRotationOffset);

        // Apply handling rotation to engine direction
        Quaternion engineRotationUpdate = Quaternion.AngleAxis(targetTurnAngle, floorNormal);
        baseEngineDirection = engineRotationUpdate * baseEngineDirection;  // realEngineDirection updated in calculateSlip()

        // Apply handling rotation to external velocity direction
        Quaternion externalRotationUpdate = Quaternion.AngleAxis(targetTurnAngle, Vector3.up);
        Vector3 horizontalExternalVelocity = new Vector3(externalVelocity.x, 0, externalVelocity.z);
        Vector3 verticalExternalVelocity = new Vector3(0, externalVelocity.y, 0);

        horizontalExternalVelocity = externalRotationUpdate * horizontalExternalVelocity;
        externalVelocity = horizontalExternalVelocity + verticalExternalVelocity;

        // Apply slip effects
        calculateSlip();
    }

    private void calculateSlip()
    {
        // Calculate target slip angle based on oil state
        float targetSlipAngle = 0f;
        if (oilTimer > 0f)
            targetSlipAngle = -1 * maxSlipAngle * inputManager.getSidewaysInputSign() * speedSign;
        
        // Update current slipTurnOffset if grounded, otherwise keep same slip angle
        float offsetDiff = Math.Abs(targetSlipAngle - slipTurnOffset);
        if (airtime == 0 && offsetDiff > zeroThreshold)
            slipTurnOffset = Mathf.Lerp(slipTurnOffset, targetSlipAngle, slipDeviationSpeed / offsetDiff * Time.fixedDeltaTime);
        
        // Apply change in rotation to realEngineDirection
        Quaternion slipRotationOffset = Quaternion.AngleAxis(slipTurnOffset, floorNormal);
        realEngineDirection = slipRotationOffset * baseEngineDirection;
    }

    private void calculateStickyRoad()
    {
        // Reset sticky road adjustment and exit if sticky road is false
        stickyRoadAdjust = Vector3.zero;
        if (!stickyRoad)
            return;
        
        // Iterate through wheels and perform downward raycast to check for sticky road
        bool foundStickyRoad = false;
        Vector3 totalNormal = Vector3.zero;
        float minDistance = maxStickyRoadSpeed;
        foreach (GameObject wheel in truckWheels)
        {
            Ray stickyRoadRay = new Ray(wheel.transform.position, -transform.up);
            RaycastHit[] hits = Physics.RaycastAll(stickyRoadRay, stickyRoadDistance, (roadMask | stickyRoadMask | boostPanelMask));
            if (hits.Length == 0)
                continue;
            
            // Update distance and sticky road normal with closest hit
            Array.Sort(hits, (hit, otherHit) => hit.distance.CompareTo(otherHit.distance));
            totalNormal += hits[0].normal;
            if (hits[0].distance < minDistance)
                minDistance = hits[0].distance;

            // Check if sticky road is encountered in raycast
            foreach (RaycastHit hit in hits)
            {
                if (1 << hit.collider.gameObject.layer == stickyRoadMask)
                {
                    foundStickyRoad = true;
                    break;
                }
            }
        }

        // Use minimum distance and sticky road normal to form final stickyRoadAdjust
        if (foundStickyRoad && totalNormal.magnitude > zeroThreshold)
            stickyRoadAdjust = minDistance * (-totalNormal.normalized);
    }

    private void calculateJump()
    {
        // Re-enable jump if player touches a drivable surface and is not holding the jump button
        if (airtime == 0 && !inputManager.isJumpPressed())
            canJump = true;
        
        // Apply jump velocity along floor normal to external velocity if jump is pressed and wheels (note, no fixedDeltaTime, direct one-time application)
        if (inputManager.isJumpPressed() && canJump && airtimeWheels <= airtimeThreshold)
        {
            externalVelocity += jumpSpeed * floorNormal;
            stickyRoad = false;
            canJump = false;
        }
    }

    private void updatePlatformVelocity()
    {
        // Separate platform velocity components into horizontal and vertical
        Vector3 platformVelocityHorizontal = new Vector3(platformVelocity.x, 0f, platformVelocity.z);
        Vector3 platformVelocityTargetHorizontal = new Vector3(platformVelocityTarget.x, 0f, platformVelocityTarget.z);
        
        // Move horizontal components smoothly, set vertical component instantly
        platformVelocityHorizontal = Vector3.MoveTowards(platformVelocityHorizontal, platformVelocityTargetHorizontal, platformAccel * Time.fixedDeltaTime);
        platformVelocity.x = platformVelocityHorizontal.x;
        platformVelocity.y = platformVelocityTarget.y;
        platformVelocity.z = platformVelocityHorizontal.z;
    }

    private void applyCappedVelocityUpdates()
    {
        // Calculate applied velocity for this tick and cap if needed
        appliedVelocity = engineSpeed * realEngineDirection + externalVelocity + platformVelocity;
        float newSpeed = appliedVelocity.magnitude;
        if (newSpeed > globalSpeedCap)
            appliedVelocity *= globalSpeedCap / newSpeed;

        // Set velocity of rigidBody to appliedVelocity and add stickyRoadAdjust (physics updates have already been incorporated into component vectors)
        rigidBody.linearVelocity = appliedVelocity + stickyRoadAdjust;
    }

    private void updateWheelAnimations()
    {
        wheelAnimator.updateMovementInfo(engineSpeed, inputManager.getSidewaysInputSign(), inputManager.isJumpPressed());
    }

    private void updateEngineAudio()
    {
        if (engineRevTimer == 0f && !engineAudio.isPlaying && Math.Abs(engineSpeed) < 0.25 * topEngineSpeed && inputManager.getForwardInputSign() != 0)
        {
            engineAudio.Play();
            engineRevTimer = 0.35f;
        }
    }

    private void updateTimersAndEngineCap()
    {
        // Update timers
        airtime += Time.fixedDeltaTime;
        airtimeWheels += Time.fixedDeltaTime;
        boostTimer -= Time.fixedDeltaTime;
        if (boostTimer < 0f)
            boostTimer = 0f;
        oilTimer -= Time.fixedDeltaTime;
        if (oilTimer < 0f)
            oilTimer = 0f;
        nailTimer -= Time.fixedDeltaTime;
        if (nailTimer < 0f)
            nailTimer = 0f;
        engineRevTimer -= Time.fixedDeltaTime;
        if (engineRevTimer < 0f)
            engineRevTimer = 0f;
        if (engineRevTimer > 0 && (engineSpeed > 0.25f * topEngineSpeed || inputManager.getForwardInputSign() != 0))
            engineRevTimer = 0.35f;

        // Check state and update engine speed cap
        currentEngineCap = topEngineSpeed;
        if (boostTimer > 0f)
            currentEngineCap *= boostSpeedCapMultiplier;
        if (nailTimer > 0f)
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

    public void applyStickyRoad()
    {
        stickyRoad = true;
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

    // Necessary getters
    public Vector3 getTotalVelocity()
    {
        return rigidBody.linearVelocity;  // For collision audio
    }
    public Vector3 getEngineDirection()
    {
        return realEngineDirection;  // For camera follow
    }

    public Vector3 getFloorNormal()
    {
        return floorNormal; // For camera follow
    }

    public float getAirtime()
    {
        return airtime;  // For collision audio
    }

    public void applyKnockback(Vector3 collisionNormal)
    {
        Vector3 knockback = bossKnockbackStrength * collisionNormal;        
        externalVelocity += knockback;
    }
}
