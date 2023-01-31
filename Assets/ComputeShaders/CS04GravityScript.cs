using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class CS04GravityScript : MonoBehaviour
{
    private struct Ball
    {
        public uint id;
        public Vector3 position;
        public Vector3 velocity;
    }
    
    public ComputeShader ComputeShader;
    [Range(1, 10000)] public int BallCount = 100;
    public float[] Gravity = {0, -9.81f, 0};
    public bool IsRunning = false;

    private Ball[] balls;
    private Transform[] transforms;
    private ComputeBuffer ballBuffer;
    private int kernelHandle;

    // Start is called before the first frame update
    void Start()
    {
        balls = new Ball[BallCount];
        transforms = new Transform[BallCount];
        CreateBalls();
        
        kernelHandle = ComputeShader.FindKernel("CSMain");
        ballBuffer = new ComputeBuffer(BallCount, sizeof(uint) + 6 * sizeof(float));
        ComputeShader.SetBuffer(kernelHandle, "BallBuffer", ballBuffer);
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsRunning) return;
        
        ComputeShader.SetFloats("Gravity", Gravity);
        ComputeShader.SetFloat("DeltaTime", Time.deltaTime);
        ComputeShader.Dispatch(kernelHandle, Mathf.CeilToInt(BallCount / 32f), 1, 1);
        ballBuffer.GetData(balls);
        
        for (int i = 0; i < BallCount; i++)
        {
            transforms[i].localPosition = balls[i].position;
        }
    }

    private void OnDestroy()
    {
        ballBuffer.Dispose();
    }

    private void CreateBalls()
    {
        for (int i = 0; i < BallCount; i++)
        {
            GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.transform.parent = transform;
            ball.transform.localPosition = new Vector3(Random.Range(-50, 50), Random.Range(0, 100), Random.Range(-50, 50));
            Destroy(ball.GetComponent<SphereCollider>());
            
            transforms[i] = ball.transform;
            balls[i].position = ball.transform.localPosition;
            balls[i].velocity = new Vector3(0, Random.Range(0, 20), 0);
            balls[i].id = (uint) i;
        }
    }
}
