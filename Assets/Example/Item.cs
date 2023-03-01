using UnityEngine;

namespace Example {
[CreateAssetMenu(fileName = "Item", menuName = "ScriptableObjects/Item", order = 1)]
public class Item : ScriptableObject {
	public string name;
	public Sprite sprite;
}
}