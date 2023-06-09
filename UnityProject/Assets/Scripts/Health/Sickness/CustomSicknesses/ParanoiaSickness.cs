using System.Collections;
using System.Collections.Generic;
using Core;
using Core.Chat;
using HealthV2;
using ScriptableObjects.RP;
using UnityEngine;

namespace Health.Sickness
{
	public class ParanoiaSickness : Sickness
	{
		[SerializeField] private List<string> theThoughtsOfSomeoneAboutToRunOverSomeGreenGlowies = new List<string>();

		public override void SicknessBehavior(LivingHealthMasterBase health)
		{
			if (IsOnCooldown) return;
			Chat.AddExamineMsg(health.gameObject, theThoughtsOfSomeoneAboutToRunOverSomeGreenGlowies.PickRandom());
			if (CurrentStage >= 4) health.CannotRecognizeNames = Random13.Prob();
			if (CurrentStage >= 2) health.playerScript.PlayerNetworkActions.CmdSetCurrentIntent(Intent.Harm);
			//TODO : ALLOW PLAYERS TO SEE VISUAL HALLUCINATIONS AS WELL
			base.SicknessBehavior(health);
		}

		public override void SymptompFeedback(LivingHealthMasterBase health)
		{
			if(CurrentStage >= 3) EmoteActionManager.DoEmote(emoteFeedback, health.gameObject);
		}
	}
}