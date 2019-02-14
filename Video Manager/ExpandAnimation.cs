using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

namespace Video_Manager
{
	public class ExpandAnimation
	{
		private Storyboard openAnimation;
		private Storyboard closeAnimation;
		private FrameworkElement element;

		public ExpandAnimation(FrameworkElement element) : this(element, -1)
		{ }

		public ExpandAnimation(FrameworkElement element, double initialHeight)
		{
			this.element = element;
			openAnimation = CreateStoryboard(true, initialHeight);
			closeAnimation = CreateStoryboard(false, initialHeight);
			element.Height = 0;
		}

		private Storyboard CreateStoryboard(bool open, double initialHeight)
		{
			double refHeight = initialHeight == -1 ? element.ActualHeight : initialHeight;
			QuadraticEase ease = new QuadraticEase();
			ease.EasingMode = open ? EasingMode.EaseOut : EasingMode.EaseIn;
			ease.Freeze();

			DoubleAnimation animation = new DoubleAnimation();
			animation.From = open ? 0 : refHeight;
			animation.To = open ? refHeight : 0;
			animation.Duration = new Duration(TimeSpan.FromMilliseconds(300));
			animation.EasingFunction = ease;

			Storyboard s = new Storyboard();
			s.Children.Add(animation);
			Storyboard.SetTargetName(animation, element.Name);
			Storyboard.SetTargetProperty(animation, new PropertyPath(FrameworkElement.HeightProperty));

			return s;
		}

		public void Open()
		{
			openAnimation.Begin(element);
		}

		public void Close()
		{
			closeAnimation.Begin(element);
		}
	}
}
