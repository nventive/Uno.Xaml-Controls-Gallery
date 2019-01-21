using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using AppUIBasics.ControlPages;
using Windows.UI.Xaml.Media.Animation;

namespace AppUIBasics.ConnectedAnimationPages
{
    public sealed partial class DetailedInfoPage : Page
    {
        public CustomDataObject DetailedObject { get; set; }
        public DetailedInfoPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Store the item to be used in binding to UI
            DetailedObject = e.Parameter as CustomDataObject;

#if NETFX_CORE // UNO TODO
            ConnectedAnimation imageAnimation = ConnectedAnimationService.GetForCurrentView().GetAnimation("ForwardConnectedAnimation");
            if (imageAnimation != null)
            {
                // Connected animation + coordinated animation
                imageAnimation.TryStart(detailedImage, new UIElement[] { coordinatedPanel });

            }
#endif
        }

        // Create connected animation back to collection page.
        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);

#if NETFX_CORE // UNO TODO
            ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("BackConnectedAnimation", detailedImage);
#endif
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.GoBack();
        }
    }
}
