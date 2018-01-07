using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using VirtualPianoPlayer.MusicPlayer;

namespace VirtualPianoPlayer
{
	public partial class MainForm : Form
	{
		private MusicFile _currentFile = null;
		private MusicFilePlayer _player = new MusicFilePlayer();
		CancellationTokenSource _tokenSource = new CancellationTokenSource();

		public MainForm()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Opens a file in the program
		/// </summary>
		/// <param name="filePath"></param>
		private void OpenFile(string filePath)
		{
			try
			{
				_currentFile = new MusicFile(filePath);
			}
			catch (ParseErrorException ex)
			{
				MessageBox.Show(ex.Message);
				return;
			}

			// display "compiled" code
			var builder = new StringBuilder();
			foreach (Line action in _currentFile.Actions)
			{
				builder.AppendLine(action.ToString());
			}
			testTextBox.Text = builder.ToString();
		}

		private void PlayFile(MusicFile file)
		{
			_player.Stop();
			_player.Play(file, _tokenSource.Token, PlayCallback);
		}

		private void PlayCallback(Exception ex)
		{
			if (ex != null)
				throw ex;
		}

		private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
		{
			using (var dialog = new OpenFileDialog()
			{
				CheckFileExists = true
			})
			{
				var result = dialog.ShowDialog();

				if (result != DialogResult.OK)
					return;

				OpenFile(dialog.FileName);
			}
		}

		private void PlayToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (_currentFile != null)
				PlayFile(_currentFile);
		}
	}
}
