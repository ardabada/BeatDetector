using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using System.Windows.Media.Animation;

namespace BeatDetectorTry
{
    /// <summary>
    /// Логика взаимодействия для BeatTester.xaml
    /// </summary>
    public partial class BeatTester : Window
    {
        public BeatTester()
        {
            InitializeComponent();

            //Background = new SolidColorBrush(Colors.Black);
            Background = new SolidColorBrush(Colors.White);
            //animateColors();
        }

        Color[] colors = new Color[]
        {
            Color.FromArgb(255, 255, 0, 0),
            Color.FromArgb(255, 0, 255, 0),
            Color.FromArgb(255, 0, 0, 255)
        };
        int currentColor = -1;

        void animateColors()
        {
            return;

            currentColor++;
            if (currentColor >= colors.Length || currentColor < 0)
                currentColor = 0;
            ColorAnimation animation = new ColorAnimation(colors[currentColor], TimeSpan.FromMilliseconds(500));
            animation.Completed += (s, e) => animateColors();
            Background.BeginAnimation(SolidColorBrush.ColorProperty, animation);
        }

        public void Toggle()
        {
            DoubleAnimation anim = new DoubleAnimation(0, TimeSpan.FromMilliseconds(100));
            anim.Completed += (s, e) =>
            {
                DoubleAnimation anim2 = new DoubleAnimation(1, TimeSpan.FromMilliseconds(100));
                blackGrid.BeginAnimation(OpacityProperty, anim2);
            };
            blackGrid.BeginAnimation(OpacityProperty, anim);
            //ColorAnimation animation = new ColorAnimation(Colors.Blue, TimeSpan.FromMilliseconds(100));
            //animation.Completed += Animation_Completed;
            //Background.BeginAnimation(SolidColorBrush.ColorProperty, animation);
        }

        private void Animation_Completed(object sender, EventArgs e)
        {
            ColorAnimation animation = new ColorAnimation(Colors.Black, TimeSpan.FromMilliseconds(100));
            Background.BeginAnimation(SolidColorBrush.ColorProperty, animation);
        }
    }
}
