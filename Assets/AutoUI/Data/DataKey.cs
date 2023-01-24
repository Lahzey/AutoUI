using System;
using System.Collections.Generic;

public class DataKeyBase
{
    private static Dictionary<string, DataKeyBase> dataKeyRegistry = null;
    
    public readonly Type Type;
    public readonly string Key;
    
    internal DataKeyBase(Type type, string key)
    {
        Type = type;
        Key = key;
    }
    
    public static DataKeyBase Get(string key)
    {
        if (dataKeyRegistry == null) initRegistry();
        key = key.ToLower();
        return dataKeyRegistry.ContainsKey(key) ? dataKeyRegistry[key] : null;
    }

    private static void initRegistry()
    {
        dataKeyRegistry = new Dictionary<string, DataKeyBase>();
        
        // use reflection to get all subclasses of DataKey<T> and add all their static fields of type DataKey<T> to the registry
        Type dataKeyType = typeof(DataKeyBase);
        Type[] types = dataKeyType.Assembly.GetTypes();
        foreach (Type type in types)
        {
            if (type.GetCustomAttributes(typeof(DataKeyRegistryAttribute), true).Length == 0) continue;
            foreach (System.Reflection.FieldInfo field in type.GetFields())
            {
                if (!field.FieldType.IsSubclassOf(dataKeyType)) continue;
                
                DataKeyBase dataKey = (DataKeyBase) field.GetValue(null);
                dataKeyRegistry.Add(dataKey.Key.ToLower(), dataKey);
            }
        }
    }
}

public class DataKey<T> : DataKeyBase
{

    public readonly string Key;
    
    public DataKey(string key) : base (typeof(T), key)
    {
        Key = key;
    }
}

/// <summary>
/// Use this attribute on a class to mark it as a registry for DataKey subclasses.<br/>
/// This will cause all static fields (of type DataKey) in the class to be added to the DataKey registry.<br/>
/// Having data keys in the registry will enable the inspector to provide autocomplete options and code validation.
/// </summary>
public class DataKeyRegistryAttribute : Attribute
{
}