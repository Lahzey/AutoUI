using System.Collections.Generic;
using AutoUI.Data;
using UnityEngine;

namespace Example {
public class Player : MonoBehaviour {
	public string name = "Player";
	public float maxHealth = 100;
	public float currentHealth = 100;
	public float healthRegen;

	[SerializeField] private Item appleItem;
	[SerializeField] private Item orangeItem;
	[SerializeField] private Item bananaItem;

	public Dictionary<Item, int> inventory = new();

	private void Awake() {
		DataStore.Instance.Set(DataKeys.Player, this);
	}

	private void Update() {
		currentHealth += healthRegen * Time.deltaTime;
		if (currentHealth < 0) currentHealth = 0;
		else if (currentHealth > maxHealth) currentHealth = maxHealth;

		bool shiftDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
		if (Input.GetKeyDown(KeyCode.I))
			DataStore.Instance.Set(DataKeys.ShowInventory, !DataStore.Instance.Get(DataKeys.ShowInventory));
		if (Input.GetKeyDown(KeyCode.A))
			if (shiftDown) DropItem(appleItem);
			else PickupItem(appleItem);
		if (Input.GetKeyDown(KeyCode.O))
			if (shiftDown) DropItem(orangeItem);
			else PickupItem(orangeItem);
		if (Input.GetKeyDown(KeyCode.B))
			if (shiftDown) DropItem(bananaItem);
			else PickupItem(bananaItem);
	}

	private void PickupItem(Item item) {
		if (inventory.ContainsKey(item))
			inventory[item]++;
		else
			inventory[item] = 1;
	}

	private void DropItem(Item item) {
		if (!inventory.ContainsKey(item)) return;

		int count = inventory[item] - 1;
		if (count <= 0) inventory.Remove(item);
		else inventory[item] = count;
	}
}
}