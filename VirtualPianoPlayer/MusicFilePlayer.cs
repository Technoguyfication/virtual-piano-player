using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using VirtualPianoPlayer.MusicPlayer;
using Timer = System.Timers.Timer;
using WindowsInput;
using WindowsInput.Native;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace VirtualPianoPlayer
{
	public class MusicFilePlayer
	{
		/// <summary>
		/// The amount of time to wait between key down and key up, in milliseconds
		/// </summary>
		private const int KEY_DELAY = 30;

		/// <summary>
		/// The amount of milliseconds in one beat per minute
		/// </summary>
		private const int MS_PER_1_BPM = (int)6e4;

		private MusicFile _currentFile;
		private CancellationToken _cancellationToken;
		private bool _playing = false;
		private PlayCallback _doneCallback;
		private int _currentActionIndex = 0;
		private int _wait = 0;
		private InputSimulator _inputSimulator = new InputSimulator();
		private Dictionary<int, int> _gotoHistory = new Dictionary<int, int>();
		private Stopwatch _stopwatch = new Stopwatch();

		[DllImport("user32.dll")]
		static extern short VkKeyScan(char ch);

		/// <summary>
		/// The callback for the <see cref="Play(MusicFile, CancellationToken, Action)"/> void.
		/// If exception is not null, the operation was interrupted, and it should be treated as a thrown exception.
		/// </summary>
		/// <param name="exception"></param>
		public delegate void PlayCallback(Exception exception = null);

		public int BPM { get; set; }

		/// <summary>
		/// Begins playing a music file
		/// </summary>
		/// <param name="file"></param>
		/// <param name="cancellationToken"></param>
		/// <param name="callback"></param>
		public void Play(MusicFile file, CancellationToken cancellationToken, PlayCallback callback)
		{
			_doneCallback = callback;
			_cancellationToken = cancellationToken;
			_currentFile = file;

			_currentActionIndex = 0;
			_playing = true;

			// set the BPM to one to begin the script
			BPM = 30;

			Task.Run(() =>
			{
				while (_playing)
					Tick();

				callback(null);
			});
		}

		/// <summary>
		/// Stops playing the piece
		/// </summary>
		public void Stop()
		{
			_playing = false;
		}

		/// <summary>
		/// Called for each beat of the piece
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Tick()
		{
			try
			{
				_stopwatch.Restart();

				// waits for the beat to pass
				void wait()
				{
					Thread.Sleep(Math.Max((MS_PER_1_BPM / BPM) - (int)_stopwatch.ElapsedMilliseconds, 0));
					_stopwatch.Reset();
				}

				// check whether we should cancel
				if (_cancellationToken.IsCancellationRequested || !_playing || _currentActionIndex >= _currentFile.Actions.Count)
				{
					Stop();
					return;
				}

				// check if we should skip this interval
				if (_wait > 0)
				{
					_wait--;
					wait();
					return;
				}

				Type actionType = _currentFile.Actions[_currentActionIndex].GetType();

				// get action type
				if (actionType == typeof(MusicLine))
				{
					var keyboard = _inputSimulator.Keyboard;

					// get line from action
					MusicLine line = (MusicLine)(_currentFile.Actions[_currentActionIndex]);

					// iterate through every key in the key pairs
					for (int i = 0; i < line.Notes.Length; i++)
					{
						// find if any letters in the squence are uppercase
						bool upper = (line.Notes[i].Any(c =>
						{
							return char.IsUpper(c);
						}));

						// press shift if the keys are uppercase
						if (upper)
							keyboard.KeyDown(VirtualKeyCode.SHIFT);

						// get array of virtual keycodes
						byte[] codes = line.Notes[i].Select(key =>
							new KeyData
							{
								Value = VkKeyScan(key)
							}.Low).ToArray();


						// press all keys down
						foreach (byte c in codes)
						{
							keyboard.KeyDown((VirtualKeyCode)c);
							keyboard.Sleep(2);
						}

						// wait
						keyboard.Sleep(KEY_DELAY);

						// pull all keys up
						foreach (byte c in codes)
							keyboard.KeyUp((VirtualKeyCode)c);

						// undo shift
						if (upper)
							keyboard.KeyUp(VirtualKeyCode.SHIFT);

						wait();
					}
				}
				else if (actionType == typeof(DirectiveLine))
				{
					var line = (DirectiveLine)_currentFile.Actions[_currentActionIndex];

					switch (line.Type)
					{
						case DirectiveType.BPM:
							{
								checkArgCount(1);

								// get bpm number
								if (!int.TryParse(line.Arguments[0], out int bpm))
									throw new RuntimeErrorException($"Argument provided for bpm was not a valid integer number @ line {line.Line}");

								BPM = bpm;
								break;
							}
						case DirectiveType.WAIT:
							{
								checkArgCount(1);

								// get note count
								if (!int.TryParse(line.Arguments[0], out int count))
									throw new RuntimeErrorException($"Argument provided for wait was not a valid integer number @ line {line.Line}");

								_wait = count;
								break;
							}
						case DirectiveType.GOTO:
							{
								checkArgCount(2);

								// get and check args
								if (!_currentFile.Tags.ContainsKey(line.Arguments[0]))
									throw new RuntimeErrorException($"Tag not found: {line.Arguments[0]} @ line {line.Line}");

								if (!int.TryParse(line.Arguments[1], out int count))
									throw new RuntimeErrorException($"Argument provided for goto count was not a valid integer number @ line {line.Line}");

								// check if goto directive has not been completed
								// because we can only have one directive per line, we track GOTOs by line number
								if (_gotoHistory.ContainsKey(line.Line) && _gotoHistory[line.Line] > count)
								{
									// create / update item for tracking
									if (!_gotoHistory.ContainsKey(line.Line))
										_gotoHistory.Add(line.Line, 1);
									else
										_gotoHistory[line.Line]++;

									// move execution to the tag
									_currentActionIndex = _currentFile.Tags[line.Arguments[0]];
								}
								break;
							}
						default:
							// ignore anything else
							break;
					}

					void checkArgCount(int argCount)
					{
						if (line.Arguments.Length < argCount)
							throw new RuntimeErrorException($"Directive must have at least {argCount} argument(s) @ line {line.Line}");
					}
				}
			}
			catch (Exception ex)
			{
				Stop();

				// send exceptions back via the callback
				_doneCallback(ex);
				return;
			}

			_currentActionIndex++;
		}
	}

	/// <summary>
	/// Allows us to get the high and low bytes from a short
	/// </summary>
	[StructLayout(LayoutKind.Explicit)]
	public struct KeyData
	{
		[FieldOffset(0)]
		public short Value;

		[FieldOffset(0)]
		public byte Low;

		[FieldOffset(1)]
		public byte High;
	}
}
