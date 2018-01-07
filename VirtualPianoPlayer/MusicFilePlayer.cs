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

namespace VirtualPianoPlayer
{
	public class MusicFilePlayer
	{
		/// <summary>
		/// The amount of time to wait between key down and key up, in milliseconds
		/// </summary>
		private const int KEY_DELAY = 30;

		private Timer _tickTimer;
		private MusicFile _currentFile;
		private CancellationToken _cancellationToken;
		private bool _playing = false;
		private Action _doneCallback;
		private int _currentActionIndex = 0;
		private int _wait = 0;
		private InputSimulator _inputSimulator = new InputSimulator();

		public int BPM
		{
			get
			{
				return _bpm;
			}
			private set
			{
				_tickTimer.Interval = _bpm = value;
			}
		}
		private int _bpm = 1;

		public MusicFilePlayer()
		{
			_tickTimer = new Timer(BPM)
			{
				AutoReset = true,
				Enabled = false,
			};
			_tickTimer.Elapsed += Tick;
		}

		/// <summary>
		/// Begins playing a music file
		/// </summary>
		/// <param name="file"></param>
		/// <param name="cancellationToken"></param>
		/// <param name="callback"></param>
		public void Play(MusicFile file, CancellationToken cancellationToken, Action callback)
		{
			_doneCallback = callback;
			_cancellationToken = cancellationToken;
			_currentFile = file;

			_currentActionIndex = 0;

			// set the BPM to one to begin the script
			BPM = 1;
			_tickTimer.Start();
		}

		/// <summary>
		/// Stops playing the piece
		/// </summary>
		public void Stop()
		{
			_playing = false;
			_tickTimer.Stop();
		}

		/// <summary>
		/// Called for each beat of the piece
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Tick(object sender, ElapsedEventArgs e)
		{
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
				for (int i = 0; i > line.Notes.Length; i++)
				{
					// find if any letters in the squence are uppercase
					bool upper = (line.Notes[i].Any(c =>
					{
						return char.IsUpper(c);
					}));

					// press shift if the keys are uppercase
					if (upper)
						keyboard.KeyDown(VirtualKeyCode.SHIFT);

					// press all keys down
					foreach (char c in line.Notes[i])
						keyboard.KeyDown((VirtualKeyCode)c);

					// wait
					keyboard.Sleep(KEY_DELAY);

					// pull all keys up
					foreach (char c in line.Notes[i])
						keyboard.KeyUp((VirtualKeyCode)c);

					// undo shift
					if (upper)
						keyboard.KeyUp(VirtualKeyCode.SHIFT);
				}
			}
			else if (actionType == typeof(DirectiveLine))
			{
				// directive action
			}
		}
	}
}
