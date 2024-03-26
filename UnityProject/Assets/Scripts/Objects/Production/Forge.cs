using Mirror;
using Systems.Explosions;
using UnityEngine;

namespace Objects.Production
{
	[RequireComponent(typeof(BurningStorage))]
	[RequireComponent(typeof(FireSource))]
	public class Forge : NetworkBehaviour, ICheckedInteractable<HandApply>
	{
		[SerializeField] private SpriteHandler forgeSpriteHandler;
		[SerializeField] private SpriteDataSO forgeEmpty;
		[SerializeField] private SpriteDataSO forgeReady;
		[SerializeField] private SpriteDataSO forgeRunning;
		[SerializeField] private ParticleSystem forgeSmoke;

		private BurningStorage burningStorage;
		[SyncVar(hook = nameof(UpdateSmokeEffect))] private bool smokeActive = false;

		private void Awake()
		{
			burningStorage = GetComponent<BurningStorage>();
			if (CustomNetworkManager.IsServer == false) return;
			SparkUtil.TrySpark(gameObject);
			burningStorage.Storage.OnObjectRetrieved.AddListener(UpdateVisuals);
			burningStorage.Storage.OnObjectStored.AddListener(UpdateVisuals);
		}


		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (burningStorage.IsBurning) return false;
			return DefaultWillInteract.Default(interaction, side);
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (interaction.IsAltClick)
			{
				ActivateForge();
			}
			if (interaction.HandObject != null)
			{
				burningStorage.Storage.StoreObject(interaction.HandObject);
			}
		}

		private void ActivateForge()
		{
			if (burningStorage.IsBurning)
			{
				burningStorage.TurnOff();
				burningStorage.Storage.RetrieveObjects();
				UpdateVisuals(null);
			}
			else
			{
				burningStorage.Storage.GatherObjects();
				burningStorage.TurnOn();
				UpdateVisuals(null);
			}
		}

		private void UpdateVisuals(GameObject obj)
		{
			if (burningStorage.IsBurning)
			{
				forgeSpriteHandler.SetSpriteSO(forgeRunning);
			}
			else
			{
				forgeSpriteHandler.SetSpriteSO(burningStorage.Storage.StoredObjectsCount == 0 ? forgeEmpty : forgeReady);
			}
			smokeActive = burningStorage.IsBurning;
		}

		private void UpdateSmokeEffect(bool oldState, bool newState)
		{
			forgeSmoke.gameObject.SetActive(newState);
		}
	}
}