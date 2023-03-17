using TMPro;
using UnityEngine;

namespace Map.Areas
{
	public class AreaVisual : MonoBehaviour
	{
		[SerializeField] private TMP_Text idText;
		[SerializeField] private SpriteRenderer sprite;

		public void Setup(string text, Color? newColor)
		{
			idText.text = text;
			if (newColor != null)
			{
				sprite.color = (Color)newColor;
			}
		}
	}
}