using System.Collections.Generic;

namespace AutoUI.Data {
public class DataContext {
	private readonly Dictionary<ulong, object> data = new();

	private readonly DataContext parent;

	public DataContext(DataContext parent) {
		this.parent = parent;
	}

	public void Set(string key, object value) {
		DataContext context = this;
		while (context.parent != null) context = context.parent;
		context.SetLocal(key, value);
	}

	public void SetLocal(string key, object value) {
		data[CalculateHash(key)] = value;
	}

	public object Get(string key, object defaultValue = null) {
		return GetByHash(CalculateHash(key), defaultValue);
	}

	private object GetByHash(ulong hash, object defaultValue) {
		if (data.ContainsKey(hash))
			return data[hash];
		if (parent != null)
			return parent.GetByHash(hash, defaultValue);
		return defaultValue;
	}

	// copied from https://stackoverflow.com/questions/9545619/a-fast-hash-function-for-string-in-c-sharp
	private static ulong CalculateHash(string read) {
		ulong hashedValue = 3074457345618258791ul;
		for (int i = 0; i < read.Length; i++) {
			hashedValue += read[i];
			hashedValue *= 3074457345618258799ul;
		}

		return hashedValue;
	}
}
}