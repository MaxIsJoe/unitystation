using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Core.Identity;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;

namespace Core.Editor.Tools
{
	/// <summary>
	/// This tool is a single use tool that will migrate names and descriptions from the Attributes component to the new Identity component.
	/// </summary>
	public class MigrateToIdentity: ScriptableWizard
	{

		[MenuItem("Tools/Migrations/Migrate to Identity")]
		private static void CreateWizard()
		{
			DisplayWizard<MigrateToIdentity>("Migrate to Identity", "Migrate");
		}

		/// <summary>
		/// Reads all game objects in the Assets folder and returns them as an array.
		/// </summary>
		/// <returns></returns>
		private GameObject[] AllGameObjects()
		{
			string[] guids = AssetDatabase.FindAssets("t:GameObject");
			GameObject[] gameObjects = new GameObject[guids.Length];
			for (int i = 0; i < guids.Length; i++)
			{
				string path = AssetDatabase.GUIDToAssetPath(guids[i]);
				gameObjects[i] = AssetDatabase.LoadAssetAtPath<GameObject>(path);
			}

			return gameObjects;
		}

		private IEnumerator MigrateNameAndDescription(GameObject[] gameObjects)
		{
			if (Application.isPlaying == false)
			{
				Logger.LogError("This must be called from within playmode..");
				yield break;
			}
			Logger.Log($"Starting migration process.. Editing {gameObjects.Length} assets.");
			var totalTime = new Stopwatch();
			totalTime.Start();
			foreach (var go in gameObjects)
			{
				yield return WaitFor.EndOfFrame;
				var entityIdentity = go.GetComponent<SimpleIdentity>();
				var attributes = go.GetComponent<global::Attributes>();

				if (entityIdentity == null || attributes == null) continue;
				entityIdentity.SetDisplayName(string.Empty, attributes.ArticleName);
				entityIdentity.SetDescription(string.Empty ,BuildDescription(attributes.InitialDescription));
				EditorUtility.SetDirty(go);
				Logger.Log($"Edited {go.name} successfully.");
			}
			Logger.Log($"Finished editing batch after {totalTime.Elapsed.Seconds} seconds.. Saving..");
			totalTime.Stop();
			AssetDatabase.SaveAssets();
		}

		private string BuildDescription(string description)
		{
			if (string.IsNullOrEmpty(description))
			{

				var article = "a";
				if (description!.StartsWith("a", true, CultureInfo.InvariantCulture)
				    || description.StartsWith("e", true, CultureInfo.InvariantCulture)
				    || description.StartsWith("i", true, CultureInfo.InvariantCulture)
				    || description.StartsWith("o", true, CultureInfo.InvariantCulture)
				    || description.StartsWith("u", true, CultureInfo.InvariantCulture))
				{
					article = "an";
				}
				return "This is "+ article + " {0}.";
			}

			return description;
		}

		private void OnWizardCreate()
		{
			var allObjs = AllGameObjects();
			var listOne = allObjs.Take(allObjs.Length / 2);
			var listTwo = allObjs.Take(allObjs.Length);
			GameManager.Instance.StartCoroutine(MigrateNameAndDescription(listOne.ToArray()));
			GameManager.Instance.StartCoroutine(MigrateNameAndDescription(listTwo.ToArray()));
		}
	}
}