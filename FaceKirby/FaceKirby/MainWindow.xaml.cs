using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using KLibrary.Labs.UI;
using KLibrary.Labs.UI.Input;
using Reactive.Bindings;

namespace FaceKirby
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        static readonly SolidColorBrush NormalBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#99FF9933"));
        static readonly SolidColorBrush PressedBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#99FF3300"));

        static readonly Dictionary<string, Keys> KeysMap = new Dictionary<string, Keys>
        {
            { "⬆", Keys.I },
            { "⬇", Keys.M },
            { "⬅", Keys.J },
            { "➡", Keys.K },
            { "SELECT", Keys.A },
            { "START", Keys.Return },
            { "B", Keys.Z },
            { "A", Keys.X },
        };

        static readonly Dictionary<string, string> KirbyButtonsMap = new Dictionary<string, string>
        {
            { "進", "➡" },
            { "退", "⬅" },
            { "翔", "⬆" },
            { "跳", "A" },
            { "屈", "⬇" },
            { "吸", "B" },
            { "扉", "⬆" },
        };

        Dictionary<string, ReactiveProperty<bool>> KeysStates;
        Dictionary<string, ReactiveProperty<bool>> KirbyButtonsStates;

        AppModel appModel = new AppModel();

        public MainWindow()
        {
            InitializeComponent();

            this.SetBorderless();
            this.SetInactive();
            MouseLeftButtonDown += (o, e) => DragMove();

            KeysStates = KeysMap.Keys.ToDictionary(x => x, x => new ReactiveProperty<bool>());
            KirbyButtonsStates = KirbyButtonsMap.Keys.ToDictionary(x => x, x => new ReactiveProperty<bool>());

            var gamepadButtons = GamepadButtonsPanel.Children.OfType<TextBlock>();
            foreach (var button in gamepadButtons)
            {
                var key = KeysMap[button.Text];
                var pressed = KeysStates[button.Text];

                pressed
                    .ObserveOnDispatcher()
                    .Do(b => button.Background = b ? PressedBrush : NormalBrush)
                    .Subscribe(b =>
                    {
                        if (b)
                            KeyboardInjection.KeyDown(key);
                        else
                            KeyboardInjection.KeyUp(key);
                    });

                button.TouchDown += (o, e) => pressed.Value = true;
                button.TouchUp += (o, e) => pressed.Value = false;
            }

            var kirbyButtons = KirbyButtonsPanel.Children.OfType<TextBlock>();
            foreach (var button in kirbyButtons)
            {
                var key = KeysStates[KirbyButtonsMap[button.Text]];
                var pressed = KirbyButtonsStates[button.Text];

                pressed
                    .ObserveOnDispatcher()
                    .Do(b => button.Background = b ? PressedBrush : NormalBrush)
                    .Subscribe(b => key.Value = b);

                button.TouchDown += (o, e) => pressed.Value = true;
                button.TouchUp += (o, e) => pressed.Value = false;
            }

            appModel.IsRightOriented.Subscribe(b => KirbyButtonsStates["進"].Value = b);
            appModel.IsLeftOriented.Subscribe(b => KirbyButtonsStates["退"].Value = b);
            appModel.AreHandsAbove.Subscribe(b => KirbyButtonsStates["翔"].Value = b);
            appModel.IsJumping.Subscribe(b => KirbyButtonsStates["跳"].Value = b);
            appModel.IsSquat.Subscribe(b => KirbyButtonsStates["屈"].Value = b);
            appModel.IsMouthOpen.Subscribe(b => KirbyButtonsStates["吸"].Value = b);
            appModel.IsHandHit.Subscribe(b => KirbyButtonsStates["扉"].Value = b);
        }
    }
}
