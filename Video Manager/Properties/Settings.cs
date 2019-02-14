using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Video_Manager.Properties
{
    partial class Settings
    {
		public Settings()
		{
			SettingsLoaded += Settings_SettingsLoaded;
		}

		private void Settings_SettingsLoaded(object sender, System.Configuration.SettingsLoadedEventArgs e)
		{
			if (!Directory.Exists(Default.WorkingFolder))
			{
				Default.WorkingFolder = Environment.GetFolderPath(Environment.SpecialFolder.CommonVideos);
				Default.Save();
			}
		}
	}
}
