using Chemistry;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Util;

namespace US13.UI.Objects.Chemistry.ReactionsGuide.Atoms
{
	public class ReactionDisplay : MonoBehaviour
	{
		public Image SplatColorImage;
		public TMP_Text DisplayName;

		public GameObject ReagentButtonTemplate;

		public GameObject ReagentButtonsList;

		public void Initialize(Reaction reaction)
		{
			SplatColorImage.color = reaction.GetReactionColor();
			DisplayName.text = string.IsNullOrEmpty(reaction.DisplayName) ? reaction.name : $"{reaction.DisplayName}";
			ReagentButtonsList.DestroyAllChildren();
			foreach (var ingredient in reaction.ingredients.Keys)
			{
				var newButton = Instantiate(ReagentButtonTemplate, ReagentButtonsList.transform);
				var buttonText = newButton.GetComponentInChildren<TMP_Text>();
				buttonText.text = string.IsNullOrEmpty(ingredient.Name) ? ingredient.name : $"{ingredient.Name}";
			}
		}
	}
}