using System;
using System.Collections.Generic;

[DataKeyRegistry]
public class DataKeys
{
    public static DataKey<Player> Player = new DataKey<Player>("player");
    public static DataKey<bool> ShowInventory = new DataKey<bool>("showInventory");
}