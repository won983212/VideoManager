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
		public static DependencyProperty ImageBitmapProperty
			= DependencyProperty.Register("ImageBitmap", typeof(ImageSource), typeof(ThumbnailBlock));
		public static DependencyProperty TimeLabelProperty
			= DependencyProperty.Register("TimeLabel", typeof(string), typeof(ThumbnailBlock));

		public ThumbnailBlock()
		{
			InitializeComponent();
		}
		
		public bool IsCheckable { get; set; } = true;
		public bool IsChecked { get; set; } = false;
		public ImageSource ImageBitmap
		{
			get => (ImageSource)GetValue(ImageBitmapProperty);
			set => SetValue(ImageBitmapProperty, value);
		}
		public string TimeLabel
		{
			get => (string)GetValue(TimeLabelProperty);
			set => SetValue(TimeLabelProperty, value);
		}

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
