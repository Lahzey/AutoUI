using AutoUI.Data;

namespace Example {
[DataKeyRegistry]
public class DataKeys {
	public static DataKey<Player> player = new DataKey<Player>("player");
	public static DataKey<bool> showInventory = new DataKey<bool>("showInventory");
}
}