using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using Util;

/// <summary>
/// A document in our dev spawner search (lives in our Trie nodes), representing something spawnable. Currently only supports prefabs
/// but could be extended to support other capabilities.
/// </summary>
public struct DevSpawnerDocument
{
	/// <summary>
	/// Prefab this document represents.
	/// </summary>
	public readonly GameObject Prefab;
	/// <summary>
	/// Name cleaned up for searchability (like lowercase).
	/// </summary>
	public readonly string SearchableName;

	public readonly bool CanBeSpanwedInSandboxMode;

	private DevSpawnerDocument(GameObject prefab)
	{
		Prefab = prefab;
		SearchableName = SpawnerSearch.Standardize(prefab.name);
		var tracker = prefab.GetComponent<PrefabTracker>();
		CanBeSpanwedInSandboxMode = tracker == null ? true : tracker.SpawnableInSandboxMode;
	}

	/// <summary>
	/// Create a dev spawner document representing this prefab.
	/// </summary>
	/// <param name="prefab"></param>
	public static DevSpawnerDocument ForPrefab(GameObject prefab)
	{
		return new DevSpawnerDocument(prefab);
	}
}

