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

		public bool IsChecked { get; set; } = false;
		public string TimeLabel { get => label; set { label = value; OnPropertyChanged("TimeLabel"); } }
		public event PropertyChangedEventHandler PropertyChanged;

		private void Background_MouseDown(object sender, MouseButtonEventArgs e)
		{
			IsChecked = !IsChecked;
			OnPropertyChanged("IsChecked");
		}

		private void OnPropertyChanged(string name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}
	}
}
