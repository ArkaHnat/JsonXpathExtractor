using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

class Program
{
	private const string arrayWildcard = "[*]";
	private const string inputPath = "input.json";
	private const string xPathPath = "xpathlist.json";
	private const string outputPath = "output.json";
	private const bool ignoreEmptyJsonObjects = true;
	private const char leftSquareBracket = '[';
	private const char rightSquareBracket = ']';
	private const string curlyBracketPair = "{}";

	static void Main(string[] args)
	{
		string inputJson = File.ReadAllText(inputPath);
		string[] xpathList = JsonConvert.DeserializeObject<string[]>(File.ReadAllText(xPathPath));

		JObject inputObject = JObject.Parse(inputJson);

		JObject outputObject = new JObject();

		foreach (string xpath in xpathList)
		{
			AddFieldToOutput(inputObject, outputObject, xpath);
		}

		File.WriteAllText(outputPath, JsonConvert.SerializeObject(outputObject, Newtonsoft.Json.Formatting.Indented));
		Console.WriteLine($"Filtered JSON saved to {outputPath}");
	}

	static void AddFieldToOutput(JObject input, JObject output, string xpath)
	{
		string[] parts = xpath.Split('.');
		JToken currentInput = input;
		JObject currentOutput = output;

		for (int i = 0; i < parts.Length; i++)
		{
			string part = parts[i];
			bool isArrayWildcard = part.EndsWith(arrayWildcard);
			string arrayPart = null;

			if (isArrayWildcard)
			{
				part = part.Substring(0, part.Length - 3); 
			}
			else if (part.Contains(leftSquareBracket) && part.Contains(rightSquareBracket))
			{
				int startIndex = part.IndexOf(leftSquareBracket);
				arrayPart = part.Substring(startIndex).Trim(leftSquareBracket, rightSquareBracket);
				part = part.Substring(0, startIndex);
			}

			if (currentInput[part] != null)
			{
				if (currentInput[part] is JArray array)
				{
					if (isArrayWildcard)
					{
						JArray filteredArray = new JArray();
						foreach (JToken element in array)
						{
							if (element is JObject obj)
							{
								JObject filteredElement = new JObject();
								for (int j = i + 1; j < parts.Length; j++)
								{
									string subPart = parts[j];
									if (obj[subPart] != null)
									{
										filteredElement[subPart] = obj[subPart];
									}
								}
								filteredArray.Add(filteredElement);
							}
						}
						currentOutput[part] = filteredArray;
						break;
					}
					else if (arrayPart != null && int.TryParse(arrayPart, out int index) && index < array.Count)
					{
						if (i == parts.Length - 1)
						{
							currentOutput[part] = currentOutput[part] ?? new JArray();
							((JArray)currentOutput[part]).Add(array[index]);
						}
						else
						{
							currentInput = array[index];
							currentOutput[part] = currentOutput[part] ?? new JObject();
							currentOutput = (JObject)currentOutput[part];
						}
					}
				}
				else
				{
					if (i == parts.Length - 1)
					{
						if (ignoreEmptyJsonObjects && currentInput[part].ToString().Trim() == curlyBracketPair)
						{
							continue;
						}
						currentOutput[part] = currentInput[part];
					}
					else
					{
						currentInput = currentInput[part];
						currentOutput[part] = currentOutput[part] ?? new JObject();
						currentOutput = (JObject)currentOutput[part];
					}
				}
			}
		}
	}
}

