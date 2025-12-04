using UnityEngine;

public class BossMove : MonoBehaviour
{
    // Referenced Game Objects and Components
    public WheelAnimator wheelAnimator;
    
    void Start()
    {
        
    }

    void Update()
    {
        wheelAnimator.updateMovementInfo(10, 1, false);
    }
}
