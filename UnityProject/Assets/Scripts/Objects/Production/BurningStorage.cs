using System.Collections.Generic;
using HealthV2;
using UnityEngine;
using UnityEngine.Events;

namespace Objects.Production
{
	[RequireComponent(typeof(ObjectContainer))]
	public class BurningStorage : MonoBehaviour
	{
		public ObjectContainer Storage;
		public bool IsBurning { get; private set; } = false;
		private Dictionary<GameObject, BurningStorageData> storedObjects = new Dictionary<GameObject, BurningStorageData>();

		[SerializeField] private float burningDamage = 10;
		[SerializeField] private float secondsBeforeEachDamage = 1.25f;
		[SerializeField] private bool turnOffWhenAllContentsDestroyed = true;

		public UnityEvent OnTurnOff;

		private void Awake()
		{
			if (CustomNetworkManager.IsServer == false) return;
			Storage.OnObjectStored.AddListener(CheckStoredObject);
			Storage.OnObjectRetrieved.AddListener(RemoveStoredObject);
		}

		private void CheckStoredObject(GameObject obj)
		{
			BurningStorageData data = new BurningStorageData();
			if (obj.TryGetComponent<LivingHealthMasterBase>(out var creature))
			{
				data.creature = creature;
				storedObjects.Add(obj, data);
			}
			if (obj.TryGetComponent<Integrity>(out var item))
			{
				data.item = item;
				storedObjects.Add(obj, data);
			}
			ContentCountCheck();
		}

		private void RemoveStoredObject(GameObject obj)
		{
			storedObjects.Remove(obj);
			ContentCountCheck();
		}

		private void ContentCountCheck()
		{
			if (Storage.StoredObjectsCount == 0 && turnOffWhenAllContentsDestroyed)
			{
				TurnOff();
			}
		}

		private void BurnContent()
		{
			var objectsToBurn = new List<KeyValuePair<GameObject, BurningStorageData>>(storedObjects);
			foreach (var obj in objectsToBurn)
			{
				if (obj.Key == null) continue;
				obj.Value.creature.OrNull()?.ApplyDamageAll(gameObject, burningDamage, AttackType.Fire, DamageType.Burn, false, TraumaticDamageTypes.BURN);
				obj.Value.item.OrNull()?.ApplyDamage(burningDamage, AttackType.Fire, DamageType.Burn, true);
			}
		}

		public void TurnOn()
		{
			UpdateManager.Add(BurnContent, secondsBeforeEachDamage);
			IsBurning = true;
		}

		public void TurnOff()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, BurnContent);
			IsBurning = false;
			OnTurnOff?.Invoke();
		}

		struct BurningStorageData
		{
			public LivingHealthMasterBase creature;
			public Integrity item;
		}
	}
}
