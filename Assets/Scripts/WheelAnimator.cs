using System;
using System.Collections.Generic;
using UnityEngine;

public class WheelAnimator : MonoBehaviour
{
    // Constants, set in game engine
    public float wheelRadius;
    public float steerSpeed;
    public float maxSteerAngle;
    public float extendSpeed;
    public float retreatSpeed;
    
    // Instance variables
    private List<GameObject> frontWheels;
    private List<GameObject> backWheels;
    private float wheelCircumference = 1;
    private float engineSpeed = 0;
    private int steerDirection = 0;
    private bool jumpPressed = false;

    private float turnAngle;   // Current front-back rotation from tires moving
    private float steerAngle;  // Current sideways offset of tires from steering
    private float jumpOffset;  // Current vertical offset of tires from jumping

    void Start()
    {
        // Get wheels marked as front or back from children
        frontWheels = new List<GameObject>();
        backWheels = new List<GameObject>();
        foreach (Transform childTransform in gameObject.transform)
        {
            if (childTransform.gameObject.layer == LayerMask.NameToLayer("FrontWheelAnimate"))
                frontWheels.Add(childTransform.gameObject);
            else if (childTransform.gameObject.layer == LayerMask.NameToLayer("BackWheelAnimate"))
                backWheels.Add(childTransform.gameObject);
        }

        // Derive circumference (which speed is based on) from first encountered wheel's radius, disable if no wheels found
        wheelCircumference = 2 * (float)Math.PI * wheelRadius;
        if (frontWheels.Count == 0 && backWheels.Count == 0)
            this.enabled = false;
    }

    void Update()
    {
        turnAngle = (turnAngle + 360 * engineSpeed / wheelCircumference * Time.deltaTime) % 360;
        if (steerDirection == 0)
        {
            if (Math.Abs(steerAngle) < steerSpeed * Time.deltaTime)
                steerAngle = 0;
            else
                steerAngle -= steerSpeed * Math.Sign(steerAngle) * Time.deltaTime;
        }
        else
        {
            steerAngle += steerSpeed * steerDirection * Time.deltaTime;
            if (Math.Abs(steerAngle) > maxSteerAngle)
                steerAngle = maxSteerAngle * steerDirection;
        }

        Quaternion frontWheelRotation = Quaternion.Euler(turnAngle, steerAngle, 0);
        Quaternion backWheelRotation = Quaternion.Euler(turnAngle, 0, 0);
        foreach (GameObject wheelModel in frontWheels)
            wheelModel.transform.localRotation = frontWheelRotation;
        foreach (GameObject wheelModel in backWheels)
            wheelModel.transform.localRotation = backWheelRotation;
        
        // TODO jumps
    }

    public void updateMovementInfo(float engineSpeed, int steerDirection, bool jumpPressed)
    {
        this.engineSpeed = engineSpeed;
        this.steerDirection = steerDirection;
        this.jumpPressed = jumpPressed;
    }
}
