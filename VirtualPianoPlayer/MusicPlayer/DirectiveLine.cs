using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualPianoPlayer.MusicPlayer
{
	/// <summary>
	/// Represents a single directive
	/// </summary>
	public struct DirectiveLine
	{
		public DirectiveType Type { get; set; }
		public string[] Arguments { get; set; }

		/// <summary>
		/// The line number of the line
		/// </summary>
		public int Line { get; set; }
	}

	/// <summary>
	/// A list of directive types
	/// </summary>
	public enum DirectiveType
	{
		BPM,
		WAIT,
		STOP,
		TAG,
		GOTO,
		SET
	}
}
