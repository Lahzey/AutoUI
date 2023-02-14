using System;
using System.Collections.Generic;

public class DataContext
{
    private Dictionary<UInt64, object> data = new Dictionary<UInt64, object>();

    private DataContext parent;
    
    public DataContext(DataContext parent)
    {
        this.parent = parent;
    }
    
    public void Set(string key, object value)
    {
        DataContext context = this;
        while (context.parent != null) context = context.parent;
        context.SetLocal(key, value);
    }
    
    public void SetLocal(string key, object value)
    {
        data[CalculateHash(key)] = value;
    }
    
    public object Get(string key, object defaultValue = null)
    {
        return GetByHash(CalculateHash(key), defaultValue);
    }
    
    private object GetByHash(UInt64 hash, object defaultValue)
    {
        if (data.ContainsKey(hash))
        {
            return data[hash];
        }
        else if (parent != null)
        {
            return parent.GetByHash(hash, defaultValue);
        }
        else
        {
            return defaultValue;
        }
    }
    
    // copied from https://stackoverflow.com/questions/9545619/a-fast-hash-function-for-string-in-c-sharp
    static UInt64 CalculateHash(string read)
    {
        UInt64 hashedValue = 3074457345618258791ul;
        for(int i=0; i<read.Length; i++)
        {
            hashedValue += read[i];
            hashedValue *= 3074457345618258799ul;
        }
        return hashedValue;
    }
}