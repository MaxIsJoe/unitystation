using System;
using System.Collections.Generic;
using System.Linq;
using Chemistry;
using Logs;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using US13.Core.Modular;
using US13.ScriptableObjects;
using US13.UI.Objects.Chemistry.ReactionsGuide.Atoms;
using Util;

namespace US13.UI.Objects.Chemistry.ReactionsGuide
{
	/// <summary>
	/// The GUI responsible for showing a list of reactions immediately inside the game for things like the chem dispenser or other machines that can dispense reagents.
	/// It can either show all reactions or only the reactions that are relevant to a specific machine.
	/// </summary>
	public class GUI_ReactionGuide : MonoBehaviour
	{
		[BoxGroup("Settings")]
		public bool DisplayAllReactions = false;
		[SerializeField, BoxGroup("Settings")]
		private int reactionsPerPage = 4;

		[Required("A standard way to dispense reagents is required."), BoxGroup("Setup")]
		public GameObject TargetReagentDispenser;
		public IReagentDispenser ReagentDispenser;

		[HideIf(nameof(DisplayAllReactions)), BoxGroup("Setup")]
		public GameObject ParentObjectToUseToFindMachineReactionsFromButtons;

		[BoxGroup("Setup")] public GameObject ReactionListContainer;
		[BoxGroup("Setup")] public TMP_Text PageNumberText;
		[BoxGroup("Setup")] public TMP_Text NumberOfReactionsText;
		[BoxGroup("Setup")] public TMP_InputField SearchBarField;
		[BoxGroup("Setup")] public Button SearchButton;

		[SerializeField, HideIf(nameof(DisplayAllReactions))]
		private List<Reaction> reactionsToDisplay = new();

		[ReadOnly]
		private List<Reaction> storedReactions = new();

		[BoxGroup("Templates")]
		public GameObject ReactionEntryTemplate;

		private int currentReactionPageOffset = 0;
		private int TotalPages => Mathf.CeilToInt((float)storedReactions.Count / reactionsPerPage);

		private void Awake()
		{
			if (GrabAllReactionsFromParent() == false || CheckNothingIsMissing() == false)
			{
				Loggy.Error("GUI_ReactionGuide is missing required components. Please check the inspector.");
				return;
			}
			SearchBarField?.onSubmit.AddListener(SearchReactions);
			SearchButton?.onClick.AddListener(() => SearchReactions(SearchBarField.text));
		}

		private void OnDestroy()
		{
			SearchBarField.onSubmit.RemoveAllListeners();
			SearchButton.onClick.RemoveAllListeners();
		}

		private bool CheckNothingIsMissing()
		{
			if (TargetReagentDispenser == null)
			{
				Loggy.Error("TargetReagentDispenser is null. Please assign it in the inspector.");
				return false;
			}

			ReagentDispenser = TargetReagentDispenser.GetComponent<IReagentDispenser>();
			if (ReagentDispenser == null)
			{
				Loggy.Error("TargetReagentDispenser does not have a component that implements IReagentDispenser. Please assign a valid object in the inspector.");
				return false;
			}

			if (ReactionEntryTemplate == null)
			{
				Loggy.Error("ReactionEntryTemplate is null. Please assign it in the inspector.");
				return false;
			}
			return true;
		}

		private bool GrabAllReactionsFromParent()
		{
			if (DisplayAllReactions)
			{
				storedReactions = ChemistryReagentsSO.Instance.AllChemistryReactions;
				UpdatePage(1);
				return true;
			}
			if (ParentObjectToUseToFindMachineReactionsFromButtons == null)
			{
				Loggy.Error("ParentObjectToUseToFindMachineReactionsFromButtons is null. Please assign it in the inspector.");
				return false;
			}

			//compatibility incase someone updates to TMP_Text in one of the prefabs in the future.
			Text[] buttonText = ParentObjectToUseToFindMachineReactionsFromButtons.GetComponentsInChildren<Text>(true);
			TMP_Text[] buttonTextTMP = ParentObjectToUseToFindMachineReactionsFromButtons.GetComponentsInChildren<TMP_Text>(true);
			List<Reaction> reactionsFound = new List<Reaction>();
			foreach (Text textButton in buttonText)
			{
				GetReactionFromString(reactionsFound, textButton.text);
			}
			foreach (TMP_Text tmpText in buttonTextTMP)
			{
				GetReactionFromString(reactionsFound, tmpText.text);
			}
			storedReactions = reactionsFound;
			UpdatePage(1);
			return true;
		}

		private static void GetReactionFromString(List<Reaction> reactionsFound, string text)
		{
			List<Reaction> connectedReactions = ChemistryReagentsSO.Instance.FindAllReactionsThatUseReagentInIngredients(text);
			reactionsFound.AddRange(connectedReactions);
		}

		public void UpdatePage(int page)
		{
			currentReactionPageOffset = Mathf.Clamp(page, 1, TotalPages);
			reactionsToDisplay = storedReactions.Skip((currentReactionPageOffset - 1) * reactionsPerPage).Take(reactionsPerPage).ToList();
			PageNumberText.text = $"Page {currentReactionPageOffset} of {TotalPages}";
			NumberOfReactionsText.text = $"Available Reactions: {storedReactions.Count}";
			ReactionListContainer.DestroyAllChildren();
			foreach (Reaction reaction in reactionsToDisplay)
			{
				var reactionEntry = Instantiate(ReactionEntryTemplate, ReactionListContainer.transform);
				var reactionDisplay = reactionEntry.GetComponent<ReactionDisplay>();
				if (reactionDisplay != null)
				{
					reactionDisplay.Initialize(reaction, ReagentDispenser);
				}
			}
		}

		public void NextPage()
		{
			if (currentReactionPageOffset < TotalPages)
			{
				UpdatePage(currentReactionPageOffset + 1);
			}
		}

		public void PreviousPage()
		{
			if (currentReactionPageOffset > 1)
			{
				UpdatePage(currentReactionPageOffset - 1);
			}
		}

		public void SearchReactions(string searchTerm)
		{
			if (string.IsNullOrWhiteSpace(searchTerm))
			{
				if (DisplayAllReactions == false)
				{
					GrabAllReactionsFromParent();
				}
				else
				{
					storedReactions = ChemistryReagentsSO.Instance.AllChemistryReactions;
				}
			}
			else
			{
				storedReactions = ChemistryReagentsSO.Instance.AllChemistryReactions
					.Where(r => r.DisplayName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) || r.name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
					.ToList();
			}
			UpdatePage(1);
		}
	}
}