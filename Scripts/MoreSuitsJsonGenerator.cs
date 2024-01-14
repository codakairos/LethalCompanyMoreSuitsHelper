using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Needs the package "com.unity.nuget.newtonsoft-json"
using Newtonsoft.Json;

namespace MoreSuitsHelper
{
	public class MoreSuitsJsonGeneratorEditorWindow : EditorWindow
	{
		private Material material = null;
		private TextAsset textAsset = null;

		private string skinName = "Skin";
		private int price = 60;

		[MenuItem("Window/More Suits Json exporter")]
		public static void ShowExample()
		{
			MoreSuitsJsonGeneratorEditorWindow wnd = GetWindow<MoreSuitsJsonGeneratorEditorWindow>();
			wnd.titleContent = new GUIContent("More Suits Json exporter");
		}

		private void OnGUI()
		{
			GUILayout.Label(new GUIContent("Material to export", "Needs to be using the default HDRP/Lit shader."));
			material = (Material)EditorGUILayout.ObjectField(material, typeof(Material), false);

			GUILayout.Label(new GUIContent("Base Material Json",
				"Json generated from the default material, used to diff and avoid exporting default values."));
			textAsset = (TextAsset)EditorGUILayout.ObjectField(textAsset, typeof(TextAsset), false);

			GUILayout.Label(new GUIContent("Name", "The name of the generated skin"));
			skinName = EditorGUILayout.TextField(skinName);

			GUILayout.Label(new GUIContent("Price", "The price of the suit in-game"));
			price = EditorGUILayout.IntField(price);

			GUI.enabled = false;
			if (material)
			{
				if (material.shader.name != "HDRP/Lit")
				{
					EditorGUILayout.HelpBox("The selected material needs to use the default HDRP/Lit shader",
						MessageType.Error);
				}
				else
				{
					GUI.enabled = true;
					;
				}
			}

			if (GUILayout.Button(new GUIContent("Export to folder")))
			{
				string exportPath = EditorUtility.OpenFolderPanel("Save to folder", "Assets/", "Export");
				ExportMaterialDataToPath(exportPath);
			}

			GUI.enabled = true;
		}

		private void ExportMaterialDataToPath(string path)
		{
			Dictionary<string, string> dictionary = new();
			Dictionary<string, string> diff = new();

			if (textAsset)
				diff = JsonConvert.DeserializeObject<Dictionary<string, string>>(textAsset.text);

			// Price
			dictionary["PRICE"] = price.ToString();

			// Enabled Shader Keywords
			foreach (string keyword in material.shaderKeywords)
			{
				if (!diff.ContainsKey(keyword) ||
				    (diff.ContainsKey(keyword) && diff[keyword] == "DISABLEKEYWORD"))
				{
					dictionary[keyword] = "KEYWORD";
				}
			}

			// Enabled Shader Passes
			for (int i = 0; i < material.passCount; i++)
			{
				string shaderPass = material.GetPassName(i);
				string value = material.GetShaderPassEnabled(shaderPass) ? "SHADERPASS" : "DISABLESHADERPASS";

				if (!diff.ContainsKey(shaderPass) ||
				    (diff.ContainsKey(shaderPass) && diff[shaderPass] != value))
				{
					dictionary[shaderPass] = value;
				}
			}

			// Material Parameters
			for (int i = 0; i < material.shader.GetPropertyCount(); i++)
			{
				string propertyName = material.shader.GetPropertyName(i);
				string value = material.shader.GetPropertyType(material.shader.FindPropertyIndex(propertyName)) switch
				{
					ShaderPropertyType.Color => material.HasColor(propertyName)
						? material.GetVector(propertyName).ToString().Replace("(", "").Replace(")", "")
						: null,
					ShaderPropertyType.Vector => material.HasVector(propertyName)
						? material.GetVector(propertyName).ToString().Replace("(", "").Replace(")", "")
						: null,
					ShaderPropertyType.Float => material.HasFloat(propertyName)
						? material.GetFloat(propertyName).ToString(CultureInfo.InvariantCulture)
						: null,
					ShaderPropertyType.Range => material.HasFloat(propertyName)
						? material.GetFloat(propertyName).ToString(CultureInfo.InvariantCulture)
						: null,
					ShaderPropertyType.Texture => material.HasTexture(propertyName)
						? material.GetTexture(propertyName)?.name
						: null,
					ShaderPropertyType.Int => material.HasInt(propertyName)
						? material.GetInt(propertyName).ToString()
						: null,
					_ => throw new ArgumentOutOfRangeException()
				};

				if (value == null)
					continue;

				if (!diff.ContainsKey(propertyName) ||
				    (diff.ContainsKey(propertyName) && diff[propertyName] != value))
				{
					dictionary[propertyName] = value;
				}
			}

			File.WriteAllText($"{path}/{skinName}.json", JsonConvert.SerializeObject(dictionary, Formatting.Indented));
		}
	}
}
