using System;

public interface IJson<T>
{
	public string ToJson();
	public static T? FromJson(string json) => throw new NotImplementedException();
}
