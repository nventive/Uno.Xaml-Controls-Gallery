//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************
using AppUIBasics.Common;
using AppUIBasics.Data;
using System;
using System.Linq;
using System.Numerics;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.System;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace AppUIBasics
{
    /// <summary>
    /// A page that displays details for a single item within a group.
    /// </summary>
    public partial class ItemPage : Page
    {
        private Compositor _compositor;
        private ElementTheme? _currentElementTheme;


        // UNO TODO x:Bind evaluation sequence is incorrect for properties
        public ControlInfoDataItem Item
        {
            get => (ControlInfoDataItem)GetValue(ItemProperty);
            set => SetValue(ItemProperty, value);
        }

        public static readonly DependencyProperty ItemProperty =
            DependencyProperty.Register("Item", typeof(ControlInfoDataItem), typeof(ItemPage), new PropertyMetadata(null));

        public ItemPage()
        {
            this.InitializeComponent();

            LayoutVisualStates.CurrentStateChanged += (s, e) => UpdateSeeAlsoPanelVerticalTranslationAnimation();
            Loaded += (s,e) => SetInitialVisuals();
            contentFrame.NavigationFailed += ContentFrame_NavigationFailed;
        }

        private void ContentFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            string errorString = $"Navigation to {e.SourcePageType} failed. {e.Exception}";
            Console.WriteLine(errorString);
            contentFrame.Content = new TextBlock { Text = errorString, TextWrapping = TextWrapping.Wrap };
        }

        public void SetInitialVisuals()
        {
            NavigationRootPage.Current.PageHeader.TopCommandBar.Visibility = Visibility.Visible;
            NavigationRootPage.Current.PageHeader.ToggleThemeAction = OnToggleTheme;

            if (NavigationRootPage.Current.IsFocusSupported)
            {
                this.Focus(FocusState.Programmatic);
            }

            _compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;

            UpdateSeeAlsoPanelVerticalTranslationAnimation();
        }

        private void UpdateSeeAlsoPanelVerticalTranslationAnimation()
        {
#if NETFX_CORE
            var isEnabled = LayoutVisualStates.CurrentState == LargeLayout;

            ElementCompositionPreview.SetIsTranslationEnabled(seeAlsoPanel, true);

            var targetPanelVisual = ElementCompositionPreview.GetElementVisual(seeAlsoPanel);
            targetPanelVisual.Properties.InsertVector3("Translation", Vector3.Zero);

            if (isEnabled)
            {
                var scrollProperties = ElementCompositionPreview.GetScrollViewerManipulationPropertySet(svPanel);

                var expression = _compositor.CreateExpressionAnimation("ScrollManipulation.Translation.Y * -1");
                expression.SetReferenceParameter("ScrollManipulation", scrollProperties);
                expression.Target = "Translation.Y";
                targetPanelVisual.StartAnimation(expression.Target, expression);
            }
            else
            {
                targetPanelVisual.StopAnimation("Translation.Y");
            }
#endif
        }

        private void OnToggleTheme()
        {
            var currentElementTheme = ((_currentElementTheme ?? ElementTheme.Default) == ElementTheme.Default) ? App.ActualTheme : _currentElementTheme.Value;
            var newTheme = currentElementTheme == ElementTheme.Dark ? ElementTheme.Light : ElementTheme.Dark;
            SetControlExamplesTheme(newTheme);
        }

        private void SetControlExamplesTheme(ElementTheme theme)
        {
#if NETFX_CORE // UNO TODO
            var controlExamples = (this.contentFrame.Content as UIElement)?.GetDescendantsOfType<ControlExample>();

            if (controlExamples != null)
            {
                _currentElementTheme = theme;
                foreach (var controlExample in controlExamples)
                {
                    var exampleContent = controlExample.Example as FrameworkElement;
                    exampleContent.RequestedTheme = theme;
                    controlExample.ExampleContainer.RequestedTheme = theme;
                }
            }
#endif
        }

        private void OnRelatedControlClick(object sender, RoutedEventArgs e)
        {
            ButtonBase b = (ButtonBase)sender;

            this.Frame.Navigate(typeof(ItemPage), b.DataContext.ToString());
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            var item = await ControlInfoDataSource.Instance.GetItemAsync((String)e.Parameter);

            if (item != null)
            {
                Item = item;

                // Load control page into frame.
                var loader = ResourceLoader.GetForCurrentView();

#if NETFX_CORE
				string pageRoot = loader.GetString("PageStringRoot");
#else
                // UNO TODO, path parsing for resource files is incorrect
                string pageRoot = typeof(ControlPages.BorderPage).Namespace + ".";
#endif
                // Mono-wasm requires a fully qualified assembly name
                string pageString = pageRoot + item.UniqueId + "Page, " + typeof(ControlPages.BorderPage).Assembly.FullName;
                Type pageType = Type.GetType(pageString);

                if (pageType != null)
                {
                    this.contentFrame.Navigate(pageType);
                }

                NavigationRootPage.Current.NavigationView.Header = item?.Title;
                if (item.IsNew && NavigationRootPage.Current.CheckNewControlSelected())
                {
                    PlayConnectedAnimation();
                    return;
                }

                ControlInfoDataGroup group = await ControlInfoDataSource.Instance.GetGroupFromItemAsync((String)e.Parameter);
                var menuItem = NavigationRootPage.Current.NavigationView.MenuItems.Cast<Windows.UI.Xaml.Controls.NavigationViewItemBase>().FirstOrDefault(m => m.Tag?.ToString() == group.UniqueId);
                if (menuItem != null)
                {
                    menuItem.IsSelected = true;
                }

                PlayConnectedAnimation();
            }

            base.OnNavigatedTo(e);
        }

        void PlayConnectedAnimation()
        {
// UNO TODO
#if NETFX_CORE
            if (NavigationRootPage.Current.PageHeader != null)
            {
                var connectedAnimation = ConnectedAnimationService.GetForCurrentView().GetAnimation("controlAnimation");

                if (connectedAnimation != null)
                {
                    var target = NavigationRootPage.Current.PageHeader.TitlePanel;

                    // Setup the "basic" configuration if the API is present. 
                    if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 7))
                    {
                        connectedAnimation.Configuration = new BasicConnectedAnimationConfiguration();
                    }

                    connectedAnimation.TryStart(target, new UIElement[] { subTitleText });
                }
            }
#endif
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            SetControlExamplesTheme(App.ActualTheme);

            base.OnNavigatingFrom(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            NavigationRootPage.Current.PageHeader.TopCommandBar.Visibility = Visibility.Collapsed;
            NavigationRootPage.Current.PageHeader.ToggleThemeAction = null;

#if NETFX_CORE
            //Reverse Connected Animation
            if (e.SourcePageType != typeof(ItemPage))
            {
                var target = NavigationRootPage.Current.PageHeader.TitlePanel;
                ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("controlAnimation", target);
            }
#endif

            base.OnNavigatedFrom(e);
        }

        private void OnContentRootSizeChanged(object sender, SizeChangedEventArgs e)
        {
            string targetState = "NormalFrameContent";

#if NETFX_CORE
            if ((contentColumn.ActualWidth) >= 1000)
            {
                targetState = "WideFrameContent";
            }
#endif

            VisualStateManager.GoToState(this, targetState, false);
        }
    }
}
