using System;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Media.Animation;
using System.Windows;

namespace BeatDetectorHost
{
    public partial class OverlayWindow : Window
    {
        DispatcherTimer timer, infoTimer;
        DateTime lastInfoShown;
        double blackAppearAnimationTime = 100;
        double blackWaitAnimationTime = 0;

        public event OnTimerRequestedEventHandler OnTimerChangeRequested;
        public event OnAverageCountersRequestedEventHandler OnAverageCountersRequested;
        public event EventHandler ToggleManualPeak;
        public event OnManualPeakEventHandler OnManualPeak;


        public OverlayWindow()
        {
            InitializeComponent();

            Topmost = false;

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(2.5);
            timer.Tick += Timer_Tick;

            infoTimer = new DispatcherTimer();
            infoTimer.Interval = TimeSpan.FromSeconds(1);
            infoTimer.Tick += InfoTimer_Tick;

            Loaded += (s, e) =>
            {
                alignInfoLbl();
            };

            Background = new SolidColorBrush(Colors.Black);
            SetAnimation(OverlayAnimationType.Default);
        }

        bool _canExit = false;
        public void SetWindowStyle()
        {
            AllowsTransparency = false;
            WindowStyle = WindowStyle.SingleBorderWindow;
            _canExit = false;
        }
        public void SetOverlayStyle()
        {
            AllowsTransparency = true;
            WindowStyle = WindowStyle.None;
            _canExit = false;
        }
        
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F1:
                    if (this.Opacity > 0.5)
                        this.Opacity -= 0.1;
                    Cursor = Cursors.Arrow;
                    startIdleWait();
                    SetInfo("Opacity decreased");
                    break;
                case Key.F2:
                    if (this.Opacity < 1)
                        this.Opacity += 0.1;
                    Cursor = Cursors.Arrow;
                    startIdleWait();
                    SetInfo("Opacity increased");
                    break;
                case Key.F3:
                    if (Opacity == 1)
                        Opacity = 0.9;
                    this.ToTransparentWindow();
                    Cursor = Cursors.Arrow;
                    startIdleWait();
                    SetInfo("Overlay enabled");
                    break;
                case Key.F4:
                    setBeatWindow();
                    break;

                case Key.D0:
                    SetAnimation(OverlayAnimationType.Default);
                    break;
                case Key.D1:
                    SetAnimation(OverlayAnimationType.WhiteColor);
                    break;
                case Key.D2:
                    SetAnimation(OverlayAnimationType.RedColor);
                    break;
                case Key.D3:
                    SetAnimation(OverlayAnimationType.GreenColor);
                    break;
                case Key.D4:
                    SetAnimation(OverlayAnimationType.BlueColor);
                    break;
                case Key.D5:
                    SetAnimation(OverlayAnimationType.YellowColor);
                    break;
                case Key.D6:
                    SetAnimation(OverlayAnimationType.PinkColor);
                    break;
                case Key.D7:
                    SetAnimation(OverlayAnimationType.RandomColor);
                    break;

                case Key.Q:
                    changeAppearBlackAnimation(-50);
                    break;
                case Key.W:
                    changeAppearBlackAnimation(+50);
                    break;
                case Key.A:
                    changeDelayBlackAnimation(-50);
                    break;
                case Key.S:
                    changeDelayBlackAnimation(+50);
                    break;

                case Key.Z:
                    OnTimerChangeRequested?.Invoke(-1);
                    break;
                case Key.X:
                    OnTimerChangeRequested?.Invoke(+1);
                    break;
                case Key.C:
                    OnAverageCountersRequested?.Invoke(-1);
                    break;
                case Key.V:
                    OnAverageCountersRequested?.Invoke(+1);
                    break;

                case Key.M:
                    ToggleManualPeak?.Invoke(this, EventArgs.Empty);
                    break;
                case Key.OemComma:
                    if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                        OnManualPeak?.Invoke(-0.05);
                    else OnManualPeak?.Invoke(-0.01);
                    break;
                case Key.OemPeriod:
                    if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                        OnManualPeak?.Invoke(+0.05);
                    else OnManualPeak?.Invoke(+0.01);
                    break;
            }
        }
        private void Timer_Tick(object sender, EventArgs e)
        {
            if (Win32.GetIdleTime() > 5000)
                setBeatWindow();
        }

        void setBeatWindow()
        {
            this.ToNormalWindow();
            Opacity = 1;
            Cursor = Cursors.None;
            Win32.SetCursorPos((int)Width / 2, (int)Height / 2);
            stopIdleWait();
            Activate();
            Topmost = true;
            WindowState = WindowState.Maximized;
            Focus();
            SetInfo("Overlay disabled");
        }

        void startIdleWait()
        {
            if (!timer.IsEnabled)
                timer.Start();
        }
        void stopIdleWait()
        {
            if (timer.IsEnabled)
                timer.Stop();
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
            if (overlayAnimationType != OverlayAnimationType.Default)
            {
                SetAnimation(overlayAnimationType);
                return;
            }
            currentColor++;
            if (currentColor >= colors.Length || currentColor < 0)
                currentColor = 0;
            ColorAnimation animation = new ColorAnimation(colors[currentColor], TimeSpan.FromMilliseconds(500));
            animation.Completed += (s, e) => animateColors();
            Background.BeginAnimation(SolidColorBrush.ColorProperty, animation);
        }

        public void Toggle()
        {
            if (overlayAnimationType == OverlayAnimationType.RandomColor)
                setRandomBackground();
            DoubleAnimation anim = new DoubleAnimation(0, TimeSpan.FromMilliseconds(blackAppearAnimationTime));
            anim.Completed += (s, e) =>
            {
                DoubleAnimation anim2 = new DoubleAnimation(1, TimeSpan.FromMilliseconds(blackAppearAnimationTime));
                anim2.BeginTime = TimeSpan.FromMilliseconds(blackWaitAnimationTime);
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

        void alignInfoLbl()
        {
            double leftMargin = 0, topMargin = 0;
            if (Left < 0)
                leftMargin = -Left;
            if (Top < 0)
                topMargin = -Top;
            infoLbl.Margin = new Thickness(leftMargin, topMargin, 0, 0);
        }
        public void ForceClose()
        {
            _canExit = true;
            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = !_canExit;
        }

        private bool _showInfo = true;
        public bool ShowInfo
        {
            get { return _showInfo; }
            set
            {
                _showInfo = value;
                if (!_showInfo)
                    infoLbl.Visibility = Visibility.Hidden;
            }
        }

        public void SetInfo(string text)
        {
            infoLbl.Text = text;
            lastInfoShown = DateTime.Now;
            if (ShowInfo)
            {
                infoLbl.Visibility = Visibility.Visible;
                if (!infoTimer.IsEnabled)
                    infoTimer.Start();
            }
        }
        
        private void InfoTimer_Tick(object sender, EventArgs e)
        {
            if (DateTime.Now - lastInfoShown >= TimeSpan.FromSeconds(2))
            {
                infoLbl.Visibility = Visibility.Hidden;
                infoTimer.Stop();
            }
        }

        private OverlayAnimationType overlayAnimationType = OverlayAnimationType.Default;

        public void SetAnimation(OverlayAnimationType type)
        {
            overlayAnimationType = type;
            switch (type)
            {
                case OverlayAnimationType.WhiteColor:
                    Background = new SolidColorBrush(Colors.White);
                    SetInfo("White background applied");
                    break;
                case OverlayAnimationType.RedColor:
                    Background = new SolidColorBrush(Colors.Red);
                    SetInfo("Red background applied");
                    break;
                case OverlayAnimationType.GreenColor:
                    Background = new SolidColorBrush(Colors.Lime);
                    SetInfo("Green background applied");
                    break;
                case OverlayAnimationType.BlueColor:
                    Background = new SolidColorBrush(Colors.Blue);
                    SetInfo("Blue background applied");
                    break;
                case OverlayAnimationType.YellowColor:
                    Background = new SolidColorBrush(Colors.Yellow);
                    SetInfo("Yellow background applied");
                    break;
                case OverlayAnimationType.PinkColor:
                    Background = new SolidColorBrush(Colors.Magenta);
                    SetInfo("Pink background applied");
                    break;
                case OverlayAnimationType.RandomColor:
                    //Background = new SolidColorBrush(Colors.White);
                    SetInfo("Random background applied");
                    break;
                default:
                    animateColors();
                    SetInfo("Default animation applied");
                    break;
            }
        }

        void setRandomBackground()
        {
            var r = (byte)Helpers.random.Next(0, 256);
            var g = (byte)Helpers.random.Next(0, 256);
            var b = (byte)Helpers.random.Next(0, 256);
            var luma = 0.2126 * r + 0.7152 * g + 0.0722 * b;
            if (luma < 40)
                setRandomBackground();
            else
                Background = new SolidColorBrush(Color.FromArgb(255, r, g, b));
        }
        void changeAppearBlackAnimation(double value)
        {
            blackAppearAnimationTime += value;
            if (blackAppearAnimationTime < 0)
                blackAppearAnimationTime = 0;
            else if (blackAppearAnimationTime > 1000)
                blackAppearAnimationTime = 1000;
            SetInfo("Appearance animation time updated: " + blackAppearAnimationTime);
        }
        void changeDelayBlackAnimation(double value)
        {
            blackWaitAnimationTime += value;
            if (blackWaitAnimationTime < 0)
                blackWaitAnimationTime = 0;
            else if (blackWaitAnimationTime > 1000)
                blackWaitAnimationTime = 1000;
            SetInfo("Delay animation time updated: " + blackWaitAnimationTime);
        }
    }
}