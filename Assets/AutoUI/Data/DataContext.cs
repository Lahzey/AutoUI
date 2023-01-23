using System.Collections.Generic;

public class DataContext
{
    private Dictionary<string, object> data = new Dictionary<string, object>();

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
        data[key] = value;
    }
    
    public object Get(string key)
    {
        if (data.ContainsKey(key))
        {
            return data[key];
        }
        else if (parent != null)
        {
            return parent.Get(key);
        }
        else
        {
            return null;
        }
    }
}