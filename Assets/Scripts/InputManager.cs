using UnityEngine;

public class InputManager : MonoBehaviour
{
    private bool disableInputReads;
    private int forwardInputSign;   // 0 = neutral, 1 = forwards, -1 = backwards
    private int sidewaysInputSign;  // 0 = neutral, -1 = left, 1 = right
    private bool jumpPressed;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        disableInputReads = false;
        forwardInputSign = 0;
        sidewaysInputSign = 0;
        jumpPressed = false;
    }

    // Update is called once per frame
    void Update()
    {
        readPlayerInputs();
    }

    private void readPlayerInputs()
    {
        if (disableInputReads)
            return;
        
        // Get inputs for acceleration
        forwardInputSign = 0;
        if (Input.GetKey(KeyCode.W))
            forwardInputSign++;
        if (Input.GetKey(KeyCode.S))
            forwardInputSign--;

        // Get inputs for turning
        sidewaysInputSign = 0;
        if (Input.GetKey(KeyCode.A))
            sidewaysInputSign--;
        if (Input.GetKey(KeyCode.D))
            sidewaysInputSign++;
        
        // Get jump input
        jumpPressed = Input.GetKey(KeyCode.Space);
    }

    public void disableInputs()
    {
        disableInputReads = true;
        forwardInputSign = 0;
        sidewaysInputSign = 0;
        jumpPressed = false;
    }
    
    public int getForwardInputSign()
    {
        return forwardInputSign;
    }

    public int getSidewaysInputSign()
    {
        return sidewaysInputSign;
    }

    public bool isJumpPressed()
    {
        return jumpPressed;
    }
}
