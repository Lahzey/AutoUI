using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CS01VectorScript : MonoBehaviour
{
    public ComputeShader ComputeShader;
    public int DispatchSize;

    private Vector3[] vectors;
    private int vectorCount;

    private ComputeBuffer buffer;
    private int kernelHandle;
    
    void Start()
    {
        kernelHandle = ComputeShader.FindKernel("CSMain");

        vectorCount = 8 * DispatchSize;
        vectors = new Vector3[vectorCount];

        using (buffer = new ComputeBuffer(vectorCount, sizeof(float) * 3))
        {
            buffer.SetData(vectors);
        
            ComputeShader.SetBuffer(kernelHandle, "buffer", buffer);
            ComputeShader.Dispatch(kernelHandle, DispatchSize, 1, 1);
        
            buffer.GetData(vectors);
        
            foreach (Vector3 vector in vectors)
            {
                Debug.Log(vector.x);
            }
        }
    }
}
