using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class TorchUpdate : Torch
{
    private bool isActive = false;
    
    private void Update()
    {
        if (isActive)
        {
            Fuel -= Time.deltaTime;
            if (Fuel <= 0)
            {
                isActive = false;
                DeactiveTorch();
            }
        }
    }

    public override void InitializeTorch(float minFuel, float maxFuel)
    {
        Fuel = Random.Range(minFuel, maxFuel);
        isActive = true;
    }

    public override void PauseTorch()
    {
        isActive = false;
    }

    public override void ResumeTorch()
    {
        isActive = true;
    }
}