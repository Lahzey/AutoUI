using System;
using System.Collections.Generic;
using AutoUI.Constraints;
using AutoUI.Data;
using UnityEngine;

namespace AutoUI {
public class UINode {
	private readonly List<UINode> children = new();

	private readonly List<AutoUIConstraint> constraints = new();
	public readonly GameObject GameObject;
	public readonly string HierarchyPath;
	public readonly int InstanceId;

	public readonly List<Func<UINode, DataContext, DataContext>> PrepareChildContext = new();
	private ShowConstraint showConstraint; // handle show constraints separately

	public UINode(GameObject gameObject) {
		GameObject = gameObject;
		HierarchyPath = GetHierarchyPath(gameObject.transform);
		InstanceId = gameObject.GetInstanceID();
	}

	public void AddChild(UINode child) {
		foreach (UINode uiNode in children)
			if (child.HierarchyPath.StartsWith(uiNode.HierarchyPath)) {
				uiNode.AddChild(child);
				return;
			}

		children.Add(child);
	}

	public void RemoveChild(UINode child) {
		if (children.Remove(child)) return;

		foreach (UINode uiNode in children)
			if (child.HierarchyPath.StartsWith(uiNode.HierarchyPath)) {
				uiNode.RemoveChild(child);
				return;
			}
	}

	public static UINode AddToNode(AutoUIConstraint constraint) {
		RenderManager renderManager = RenderManager.Default;
		UINode node = renderManager.GetUINode(constraint.gameObject.GetInstanceID()) ?? renderManager.AddUINode(constraint.gameObject);
		if (constraint is ShowConstraint showConstraint) node.showConstraint = showConstraint;
		else node.constraints.Add(constraint);
		return node;
	}

	public void Remove(AutoUIConstraint constraint) {
		if (constraint == showConstraint) showConstraint = null;
		else constraints.Remove(constraint);

		if (constraints.Count == 0 && showConstraint is null) {
			RenderManager renderManager = RenderManager.Default;
			renderManager.RemoveUINode(this);
		}
	}

	private static string GetHierarchyPath(Transform transform) {
		string path = "";
		do {
			path = transform.gameObject.GetInstanceID() + "/" + path;
		} while ((transform = transform.parent) != null);

		return path;
	}

	public void Render(DataContext context) {
		if (showConstraint is not null) showConstraint.Render(context); // do not use null propagation here, we do NOT want to use null equality checks

		if (!GameObject.activeInHierarchy) return; // not rendering hidden constraints

		foreach (AutoUIConstraint constraint in constraints) constraint.Render(context);

		for (int i = 0; i < children.Count; i++) {
			UINode child = children[i];
			// TODO: find a way to check if the child is scheduled for destruction to prevent getting errors for 1 frame when destroying a child
			foreach (Func<UINode, DataContext, DataContext> prepareChildContext in PrepareChildContext) context = prepareChildContext(child, context);
			child.Render(context);
		}
	}
}
}