using AutoUI.Data;

namespace Example {
[DataKeyRegistry]
public class DataKeys {
	public static DataKey<Player> player = new("player");
	public static DataKey<bool> showInventory = new("showInventory");
}
}