using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualPianoPlayer.MusicPlayer
{
	public class MusicLine : Line
	{
		public char[] Notes { get; set; }

		/// <summary>
		/// The line number of the line
		/// </summary>
		public int Line { get; set; }

		public override string ToString()
		{
			return $"{Line.ToString("D3")}: PLAY: {string.Join(string.Empty, Notes)}";
		}
	}
}
