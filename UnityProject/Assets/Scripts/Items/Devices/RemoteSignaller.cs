using Communications;
using Managers;
using Messages.Server;
using Objects;
using UnityEngine;

namespace Items.Devices
{
	public class RemoteSignaller : MonoBehaviour, IInteractable<HandActivate>, ITrapComponent, ISignalEmitter
	{
		[SerializeField] private SignalData signalData;

		private Pickupable pickupable;
		private ISignalEmitter emitter;

		private void Awake()
		{
			pickupable = GetComponent<Pickupable>();
			emitter = this;
		}

		public void SignalFail()
		{
			if (pickupable.ItemSlot != null && pickupable.ItemSlot.Player != null)
			{
				UpdateChatMessage.Send(pickupable.ItemSlot.Player.gameObject, ChatChannel.Examine, ChatModifier.None, "You feel your signaler vibrate.");
			}
		}

		public void ServerPerformInteraction(HandActivate interaction)
		{
			Chat.AddExamineMsg(interaction.Performer, $"You press a button and send a signal through the {gameObject.ExpensiveName()}");
			emitter.EmitSignal(signalData, null);
		}

		public void TriggerTrap()
		{
			emitter.EmitSignal(signalData, null);
		}
	}

}
