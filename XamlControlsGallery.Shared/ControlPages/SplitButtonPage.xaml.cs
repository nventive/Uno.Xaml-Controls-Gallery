using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace AppUIBasics.ControlPages
{
    public sealed partial class SplitButtonPage : Page
    {
        public SplitButtonPage()
        {
            this.InitializeComponent();
        }

        private void ColorButton_Click(object sender, RoutedEventArgs e)
        {
            // Extract the color of the button that was clicked.
            Button clickedColor = (Button)sender;
            var rectangle = (Windows.UI.Xaml.Shapes.Rectangle)clickedColor.Content;
            var color = ((Windows.UI.Xaml.Media.SolidColorBrush)rectangle.Fill).Color;

#if NETFX_CORE // UNO TODO
            myRichEditBox.Document.Selection.CharacterFormat.ForegroundColor = color;
#endif
            CurrentColor.Fill = new SolidColorBrush(color);

            myColorButton.Flyout.Hide();

#if NETFX_CORE // UNO TODO
            myRichEditBox.Focus(Windows.UI.Xaml.FocusState.Keyboard);
#endif
        }

        private void myColorButton_Click(Windows.UI.Xaml.Controls.SplitButton sender, Windows.UI.Xaml.Controls.SplitButtonClickEventArgs args)
        {
            var rectangle = (Windows.UI.Xaml.Shapes.Rectangle)sender.Content;
            var color = ((Windows.UI.Xaml.Media.SolidColorBrush)rectangle.Fill).Color;

#if NETFX_CORE // UNO TODO
            myRichEditBox.Document.Selection.CharacterFormat.ForegroundColor = color;
#endif
        }
    }
}
