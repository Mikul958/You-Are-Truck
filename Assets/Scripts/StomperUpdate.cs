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
    private Vector3 startPos;
    private Vector3 upVector;
    private int state;
    private float timer;
    private RunState runState;
    
    void Start()
    {
        startPos = transform.position;
        upVector = transform.up;
        timer = 0f;
        runState = this.runInitialWait;
        
        if (isLethal)
            killPlane.layer = LayerMask.NameToLayer("Wall");
        else
            killPlane.layer = LayerMask.NameToLayer("Road");
    }

    void Update()
    {
        runState();
    }

    private void runInitialWait()
    {
        timer += Time.deltaTime;
        if (timer >= initialWait)
            invokeWait();
    }

    private void invokeWait()
    {
        timer = 0f;
        runState = this.runWait;
    }

    private void runWait()
    {
        timer += Time.deltaTime;
        if (timer >= idleWait)
            invokeShake();
    }

    private void invokeShake()
    {
        timer = 0f;
        runState = this.runShake;
    }

    private void runShake()
    {
        timer += Time.deltaTime;
        if (timer >= shakeDuration)
        {
            invokeStomp();
            return;
        }
        
        // TODO change this to work based on rotation
        float newY = startPos.y + shakeAmplitude * (float)Math.Sin(timer / shakePeriod * 2 * Math.PI);
        rigidBody.MovePosition(new Vector3(startPos.x, newY, startPos.z));
    }

    private void invokeStomp()
    {
        rigidBody.MovePosition(startPos);
        timer = 0f;
        runState = this.runStomp;

        if (isLethal)
            killPlane.layer = LayerMask.NameToLayer("KillSquish");
    }

    private void runStomp()
    {
        // TODO check for collision here and invoke retreat wait + sound effect
        

        // TODO change this to work based on rotation
        float newY = rigidBody.position.y - stompSpeed * Time.deltaTime;
        if (newY <= startPos.y - maxStompLength)
        {
            rigidBody.MovePosition(new Vector3(startPos.x, startPos.y - maxStompLength, startPos.z));
            invokeRetreatWait();
        }
        else
        {
            rigidBody.MovePosition(new Vector3(startPos.x, newY, startPos.z));
        }
    }

    private void invokeRetreatWait()
    {
        timer = 0f;
        runState = this.runRetreatWait;

        if (isLethal)
        {
            killPlane.layer = LayerMask.NameToLayer("Wall");
            // TODO invoke sound here
        }
    }

    private void runRetreatWait()
    {
        timer += Time.deltaTime;
        if (timer >= retreatWait)
            invokeRetreat();
    }

    private void invokeRetreat()
    {
        timer = 0f;
        runState = this.runRetreat;
    }

    private void runRetreat()
    {
        // TODO change this to work based on rotation
        float newY = rigidBody.position.y + retreatSpeed * Time.deltaTime;
        if (newY >= startPos.y)
        {
            rigidBody.MovePosition(new Vector3(startPos.x, startPos.y, startPos.z));
            invokeWait();
        }
        else
        {
            rigidBody.MovePosition(new Vector3(startPos.x, newY, startPos.z));
        }
    }
}
