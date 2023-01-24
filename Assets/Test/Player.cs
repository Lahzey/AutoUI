using System;
using UnityEngine;

public class Player : MonoBehaviour
{
    public string name = "Player";

    private void Awake()
    {
        DataStore.Instance.Set(DataKeys.Player, this);
    }
}