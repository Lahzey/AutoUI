using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TorchCoroutine : Torch
{
    private float startTime;
    private IEnumerator burnTorchCoroutine;
    
    public override void InitializeTorch(float minFuel, float maxFuel)
    {
        Fuel = Random.Range(minFuel, maxFuel);
        startTime = Time.time;
        burnTorchCoroutine = BurnTorch();
        StartCoroutine(burnTorchCoroutine);
    }

    public override void PauseTorch()
    {
        StopCoroutine(burnTorchCoroutine);
        Fuel -= Time.time - startTime;
    }

    public override void ResumeTorch()
    {
        if (Fuel <= 0)
            return;
        
        startTime = Time.time;
        StartCoroutine(burnTorchCoroutine);
    }
    
    private IEnumerator BurnTorch()
    {
        yield return new WaitForSeconds(Fuel);
        
        DeactiveTorch();
    }
}
