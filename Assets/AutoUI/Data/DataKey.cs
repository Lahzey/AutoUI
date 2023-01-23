using System;
using System.Collections.Generic;

public class DataKeyBase
{
    public readonly Type Type;
    public readonly string Key;
    
    internal DataKeyBase(Type type, string key)
    {
        Type = type;
        Key = key;
    }
}

public class DataKey<T> : DataKeyBase
{
    private static Dictionary<string, DataKeyBase> dataKeyRegistry = null;

    public readonly string Key;
    
    public DataKey(string key) : base (typeof(T), key)
    {
        Key = key;
    }
    
    public static DataKey<T> Get(string key)
    {
        if (dataKeyRegistry == null) initRegistry();
        if (dataKeyRegistry.ContainsKey(key))
        {
            return (DataKey<T>)dataKeyRegistry[key];
        }

        return null;
    }

    private static void initRegistry()
    {
        dataKeyRegistry = new Dictionary<string, DataKeyBase>();
        
        // use reflection to get all subclasses of DataKey<T> and add all their static fields of type DataKey<T> to the registry
        Type dataKeyType = typeof(DataKey<>);
        Type[] types = dataKeyType.Assembly.GetTypes();
        foreach (Type type in types)
        {
            if (!type.IsSubclassOf(dataKeyType)) continue;
            foreach (System.Reflection.FieldInfo field in type.GetFields())
            {
                if (!field.FieldType.IsSubclassOf(dataKeyType)) continue;
                
                DataKeyBase dataKey = (DataKeyBase) field.GetValue(null);
                dataKeyRegistry.Add(dataKey.Key, dataKey);
            }
        }
    }
}