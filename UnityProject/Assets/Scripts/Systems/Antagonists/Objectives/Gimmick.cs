﻿using UnityEngine;

namespace Antagonists
{
	/// <summary>
	/// Gimmick objective, always completed
	/// </summary>
	[CreateAssetMenu(menuName="ScriptableObjects/Objectives/Gimmick")]
	public class Gimmick : Objective
	{
		protected override void Setup(){ }

		protected override bool CheckCompletion()
		{
			//Always true
			return true;
		}
	}
}

