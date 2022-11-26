using System;
using Managers;
using Mirror;
using ScriptableObjects.Communications;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Communications
{
	public abstract class SignalReceiver : NetworkBehaviour, IServerDespawn, IServerSpawn
	{
		[field: SerializeField] public SignalType SignalTypeToReceive { get; set; } = SignalType.PING;
		[field: SerializeField] public float Frequency { get; set; } = 122F;
		[field: SerializeField] public ISignalEmitter Emitter { get; set; }
		//How many seconds of delay before the SignalReceive logic happens for weak signals
		[field: SerializeField] public float DelayTime { get; set; } = 3f;
		[field: SerializeField] public int PassCode { get; set; }
		//For devices that are designed for spying and hacking
		[field: SerializeField] public bool ListenToEncryptedData { get; set; } = false;


		public virtual void OnSpawnServer(SpawnInfo info)
		{
			SignalsManager.Instance.Receivers.Add(this);
		}

		public void OnDespawnServer(DespawnInfo info)
		{
			RemoveSelfFromManager();
		}

		/// <summary>
		/// Sometimes OnDisable() gets overriden or doesn't get called properly when called using the Despawn class so manually call this in your extended script.
		/// Or when you need to remove this receiver from the manager for whatever reason.
		/// </summary>
		protected void RemoveSelfFromManager()
		{
			if(CustomNetworkManager.IsServer == false) return;
			SignalsManager.Instance.Receivers.Remove(this);
		}

		protected void RandomizeFreqAndCode()
		{
			Frequency = Random.Range(120.00f, 122.99f);
			PassCode = Random.Range(1,255);
		}

		/// <summary>
		/// Logic to do when
		/// </summary>
		public abstract void ReceiveSignal(SignalStrength strength, GameObject responsibleEmitter, ISignalMessage message = null);


		/// <summary>
		/// Optional. If ReceiveSignal logic has been successful we can respond to the emitter with some logic.
		/// </summary>
		public virtual void Respond(GameObject signalEmitter) { }

		private bool ValidSignal(SignalData data)
		{
			if(PassCode == 0) return true; //0 means that this explosive will accept any signal it passes through it even if it's not on the emitter list.
			return data.SecurityCode == PassCode;
		}
	}
}