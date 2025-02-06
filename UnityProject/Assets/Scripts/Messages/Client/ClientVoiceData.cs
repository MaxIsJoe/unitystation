#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX
using Adrenak.UniVoice;
using Mirror;

namespace Messages.Client
{
	public class ClientVoiceData : ClientMessage<ClientVoiceData.UniVoiceMessage>
	{
		public struct UniVoiceMessage : NetworkMessage
		{
			public short audioSender;
			public string Tag;
			public short recipient;
			public ChatroomAudioSegment data;
		}

		public override void Process(UniVoiceMessage msg)
		{
			VoiceChatManager.Instance.Server_OnMessage(SentByPlayer.Connection, msg);
		}

		public static UniVoiceMessage Send( UniVoiceMessage msg)
		{
			NetworkClient.Send(msg, Mirror.Channels.Unreliable);
			return msg;
		}

	}
}
#endif