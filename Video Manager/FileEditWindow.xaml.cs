using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Video_Manager
{
	/// <summary>
	/// FileEditWindow.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class FileEditWindow : Window
	{
		private List<VideoEntry> files;
		private string destPath; // if null, files will be deleted.

		private FileEditWindow(IEnumerable<VideoEntry> videoFiles) : this(videoFiles, null)
		{ }

		private FileEditWindow(IEnumerable<VideoEntry> videoFiles, string copyPath)
		{
			InitializeComponent();
			files = new List<VideoEntry>(videoFiles);
			destPath = copyPath;
			ProcessFileAsync();
		}

		private void Progress(double percent, string state)
		{
			Dispatcher.Invoke(() =>
			{
				tblPercent.Text = ((int)percent).ToString() + "%";
				tblState.Text = state;
				prgCurrent.Value = percent;
			});
		}

		private async void ProcessFileAsync()
		{
			bool isCopy = destPath != null;
			if (isCopy)
				tblMethod.Text = "Copying..";
			else
				tblMethod.Text = "Deleting..";

			await Task.Factory.StartNew(() =>
			{
				if (isCopy)
					FileCopy();
				else
					FileDelete();
				Thread.Sleep(100);
			});
			Close();
		}

		private void FileCopy()
		{
			byte[] buffer = new byte[1024 * 1024];
			long totalFileSize = 0;
			long processed = 0;

			foreach (VideoEntry ent in files)
				totalFileSize += new FileInfo(ent.Path).Length;

			foreach (VideoEntry ent in files)
			{
				string filename = System.IO.Path.GetFileName(ent.Path);
				using (FileStream dest = new FileStream(System.IO.Path.Combine(destPath, filename), FileMode.CreateNew, FileAccess.Write))
				{
					using (FileStream src = new FileStream(ent.Path, FileMode.Open, FileAccess.Read))
					{
						int len = -1;
						while ((len = src.Read(buffer, 0, buffer.Length)) > 0)
						{
							processed += len;
							dest.Write(buffer, 0, len);
							Thread.Sleep(50);
							Progress(processed * 100.0 / totalFileSize, filename);
						}
					}
				}
			}
		}

		private void FileDelete()
		{
			int processed = 0;
			foreach (VideoEntry ent in files)
			{
				string filename = System.IO.Path.GetFileName(ent.Path);
				File.Delete(ent.Path);
				Thread.Sleep(1000);
				Progress(processed++ * 100.0 / files.Count, filename);
			}
		}

		private void Window_MouseDown(object sender, MouseButtonEventArgs e)
		{
			DragMove();
		}

		public static void ShowFileDeleteDialog(IEnumerable<VideoEntry> videoFiles)
		{
			new FileEditWindow(videoFiles).ShowDialog();
		}

		public static void ShowFileCopyDialog(IEnumerable<VideoEntry> videoFiles, string copyPath)
		{
			new FileEditWindow(videoFiles, copyPath).ShowDialog();
		}
	}
}
