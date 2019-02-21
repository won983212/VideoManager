using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Video_Manager
{
	public class FileDatabase
	{
		private Dictionary<string, int> allTagCounts = new Dictionary<string, int>();
		private Dictionary<string, VideoMetadata> database = new Dictionary<string, VideoMetadata>();
		private string dbPath = "";

		public VideoMetadata RetrieveMetadata(string filename)
		{
			if (database.ContainsKey(filename))
				return database[filename];

			VideoMetadata meta = new VideoMetadata(allTagCounts);
			meta.CopyedCount = 0;

			using (var shell = ShellObject.FromParsingName(filename))
			{
				IShellProperty property = shell.Properties.System.Media.Duration;
				ulong dura = (ulong)property.ValueAsObject;
				meta.Duration = TimeSpan.FromTicks((long)dura);
			}

			database.Add(filename, meta);
			return meta;
		}

		public IEnumerable<string> GetAllTags()
		{
			return allTagCounts.Keys;
		}

		public void SetWorkingFolder(string working_folder)
		{
			dbPath = Path.Combine(working_folder, ".videoinfo");
		}

		public void Load()
		{
			if (!File.Exists(dbPath)) return;
			using (FileStream fileStream = new FileStream(dbPath, FileMode.Open))
			{
				using (BinaryReader reader = new BinaryReader(fileStream))
				{
					int countOfDatas = reader.ReadInt32();
					for(int i = 0; i < countOfDatas; i++)
					{
						VideoMetadata meta = new VideoMetadata(allTagCounts);
						string key = reader.ReadString();
						meta.CopyedCount = reader.ReadInt32();
						meta.Duration = TimeSpan.FromTicks(reader.ReadInt64());

						int countOfTags = reader.ReadInt32();
						for (int j = 0; j < countOfTags; j++)
							meta.AddTag(reader.ReadString());
						database.Add(key, meta);
					}
				}
			}
		}

		public void Save()
		{
			if (!Directory.Exists(Directory.GetParent(dbPath).FullName)) return;
			using (FileStream fileStream = new FileStream(dbPath, FileMode.Create))
			{
				using (BinaryWriter writer = new BinaryWriter(fileStream))
				{
					writer.Write(database.Count);
					foreach (var ent in database)
					{
						writer.Write(ent.Key);
						writer.Write(ent.Value.CopyedCount);
						writer.Write(ent.Value.Duration.Ticks);
						writer.Write(ent.Value.Tags.Count());
						foreach (string tag in ent.Value.Tags)
							writer.Write(tag);
					}
				}
			}
		}
	}

	public class VideoMetadata
	{
		private Dictionary<string, int> allTagsRef;
		private ObservableCollection<string> tagList = new ObservableCollection<string>();

		public VideoMetadata(Dictionary<string, int> allTagsRef)
		{
			this.allTagsRef = allTagsRef;
		}

		public int CopyedCount { get; set; } = -1;
		public TimeSpan Duration { get; set; }
		public IEnumerable<string> Tags
		{
			get
			{
				return tagList;
			}
		}

		public void AddTag(string tag)
		{
			if (!tagList.Contains(tag))
			{
				tagList.Add(tag);
				if (allTagsRef.ContainsKey(tag))
					allTagsRef[tag]++;
				else
					allTagsRef.Add(tag, 1);
			}
		}

		public void RemoveTag(string tag)
		{
			if (tagList.Remove(tag))
			{
				if (allTagsRef[tag] > 1)
					allTagsRef[tag]--;
				else
					allTagsRef.Remove(tag);
			}
		}
	}
}
