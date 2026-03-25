namespace AutoUI.Data {
public class DataStore : DataContext {
	public static readonly DataStore INSTANCE = new DataStore();

	private DataStore() : base(null) { }

	public void Set<T>(DataKey<T> key, T value) {
		SetLocal(key.key, value);
	}

	public T Get<T>(DataKey<T> key) {
		return (T)base.Get(key.key, default(T));
	}
}
}