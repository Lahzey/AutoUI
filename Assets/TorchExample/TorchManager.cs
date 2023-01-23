using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TorchMode
{
    Update, // each light has its own update
    Coroutine, // each light has its own coroutine
    ManagerCoroutine // all lights are updated by a single coroutine
}

public class TorchManager : MonoBehaviour
{
    public GameObject TorchPrefab;
    public int TorchCount;
    public TorchMode TorchMode;
    public Vector2 FuelValueRange;
    
    private Torch[] torches;
    
    // Start is called before the first frame update
    void Start()
    {
        torches = new Torch[TorchCount];

        GameObject torch;
        Torch t;
        
        for (int i = 0; i < TorchCount; i++)
        {
            torch = Instantiate(TorchPrefab, transform);
            torch.transform.position = new Vector3(Random.Range(-5, 5), 1, Random.Range(-5, 5));
            
            switch (TorchMode)
            {
                case TorchMode.Update:
                    t = torch.AddComponent<TorchUpdate>();
                    break;
                case TorchMode.Coroutine:
                    t = torch.AddComponent<TorchCoroutine>();
                    break;
                case TorchMode.ManagerCoroutine:
                    // t = torch.AddComponent<TorchManagerCoroutine>();
                    // break;
                default:
                    t = torch.AddComponent<TorchUpdate>();
                    break;
            }
            
            t.InitializeTorch(FuelValueRange.x, FuelValueRange.y);
            torches[i] = t;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
