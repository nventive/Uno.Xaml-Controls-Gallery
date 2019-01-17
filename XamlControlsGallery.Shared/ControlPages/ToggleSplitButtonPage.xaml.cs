using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Controls;

namespace AppUIBasics.ControlPages
{
    public sealed partial class ToggleSplitButtonPage : Page
    {
        private MarkerType _type = MarkerType.Bullet;
        public ToggleSplitButtonPage()
        {
            this.InitializeComponent();
        }

        private void myListButton_Click(Windows.UI.Xaml.Controls.SplitButton sender, Windows.UI.Xaml.Controls.SplitButtonClickEventArgs args)
        {
#if NETFX_CORE
            if ((sender as Windows.UI.Xaml.Controls.ToggleSplitButton).IsChecked)
            {
                //add bulleted list
                myRichEditBox.Document.Selection.ParagraphFormat.ListType = _type;                
            }
            else
            {
                //remove bulleted list
                myRichEditBox.Document.Selection.ParagraphFormat.ListType = MarkerType.None;
            }      
#endif
        }

        private void BulletButton_Click(object sender, RoutedEventArgs e)
        {
            Button clickedBullet = (Button)sender;
            SymbolIcon symbol = (SymbolIcon)clickedBullet.Content;

            if (symbol.Symbol == Symbol.List)
            {
                _type = MarkerType.Bullet;
                mySymbolIcon.Symbol = Symbol.List;
                myListButton.SetValue(AutomationProperties.NameProperty, "Bullets");
            }
            else if (symbol.Symbol == Symbol.Bullets)
            {
                _type = MarkerType.UppercaseRoman;
                mySymbolIcon.Symbol = Symbol.Bullets;
                myListButton.SetValue(AutomationProperties.NameProperty, "Roman Numerals");
            }

#if NETFX_CORE
            myRichEditBox.Document.Selection.ParagraphFormat.ListType = _type;
#endif

            myListButton.IsChecked = true;
            myListButton.Flyout.Hide();

#if NETFX_CORE // UNO TODO
            myRichEditBox.Focus(FocusState.Keyboard);
#endif
        }
    }
}
