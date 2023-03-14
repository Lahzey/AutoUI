using System.Collections;
using System.Collections.Generic;
using AutoUI.Data;
using AutoUI.Inspector;
using AutoUI.Parsing;
using AutoUI.Parsing.Expressions;
using UnityEngine;

namespace AutoUI.Constraints {
public class ForEachConstraint : AutoUIConstraint {
	private static readonly List<object> EMPTY_LIST = new();

	[SerializeField] private string varName;
	[SerializeField] private CodeInput collectionConstraint;
	private readonly Dictionary<string, int> childIndices = new(); // the UINodes passed as children may not be direct children, so getting the index requires calculation which we cash here
	private readonly List<GameObject> children = new();

	private GameObject[] childTemplates; // the initial children on startup, these game objects will remain disabled so we can use them as templates (a set of these for each element in the list)

	private readonly List<object> collectionContents = new();

	private Expression collectionExpression;

	protected override void Awake() {
		base.Awake();

		ParseResult parseResult = collectionConstraint.Result;
		collectionExpression = parseResult is { Success: true } ? parseResult.Expression : null;
		if (collectionExpression == null) Debug.LogError("Failed to parse collection expression '" + collectionConstraint.Input + "', defaulting to empty list.", this);

		childTemplates = new GameObject[gameObject.transform.childCount];
		for (int i = 0; i < gameObject.transform.childCount; i++) {
			GameObject child = gameObject.transform.GetChild(i).gameObject;
			child.SetActive(false);
			childTemplates[i] = child;
		}
	}

	private void OnEnable() {
		node.PrepareChildContext.Add(PrepareChildContext);
	}

	private void OnDisable() {
		node.PrepareChildContext.Remove(PrepareChildContext);
	}

	private DataContext PrepareChildContext(UINode childNode, DataContext context) {
		if (childTemplates.Length == 0) return context;

		int childIndex = -1;
		if (childIndices.ContainsKey(childNode.HierarchyPath)) childIndex = childIndices[childNode.HierarchyPath];
		else
			for (int i = 0; i < children.Count; i++) {
				string directChildHierarchyPath = node.HierarchyPath + children[i].gameObject.GetInstanceID();
				string distantChildHierarchyPath = childNode.HierarchyPath;
				bool startsWith = distantChildHierarchyPath.StartsWith(directChildHierarchyPath);
				if (startsWith) {
					childIndices[distantChildHierarchyPath] = i;
					childIndex = i;
					break;
				}
			}

		if (childIndex >= 0 && childIndex < collectionContents.Count) {
			context = new DataContext(context);
			context.SetLocal(varName, collectionContents[childIndex]);
			return context;
		}

		return context;
	}

	public override void Render(DataContext context) {
		try {
			object result = collectionExpression?.Evaluate(context) ?? EMPTY_LIST;
			if (result is not IEnumerable enumerable) {
				Debug.LogError("Collection must be of type ICollection: " + result, this);
				return;
			}

			collectionContents.Clear();
			foreach (object element in enumerable) collectionContents.Add(element);
			int countDiff = collectionContents.Count * childTemplates.Length - children.Count;
			switch (countDiff) {
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
		catch (EvaluationException e) {
			Debug.LogException(e, this);
		}
	}
}
}