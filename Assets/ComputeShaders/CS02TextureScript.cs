using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CS02TextureScript : MonoBehaviour
{
    public ComputeShader ComputeShader;
    public Vector2Int Resolution;
    
    private RenderTexture texture;
    private int[] res = new int[2];
    private int kernelHandle;
    private float[] numThreads;
    
    // Start is called before the first frame update
    void Start()
    {
        texture = new RenderTexture(Resolution.x, Resolution.y, 24);
        texture.enableRandomWrite = true;
        texture.Create();
        
        kernelHandle = ComputeShader.FindKernel("CSMain");
        res[0] = Resolution.x;
        res[1] = Resolution.y;
        
        ComputeShader.GetKernelThreadGroupSizes(kernelHandle, out uint x, out uint y, out uint z);
        numThreads = new float[] {x, y, z};
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        ComputeShader.SetTexture(kernelHandle, "Result", texture);
        ComputeShader.SetInts("Resolution", res);
        
        ComputeShader.Dispatch(kernelHandle, Mathf.CeilToInt(Resolution.x / numThreads[0]), Mathf.CeilToInt(Resolution.y / numThreads[1]), 1);
        Graphics.Blit(texture, dest);
    }
}
