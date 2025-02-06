
using UnityEngine;

public class VoiceChatInitialiser : MonoBehaviour, IClientInteractable<HandActivate>
{
	public bool Interact(HandActivate interaction)
	{
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX
		VoiceChatManager.Instance.SetUp();
#endif
		return true;
	}
}