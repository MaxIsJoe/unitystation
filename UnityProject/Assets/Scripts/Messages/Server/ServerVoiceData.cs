#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX
using Adrenak.UniVoice;
using Mirror;

namespace Messages.Server
{
	public class ServerVoiceData : ServerMessage<ServerVoiceData.UniVoiceMessage>
	{
		public struct UniVoiceMessage : NetworkMessage
		{
			public short audioSender;
			public uint Object;
			public short recipient;
			public ChatroomAudioSegment data;
			public string Tag;
			public short[] PeerIDs;

		}
		public override void Process(UniVoiceMessage msg)
		{

			if (VoiceChatManager.Instance == null || VoiceChatManager.Instance.UniVoiceMirrorNetwork == null)
			{
				if (msg.Tag != "AUDIO_SEGMENT")
				{
					VoiceChatManager.CachedMessage.Add(msg);
				}
			}
			else
			{
				if (VoiceChatManager.CachedMessage != null)
				{
					foreach (var Message in VoiceChatManager.CachedMessage)
					{
						VoiceChatManager.Instance.Client_OnMessage(Message);
					}

					VoiceChatManager.CachedMessage.Clear();
				}
				VoiceChatManager.Instance.Client_OnMessage(msg);
			}

		}




		public static UniVoiceMessage Send( UniVoiceMessage msg)
		{
			NetworkServer.SendToAll(msg, Mirror.Channels.Unreliable, sendToReadyOnly: true);
			return msg;
		}
		public static UniVoiceMessage SendTo( NetworkConnection recipient, UniVoiceMessage msg)
		{
			recipient.Send(msg, Mirror.Channels.Unreliable);
			return msg;
		}

	}
}
#endif