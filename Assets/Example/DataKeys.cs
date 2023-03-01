using AutoUI.Data;

namespace Example {
[DataKeyRegistry]
public class DataKeys {
	public static DataKey<Player> Player = new("player");
	public static DataKey<bool> ShowInventory = new("showInventory");
}
}