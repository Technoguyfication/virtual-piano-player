using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using VirtualPianoPlayer.MusicPlayer;

namespace VirtualPianoPlayer
{
	/// <summary>
	/// Represents a playable music file
	/// </summary>
	public class MusicFile
	{
		/// <summary>
		/// The file path of the music file
		/// </summary>
		public string FilePath { get; set; }

		/// <summary>
		/// The compiled list of actions that the music file makes
		/// </summary>
		public List<object> Actions { get; private set; } = new List<object>();

		public Dictionary<string, string> Variables { get; set; } = new Dictionary<string, string>();

		public MusicFile(string filePath)
		{
			FilePath = filePath;
			ParseFile();
		}

		/// <summary>
		/// Interprets the file contents
		/// </summary>
		private void ParseFile()
		{
			Actions.Clear();
			string[] lines = File.ReadAllLines(FilePath);

			// load the script as actions
			for (int i = 0; i < lines.Length; i++)
			{
				// strip all non-ascii characters from input
				lines[i] = Regex.Replace(lines[i], @"[^\u0020-\u007E]+", string.Empty);

				// test that the lines[i] contains data
				if (string.IsNullOrEmpty(lines[i]))
					continue;

				// test that the lines[i] is not a comment
				if (lines[i].StartsWith("#"))
					continue;

				// expand variables
				int? varStart = null;
				restartVarScan:
				for (int j = 0; j < lines[i].Length; j++)
				{
					if (lines[i][j] == '{')
					{
						if (varStart == null)
							varStart = j + 1;
						else
							throw new ParseErrorException($"Variable use started inside another variable use @ line {i  + 1}: {j}");
					}
					else if (lines[i][j] == '}')
					{
						if (varStart == null)
							throw new ParseErrorException($"Closing brace without preceeding opening brace @ line {i + 1}: {j}");

						string varName = lines[i].Substring((int)varStart, (j - (int)varStart));
						if (!Variables.ContainsKey(varName))
							throw new ParseErrorException($"Variable {varName} does not exist @ line {i}: {(int)varStart}");

						// expand variable inside line
						string newLine = lines[i].Remove((int)varStart - 1, (j - (int)varStart) + 2);
						newLine = newLine.Insert((int)varStart - 1, Variables[varName]);

						// apply values
						varStart = null;
						lines[i] = newLine;
						goto restartVarScan;
					}
				}

				// check if the lines[i] is a directive
				if (lines[i].StartsWith(","))
				{
					// split into args
					string[] args = lines[i].Substring(1).Split(' ');
					if (args.Length < 1)
						throw new ParseErrorException($"Expected directive after \",\" @ {FilePath} line {i + 1}");

					// get directive type
					if (!Enum.TryParse(args[0].ToUpper(), out DirectiveType directiveType))
						throw new ParseErrorException($"Invalid directive: args[0] @ {FilePath} line {i + 1}");

					// add directive action
					var line = new DirectiveLine()
					{
						Type = directiveType,
						Arguments = args.SubArray(1),
						Line = i + 1
					};

					// check line for variables
					if (line.Type == DirectiveType.SET)
					{
						if (line.Arguments.Length < 2)
							throw new ParseErrorException($"Variable expected data after set directive at line {line.Line}");

						// add variable
						Variables.Add(line.Arguments[0], string.Join(" ", line.Arguments.SubArray(1)));
					}

					Actions.Add(line);
					continue;
				}

				// the lines[i] has to be notes, so add them
				Actions.Add(new MusicLine()
				{
					Notes = lines[i].ToCharArray(),
					Line = i + 1
				});
			}

			// remove all set directives since they've been converted to variables by now
			Actions.RemoveAll(action =>
			{
				// true where the action is a directive and the directive type is SET
				return (action.GetType() == typeof(DirectiveLine) && ((DirectiveLine)action).Type == DirectiveType.SET);
			});

			// iterate through each of the directives and set variables, and expand variables
			for (int i = 0; i < Actions.Count; i++)
			{
				

				// skip if it isn't a directive
				if (Actions[i].GetType() != typeof(DirectiveLine))
					continue;

				var line = (DirectiveLine)Actions[i];
			}
		}
	}
}
