using IVSoftware.Portable.Threading;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using static IVSoftware.Portable.Threading.Extensions;
using Color = System.Drawing.Color;
using RichTextBox = System.Windows.Forms.RichTextBox;

[assembly: InternalsVisibleTo("MSTest.Async.Demo")]

namespace wpf_app_test_async_void_methods
{
    public enum RuntimeMode
    {
        Production,
        UnitTest,
    }
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            base.DataContext = new MainWindowBindingContext();
            Loaded += (sender, e) =>
            {
                switch (DataContext.RuntimeMode)
                {
                    case RuntimeMode.Production:
                        RichTextBox.AppendText($"Running in {DataContext.RuntimeMode} mode.", Color.Green);
                        break;
                    case RuntimeMode.UnitTest:
                        RichTextBox.AppendText($"Running in {DataContext.RuntimeMode} mode.", Color.Red);

                        // Subscribe to visibility changes of start button.
                        var descriptor = DependencyPropertyDescriptor.FromProperty(VisibilityProperty, typeof(System.Windows.Controls.Button));
                        descriptor.AddValueChanged(buttonStartTest, OnStartButtonVisibilityChanged);
                        // Hide the start button when clicked.
                        buttonStartTest.Click += (sender, e) => buttonStartTest.Visibility = Visibility.Collapsed;

                        // Make A, B, C, D button clicks testable.
                        buttonA.Click += (sender, e) => sender.OnAwaited(new AwaitedEventArgs());
                        buttonB.Click += (sender, e) => sender.OnAwaited(new AwaitedEventArgs());
                        buttonC.Click += (sender, e) => sender.OnAwaited(new AwaitedEventArgs());
                        buttonD.Click += (sender, e) => sender.OnAwaited(new AwaitedEventArgs());
                        break;
                    default:
                        Debug.Fail("Unexpected");
                        break;
                }
            };
        }
        void OnStartButtonVisibilityChanged(object? sender, EventArgs e)
        {
            buttonStartTest.OnAwaited(new AwaitedEventArgs(args: new Dictionary<string, object>
            {
                {nameof(buttonStartTest.Visibility), Visibility.Visible },
            }));
        }

        new MainWindowBindingContext DataContext => (MainWindowBindingContext)base.DataContext;

        public void PromptInRichTextBox(string prompt, Color? color = null, bool newline = true) =>
            Dispatcher.BeginInvoke(()=> RichTextBox.AppendText(prompt, color, newline));

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (DataContext?.RuntimeMode == RuntimeMode.UnitTest)
            {
                this.OnAwaited(new AwaitedEventArgs(args: new Dictionary<string, object>
                {
                    { nameof(DependencyPropertyChangedEventArgs), e }
                }));
            }
        }
        RichTextBox RichTextBox => (RichTextBox)RichTextBoxHost.Child;

        public RuntimeMode RuntimeMode 
        {
            set => DataContext.RuntimeMode = value;
        }
        public void ShowTestButton()
        {
            Dispatcher.BeginInvoke(()=>
                buttonStartTest.Visibility = Visibility.Visible);
        }

        public async void CallSomeAsyncVoidMethod()
        {
            // Demonstration only
            await Task.Delay(TimeSpan.FromSeconds(2.5));
            await Dispatcher.BeginInvoke(() =>
                MyTargetProperty = "MyExpectedValue");
        }
        public string MyTargetProperty
        {
            get => _myTargetProperty;
            set
            {
                // For demo, fire unconditionally
                var oldValue = MyTargetProperty;
                _myTargetProperty = value;
                OnPropertyChanged(new DependencyPropertyChangedEventArgs(
                    DependencyProperty.Register(nameof(MyTargetProperty), typeof(string), typeof(MainWindow)),
                    oldValue,
                    _myTargetProperty));
            }
        }
        string _myTargetProperty = string.Empty;

    }
    class MainWindowBindingContext : INotifyPropertyChanged
    {
        public RuntimeMode RuntimeMode
        {
            get => _runtimeMode;
            set
            {
                if (!Equals(_runtimeMode, value))
                {
                    _runtimeMode = value;
                    OnPropertyChanged();
                }
            }
        }
        RuntimeMode _runtimeMode = default;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        public event PropertyChangedEventHandler? PropertyChanged;
    }

    static partial class Extensions
    {
        public static void AppendText(this RichTextBox richTextBox, string text,
                                  Color? color = null, bool newline = true)
        {
            var colorB4 = richTextBox.SelectionColor;
            richTextBox.SelectionColor = color ?? Color.Black;
            richTextBox.AppendText(text);
            if(newline)richTextBox.AppendText(Environment.NewLine);
            richTextBox.SelectionColor = colorB4;
            richTextBox.Select(richTextBox.TextLength, 0);
            richTextBox.ScrollToCaret();
        }
    }
}