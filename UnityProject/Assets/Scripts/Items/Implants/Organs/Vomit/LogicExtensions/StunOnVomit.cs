﻿using HealthV2;
using UnityEngine;

namespace Items.Implants.Organs.Vomit.LogicExtensions
{
	public class StunOnVomit : MonoBehaviour, IVomitExtension
	{
		[SerializeField] private float stunDuration = 4f;
		public void OnVomit(float amount, LivingHealthMasterBase health)
		{
			if(DMMath.Prob(50)) health.playerScript.RegisterPlayer.ServerLayDown();
			health.playerScript.RegisterPlayer.ServerStun(stunDuration, true, false);
		}
	}
}