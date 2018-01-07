using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualPianoPlayer.MusicPlayer
{
	public class MusicLine : Line
	{
		// Two dimensional array of note combinations
		public char[][] Notes { get; set; }

		/// <summary>
		/// The line number of the line
		/// </summary>
		public int Line { get; set; }

		public override string ToString()
		{
			string[] combos = new string[Notes.Length];

			// build note string
			for (int i = 0; i < Notes.Length; i++)
			{
				combos[i] = $"[{string.Join(string.Empty, Notes[i])}]";
			}

			return $"{Line.ToString("D3")}: PLAY: {string.Join(", ", combos)}";
		}
	}
}
