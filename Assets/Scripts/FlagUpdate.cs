using System;
using UnityEngine;

public class FlagUpdate : MonoBehaviour
{
    // Cycle constants, set in editor
    public float amplitude;
    public float cycleTime;

    // Instance variables
    private Vector3 startPos;
    private Vector3 startRot;
    private float time;

    void Start()
    {
        startPos = transform.position;
        startRot = transform.rotation.eulerAngles;
        time = 0;
    }

    void Update()
    {
        time = (time + Time.deltaTime) % cycleTime;
        updatePosition();
        updateRotation();
    }

    private void updatePosition()
    {
        float newPosY = startPos.y + amplitude * (float)Math.Sin(time / cycleTime * 2 * Math.PI);
        transform.position = new Vector3(startPos.x, newPosY, startPos.z);
    }

    private void updateRotation()
    {
        float newRotY = startRot.y + (time / cycleTime * 360);
        transform.rotation = Quaternion.Euler(new Vector3(startRot.x, newRotY, startRot.z));
        Debug.Log(transform.rotation.eulerAngles.y);
    }
}
