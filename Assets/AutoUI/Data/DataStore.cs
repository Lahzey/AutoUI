using System.Collections.Generic;

public class DataStore : DataContext
{
    public static readonly DataStore Instance = new DataStore();
    
    private DataStore() : base(null)
    {
    }

    public void Set<T>(DataKey<T> key, T value)
    {
        base.SetLocal(key.Key, value);
    }
    
    public T Get<T>(DataKey<T> key)
    {
        return (T) base.Get(key.Key);
    }
}