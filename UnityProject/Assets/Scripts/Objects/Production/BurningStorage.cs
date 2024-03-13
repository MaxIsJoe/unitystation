using System.Collections;
using System.Collections.Generic;
using HealthV2;
using Items.Food;
using UnityEngine;

namespace Objects.Production
{
	[RequireComponent(typeof(ItemStorage))]
	public class BurningStorage : MonoBehaviour
	{
		public ItemStorage storage;
		private List<LivingHealthMasterBase> creaturesInStorage = new List<LivingHealthMasterBase>();
		private List<Integrity> itemsInStorage = new List<Integrity>();
	}
}
