using System;
using System.Collections.Generic;
using System.Linq;
using TileMap.Behaviours;
using TileManagement;
using Tiles;
using UnityEngine;
using Logs;
using Random = UnityEngine.Random;
using Newtonsoft.Json;
using SecureStuff;
using Cysharp.Threading.Tasks;

namespace Systems.ProcGen
{
	/// <summary>
	/// A system that handles procedurally generating tilemaps on matrices on the fly.
	/// Chunks get automatically generated as players get closer to unloaded chunks, and loaded chunks can include unique blueprints.
	/// </summary>
	public class ProcGen : ItemMatrixSystemInit
	{
		[Header("Chunk Settings")]
		[SerializeField] private int chunkSize = 32;
		[SerializeField] private int loadDistance = 2; // How many chunks away to start loading

		[Header("Generation Settings")]
		[SerializeField] private bool enableProceduralGeneration = true;
		[SerializeField] private int seed = 0;
		[SerializeField] private float noiseScale = 50f;
		[SerializeField] private int octaves = 4;
		[SerializeField] private float persistence = 0.5f;
		[SerializeField] private float lacunarity = 2f;

		[Header("Tile Settings")]
		[SerializeField] private List<LayerTile> floorTile = new();
		[SerializeField] private LayerTile wallTile;
		[SerializeField] private LayerTile spaceTile;
		[SerializeField] private float wallThreshold = 0.6f;
		[SerializeField] private float floorThreshold = 0.3f;

		[Header("Blueprint Settings")] [SerializeField]
		private List<string> blueprintFiles = new List<string>();
		[SerializeField] private float blueprintChance = 0.1f;
		[SerializeField] private int minBlueprintDistance = 5;
		[SerializeField] private bool loadBlueprintsOnStart = true;

		[Header("Preservation Settings")]
		[SerializeField] private bool preserveExistingTiles = true;
		[SerializeField] private bool blueprintsCanOverrideExistingStuff = true;

		[Header("Debug")]
		[SerializeField] private bool showDebugInfo = false;
		[SerializeField] private bool showChunkBounds = false;

		// Internal state
		private Dictionary<Vector2Int, ProcGenChunk> loadedChunks = new Dictionary<Vector2Int, ProcGenChunk>();
		private HashSet<Vector2Int> generatingChunks = new HashSet<Vector2Int>();
		private System.Random random;
		private List<Vector2Int> blueprintPositions = new List<Vector2Int>();
		private List<ProcGenBlueprint> loadedBlueprints = new List<ProcGenBlueprint>();

		public override int Priority => 100; // High priority to ensure it initializes early

		private float offsetX;
		private float offsetY;

		public override void Initialize()
		{
			if (!enableProceduralGeneration) return;

			random = seed == 0 ? new System.Random() : new System.Random(seed);

			// Perlin noise offsets so terrain varies by seed
			offsetX = (float)random.NextDouble() * 10000f;
			offsetY = (float)random.NextDouble() * 10000f;

			tileChangeManager = GetComponentInParent<TileChangeManager>();
			metaTileMap = GetComponentInParent<MetaTileMap>();

			if (tileChangeManager == null || metaTileMap == null)
			{
				Loggy.Error($"[ProcGen] Missing required components on {gameObject.name}");
				return;
			}

			if (floorTile.Count == 0)
				floorTile.Add(TileManager.GetTile(TileType.Floor, "GrassFloor") as LayerTile);
			if (wallTile == null)
				wallTile = TileManager.GetTile(TileType.Wall, "iron_wall") as LayerTile;
			if (spaceTile == null)
				spaceTile = TileManager.GetTile(TileType.Effects, "ERROR") as LayerTile;

			Loggy.Info($"[ProcGen] Initialized with chunk size: {chunkSize}, load distance: {loadDistance}");

			if (loadBlueprintsOnStart)
			{
				_ = LoadBlueprintsFromFiles();
			}
		}

		private void Update()
		{
			if (enableProceduralGeneration == false|| CustomNetworkManager.IsServer == false) return;
			ManageChunksAroundPlayers();
		}


		private async UniTask LoadBlueprintsFromFiles()
		{
			if (blueprintFiles.Count == 0)
			{
				Loggy.Info("[ProcGen] No blueprint files specified");
				return;
			}

			foreach (var blueprintFile in blueprintFiles)
			{
				if (string.IsNullOrEmpty(blueprintFile)) continue;

				try
				{
					if (AccessFile.Exists(blueprintFile, true, FolderType.Maps, false))
					{
						string json = AccessFile.Load(blueprintFile, FolderType.Maps);

						MapSaver.MapSaver.MapData mapData = JsonConvert.DeserializeObject<MapSaver.MapSaver.MapData>(json);

						if (mapData != null && mapData.ContainedMatrices.Count > 0)
						{
							foreach (var matrixData in mapData.ContainedMatrices)
							{
								var blueprint = ConvertMapDataToBlueprint(matrixData, blueprintFile);
								if (blueprint != null)
								{
									loadedBlueprints.Add(blueprint);
									Loggy.Info($"[ProcGen] Loaded blueprint '{blueprint.name}' from {blueprintFile}");
								}
							}
						}
					}
					else
					{
						Loggy.Warning($"[ProcGen] Blueprint file not found: {blueprintFile}");
					}
				}
				catch (Exception e)
				{
					Loggy.Error($"[ProcGen] Error loading blueprint file {blueprintFile}: {e.Message}");
				}

				// Yield to prevent blocking
				await UniTask.Yield();
			}

			Loggy.Info($"[ProcGen] Loaded {loadedBlueprints.Count} blueprints total");
		}

		/// <summary>
		/// Converts MapData to ProcGenBlueprint format
		/// </summary>
		private ProcGenBlueprint ConvertMapDataToBlueprint(MapSaver.MapSaver.MatrixData matrixData, string fileName)
		{
			var blueprint = new ProcGenBlueprint
			{
				name = $"{fileName}_{matrixData.MatrixName ?? "Blueprint"}",
				tiles = new List<ProcGenTileData>()
			};

			if (matrixData.GitFriendlyTileMapData != null)
			{
				foreach (var xy in matrixData.GitFriendlyTileMapData.XYs)
				{
					var pos = MapSaver.MapSaver.GitFriendlyPositionToVectorInt(xy.Key);

					foreach (var tile in xy.Value)
					{
						var layerTile = TileManager.GetTile(tile.Tel);
						if (layerTile != null)
						{
							blueprint.tiles.Add(new ProcGenTileData
							{
								offset = new Vector2Int(pos.x, pos.y),
								tile = layerTile
							});
						}
					}
				}
			}

			return blueprint.tiles.Count > 0 ? blueprint : null;
		}

		/// <summary>
		/// Manages chunk loading based on player positions (chunks never unload)
		/// </summary>
		private void ManageChunksAroundPlayers()
		{
			var players = PlayerList.Instance.InGamePlayers;
			var chunksToLoad = new HashSet<Vector2Int>();

			// Find chunks that need to be loaded based on player positions
			foreach (var player in players)
			{
				if (player == null || player.Script == null) continue;

				var playerPos = player.Script.WorldPos;
				var playerChunk = WorldToChunk(playerPos);

				// Add chunks within load distance
				for (int x = -loadDistance; x <= loadDistance; x++)
				{
					for (int y = -loadDistance; y <= loadDistance; y++)
					{
						var chunkPos = playerChunk + new Vector2Int(x, y);
						chunksToLoad.Add(chunkPos);
					}
				}
			}

			foreach (var chunkPos in chunksToLoad)
			{
				if (!loadedChunks.ContainsKey(chunkPos) && !generatingChunks.Contains(chunkPos))
				{
					_ = GenerateChunk(chunkPos);
				}
			}
		}

		/// <summary>
		/// Generates a chunk at the specified position
		/// </summary>
		private async UniTask GenerateChunk(Vector2Int chunkPos)
		{
			generatingChunks.Add(chunkPos);

			var chunk = new ProcGenChunk(chunkPos, chunkSize);
			GenerateTerrain(chunk);
			CheckForBlueprintPlacement(chunk);
			await ApplyChunkToTilemap(chunk);

			// Register chunk as loaded
			loadedChunks[chunkPos] = chunk;
			generatingChunks.Remove(chunkPos);

			if (showDebugInfo)
			{
				Loggy.Info($"[ProcGen] Generated chunk at {chunkPos}");
			}
		}

		/// <summary>
		/// Generates terrain for a chunk using Perlin noise
		/// </summary>
		private void GenerateTerrain(ProcGenChunk chunk)
		{
			var noiseMap = GenerateNoiseMap(chunk.Position, chunk.Size);

			for (int x = 0; x < chunk.Size; x++)
			{
				for (int y = 0; y < chunk.Size; y++)
				{
					var worldPos = chunk.GetWorldPosition(x, y);
					var noiseValue = noiseMap[x, y];
					LayerTile tileToPlace = null;

					if (noiseValue > wallThreshold)
					{
						tileToPlace = wallTile;
					}
					else if (noiseValue > floorThreshold)
					{
						tileToPlace = floorTile.PickRandom();
					}
					else
					{
						tileToPlace = spaceTile;
					}

					chunk.SetTile(x, y, tileToPlace);
				}
			}
		}

		/// <summary>
		/// Generates a noise map for terrain generation
		/// </summary>
		private float[,] GenerateNoiseMap(Vector2Int chunkPos, int size)
		{
			var noiseMap = new float[size, size];
			var maxNoiseHeight = float.MinValue;
			var minNoiseHeight = float.MaxValue;

			// Generate noise values
			for (int x = 0; x < size; x++)
			{
				for (int y = 0; y < size; y++)
				{
					var worldX = (chunkPos.x * size + x) / noiseScale + offsetX;
					var worldY = (chunkPos.y * size + y) / noiseScale + offsetY;

					var amplitude = 1f;
					var frequency = 1f;
					var noiseHeight = 0f;

					// Generate octaves
					for (int i = 0; i < octaves; i++)
					{
						var sampleX = worldX * frequency;
						var sampleY = worldY * frequency;

						var perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
						noiseHeight += perlinValue * amplitude;

						amplitude *= persistence;
						frequency *= lacunarity;
					}

					noiseMap[x, y] = noiseHeight;

					if (noiseHeight > maxNoiseHeight)
						maxNoiseHeight = noiseHeight;
					if (noiseHeight < minNoiseHeight)
						minNoiseHeight = noiseHeight;
				}
			}

			// Normalize noise values
			for (int x = 0; x < size; x++)
			{
				for (int y = 0; y < size; y++)
				{
					noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
				}
			}

			return noiseMap;
		}

		/// <summary>
		/// Checks if a blueprint should be placed in this chunk
		/// </summary>
		private void CheckForBlueprintPlacement(ProcGenChunk chunk)
		{
			if (loadedBlueprints.Count == 0 || random.NextDouble() > blueprintChance) return;

			var blueprint = loadedBlueprints.PickRandom();
			if (blueprint == null) return;

			// Check if we're far enough from other blueprints
			var chunkWorldPos = chunk.GetWorldPosition(chunk.Size / 2, chunk.Size / 2);
			var chunkPos2D = new Vector2Int(Mathf.FloorToInt(chunkWorldPos.x), Mathf.FloorToInt(chunkWorldPos.y));

			foreach (var existingPos in blueprintPositions)
			{
				if (Vector2Int.Distance(chunkPos2D, existingPos) < minBlueprintDistance)
				{
					return; // Too close to existing blueprint
				}
			}

			PlaceBlueprint(chunk, blueprint);
			blueprintPositions.Add(chunkPos2D);
		}

		/// <summary>
		/// Places a blueprint within a chunk, checking for existing tiles
		/// </summary>
		private void PlaceBlueprint(ProcGenChunk chunk, ProcGenBlueprint blueprint)
		{
			if (blueprint.tiles == null || blueprint.tiles.Count == 0) return;

			// Find center of chunk
			var centerX = chunk.Size / 2;
			var centerY = chunk.Size / 2;

			// Check if blueprint can fit without overwriting existing tiles (if preservation is enabled)
			bool canPlaceBlueprint = true;
			foreach (var tileData in blueprint.tiles)
			{
				var x = centerX + tileData.offset.x;
				var y = centerY + tileData.offset.y;

				if (x >= 0 && x < chunk.Size && y >= 0 && y < chunk.Size)
				{
					var worldPos = chunk.GetWorldPosition(x, y);
					var localPos = metaTileMap.WorldToCell(worldPos);

					// If there's already a tile here, we can't place this blueprint
					if (blueprintsCanOverrideExistingStuff == false && metaTileMap.HasTile(localPos))
					{
						canPlaceBlueprint = false;
						break;
					}
				}
			}

			if (canPlaceBlueprint)
			{
				foreach (var tileData in blueprint.tiles)
				{
					var x = centerX + tileData.offset.x;
					var y = centerY + tileData.offset.y;

					if (x >= 0 && x < chunk.Size && y >= 0 && y < chunk.Size)
					{
						chunk.SetTile(x, y, tileData.tile);
					}
				}

				if (showDebugInfo)
				{
					Loggy.Info($"[ProcGen] Placed blueprint '{blueprint.name}' in chunk at {chunk.Position}");
				}
			}
			else if (showDebugInfo)
			{
				Loggy.Info($"[ProcGen] Skipped blueprint '{blueprint.name}' - conflicts with existing tiles");
			}
		}

		/// <summary>
		/// Applies a chunk's tiles to the tilemap, preserving existing tiles
		/// </summary>
		private async UniTask ApplyChunkToTilemap(ProcGenChunk chunk)
		{
			for (int x = 0; x < chunk.Size; x++)
			{
				for (int y = 0; y < chunk.Size; y++)
				{
					var tile = chunk.GetTile(x, y);
					if (tile != null)
					{
						var worldPos = chunk.GetWorldPosition(x, y);
						var localPos = metaTileMap.WorldToCell(worldPos);

						// Only set tile if there's no existing tile at this position (if preservation is enabled)
						if (!preserveExistingTiles || !metaTileMap.HasTile(localPos))
						{
							metaTileMap.SetTile(localPos, tile, MapSaveRecord: true);
						}
						else if (showDebugInfo)
						{
							// Log when we skip existing tiles (only in debug mode to avoid spam)
							Loggy.Info($"[ProcGen] Skipping tile at {localPos} - existing tile found");
						}
					}
				}

				// Yield every few tiles to prevent frame drops
				if (x % 8 == 0)
				{
					await UniTask.Yield();
				}
			}
		}

		/// <summary>
		/// Unloads a chunk and removes its tiles
		/// </summary>
		private void UnloadChunk(Vector2Int chunkPos)
		{
			if (loadedChunks.TryGetValue(chunkPos, out var chunk) == false) return;

			for (int x = 0; x < chunk.Size; x++)
			{
				for (int y = 0; y < chunk.Size; y++)
				{
					var worldPos = chunk.GetWorldPosition(x, y);
					var localPos = metaTileMap.WorldToCell(worldPos);

					// Clear the tile
					metaTileMap.RemoveTile(localPos);
				}
			}

			loadedChunks.Remove(chunkPos);

			if (showDebugInfo)
			{
				Loggy.Info($"[ProcGen] Unloaded chunk at {chunkPos}");
			}
		}

		/// <summary>
		/// Converts world position to chunk coordinates
		/// </summary>
		private Vector2Int WorldToChunk(Vector3 worldPos)
		{
			return new Vector2Int(
				Mathf.FloorToInt(worldPos.x / chunkSize),
				Mathf.FloorToInt(worldPos.y / chunkSize)
			);
		}

		/// <summary>
		/// Forces generation of a specific chunk (useful for testing)
		/// </summary>
		[ContextMenu("Generate Test Chunk")]
		public void GenerateTestChunk()
		{
			if (!enableProceduralGeneration) return;

			var testChunk = new Vector2Int(0, 0);
			if (loadedChunks.ContainsKey(testChunk) == false && generatingChunks.Contains(testChunk) == false)
			{
				_ = GenerateChunk(testChunk);
			}
		}

		/// <summary>
		/// Clears all loaded chunks (useful for testing)
		/// </summary>
		[ContextMenu("Clear All Chunks")]
		public void ClearAllChunks()
		{
			foreach (var chunkPos in loadedChunks.Keys.ToList())
			{
				UnloadChunk(chunkPos);
			}
			blueprintPositions.Clear();
		}

		/// <summary>
		/// Toggles tile preservation mode (useful for testing)
		/// </summary>
		[ContextMenu("Toggle Tile Preservation")]
		public void ToggleTilePreservation()
		{
			preserveExistingTiles = !preserveExistingTiles;
			Loggy.Info($"[ProcGen] Tile preservation {(preserveExistingTiles ? "enabled" : "disabled")}");
		}

#if UNITY_EDITOR
		private void OnDrawGizmos()
		{
			if (!showChunkBounds) return;

			Gizmos.color = Color.yellow;

			foreach (var chunk in loadedChunks.Values)
			{
				var worldPos = chunk.GetWorldPosition(0, 0);
				var size = chunk.Size;

				Gizmos.DrawWireCube(
					worldPos + new Vector3(size / 2f, size / 2f, 0),
					new Vector3(size, size, 1)
				);
			}

			// Draw blueprint positions
			Gizmos.color = Color.red;
			foreach (var blueprintPos in blueprintPositions)
			{
				Gizmos.DrawWireSphere(new Vector3(blueprintPos.x, blueprintPos.y, 0), 2f);
			}
		}
#endif
	}

	/// <summary>
	/// Represents a chunk of procedurally generated terrain
	/// </summary>
	[Serializable]
	public class ProcGenChunk
	{
		public Vector2Int Position { get; private set; }
		public int Size { get; private set; }
		private LayerTile[,] tiles;

		public ProcGenChunk(Vector2Int position, int size)
		{
			Position = position;
			Size = size;
			tiles = new LayerTile[size, size];
		}

		public void SetTile(int x, int y, LayerTile tile)
		{
			if (x >= 0 && x < Size && y >= 0 && y < Size)
			{
				tiles[x, y] = tile;
			}
		}

		public LayerTile GetTile(int x, int y)
		{
			if (x >= 0 && x < Size && y >= 0 && y < Size)
			{
				return tiles[x, y];
			}
			return null;
		}

		public Vector3 GetWorldPosition(int x, int y)
		{
			return new Vector3(
				Position.x * Size + x,
				Position.y * Size + y,
				0
			);
		}
	}

	/// <summary>
	/// Represents a blueprint that can be placed in procedurally generated terrain
	/// </summary>
	[Serializable]
	public class ProcGenBlueprint
	{
		public string name;
		public List<ProcGenTileData> tiles = new List<ProcGenTileData>();
		public float weight = 1f; // Relative chance of this blueprint being selected
	}

	/// <summary>
	/// Represents a single tile in a blueprint
	/// </summary>
	[Serializable]
	public class ProcGenTileData
	{
		public Vector2Int offset; // Offset from blueprint center
		public LayerTile tile;
	}
}