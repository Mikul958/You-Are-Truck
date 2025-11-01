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
    private float currentSpeed;         // Each frame, the truck moves along the facing vector, controlled by the rigidBody.
    private float currentSpeedCap;      // Tracks the current effective speed cap
    private Vector3 currentDirection;   // Unit vector representing the direction of engine speed;
    private int forwardInputSign;       // 1 = forwards, -1 = backwards, 0 = neutral
    private int sidewaysInputSign;      // -1 = left, 1 = right, 0 = neutral
    private float airtime;
    private float boostTimer;
    private float oilTimer;
    private float nailTimer;

    // LIFECYCLE FUNCTIONS
    void Start()
    {
        currentSpeed = 0;
        currentSpeedCap = topBaseSpeed;
        currentDirection = rigidBody.rotation * Vector3.forward;
        airtime = 0;
        boostTimer = 0;
        oilTimer = 0;
        nailTimer = 0;

        rigidBody.sleepThreshold = 0f;  // Make it so Rigid Body doesn't fall asleep
    }

    void Update()
    {
        getPlayerInputs();
        calculateVelocityUpdates();
        updateTimersAndCap();
    }

    private void getPlayerInputs()
    {
        // TODO switch to input action system if it's reasonable.
        
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
        calculateSpeedUpdates();
        calculateDirectionUpdates();
        applyVelocityUpdates();
    }

    private void calculateSpeedUpdates()
    {
        int speedSign = currentSpeed >= 0 ? 1 : -1;
        if (airtime < airtimeThreshold && Math.Abs(currentSpeed) <= currentSpeedCap)
        {
            Debug.Log("Can accel");
            if (boostTimer > 0)
                updateSpeedBoost();
            else
                updateSpeedBase(speedSign);
        }
        else if (Math.Abs(currentSpeed) > currentSpeedCap)
        {
            ReduceSpeedToCap(speedSign);
        }
    }

    private void updateSpeedBoost()
    {
        currentSpeed += boostAccel;
        if (currentSpeed > currentSpeedCap)
            currentSpeed = currentSpeedCap;
    }

    private void updateSpeedBase(int speedSign)
    {   
        // If holding neutral, apply neutral decel and exit
        if (forwardInputSign == 0)
        {
            currentSpeed *= baseDecelMultiplier * Time.deltaTime;
            int updatedSpeedSign = currentSpeed >= 0 ? 1 : -1;
            if (speedSign != updatedSpeedSign)
                currentSpeed = 0;
            return;
        }

        // Else, apply vehicle accel / brake decel in appropriate direction up to cap
        if (forwardInputSign == speedSign)
            currentSpeed += forwardInputSign * baseAccel * Time.deltaTime;
        else
            currentSpeed += forwardInputSign * brakeDecel * Time.deltaTime;
        Math.Clamp(currentSpeed, -currentSpeedCap, currentSpeedCap);
    }
    
    private void ReduceSpeedToCap(int speedSign)
    {
        currentSpeed += -speedSign * brakeDecel * Time.deltaTime;
        if (Math.Abs(currentSpeed) < currentSpeedCap)
            currentSpeed = speedSign * currentSpeedCap;
    }

    private void calculateDirectionUpdates()
    {
        // TODO
    }

    private void applyVelocityUpdates()
    {
        rigidBody.AddForce(currentSpeed * currentDirection, ForceMode.VelocityChange);
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
