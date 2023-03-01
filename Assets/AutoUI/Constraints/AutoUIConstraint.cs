using AutoUI.Data;
using UnityEngine;

namespace AutoUI.Constraints {
public abstract class AutoUIConstraint : MonoBehaviour {
	protected UINode node;

	protected virtual void Awake() {
		node = UINode.AddToNode(this);
	}

	protected virtual void OnDestroy() {
		node.Remove(this);
	}

	public abstract void Render(DataContext context);
}
}