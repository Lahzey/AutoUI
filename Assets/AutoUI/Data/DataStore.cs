namespace AutoUI.Data {
public class DataStore : DataContext {
	public static readonly DataStore Instance = new();

	private DataStore() : base(null) { }

	public void Set<T>(DataKey<T> key, T value) {
		SetLocal(key.Key, value);
	}

	public T Get<T>(DataKey<T> key) {
		return (T)base.Get(key.Key, default(T));
	}
}
}