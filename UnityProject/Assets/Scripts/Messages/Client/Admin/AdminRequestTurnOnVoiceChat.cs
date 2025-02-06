#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX
using Mirror;

namespace Messages.Client.Admin
{
	public class AdminRequestTurnOnVoiceChat : ClientMessage<AdminRequestTurnOnVoiceChat.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public bool Enabled;
		}

		public override void Process(NetMessage msg)
		{
			if (HasPermission(TAG.SETTING_VOICE_CHAT))
			{
				VoiceChatManager.Instance.SyncEnabled(VoiceChatManager.Instance.Enabled, msg.Enabled);
			}
		}

		public static NetMessage Send(bool SetTo)
		{
			NetMessage msg = new()
			{
				Enabled = SetTo
			};

			Send(msg);
			return msg;
		}
	}
}
#endif