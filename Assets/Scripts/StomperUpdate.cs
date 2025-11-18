using System;
using NUnit.Framework;
using Unity.VisualScripting;
using UnityEngine;

public delegate void RunState();

public class StomperUpdate : MonoBehaviour
{
    // Referenced components
    public Rigidbody rigidBody;
    public GameObject killPlane;

    // Constants, set in game engine
    public float initialWait;       // Offset period before main cycle starts
    public bool isLethal;           // If true, stomper head uses KillSquish layer and makes impact sound on stomp finish. Otherwise uses road layer
    public float maxStompLength;    // Maximum distance stomper can stomp before stomp is force stopped

    public float idleWait;          // Time to wait before beginning shake cycle
    public float shakeDuration;     // Time spent shaking before stomp
    public float shakePeriod;       // Frequency of shake
    public float shakeAmplitude;    // Maximum vertical distance covered by shake
    public float stompSpeed;        // Speed at which the stomper stomps
    public float retreatWait;       // Time spent waiting before retreat after stomp
    public float retreatSpeed;      // Speed at which the stomper retreats after stomp

    // Instance variables
    private Vector3 startPos;           // Stored start position of the stomper head, world position
    private float timer;                // General timer used for time-based states (initial wait, idle wait, retreat state)
    private RunState runCurrentState;   // Delegate that points to the function responsible for executing the current state
    
    void Start()
    {
        // Initialize variables and current state function pointer.
        startPos = transform.position;
        timer = 0f;
        runCurrentState = this.runInitialWait;
        
        // If stomper is set to kill, stomper head will become out of bounds during stomp state. Otherwise, it remains permanently drivable.
        if (isLethal)
            killPlane.layer = LayerMask.NameToLayer("Wall");
        else
            killPlane.layer = LayerMask.NameToLayer("StickyRoad");
    }

    void FixedUpdate()
    {
        // Invoke the state function stored by the runCurrentState function pointer.
        runCurrentState();
    }

    private void runInitialWait()
    {
        // Update timer. If it passes the initial wait duration, update to idle wait state. This state is never encountered again after it ends.
        timer += Time.fixedDeltaTime;
        if (timer >= initialWait)
            invokeWait();
    }

    private void invokeWait()
    {
        timer = 0f;
        runCurrentState = this.runWait;
    }

    private void runWait()
    {
        // Update timer. If it passes the idle wait duration, update to shake state.
        timer += Time.fixedDeltaTime;
        if (timer >= idleWait)
            invokeShake();
    }

    private void invokeShake()
    {
        timer = 0f;
        runCurrentState = this.runShake;
    }

    private void runShake()
    {
        // Update timer. If it passes the shake duration, update to stomp state.
        timer += Time.fixedDeltaTime;
        if (timer >= shakeDuration)
        {
            invokeStomp();
            return;
        }
        
        // Update position along up vector in a sine-based motion based on set amplitude and period.
        Vector3 newPos = startPos + transform.up * shakeAmplitude * (float)Math.Sin(timer / shakePeriod * 2 * Math.PI);
        rigidBody.MovePosition(newPos);
    }

    private void invokeStomp()
    {
        rigidBody.MovePosition(startPos);
        timer = 0f;
        runCurrentState = this.runStomp;

        if (isLethal)
            killPlane.layer = LayerMask.NameToLayer("KillSquish");
    }

    private void runStomp()
    {
        // Check for stomper collision with the course and stop early if encountered.
        // TODO implement

        // Move stomper head down along local up vector. If it passes max stomp length, move to max stomp length and update to retreat wait state.
        Vector3 newPos = rigidBody.position - transform.up * stompSpeed * Time.fixedDeltaTime;
        if ((newPos - startPos).magnitude >= maxStompLength)
        {
            rigidBody.MovePosition(startPos - transform.up * maxStompLength);
            invokeRetreatWait();
        }
        else
        {
            rigidBody.MovePosition(newPos);
        }
    }

    private void invokeRetreatWait()
    {
        timer = 0f;
        runCurrentState = this.runRetreatWait;

        if (isLethal)
        {
            killPlane.layer = LayerMask.NameToLayer("Wall");
            // TODO invoke sound here
        }
    }

    private void runRetreatWait()
    {
        timer += Time.fixedDeltaTime;
        if (timer >= retreatWait)
            invokeRetreat();
    }

    private void invokeRetreat()
    {
        timer = 0f;
        runCurrentState = this.runRetreat;
    }

    private void runRetreat()
    {
        // Move stomper head up along local up vector. If it retreats beyond its start position, move to start position and update to wait state.
        Vector3 newPos = rigidBody.position + transform.up * retreatSpeed * Time.fixedDeltaTime;
        if (Vector3.Dot(newPos - startPos, transform.up) >= 0)
        {
            rigidBody.MovePosition(startPos);
            invokeWait();
        }
        else
        {
            rigidBody.MovePosition(newPos);
        }
    }
}
