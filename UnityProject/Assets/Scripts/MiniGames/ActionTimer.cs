using System;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MiniGames
{
	public class ActionTimer : NetworkBehaviour
	{
		private float greenBarSize = 0.2f;
		private DateTime lastActionTime;
		[SyncVar] private float progress = 0;
		public Action OnSuccess;
		public Action OnFailure;
		public SpriteRenderer allowedTimeSprite;
		public SpriteRenderer cursorSprite;


		private void Start()
		{
			Setup((() => {Debug.Log("success");}), (() => {Debug.Log("failure");}));
		}

		private void Update()
		{
			progress = Math.Clamp(progress + Time.deltaTime, -0.85f, 0.85f);
			cursorSprite.transform.localPosition = new Vector3(progress, 0.23f, 0f);
			if (progress.Approx(0.85f))
			{
				progress = -0.85f;
			}
		}

		public void Setup(Action success, Action failure, float ease = 0)
		{
			this.greenBarSize = ease <= 0 ? Random.Range(0.2f ,0.8f) : ease;
			OnSuccess += success;
			OnFailure += failure;
			UpdateScales();
		}

		private void UpdateScales()
		{
			var localScale = allowedTimeSprite.transform.localScale;
			allowedTimeSprite.transform.localScale = new Vector3(greenBarSize, localScale.y, localScale.z);
		}

		public void TriggerAction()
		{
			if (cursorSprite.bounds.Intersects(allowedTimeSprite.bounds))
			{
				OnSuccess?.Invoke();
			}
			else
			{
				OnFailure?.Invoke();
			}
		}
	}
}