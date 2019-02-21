using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.WindowsAPICodePack.Shell;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
		private List<VideoEntry> allVideos = null;

		private ExpandAnimation statusBarAnimation;
		private ExpandAnimation tagBarAnimation;
		private FileDatabase filedb = new FileDatabase();
		private DispatcherTimer resizeTimer = new DispatcherTimer();
		private CollectionViewSource contentColView = null;

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

			RefreshVideos();
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
			blkVideoThumbnail.TimeLabel = vid.Length;
			blkVideoThumbnail.ImageBitmap = vid.Thumbnail;
		}
		
		private async void LoadAllAsync()
		{
			itemsVideos.Effect = blur;
			await Task.Factory.StartNew(LoadAllImpl);
			pnlLoading.Visibility = Visibility.Hidden;
			itemsVideos.Effect = null;
		}

		private void LoadAllImpl()
		{
			filedb.Load();
			Dispatcher.Invoke(() => itemsAllTags.ItemsSource = filedb.GetAllTags());

			IEnumerable entries = LoadVideos();
			Dispatcher.Invoke(() => itemsVideos.ItemsSource = entries);
		}

		private void RefreshVideos()
		{
			if (pnlLoading == null) return;

			pnlLoading.Visibility = Visibility.Visible;
			itemsVideos.Effect = blur;

			bool opened = SelectedVideos.Count > 0;
			SelectedVideos.Clear();

			if (opened && SelectedVideos.Count == 0)
				statusBarAnimation.Close();

			Task.Factory.StartNew(() =>
			{
				IEnumerable entries = LoadVideos();
				Dispatcher.Invoke(() =>
				{
					itemsVideos.ItemsSource = entries;
					pnlLoading.Visibility = Visibility.Hidden;
					itemsVideos.Effect = null;
				});
			});
		}

		private void CreateAllVideoEntries(string[] files)
		{
			int id = 0;
			int processed = 0;

			allVideos = new List<VideoEntry>();
			foreach (string path in files)
			{
				string ext = System.IO.Path.GetExtension(path);
				if (ext == ".avi" || ext == ".mp4" || ext == ".wmv" || ext == ".mkv" || ext == ".webm")
				{
					VideoEntry vid = new VideoEntry(id++, path, filedb.RetrieveMetadata(path));
					Dispatcher.Invoke(() => vid.LoadThumbnail());
					allVideos.Add(vid);
				}
				Dispatcher.Invoke(() => prgCurrent.Value = ++processed * 100.0 / files.Length);
			}
		}

		private IEnumerable LoadVideos()
		{
			CollectionViewSource colView = new CollectionViewSource();
			ObservableCollection<VideoEntry> thumbnails = new ObservableCollection<VideoEntry>();
			string[] files = Directory.GetFiles(settings.WorkingFolder);

			if (allVideos == null || allVideos.Count != files.Length)
				CreateAllVideoEntries(files);

			int processed = 0;
			foreach (VideoEntry ent in allVideos)
			{
				if (selectedTags.Count == 0 || ent.HasTag(selectedTags))
					thumbnails.Add(ent);
				Dispatcher.Invoke(() => prgCurrent.Value = ++processed * 100.0 / allVideos.Count);
			}

			IEnumerable ret = null;
			if (settings.ArrangeMode == 4)
			{
				Random r = new Random();
				for(int i = thumbnails.Count - 1; i > 0; i--)
				{
					int k = r.Next(i + 1);
					VideoEntry temp = thumbnails[i];
					thumbnails[i] = thumbnails[k];
					thumbnails[k] = temp;
				}
				ret = thumbnails;
			}
			else
			{
				colView.Source = thumbnails;
				contentColView = colView;
				switch (settings.ArrangeMode)
				{
					case 0:
						contentColView.SortDescriptions.Add(new System.ComponentModel.SortDescription("LastModifiedTicks", System.ComponentModel.ListSortDirection.Descending));
						break;
					case 1:
						contentColView.SortDescriptions.Add(new System.ComponentModel.SortDescription("LengthTicks", System.ComponentModel.ListSortDirection.Ascending));
						break;
					case 3:
						contentColView.SortDescriptions.Add(new System.ComponentModel.SortDescription("CopyedCount", System.ComponentModel.ListSortDirection.Descending));
						break;
				}
				contentColView.SortDescriptions.Add(new System.ComponentModel.SortDescription("Path", System.ComponentModel.ListSortDirection.Ascending));
				ret = colView.View;
			}

			filedb.Save();
			return ret;
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
			LoadAllAsync();
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

		private void Block_SelectClick(object sender, EventArgs e)
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
					LoadAllAsync();
				}
				e.Handled = true;
			}
		}

		private void ArrangeMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			settings.Save();
			RefreshVideos();
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
			if (res == MessageBoxResult.Yes)
			{
				FileEditWindow.ShowFileDeleteDialog(SelectedVideos);
				RefreshVideos();
				MessageBox.Show(selectedCount + "개의 파일이 삭제되었습니다.", "삭제됨", MessageBoxButton.OK, MessageBoxImage.Information);
			}
		}

		private void CopySelected_Click(object sender, RoutedEventArgs e)
		{
			CommonOpenFileDialog dialog = new CommonOpenFileDialog();
			dialog.Title = "동영상을 복사할 폴더 선택";
			dialog.IsFolderPicker = true;
			if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
			{
				FileEditWindow.ShowFileCopyDialog(filedb, SelectedVideos, dialog.FileName);
				MessageBox.Show(SelectedVideos.Count + "개의 파일이 복사되었습니다.", "복사됨", MessageBoxButton.OK, MessageBoxImage.Information);
			}
		}

		#endregion
	}
}
