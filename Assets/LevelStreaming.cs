using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelStreaming : MonoBehaviour
{
    
    // list of scene names to be loaded when the player is in this scene
    [SerializeField] private List<string> sceneNames;

    private void OnTriggerEnter(Collider other)
    {
        // unload all loaded scenes not in sceneNames and load all unloaded scenes in sceneNames
        List<string> scenesToLoad = new List<string>();
        scenesToLoad.AddRange(sceneNames);
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!sceneNames.Contains(scene.name))
            {
                SceneManager.UnloadSceneAsync(scene.name);
            }
            else
            {
                scenesToLoad.Remove(scene.name);
            }
        }
        foreach (string sceneName in scenesToLoad)
        {
            SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        }
        
        // get view frustum of camera main
        Camera camera = Camera.main;
        Vector3[] frustumCorners = new Vector3[4];
        camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), camera.farClipPlane, Camera.MonoOrStereoscopicEye.Mono, frustumCorners);
    }
}
