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
    public float maxExtend;
    
    // Instance variables
    private List<WheelInfo> frontWheels;
    private List<WheelInfo> backWheels;
    private float wheelCircumference = 1;
    private float engineSpeed = 0;
    private int steerDirection = 0;
    private bool jumpPressed = false;

    private float turnAngle = 0;        // Current front-back rotation from tires moving
    private float steerAngle = 0;       // Current sideways offset of tires from steering
    private float totalJumpOffset = 0;  // Current vertical offset of tires from jumping

    private class WheelInfo
    {
        public GameObject wheelModel;
        public Vector3 initialPosition;

        public WheelInfo(GameObject wheelReference, Vector3 localPosition)
        {
            wheelModel = wheelReference;
            initialPosition = localPosition;
        }
    }

    void Start()
    {
        // Get wheels marked as front or back from children
        frontWheels = new List<WheelInfo>();
        backWheels = new List<WheelInfo>();
        foreach (Transform childTransform in gameObject.transform)
        {
            if (childTransform.gameObject.layer == LayerMask.NameToLayer("FrontWheelAnimate"))
                frontWheels.Add(new WheelInfo(childTransform.gameObject, childTransform.localPosition));
            else if (childTransform.gameObject.layer == LayerMask.NameToLayer("BackWheelAnimate"))
                backWheels.Add(new WheelInfo(childTransform.gameObject, childTransform.localPosition));
        }

        // Derive circumference (which speed is based on) from first encountered wheel's radius, disable if no wheels found
        if (frontWheels.Count == 0 && backWheels.Count == 0)
            this.enabled = false;
        wheelCircumference = 2 * (float)Math.PI * wheelRadius;
    }

    void Update()
    {
        // Calculate new rotation values from turning and steering
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

        // Calculate new position values from jump extension
        if (jumpPressed)
        {
            totalJumpOffset += extendSpeed * Time.deltaTime;
            if (totalJumpOffset > maxExtend)
                totalJumpOffset = maxExtend;
        }
        else
        {
            totalJumpOffset -= retreatSpeed * Time.deltaTime;
            if (totalJumpOffset < 0)
                totalJumpOffset = 0;
        }
        Vector3 localOffset = totalJumpOffset * (-Vector3.up);

        foreach (WheelInfo wheel in frontWheels)
        {
            wheel.wheelModel.transform.localRotation = frontWheelRotation;
            wheel.wheelModel.transform.localPosition = wheel.initialPosition + localOffset;
        }
        foreach (WheelInfo wheel in backWheels)
        {
            wheel.wheelModel.transform.localRotation = backWheelRotation;
            wheel.wheelModel.transform.localPosition = wheel.initialPosition + localOffset;
        }
    }

    public void updateMovementInfo(float engineSpeed, int steerDirection, bool jumpPressed)
    {
        this.engineSpeed = engineSpeed;
        this.steerDirection = steerDirection;
        this.jumpPressed = jumpPressed;
    }
}
