using UnityEngine;

public class BossMove : MonoBehaviour
{
    // Referenced Game Objects and Components
    public WheelAnimator wheelAnimator;
    
    void Start()
    {
        // TODO unused :(
    }

    void Update()
    {
        wheelAnimator.updateMovementInfo(0, 0, false);
    }
}
