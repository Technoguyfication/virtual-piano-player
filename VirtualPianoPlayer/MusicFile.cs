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

		/// <summary>
		/// The variables defined by the script
		/// </summary>
		public Dictionary<string, string> Variables { get; private set; } = new Dictionary<string, string>();

		/// <summary>
		/// Contains an index of the tags in the file
		/// </summary>
		public Dictionary<string, int> Tags { get; private set; } = new Dictionary<string, int>();

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
			Tags.Clear();
			Variables.Clear();

			// read all lines from the file
			string[] lines = File.ReadAllLines(FilePath, Encoding.ASCII);

			// load the script as actions
			for (int i = 0; i < lines.Length; i++)
			{
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
							throw new ParseErrorException($"Variable use started inside another variable use @ line {i + 1}: {j}");
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
						throw new ParseErrorException($"Expected directive after \",\" @ line {i + 1}");

					// get directive type
					if (!Enum.TryParse(args[0].ToUpper(), out DirectiveType directiveType))
						throw new ParseErrorException($"Invalid directive: {args[0]} @ line {i + 1}");

					// stop immediately if it is a stop directive
					if (directiveType == DirectiveType.STOP)
						break;

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

				// the line has to be music, so parse the waits out of it
				// iterate through every note / wait character
				var noteBuilder = new StringBuilder();
				int waits = 0;
				for (int j = 0; j < lines[i].Length; j++)
				{
					// increment wait count if there is a wait
					if (lines[i][j].In(' ', '.'))
					{
						waits++;
						continue;
					}

					// if there were waits and now it's notes again, add the notes and waits as actions, and reset wait count
					if (waits != 0)
					{
						// copy notes before wait directive
						copyNotes();

						// copy the wait directive in
						Actions.Add(new DirectiveLine()
						{
							Type = DirectiveType.WAIT,
							Arguments = new string[] { waits.ToString() },
							Line = i + 1
						});

						waits = 0;
					}

					// add note to note builder
					noteBuilder.Append(lines[i][j]);
				}

				// copy any notes that weren't iterated through
				copyNotes();

				void copyNotes()
				{
					if (noteBuilder.Length > 0)
					{
						string noteString = noteBuilder.ToString();
						var chars = new List<char[]>();

						// iterate through notes and find pairs
						int? pairStart = null;
						for (int j = 0; j < noteString.Length; j++)
						{
							if (noteString[j] == '[')
							{
								if (pairStart != null)
									throw new ParseErrorException($"Note pair started inside another note pair @ line {i + 1}");

								pairStart = j + 1;
								continue;
							}
							else if (noteString[j] == ']')
							{
								if (pairStart == null)
									throw new ParseErrorException($"Note pair ended but was not begun @ line {i + 1}");

								// add note pair to notes
								string notePair = noteString.Substring((int)pairStart, j - (int)pairStart);
								chars.Add(notePair.ToCharArray());

								pairStart = null;
								continue;
							}

							// add notes if they are not inside a pair
							if (pairStart == null)
								chars.Add(new char[] { noteString[j] });
						}

						// check that there are no unclosed note pairs
						if (pairStart != null)
							throw new ParseErrorException($"Unclosed note pair (or invalid characters inside note pair) @ line {i + 1}");

						Actions.Add(new MusicLine()
						{
							Line = i + 1,
							Notes = chars.ToArray()
						});

						noteBuilder.Clear();
					}
				}
			}

			// remove all set directives since they've been converted to variables by now
			Actions.RemoveAll(action =>
			{
				// true where the action is a directive and the directive type is SET
				return (action.GetType() == typeof(DirectiveLine) && ((DirectiveLine)action).Type == DirectiveType.SET);
			});

			// index tags
			for (int i = 0; i < Actions.Count; i++)
			{
				// ignore non-directive lines
				if (Actions[i].GetType() != typeof(DirectiveLine))
					continue;

				var line = (DirectiveLine)Actions[i];

				// add tag if it is one
				if (line.Type == DirectiveType.TAG)
				{
					if (line.Arguments.Length < 1)
						throw new ParseErrorException($"Expected tag name @ line {line.Line}");

					Tags.Add(line.Arguments[0], line.Line);
				}
			}
		}
	}
}
