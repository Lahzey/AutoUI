using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GravityBallsComparison : MonoBehaviour
{
    private int ballCount;
    
    // Start is called before the first frame update
    void Start()
    {
        ballCount = GetComponent<CS04GravityScript>().BallCount;
        CreateBalls();
    }

    private void CreateBalls()
    {
        for (int i = 0; i < ballCount; i++)
        {
            GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.transform.parent = transform;
            ball.transform.position = new Vector3(Random.Range(-50, 50), Random.Range(0, 100), Random.Range(-50, 50));
            Destroy(ball.GetComponent<SphereCollider>());
            Rigidbody rigidbody = ball.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
        }
    }

    [ContextMenu("Activate Balls")]
    private void ActivateBalls()
    {
        for (int i = 0; i < ballCount; i++)
        {
            transform.GetChild(i).GetComponent<Rigidbody>().useGravity = true;
        }
    }
}
