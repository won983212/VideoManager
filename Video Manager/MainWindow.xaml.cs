using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Video_Manager.Controls;

namespace Video_Manager
{
	/// <summary>
	/// MainWindow.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class MainWindow : Window
	{
		public HashSet<VideoEntry> SelectedVideos = new HashSet<VideoEntry>();
		private Properties.Settings settings = Properties.Settings.Default;
		private List<string> selectedTags = new List<string>();
		private List<string> allVideoFiles = new List<string>();

		private ExpandAnimation statusBarAnimation;
		private ExpandAnimation tagBarAnimation;
		private FileDatabase filedb = new FileDatabase();
		private DispatcherTimer resizeTimer = new DispatcherTimer();

		public MainWindow()
		{
			InitializeComponent();

			filedb.SetWorkingFolder(settings.WorkingFolder);
			filedb.Load();

			resizeTimer.Interval = TimeSpan.FromMilliseconds(500);
			resizeTimer.Tick += ResizeTimer_Tick;

			Width = settings.InitialWidth;
			Height = settings.InitialHeight;
			
			// initialize all tag buttons
			foreach(string tag in filedb.GetAllTags())
			{
				Button tagbt = new Button();
				tagbt.Content = "#" + tag;
				tagbt.Click += Tag_Click;
				pnlAllTags.Children.Add(tagbt);
			}
		}

		private void wndRoot_Loaded(object sender, RoutedEventArgs e)
		{
			// initialize all video files
			int id = 0;
			foreach (string path in Directory.GetFiles(settings.WorkingFolder))
			{
				string ext = System.IO.Path.GetExtension(path);
				if (ext == ".avi" || ext == ".mp4" || ext == ".wmv" || ext == ".mkv" || ext == ".webm")
				{
					ThumbnailBlock block = new ThumbnailBlock();
					block.Width = 130;
					block.Height = 70;
					block.Tag = new VideoEntry(id++);
					block.MouseDown += Block_MouseDown;
					pnlVideos.Children.Add(block);
					allVideoFiles.Add(System.IO.Path.GetFileName(path));
				}
			}

			statusBarAnimation = new ExpandAnimation(pnlStatusBar);
			tagBarAnimation = new ExpandAnimation(pnlTags, 22);
		}

		private void Block_MouseDown(object sender, MouseButtonEventArgs e)
		{
			ThumbnailBlock block = sender as ThumbnailBlock;
			bool closed = SelectedVideos.Count == 0;

			if (block.IsChecked)
				SelectedVideos.Add((VideoEntry)block.Tag);
			else
				SelectedVideos.Remove((VideoEntry)block.Tag);

			if(SelectedVideos.Count == 0)
				statusBarAnimation.Close();
			else if(closed && SelectedVideos.Count > 0)
				statusBarAnimation.Open();

			tblStatusText.Text = "현재 " + SelectedVideos.Count + "개 선택됨";
		}

		private void Titlebar_MouseDown(object sender, MouseButtonEventArgs e)
		{
			DragMove();
		}

		private void Exit_Click(object sender, RoutedEventArgs e)
		{
			Application.Current.Shutdown();
		}

		private void wndRoot_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			resizeTimer.IsEnabled = true;
		}

		private void ResizeTimer_Tick(object sender, EventArgs e)
		{
			resizeTimer.IsEnabled = false;
			settings.InitialWidth = Width;
			settings.InitialHeight = Height;
			settings.Save();
		}

		private void Tag_Click(object sender, RoutedEventArgs e)
		{
			string tag = ((string)((Button)sender).Content).Substring(1);
			bool closed = selectedTags.Count == 0;

			if (selectedTags.Contains(tag))
				selectedTags.Remove(tag);
			else
				selectedTags.Add(tag);

			if (selectedTags.Count == 0)
				tagBarAnimation.Close();
			else if (closed && selectedTags.Count > 0)
				tagBarAnimation.Open();

			pnlTags.Children.Clear();
			foreach(string tagent in selectedTags)
			{
				Button tagbt = new Button();
				tagbt.Content = "#" + tagent;
				tagbt.Click += Tag_Click;
				pnlTags.Children.Add(tagbt);
			}
		}

		private void ChangedWorkingFolder_MouseDown(object sender, MouseButtonEventArgs e)
		{
			CommonOpenFileDialog dialog = new CommonOpenFileDialog();
			dialog.InitialDirectory = settings.WorkingFolder;
			dialog.IsFolderPicker = true;
			if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
			{
				settings.WorkingFolder = dialog.FileName;
				settings.Save();
			}
			e.Handled = true;
		}

		private void ArrangeMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			settings.Save();
		}
	}

	public class VideoEntry
	{
		public int Id { get; }

		public VideoEntry(int id)
		{
			Id = id;
		}

		public override int GetHashCode()
		{
			return Id.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			VideoEntry ent = obj as VideoEntry;
			if (ent != null)
				return ent.Id.Equals(Id);
			return false;
		}
	}
}
