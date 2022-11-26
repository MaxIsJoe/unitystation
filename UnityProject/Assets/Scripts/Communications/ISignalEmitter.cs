using Managers;

namespace Communications
{
	public interface ISignalEmitter
	{
		public void EmitSignal(SignalData data, ISignalMessage msg)
		{
			data.EmitterInterface = this;
			SignalsManager.Instance.SendSignal(data);
		}

		public void SignalFail()
		{
			// Most things don't actually do anything when a signal fails yet.
			return;
		}
	}
}