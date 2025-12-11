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
    private int speedSign;             // Stored sign of engine speed to cut down on sign checks
    private float slipTurnOffset;      // Current offset of velocity from facing angle
    private Vector3 externalVelocity;  // Sum of all external velocity sources
    private float externalSpeed;       // Unsigned scalar, magnitude of externalVelocity
    private Vector3 stickyRoadAdjust;  // Instantaneously velocity applied by sticky road, does not persist across updates
    private Vector3 platformVelocity;  // Currently velocity applied by moving platforms
    private Vector3 platformVelocityTarget;  // Target value, updated by TruckCollide
    private Vector3 appliedVelocity;   // Total velocity applied on this tick, used to derive physics delta on next tick
    private Vector3 physicsDelta;      // Velocity applied by Unity's physics engine, incorporated into other vectors each tick

    // Vehicle state
    private float currentEngineCap; // Current effective speed cap (m/s)
    private bool canJump;           // Whether or not a jump is permitted by pressing the jump button
    private float airtime;          // Time since the ground was last touched by any hitbox
    private float airtimeWheels;    // Time since the ground was last touched by a wheel
    private bool stickyRoad;        // Applied after physically touching sticky road, removed after sticky road AoE is fully left
    private float boostTimer;
    private float oilTimer;
    private float nailTimer;

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
        
        facingDirection = rigidBody.rotation * Vector3.forward;
        floorNormal = rigidBody.rotation * Vector3.up;
        engineDirection = facingDirection;
        engineSpeed = 0;
        speedSign = 0;
        slipTurnOffset = 0;
        externalVelocity = Vector3.zero;
        externalSpeed = 0;
        appliedVelocity = Vector3.zero;
        physicsDelta = Vector3.zero;

        currentEngineCap = topEngineSpeed;
        canJump = false;
        airtime = airtimeThreshold;
        airtimeWheels = airtimeThreshold;
        stickyRoad = false;
        boostTimer = 0;
        oilTimer = 0;
        nailTimer = 0;

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
        }

        // Update rigidBody rotation towards current engineDirection and floorNormal at floorAlignmentSpeed
        Quaternion targetRotation = Quaternion.LookRotation(engineDirection, floorNormal);
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

        // Apply rotation to rigidBody based on its local up vector
        Quaternion vehicleRotationOffset = Quaternion.Euler(0f, targetTurnAngle, 0f);
        rigidBody.MoveRotation(rigidBody.rotation * vehicleRotationOffset);

        // Calculate true turn angle based on oil state (note that engine direction is updated based on facing direction earlier on, so offset is applied each update)
        // TODO this is not great at the moment, seems to apply offset regardless of engine speed, see if it can be fixed
        float targetOffset = 0f;
        if (oilTimer > 0f)
            targetOffset = maxSlipAngle * inputManager.getSidewaysInputSign();
        float offsetDiff = Math.Abs(targetOffset - slipTurnOffset);
        if (offsetDiff > zeroThreshold)
            slipTurnOffset = Mathf.Lerp(slipTurnOffset, targetOffset, slipDeviationSpeed / offsetDiff * Time.fixedDeltaTime);

        // Apply rotation to engine velocity based on calculated floor normal (only if engineDirection was updated by ground on this frame)
        float overallTurnAngle = targetTurnAngle - (airtime == 0 ? slipTurnOffset : 0);
        Quaternion engineRotationOffset = Quaternion.AngleAxis(overallTurnAngle, floorNormal);
        engineDirection = engineRotationOffset * engineDirection;

        // Apply rotation around global up vector to horizontal component only of external velocity
        Quaternion externalRotationOffset = Quaternion.AngleAxis(overallTurnAngle, Vector3.up);
        Vector3 verticalExternalVelocity = Vector3.Project(externalVelocity, Vector3.up);
        Vector3 horizontalExternalVelocity = externalVelocity - verticalExternalVelocity;

        horizontalExternalVelocity = externalRotationOffset * horizontalExternalVelocity;
        externalVelocity = verticalExternalVelocity + horizontalExternalVelocity;
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
        appliedVelocity = engineSpeed * engineDirection + externalVelocity + platformVelocity;
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
        int forwardInputSign = inputManager.getForwardInputSign();
        if (forwardInputSign == 0)
            audioManager.updateLocalizedAudioSource(engineAudio, "TODO BLANK");
        else if (forwardInputSign == speedSign)
            audioManager.updateLocalizedAudioSource(engineAudio, "EngineRev");
        else if (forwardInputSign != speedSign && airtime == 0)
            audioManager.updateLocalizedAudioSource(engineAudio, "Brake");
        
        // TODO Find some way to check if sound has been updated or not, only play if it has
        // TODO play
    }

    private void updateTimersAndEngineCap()
    {
        // Update timers
        airtime += Time.fixedDeltaTime;
        airtimeWheels += Time.fixedDeltaTime;
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
        return engineDirection;  // For camera follow
    }

    public Vector3 getFloorNormal()
    {
        return floorNormal; // For camera follow
    }

    public float getAirtime()
    {
        return airtime;  // For collision audio
    }
}
