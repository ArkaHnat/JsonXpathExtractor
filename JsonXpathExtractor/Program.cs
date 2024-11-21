using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

class Program
{
	private const string ArrayWildcard = "[*]";
	private const string InputPath = "input.json";
	private const string XPathPath = "xpathlist.json";
	private const string OutputPath = "output.json";
	private const bool IgnoreEmptyJsonObjects = true;

	static void Main(string[] args)
	{
		string inputJson = File.ReadAllText(InputPath);
		string[] xpathList = JsonConvert.DeserializeObject<string[]>(File.ReadAllText(XPathPath));
		JObject inputObject = JObject.Parse(inputJson);
		JObject outputObject = new JObject();

		foreach (string xpath in xpathList)
		{
			AddFieldToOutput(inputObject, outputObject, xpath);
		}

		File.WriteAllText(OutputPath, JsonConvert.SerializeObject(outputObject, Formatting.Indented));
		Console.WriteLine($"Filtered JSON saved to {OutputPath}");
	}

	static void AddFieldToOutput(JToken input, JToken output, string xpath)
	{
		string[] parts = xpath.Split('.');
		JToken currentInput = input;
		JToken currentOutput = output;

		for (int i = 0; i < parts.Length; i++)
		{
			string part = parts[i];
			bool isArrayWildcard = part.EndsWith(ArrayWildcard);
			string arrayIndex = null;

			if (isArrayWildcard)
			{
				part = part.Substring(0, part.Length - ArrayWildcard.Length);
			}
			else if (part.Contains("[") && part.Contains("]"))
			{
				int bracketIndex = part.IndexOf('[');
				arrayIndex = part.Substring(bracketIndex + 1, part.Length - bracketIndex - 2);
				part = part.Substring(0, bracketIndex);
			}

			if (currentInput[part] == null)
				break;

			if (currentInput[part] is JArray array)
			{
				ProcessArray(currentInput, currentOutput, parts, i, part, array, isArrayWildcard, arrayIndex);
				break; 
			}
			else
			{
				ProcessObject(currentInput, currentOutput, parts, i, part);
			}
		}
	}

	static void ProcessArray(JToken currentInput, JToken currentOutput, string[] parts, int index, string part, JArray array, bool isArrayWildcard, string arrayIndex)
	{
		if (isArrayWildcard)
		{
			JArray filteredArray = new JArray();

			foreach (JToken element in array)
			{
				if (element is JObject obj)
				{
					JObject filteredElement = new JObject();
					string subXPath = string.Join('.', parts[(index + 1)..]);
					AddFieldToOutput(obj, filteredElement, subXPath);

					if (filteredElement.HasValues)
					{
						filteredArray.Add(filteredElement);
					}
				}
			}

			if (filteredArray.Count > 0)
			{
				((JObject)currentOutput)[part] = filteredArray;
			}
		}
		else if (int.TryParse(arrayIndex, out int arrayPos) && arrayPos < array.Count)
		{
			JToken arrayElement = array[arrayPos];
			if (index == parts.Length - 1)
			{
				((JObject)currentOutput)[part] = currentOutput[part] ?? new JArray();
				((JArray)currentOutput[part]).Add(arrayElement);
			}
			else
			{
				currentInput = arrayElement;
				((JObject)currentOutput)[part] = currentOutput[part] ?? new JObject();
				currentOutput = currentOutput[part];
			}
		}
	}

	static void ProcessObject(JToken currentInput, JToken currentOutput, string[] parts, int index, string part)
	{
		if (index == parts.Length - 1)
		{
			JToken value = currentInput[part];

			if (IgnoreEmptyJsonObjects && IsEmptyJson(value))
				return;

			((JObject)currentOutput)[part] = value;
		}
		else
		{
			currentInput = currentInput[part];
			((JObject)currentOutput)[part] = currentOutput[part] ?? new JObject();
			currentOutput = currentOutput[part];
		}
	}

	static bool IsEmptyJson(JToken token)
	{
		return token == null || token.Type == JTokenType.Object && !((JObject)token).HasValues ||
			   string.IsNullOrWhiteSpace(token.ToString());
	}
}