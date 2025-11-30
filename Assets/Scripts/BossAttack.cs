using UnityEngine;

public class BossAttack : MonoBehaviour
{
    // Referenced Game Objects and Components
    public GameObject rocketPrefab;

    // Constants, set in game engine
    public float idleWait;
    public float attackWait;
    public int attackCount;

    // Instance variables
    private bool isWaiting;
    private float timer;
    private float rocketsFired;

    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        isWaiting = true;
        timer = 0f;
    }

    // Update is called once per frame
    void Update()
    {
        if (isWaiting)
            updateWait();
        else
            updateAttack();
    }

    private void updateWait()
    {
        timer += Time.deltaTime;
        if (timer >= idleWait)
        {
            timer = 0f;
            isWaiting = false;
        }
    }

    private void updateAttack()
    {
        timer += Time.deltaTime;
        if (timer >= attackWait)
        {
            timer = 0f;
            rocketsFired++;
            spawnRocket();
        }
        if (rocketsFired >= attackCount)
        {
            rocketsFired = 0;
            isWaiting = true;
        }
    }

    private void spawnRocket()
    {
        Instantiate(rocketPrefab, Vector3.zero, Quaternion.identity);  // Initial position and rotation are handled by rocket
    }
}
