using System.Collections.Generic;
using UnityEngine;

namespace Map.Areas
{
	public class Area
	{
		public string ReadableName;
		public string ID;
		public Color? VisualColor = null;
		public List<string> Tags = new List<string>();

		public Area(string ID, string name, List<string> tags = null, Color? newColor = null)
		{
			this.ID = ID;
			ReadableName = name;
			VisualColor = newColor;
			if (tags is not null) Tags.AddRange(tags);
		}
	}
}