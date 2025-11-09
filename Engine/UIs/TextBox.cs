using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG.UIs
{
	public static class TextBox
	{
		public enum TextboxType
		{
			None = 0,
			/// <summary>
			/// All alphabetical characters.
			/// a-z.
			/// </summary>
			Alphabetical = 1 << 0,
			/// <summary>
			/// All numbers.
			/// 0-9.
			/// </summary>
			Numerical = 1 << 1,
			/// <summary>
			/// All special characters. 
			/// !@#$%^&*()_+-=[]{}\|;':",.<>/?`~
			/// </summary>
			Special = 1 << 2,
			/// <summary>
			/// Characters that integers use. Just '-' at the moment.
			/// </summary>
			Integer = Numerical | 1 << 3,
			/// <summary>
			/// Floating point precision characters. '.' and '-'.
			/// Overridden by special. Used for floats and doubles where we need decimal spaces and negatives.
			/// </summary>
			FloatingPrecision = Numerical | 1 << 4,
			/// <summary>
			/// Backspace can be pressed to delete characters.
			/// </summary>
			AllowsBackspace = 1 << 5,
			/// <summary>
			/// Enter can be pressed.
			/// </summary>
			AllowsEnter = 1 << 6,
			/// <summary>
			/// Enter exits the textbox.
			/// </summary>
			EnterExits = 1 << 7,
			/// <summary>
			/// Tab can be pressed.
			/// </summary>
			AllowsTab = 1 << 8,
			/// <summary>
			/// Allows all alphabetical and numerical characters.
			/// Does not add backspace, enter, and tab.
			/// </summary>
			AlphaNumerical = Alphabetical | Numerical,
			/// <summary>
			/// Allows all alphabetical, numerical, and special characters.
			/// Does not add backspace, enter, and tab.
			/// </summary>
			AllCharacters = Alphabetical | Numerical | Special,
		}
	}
}
