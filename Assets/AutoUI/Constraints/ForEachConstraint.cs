﻿using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ForEachConstraint : AutoUIConstraint
{

    [SerializeField] private string varName;
    [SerializeField] private string collection;
    
    private GameObject[] childTemplates; // the initial children on startup, these game objects will remain disabled so we can use them as templates (a set of these for each element in the list)
    private List<GameObject> children = new List<GameObject>();
    
    private List<object> collectionContents = new List<object>();
    private Dictionary<string, int> childIndices = new Dictionary<string, int>(); // the UINodes passed as children may not be direct children, so getting the index requires calculation which we cash here

    protected override void Awake()
    {
        base.Awake();
        
        AddValueInput(collection);

        childTemplates = new GameObject[gameObject.transform.childCount];
        for (int i = 0; i < gameObject.transform.childCount; i++)
        {
            GameObject child = gameObject.transform.GetChild(i).gameObject;
            child.SetActive(false);
            childTemplates[i] = child;
        }
    }

    private void OnEnable()
    {
        node.PrepareChildContext.Add(PrepareChildContext);
    }

    private void OnDisable()
    {
        node.PrepareChildContext.Remove(PrepareChildContext);
    }

    private DataContext PrepareChildContext(UINode childNode, DataContext context)
    {
        if (childTemplates.Length == 0) return context;

        int childIndex = -1;
        if (childIndices.ContainsKey(childNode.HierarchyPath)) childIndex = childIndices[childNode.HierarchyPath];
        else
        {   
            for (int i = 0; i < children.Count; i++)
            {
                string directChildHierarchyPath = node.HierarchyPath + children[i].gameObject.GetInstanceID();
                string distantChildHierarchyPath = childNode.HierarchyPath;
                bool startsWith = distantChildHierarchyPath.StartsWith(directChildHierarchyPath);
                if (startsWith)
                {
                    Debug.Log($"{distantChildHierarchyPath} starts with {directChildHierarchyPath}");
                    childIndices[distantChildHierarchyPath] = i;
                    childIndex = i;
                    break;
                }
            }
        }

        if (childIndex >= 0 && childIndex < collectionContents.Count)
        {
            context = new DataContext(context);
            context.SetLocal(varName, collectionContents[childIndex]);
            return context;
        } else return context;
    }

    public override void Render(DataContext context)
    {
        try
        {
            object result = values[0].Evaluate(DataStore.Instance);
            if (result is not IEnumerable)
            {
                Debug.LogError("Collection must be of type ICollection: " + result, this);
                return;
            }

            collectionContents.Clear();
            foreach (object element in (IEnumerable) result) collectionContents.Add(element);
            int countDiff = collectionContents.Count * childTemplates.Length - children.Count;
            switch (countDiff)
            {
                case > 0:
                    for (int i = 0; i < countDiff; i++) // add new children
                    {
                        GameObject child = Instantiate(childTemplates[i % childTemplates.Length], gameObject.transform);
                        child.SetActive(true);
                        children.Add(child);
                    }
                    break;
                case < 0:
                    for (int i = children.Count - 1; i >= collectionContents.Count * childTemplates.Length; i--) // remove children
                    {
                        GameObject child = children[i];
                        children.Remove(child);
                        Destroy(child);
                    }
                    break;
            }
        }
        catch (EvaluationException e)
        {
            Debug.LogException(e, this);
        }
    }
}