using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public abstract class AutoUIConstraint : MonoBehaviour
{
    protected UINode node;
    
    protected virtual void Awake()
    {
        node = UINode.AddToNode(this);
    }

    protected virtual void OnDestroy()
    {
        node.Remove(this);
    }

    public abstract void Render(DataContext context);


}
