using System.Collections.Generic;
using System.Linq;
using AutoUI.Data;
using UnityEngine;

namespace AutoUI {
public class RenderManager : MonoBehaviour {
	private static RenderManager defaultInternal;
	public static RenderManager Default => defaultInternal ? defaultInternal : InitDefaultRenderManager(); // apparently ?? does not use the unity null override, so we have to use ? : instead

	[SerializeField] private float renderInterval = 0.1f;

	public readonly List<UINode> RootUINodes = new();
	private readonly Dictionary<int, UINode> UINodeMapping = new(); // maps gameObject instanceIDs to UINodes
	private float storedDeltaTime = 0f;


	private static RenderManager InitDefaultRenderManager() {
		// create object in scene to hold the default render manager
		GameObject renderManagerObject = new("[AutoUI] Default RenderManager");
		defaultInternal = renderManagerObject.AddComponent<RenderManager>();
		return defaultInternal;
	}

	private void Update() {
		Render();
		// storedDeltaTime += Time.unscaledDeltaTime;
		// if (storedDeltaTime >= renderInterval)
		// {
		//     storedDeltaTime = 0f;
		//     Render();
		// }
	}

	private void Render() {
		foreach (UINode uiNode in RootUINodes) uiNode.Render(DataStore.Instance);
	}

	public UINode GetUINode(int instanceID) {
		if (UINodeMapping.ContainsKey(instanceID))
			return UINodeMapping[instanceID];
		return null;
	}

	public UINode AddUINode(GameObject gameObject) {
		UINode uiNode = new(gameObject);
		UINodeMapping.Add(uiNode.InstanceId, uiNode);

		foreach (UINode rootUINode in RootUINodes.ToList()) // use ToList() to prevent modifying the list while iterating over it, not efficient but that is not crucial here
		{
			// check if this node is a child of the root node
			if (uiNode.HierarchyPath.StartsWith(rootUINode.HierarchyPath)) {
				rootUINode.AddChild(uiNode);
				return uiNode; // return early to prevent adding this node as a root node, if this node is a child of a root node there shouldn't be any other root nodes that are a child of this node anyway
			}

			// check if the root node should be a child of this node
			if (rootUINode.HierarchyPath.StartsWith(uiNode.HierarchyPath)) {
				uiNode.AddChild(rootUINode);
				RootUINodes.Remove(rootUINode);
			}
		}

		RootUINodes.Add(uiNode);
		return uiNode;
	}

	public void RemoveUINode(UINode uiNode) {
		UINodeMapping.Remove(uiNode.InstanceId);

		foreach (UINode rootUINode in RootUINodes)
			if (uiNode.HierarchyPath.StartsWith(rootUINode.HierarchyPath)) {
				rootUINode.RemoveChild(uiNode);
				return;
			}

		RootUINodes.Remove(uiNode);
	}
}
}