using System;
using System.Text.Json;
using Microsoft.CodeAnalysis;

class _
{
	static void Main()
	{
		A a = new();

		JsonSerializerOptions options = new()
		{
			TypeInfoResolver = SourceGenerationContext.Default,
		};

		Console.WriteLine("jsonString: " + JsonSerializer.Serialize(a, options));
	}
}

class A
{
	public string name { get; set; } = "name A";
	public A() { }
}
