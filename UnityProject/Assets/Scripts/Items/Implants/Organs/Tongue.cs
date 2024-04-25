﻿using System.Collections.Generic;
using HealthV2;
using Player.Language;
using ScriptableObjects;
using UnityEngine;

namespace Items.Implants.Organs
{
	public class Tongue : BodyPartFunctionality
	{
		private MobLanguages mobLanguages;

		[SerializeField] private List<LanguageSO> languages = new List<LanguageSO>();
		[SerializeField] private ChatModifier speechModifiers = ChatModifier.None;
		[field: SerializeField] public bool CannotSpeak {get; private set; }

		public override void OnAddedToBody(LivingHealthMasterBase livingHealth)
		{
			RelatedPart = GetComponent<BodyPart>();
			mobLanguages = livingHealth.GetComponent<MobLanguages>();

			if(CustomNetworkManager.IsServer == false) return;

			foreach (var language in languages)
			{
				mobLanguages.LearnLanguage(language, true);
			}
			livingHealth.IsMute.RecordPosition(this, CannotSpeak);
			LivingHealthMaster.playerScript.inventorySpeechModifiers |= speechModifiers;
		}

		public override void OnRemovedFromBody(LivingHealthMasterBase livingHealth)
		{
			if(CustomNetworkManager.IsServer == false) return;
			livingHealth.IsMute.RemovePosition(this);
			foreach (var language in languages)
			{
				//Don't remove the language if it is in the default list
				if(mobLanguages.DefaultLanguages != null && mobLanguages.DefaultLanguages.UnderstoodLanguages.Contains(language)) continue;

				if(language.Flags.HasFlag(LanguageFlags.TonguelessSpeech)) continue;

				//Can no longer speak, but can still understand
				mobLanguages.RemoveLanguage(language);
			}
			LivingHealthMaster.playerScript.inventorySpeechModifiers &= ~speechModifiers;
		}

		public void SetCannotSpeak(bool inValue)
		{
			CannotSpeak = inValue;
			if (RelatedPart.HealthMaster != null)
			{
				RelatedPart.HealthMaster.IsMute.RecordPosition(this, CannotSpeak);
			}
		}
	}
}