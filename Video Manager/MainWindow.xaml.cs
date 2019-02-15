using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
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
		#region Fields

		private static readonly string[] sizeUnits = { "B", "KB", "MB", "GB" };
		private Properties.Settings settings = Properties.Settings.Default;
		private BlurEffect blur = new BlurEffect();

		public HashSet<VideoEntry> SelectedVideos = new HashSet<VideoEntry>();
		private List<string> selectedTags = new List<string>();
		private List<string> allVideoFiles = new List<string>();

		private ExpandAnimation statusBarAnimation;
		private ExpandAnimation tagBarAnimation;
		private FileDatabase filedb = new FileDatabase();
		private DispatcherTimer resizeTimer = new DispatcherTimer();

		#endregion

		public MainWindow()
		{
			InitializeComponent();
			filedb.SetWorkingFolder(settings.WorkingFolder);

			resizeTimer.Interval = TimeSpan.FromMilliseconds(500);
			resizeTimer.Tick += ResizeTimer_Tick;

			Width = settings.InitialWidth;
			Height = settings.InitialHeight;
		}

		#region Methods

		private void TagToggle(string tag)
		{
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
			foreach (string tagent in selectedTags)
			{
				Button tagbt = new Button();
				tagbt.Content = tagent;
				tagbt.Click += Tag_Click;
				pnlTags.Children.Add(tagbt);
			}
		}

		private void ShowMetadataPopup(VideoEntry vid)
		{
			pnlBody.Effect = blur;
			pnlMetadata.Visibility = Visibility.Visible;

			tblMetaName.Text = System.IO.Path.GetFileName(vid.Path);
			tblMetaCopyCount.Text = vid.Metadata.CopyedCount.ToString();
			tblMetaModifyDate.Text = File.GetLastWriteTime(vid.Path).ToString("yyyy.MM.dd");

			itemsTags.ItemsSource = vid.Metadata.Tags;
			pnlMetadata.Tag = vid;
		}

		// Load Videos and Metadata. It will initialize all ui after loading.
		// TODO WrapPanel is a bit slow. Do apply virtualization.
		private async void LoadMetaAndVidsAsync()
		{
			itemsVideos.Effect = blur;
			await Task.Factory.StartNew(LoadMetaVidsImpl);
			pnlLoading.Visibility = Visibility.Hidden;
			itemsVideos.Effect = null;
		}

		private void LoadMetaVidsImpl()
		{
			filedb.Load();
			Dispatcher.Invoke(() => itemsAllTags.ItemsSource = filedb.GetAllTags());

			List<VideoEntry> entries = LoadVideos();
			Dispatcher.Invoke(() => itemsVideos.ItemsSource = entries);
		}

		private void RefreshVideos()
		{
			pnlLoading.Visibility = Visibility.Visible;
			itemsVideos.Effect = blur;

			bool opened = SelectedVideos.Count > 0;
			SelectedVideos.Clear();

			if (opened && SelectedVideos.Count == 0)
				statusBarAnimation.Close();

			Task.Factory.StartNew(() =>
			{
				List<VideoEntry> entries = LoadVideos();
				Dispatcher.Invoke(() =>
				{
					itemsVideos.ItemsSource = entries;
					pnlLoading.Visibility = Visibility.Hidden;
					itemsVideos.Effect = null;
				});
			});
		}

		private List<VideoEntry> LoadVideos()
		{
			int id = 0;
			int processed = 0;
			List<VideoEntry> thumbnails = new List<VideoEntry>();
			string[] files = Directory.GetFiles(settings.WorkingFolder);

			foreach (string path in files)
			{
				string ext = System.IO.Path.GetExtension(path);
				if (ext == ".avi" || ext == ".mp4" || ext == ".wmv" || ext == ".mkv" || ext == ".webm")
				{
					thumbnails.Add(new VideoEntry(id++, path, filedb.RetrieveMetadata(path)));
					allVideoFiles.Add(System.IO.Path.GetFileName(path));
				}
				Dispatcher.Invoke(() => prgCurrent.Value = ++processed * 100.0 / files.Length);
			}

			VideoEntry.SortVideoEntries(thumbnails);
			return thumbnails;
		}

		private void RefreshVideoSortAsync()
		{
			if (pnlLoading == null)
				return;

			pnlLoading.Visibility = Visibility.Visible;
			itemsVideos.Effect = blur;

			Task.Factory.StartNew(() =>
			{
				List<VideoEntry> thumbnails = new List<VideoEntry>((IEnumerable<VideoEntry>)itemsVideos.ItemsSource);
				VideoEntry.SortVideoEntries(thumbnails);
				Thread.Sleep(10);
				Dispatcher.Invoke(() =>
				{
					itemsVideos.ItemsSource = thumbnails;
					pnlLoading.Visibility = Visibility.Hidden;
					itemsVideos.Effect = null;
				});
			});
		}
		
		private string GetPrettySelectedSize()
		{
			int unitIndex = 0;
			double total = 0;

			foreach (VideoEntry ent in SelectedVideos)
				total += ent.Size;

			while (total > 1024 && unitIndex < sizeUnits.Length - 1)
			{
				unitIndex++;
				total /= 1024;
			}

			return ((int)total * 100) / 100.0 + sizeUnits[unitIndex];
		}

		#endregion

		#region Events

		private void WindowLoaded(object sender, RoutedEventArgs e)
		{
			LoadMetaAndVidsAsync();
			statusBarAnimation = new ExpandAnimation(pnlStatusBar);
			tagBarAnimation = new ExpandAnimation(pnlTags, 30);
		}

		private void Block_SelectionChanged(object sender, EventArgs e)
		{
			ThumbnailBlock block = sender as ThumbnailBlock;
			bool closed = SelectedVideos.Count == 0;

			if (block.IsChecked)
				SelectedVideos.Add((VideoEntry)block.Tag);
			else
				SelectedVideos.Remove((VideoEntry)block.Tag);

			if (SelectedVideos.Count == 0)
				statusBarAnimation.Close();
			else if (closed && SelectedVideos.Count > 0)
				statusBarAnimation.Open();
			
			tblStatusText.Text = "현재 " + SelectedVideos.Count + "개 선택됨 (" + GetPrettySelectedSize() + ")";
		}

		private void Block_ShiftClick(object sender, EventArgs e)
		{
			ThumbnailBlock block = sender as ThumbnailBlock;
			ShowMetadataPopup((VideoEntry)block.Tag);
		}

		private void Titlebar_MouseDown(object sender, MouseButtonEventArgs e)
		{
			DragMove();
		}

		private void Exit_Click(object sender, RoutedEventArgs e)
		{
			Application.Current.Shutdown();
		}

		private void WindowSizeChanged(object sender, SizeChangedEventArgs e)
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
			TagToggle((string)((Button)sender).Content);
		}

		private void ChangedWorkingFolder_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ClickCount == 2)
			{
				CommonOpenFileDialog dialog = new CommonOpenFileDialog();
				dialog.InitialDirectory = settings.WorkingFolder;
				dialog.IsFolderPicker = true;
				if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
				{
					settings.WorkingFolder = dialog.FileName;
					settings.Save();
					RefreshVideos();
				}
				e.Handled = true;
			}
		}

		private void ArrangeMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			RefreshVideoSortAsync();
			settings.Save();
		}

		private void MetadataPopup_Play_Click(object sender, RoutedEventArgs e)
		{
			Process.Start(((VideoEntry)pnlMetadata.Tag).Path);
		}

		private void MetadataPopup_Close_Click(object sender, RoutedEventArgs e)
		{
			pnlBody.Effect = null;
			pnlMetadata.Visibility = Visibility.Hidden;
		}

		private void AddTagbox_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				((VideoEntry)pnlMetadata.Tag).Metadata.AddTag(tbxAddTag.Text);
				CollectionViewSource.GetDefaultView(filedb.GetAllTags()).Refresh();
				tbxAddTag.Text = "";
				filedb.Save();
			}
		}

		private void MetaPanelInnerTag_Click(object sender, RoutedEventArgs e)
		{
			TagToggle((string)((Button)sender).Tag);
		}

		private void DeleteTag_Click(object sender, MouseButtonEventArgs e)
		{
			string tag = (string)((Image)sender).Tag;
			((VideoEntry)pnlMetadata.Tag).Metadata.RemoveTag(tag);
			CollectionViewSource.GetDefaultView(filedb.GetAllTags()).Refresh();
			e.Handled = true;
			filedb.Save();
		}

		private void WindowKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.F5)
				RefreshVideos();
		}
		
		private void DeleteSelected_Click(object sender, RoutedEventArgs e)
		{
			int selectedCount = SelectedVideos.Count;
			MessageBoxResult res = MessageBox.Show("선택된 " + selectedCount + "개의 파일이 영구적으로 삭제됩니다. 계속하시겠습니까?", "삭제 경고", MessageBoxButton.YesNo, MessageBoxImage.Warning);
			if(res == MessageBoxResult.Yes)
			{
				FileEditWindow.ShowFileDeleteDialog(SelectedVideos);
				RefreshVideos();
				MessageBox.Show(selectedCount + "개의 파일이 삭제되었습니다.");
			}
		}

		private void CopySelected_Click(object sender, RoutedEventArgs e)
		{
			CommonOpenFileDialog dialog = new CommonOpenFileDialog();
			dialog.Title = "동영상을 복사할 폴더 선택";
			dialog.IsFolderPicker = true;
			if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
				FileEditWindow.ShowFileCopyDialog(SelectedVideos, dialog.FileName);
		}

		#endregion
	}

	public class VideoEntry
	{
		public int Id { get; }
		public string Path { get; }
		public long Size { get; }
		public VideoMetadata Metadata { get; }

		private static readonly SortByModifyDate _sortRuleModifyDate = new SortByModifyDate();
		private static readonly SortByName _sortRuleName = new SortByName();
		private static readonly SortByCopyCount _sortRuleCopyCount = new SortByCopyCount();

		public VideoEntry(int id, string path, VideoMetadata meta)
		{
			Id = id;
			Path = path;
			Size = new FileInfo(path).Length;
			Metadata = meta;
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

		public static void SortVideoEntries(List<VideoEntry> list)
		{
			int mode = Properties.Settings.Default.ArrangeMode; // modifydata, name, copyed, shuffle
			switch (mode)
			{
				case 0:
					list.Sort(_sortRuleModifyDate);
					break;
				case 1:
					list.Sort(_sortRuleName);
					break;
				case 2:
					list.Sort(_sortRuleCopyCount);
					break;
				default:
					Random r = new Random();
					for (int i = list.Count - 1; i > 0; i--)
					{
						int k = r.Next(i + 1);
						VideoEntry temp = list[i];
						list[i] = list[k];
						list[k] = temp;
					}
					break;
			}
		}

		private class SortByName : IComparer<VideoEntry>
		{
			public int Compare(VideoEntry x, VideoEntry y)
			{
				string file1 = System.IO.Path.GetFileName(x.Path);
				string file2 = System.IO.Path.GetFileName(y.Path);
				return file1.CompareTo(file2);
			}
		}

		private class SortByModifyDate : IComparer<VideoEntry>
		{
			public int Compare(VideoEntry x, VideoEntry y)
			{
				return File.GetLastWriteTime(y.Path).CompareTo(File.GetLastWriteTime(x.Path));
			}
		}

		private class SortByCopyCount : IComparer<VideoEntry>
		{
			public int Compare(VideoEntry x, VideoEntry y)
			{
				return x.Metadata.CopyedCount - y.Metadata.CopyedCount;
			}
		}
	}
}
