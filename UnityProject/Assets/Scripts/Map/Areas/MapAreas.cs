using System.Collections.Generic;
using System.IO;
using NaughtyAttributes;
using Newtonsoft.Json;
using Shared.Managers;
using UnityEditor;
using UnityEngine;

namespace Map.Areas
{
	public class MapAreas : MonoBehaviour
	{
		[field: SerializeField] public string MapAreaDataID { get; private set; }
		public Dictionary<Vector3Int, Area> Areas { get; private set; } = new Dictionary<Vector3Int, Area>();

		[SerializeField] private GameObject visualizer;

		public void Awake()
		{
			if (MapAreaDataID is null or "")
			{
				Logger.LogWarning("[MapAreas] - No ID set.");
				return;
			}
		}

		public void CreateNewArea(Vector3Int location, string ID, string name, List<string> tags = null, Color? newColor = null)
		{
			if (Areas.ContainsKey(location))
			{
				Logger.LogWarning($"[MapAreas] - {location} already has defined Area. Skipping.");
				return;
			}
			Areas.Add(location, new Area(ID, name, tags, newColor));
		}

		public void RemoveArea(Vector3Int tile)
		{
			if (Areas.ContainsKey(tile) == false) return;
			Areas.Remove(tile);
		}

		public void EditTags(Vector3Int tile, List<string> newTags)
		{
			if (Areas.ContainsKey(tile) == false) return;
			Areas[tile].Tags = newTags;
		}

		[Button("Save Current Areas")]
		public void SaveCurrentAreaData()
		{
			if (Application.isPlaying == false) return;
			var data = JsonConvert.SerializeObject(Areas);
			File.WriteAllText(Path.Combine(Application.streamingAssetsPath, "MapData", $"{MapAreaDataID}.json"), data);
		}

		[Button("Load Areas From File")]
		public void LoadCurrentAreaData()
		{
			if (Application.isPlaying == false) return;
			Areas = JsonUtility.FromJson<Dictionary<Vector3Int, Area>>(File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "MapData", $"{MapAreaDataID}.json")));
		}

		[Button("[DEBUG] Set Test Data Under Player")]
		public void SetDataUnderPlayer()
		{
			CreateNewArea(PlayerManager.LocalPlayerObject.RegisterTile().LocalPosition, "test", "Test Zone");
		}

		[Button("[DEBUG] Set Maint Area Data Under Player")]
		public void SetMaintAreaUnderPlayer()
		{
			CreateNewArea(PlayerManager.LocalPlayerObject.RegisterTile().LocalPosition, "maint", "Maintainece",
				new List<string>()
			{
				"Amb_Maint",
				"Amb_Station",
				"Spawn_Maint"
			});
		}


		[Button("Draw Data")]
		public void DrawData()
		{
			foreach (var oVisual in FindObjectsOfType<AreaVisual>())
			{
				Despawn.ClientSingle(oVisual.gameObject);
			}
			foreach (var area in Areas)
			{
				var result = Spawn.ClientPrefab(visualizer, area.Key);
				result.GameObject.GetComponent<AreaVisual>()?.Setup(area.Value.ID, area.Value.VisualColor);
			}
		}
	}
}