using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Video_Manager
{
	public class VideoEntry : INotifyPropertyChanged
	{
		public const double FrameWidth = 130;
		public const double FrameHeight = 70;

		public int Id { get; }
		public string Path { get; }
		public long Size { get; }
		public VideoMetadata Metadata { get; }

		public string Length { get => Metadata.Duration.ToString("mm':'ss"); }
		public long LengthTicks { get => Metadata.Duration.Ticks; }
		public long LastModifiedTicks { get => File.GetLastWriteTime(Path).Ticks; }
		public int CopyedCount { get => Metadata.CopyedCount; }

		public BitmapSource Thumbnail
		{
			get
			{
				return _thumbnail;
			}
			set
			{
				_thumbnail = value;
				OnPropertyChanged(nameof(Thumbnail));
			}
		}
		
		private BitmapSource _thumbnail = null;

		public event PropertyChangedEventHandler PropertyChanged;

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
		
		public void LoadThumbnail()
		{
			string cacheParentPath = System.IO.Path.Combine(Directory.GetParent(Path).FullName, "thumbnails");
			string cachePath = System.IO.Path.Combine(cacheParentPath, System.IO.Path.GetFileNameWithoutExtension(Path));
			if (!Directory.Exists(cacheParentPath))
				Directory.CreateDirectory(cacheParentPath);

			BitmapSource img = null;
			if (File.Exists(cachePath))
			{
				BitmapImage imgsrc = new BitmapImage();
				imgsrc.BeginInit();
				imgsrc.UriSource = new Uri(cachePath);
				imgsrc.EndInit();
				img = imgsrc;
			}
			else
			{
				ShellFile sFile = ShellFile.FromFilePath(Path);
				img = sFile.Thumbnail.LargeBitmapSource;
				sFile.Dispose();

				JpegBitmapEncoder encoder = new JpegBitmapEncoder();
				encoder.Frames.Add(BitmapFrame.Create(img));
				using (FileStream fs = new FileStream(cachePath, FileMode.Create))
					encoder.Save(fs);
			}
			
			Thumbnail = img;
		}

		public bool HasTag(List<string> tags)
		{
			foreach (string tag in tags)
				if (!Metadata.Tags.Contains(tag)) return false;
			return true;
		}

		private void OnPropertyChanged(string name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}
	}
}
