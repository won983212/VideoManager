using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Video_Manager
{
    public class FileDatabase
    {
		private const char BlockBeginner = (char)31;
		private HashSet<string> allTags = new HashSet<string>();
		private Dictionary<string, VideoMetadata> database = new Dictionary<string, VideoMetadata>();
		private string dbPath = "";

		public VideoMetadata RetrieveMetadata(string filename)
		{
			if (database.ContainsKey(filename))
				return database[filename];

			VideoMetadata meta = new VideoMetadata(allTags);
			database.Add(filename, meta);
			return meta;
		}

		public IEnumerable<string> GetAllTags()
		{
			return allTags;
		}

		public void SetWorkingFolder(string working_folder)
		{
			dbPath = Path.Combine(working_folder, ".videoinfo");
		}

		public void Load()
		{
			if (!File.Exists(dbPath)) return;
			using(StreamReader reader = new StreamReader(dbPath))
			{
				string line = null;
				string file = null;
				VideoMetadata meta = null;

				while((line = reader.ReadLine()) != null)
				{
					if (line[0] == BlockBeginner)
					{
						file = line;
						if (file != null && meta != null)
							database.Add(file, meta);
						meta = new VideoMetadata(allTags);
					}
					else if(meta != null)
					{
						if(meta.CopyedCount == -1)
							meta.CopyedCount = int.Parse(line);
						else
							meta.AddTag(line);
					}
				}

				if (file != null && meta != null)
					database.Add(file, meta);
			}
		}

		public void Save()
		{
			if (!Directory.Exists(Directory.GetParent(dbPath).FullName)) return;
			using(StreamWriter writer = new StreamWriter(dbPath))
			{
				foreach(var ent in database)
				{
					writer.Write(BlockBeginner);
					writer.WriteLine(ent.Key);
					writer.WriteLine(ent.Value.CopyedCount);
					foreach (string tag in ent.Value.Tags)
						writer.WriteLine(tag);
				}
			}
		}
    }

	public class VideoMetadata
	{
		private HashSet<string> allTagsRef;
		private List<string> tagList = new List<string>();

		public VideoMetadata(HashSet<string> allTagsRef)
		{
			this.allTagsRef = allTagsRef;
		}

		public int CopyedCount { get; set; } = -1;
		public IEnumerable<string> Tags
		{
			get
			{
				return tagList;
			}
		}

		public void AddTag(string tag)
		{
			tagList.Add(tag);
			allTagsRef.Add(tag);
		}
	}
}
