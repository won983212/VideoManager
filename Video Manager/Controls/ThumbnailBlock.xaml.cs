using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Video_Manager.Controls
{
	/// <summary>
	/// ThumbnailBlock.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class ThumbnailBlock : UserControl, INotifyPropertyChanged
	{
		private string label = "00:00";

		public ThumbnailBlock()
		{
			InitializeComponent();
		}

		public bool IsCheckable { get; set; } = true;
		public bool IsChecked { get; set; } = false;
		public string TimeLabel { get => label; set { label = value; OnPropertyChanged("TimeLabel"); } }
		public event PropertyChangedEventHandler PropertyChanged;
		public event EventHandler SelectClick;
		public event EventHandler SelectionChanged;

		private void Background_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (SelectClick != null && e.ClickCount == 2)
				SelectClick(this, null);
			else if (IsCheckable)
			{
				IsChecked = !IsChecked;
				OnPropertyChanged("IsChecked");
				SelectionChanged?.Invoke(this, null);
			}
		}

		private void OnPropertyChanged(string name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}
	}
}
