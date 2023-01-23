using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Torch : MonoBehaviour
{
    public float Fuel;
    public Light Light;

    private void Awake()
    {
        Light = GetComponent<Light>();
    }
    
    public abstract void InitializeTorch(float minFuel, float maxFuel);
    
    public abstract void PauseTorch();
    
    public abstract void ResumeTorch();

    public void DeactiveTorch()
    {
        Fuel = 0;
        Light.intensity = 0;
        gameObject.SetActive(false);
    }
}
