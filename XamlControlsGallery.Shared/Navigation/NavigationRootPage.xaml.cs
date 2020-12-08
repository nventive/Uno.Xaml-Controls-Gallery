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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.ApplicationModel.Core;
using Windows.Devices.Input;
using Windows.Gaming.Input;
using Windows.System;
using Windows.System.Profile;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Foundation.Metadata;
using Windows.UI;
using muxc = Windows.UI.Xaml.Controls;
using AppUIBasics.ControlPages;
using AppUIBasics.Helper;

namespace AppUIBasics
{
    public sealed partial class NavigationRootPage : Page
    {
        public static NavigationRootPage Current;
        public static Frame RootFrame = null;

        public VirtualKey ArrowKey;

        private RootFrameNavigationHelper _navHelper;
        private PageHeader _header;
        private bool _isGamePadConnected;
        private bool _isKeyboardConnected;
        private Windows.UI.Xaml.Controls.NavigationViewItem _allControlsMenuItem;
        private Windows.UI.Xaml.Controls.NavigationViewItem _newControlsMenuItem;

        public Windows.UI.Xaml.Controls.NavigationView NavigationView
        {
            get { return NavigationViewControl; }
        }

        public DeviceType DeviceFamily { get; set; }

        public bool IsFocusSupported
        {
            get
            {
                return DeviceFamily == DeviceType.Xbox || _isGamePadConnected || _isKeyboardConnected;
            }
        }

        public PageHeader PageHeader
        {
            get
            {
                return _header ?? (_header = UIHelper.GetDescendantsOfType<PageHeader>(NavigationViewControl).FirstOrDefault());
            }
        }

        public NavigationRootPage()
        {
            this.InitializeComponent();

            // Workaround for VisualState issue that should be fixed
            // by https://github.com/microsoft/microsoft-ui-xaml/pull/2271
            NavigationViewControl.PaneDisplayMode = muxc.NavigationViewPaneDisplayMode.Left;

            _navHelper = new RootFrameNavigationHelper(rootFrame, NavigationViewControl);

            SetDeviceFamily();
            AddNavigationMenuItems();
            Current = this;
            RootFrame = rootFrame;

            this.GotFocus += (object sender, RoutedEventArgs e) =>
            {
                // helpful for debugging focus problems w/ keyboard & gamepad
                if (FocusManager.GetFocusedElement() is FrameworkElement focus)
                {
                    System.Diagnostics.Debug.WriteLine("got focus: " + focus.Name + " (" + focus.GetType().ToString() + ")");
                }
            };

            Gamepad.GamepadAdded += OnGamepadAdded;
            Gamepad.GamepadRemoved += OnGamepadRemoved;

            Windows.UI.Xaml.Window.Current.SetTitleBar(AppTitleBar);

#if __WASM__
			switch (Environment.GetEnvironmentVariable("UNO_BOOTSTRAP_MONO_RUNTIME_MODE"))
			{
				case "Interpreter":
					UnoShell.AppEnvironmentMode = "Interpreted";
					break;
				case "FullAOT":
					UnoShell.AppEnvironmentMode = "AOT";
					break;
				case "InterpreterAndAOT":
					UnoShell.AppEnvironmentMode = "Mixed";
					break;
			}
#endif

#if NETFX_CORE
            CoreApplication.GetCurrentView().TitleBar.LayoutMetricsChanged += (s, e) => UpdateAppTitle(s);

            _isKeyboardConnected = Convert.ToBoolean(new KeyboardCapabilities().KeyboardPresent);
#endif

            // remove the solid-colored backgrounds behind the caption controls and system back button if we are in left mode
            // This is done when the app is loaded since before that the actual theme that is used is not "determined" yet
            Loaded += delegate (object sender, RoutedEventArgs e)
            {
                NavigationOrientationHelper.UpdateTitleBar(NavigationOrientationHelper.IsLeftMode);
            };

            NavigationViewControl.RegisterPropertyChangedCallback(muxc.NavigationView.PaneDisplayModeProperty, new DependencyPropertyChangedCallback(OnPaneDisplayModeChanged));
		}

        private void OnPaneDisplayModeChanged(DependencyObject sender, DependencyProperty dp)
        {
            var navigationView = sender as muxc.NavigationView;
            NavigationRootPage.Current.AppTitleBar.Visibility = navigationView.PaneDisplayMode == muxc.NavigationViewPaneDisplayMode.Top ? Visibility.Collapsed : Visibility.Visible;
        }

		void UpdateAppTitle(CoreApplicationViewTitleBar coreTitleBar)
        {
            var full = (ApplicationView.GetForCurrentView().IsFullScreenMode);
            var left = 12 + (full ? 0 : coreTitleBar.SystemOverlayLeftInset);
            AppTitle.Margin = new Thickness(left, 8, 0, 0);
            AppTitleBar.Height = coreTitleBar.Height;
        }

        public string GetAppTitleFromSystem()
        {
            return Windows.ApplicationModel.Package.Current.DisplayName;
        }

        public bool CheckNewControlSelected()
        {
            return _newControlsMenuItem.IsSelected;
        }

        private void AddNavigationMenuItems()
        {
            foreach (var group in ControlInfoDataSource.Instance.Groups.OrderBy(i => i.Title))
            {
                var item = new Windows.UI.Xaml.Controls.NavigationViewItem() { Content = group.Title, Tag = group.UniqueId, DataContext = group };
                AutomationProperties.SetName(item, group.Title);

                foreach (var groupItem in group.Items)
                {
                    var itemInGroup = new Microsoft.UI.Xaml.Controls.NavigationViewItem() { Content = groupItem.Title, Tag = groupItem.UniqueId, DataContext = groupItem};

                    itemInGroup.Icon = GetIcon(groupItem.ImagePath);

                    NavigationViewControl.MenuItems.Add(itemInGroup);
                    AutomationProperties.SetName(itemInGroup, groupItem.Title);
                }


                if (group.ImagePath.ToLowerInvariant().EndsWith(".png"))
                {
                    item.Icon = new BitmapIcon() { UriSource = new Uri(group.ImagePath, UriKind.RelativeOrAbsolute) };
                }
                else
                {
                    item.Icon = new FontIcon()
                    {
                        FontFamily = new FontFamily("Segoe MDL2 Assets"),
                        Glyph = group.ImagePath
                    };
                }

                NavigationViewControl.MenuItems.Add(item);

                if (group.UniqueId == "AllControls")
                {
                    this._allControlsMenuItem = item;
                }
                else if (group.UniqueId == "NewControls")
                {
                    this._newControlsMenuItem = item;
                }
            }

            // Move "What's New" and "All Controls" to the top of the NavigationView
            NavigationViewControl.MenuItems.Remove(_allControlsMenuItem);
            NavigationViewControl.MenuItems.Remove(_newControlsMenuItem);
            NavigationViewControl.MenuItems.Insert(0, _allControlsMenuItem);
            NavigationViewControl.MenuItems.Insert(0, _newControlsMenuItem);

            // Separate the All/New items from the rest of the categories.
            NavigationViewControl.MenuItems.Insert(2, new Windows.UI.Xaml.Controls.NavigationViewItemSeparator());

            _newControlsMenuItem.Loaded += OnNewControlsMenuItemLoaded;
        }

        private static IconElement GetIcon(string imagePath)
        {
            return imagePath.ToLowerInvariant().EndsWith(".png") ?
                        (IconElement)new BitmapIcon() { UriSource = new Uri(imagePath, UriKind.RelativeOrAbsolute) , ShowAsMonochrome = false} :
                        (IconElement)new FontIcon()
                        {
                            FontFamily = new FontFamily("Segoe MDL2 Assets"),
                            Glyph = imagePath
                        };
        }

        private void SetDeviceFamily()
        {
#if NETFX_CORE
            var familyName = AnalyticsInfo.VersionInfo.DeviceFamily;

            if (!Enum.TryParse(familyName.Replace("Windows.", string.Empty), out DeviceType parsedDeviceType))
            {
                parsedDeviceType = DeviceType.Other;
            }

            DeviceFamily = parsedDeviceType;
#endif
        }

        private void OnNewControlsMenuItemLoaded(object sender, RoutedEventArgs e)
        {
            if (IsFocusSupported && NavigationViewControl.DisplayMode == Windows.UI.Xaml.Controls.NavigationViewDisplayMode.Expanded)
            {
                _newControlsMenuItem.Focus(FocusState.Keyboard);
            }
            _newControlsMenuItem.IsSelected = true;
        }

        private void OnGamepadRemoved(object sender, Gamepad e)
        {
            _isGamePadConnected = Gamepad.Gamepads.Any();
        }

        private void OnGamepadAdded(object sender, Gamepad e)
        {
            _isGamePadConnected = Gamepad.Gamepads.Any();
        }

        private void OnNavigationViewItemInvoked(Windows.UI.Xaml.Controls.NavigationView sender, Windows.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {
                rootFrame.Navigate(typeof(SettingsPage));
            }
            else
            {
                var invokedItem = args.InvokedItemContainer;

                if (invokedItem == _allControlsMenuItem)
                {
                    rootFrame.Navigate(typeof(AllControlsPage));
                }
                else if (invokedItem == _newControlsMenuItem)
                {
                    rootFrame.Navigate(typeof(NewControlsPage));
                }
                else
                {
                    if (invokedItem.DataContext is ControlInfoDataGroup)
                    {
                        var itemId = ((ControlInfoDataGroup)invokedItem.DataContext).UniqueId;
                        rootFrame.Navigate(typeof(SectionPage), itemId);
                    }
                    else if (invokedItem.DataContext is ControlInfoDataItem)
                    {
                        var item = (ControlInfoDataItem)invokedItem.DataContext;
                        rootFrame.Navigate(typeof(ItemPage), item.UniqueId);
                    }
                }
            }
        }

        private void OnRootFrameNavigated(object sender, NavigationEventArgs e)
        {
            if (e.SourcePageType == typeof(AllControlsPage) ||
                e.SourcePageType == typeof(NewControlsPage))
            {
                NavigationViewControl.AlwaysShowHeader = false;
            }
            else
            {
                NavigationViewControl.AlwaysShowHeader = true;

                bool isFilteredPage = e.SourcePageType == typeof(SectionPage) || e.SourcePageType == typeof(SearchResultsPage);

                //TODO: APM
                //PageHeader?.UpdateBackground(isFilteredPage);
            }
        }

        private void OnControlsSearchBoxTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                var suggestions = new List<ControlInfoDataItem>();

#if __WASM___
                // Limit is temporary to improve the user experience
                if (sender.Text?.Length > 3)
#endif
                {
                    var querySplit = sender.Text.Split(" ");
                    foreach (var group in ControlInfoDataSource.Instance.Groups)
                    {

                        var matchingItems = group.Items.Where(
                            item =>
                            {
                                // Idea: check for every word entered (separated by space) if it is in the name,
                                // e.g. for query "split button" the only result should "SplitButton" since its the only query to contain "split" and "button"
                                // If any of the sub tokens is not in the string, we ignore the item. So the search gets more precise with more words
                                bool flag = true;
                                foreach (string queryToken in querySplit)
                                {
                                    // Check if token is not in string
                                    if (item.Title.IndexOf(queryToken, StringComparison.CurrentCultureIgnoreCase) < 0)
                                    {
                                        // Token is not in string, so we ignore this item.
                                        flag = false;
                                    }
                                }
                                return flag;
                            });

                        foreach (var item in matchingItems)
                        {
                            suggestions.Add(item);
                        }
                    }
                    if (suggestions.Count > 0)
                    {
                        controlsSearchBox.ItemsSource = suggestions.OrderByDescending(i => i.Title.StartsWith(sender.Text, StringComparison.CurrentCultureIgnoreCase)).ThenBy(i => i.Title);
                    }
                    else
                    {
                        controlsSearchBox.ItemsSource = new string[] { "No results found" };
                    }
                }
#if __WASM___
                else
                {
                    controlsSearchBox.ItemsSource = new string[] { "Type more characters" };
                }
#endif
            }
        }

        private void OnControlsSearchBoxQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion != null && args.ChosenSuggestion is ControlInfoDataItem)
            {
                var infoDataItem = args.ChosenSuggestion as ControlInfoDataItem;
                var itemId = infoDataItem.UniqueId;
                bool changedSelection = false;
                foreach(object rawItem in NavigationView.MenuItems)
                {
                    // Check if we encountered the separator
                    if(!(rawItem is muxc.NavigationViewItem))
                    {
                        // Skipping this item
                        continue;
                    }

                    var item = rawItem as muxc.NavigationViewItem;

                    // Check if we are this category
                    if((string)item.Content == infoDataItem.Title)
                    {
                        NavigationView.SelectedItem = item;
                        changedSelection = true;
                    }
                    // We are not :(
                    else
                    {
                        //TODO: UNO

                        // Maybe one of our items is? ಠಿ_ಠ
                        //if(item.MenuItems.Count != 0)
                        //{
                        //    foreach(muxc.NavigationViewItem child in item.MenuItems)
                        //    {
                        //        if((string)child.Content == infoDataItem.Title)
                        //        {
                        //            // We are the item corresponding to the selected one, update selection!

                        //            // Deal with differences in displaymodes
                        //            if(NavigationView.PaneDisplayMode == muxc.NavigationViewPaneDisplayMode.Top)
                        //            {
                        //                // In Topmode, the child is not visible, so set parent as selected
                        //                // Everything else does not work unfortunately
                        //                NavigationView.SelectedItem = item;
                        //                item.StartBringIntoView();
                        //            }
                        //            else
                        //            {
                        //                // Expand so we animate
                        //                item.IsExpanded = true;
                        //                // Ensure parent is expanded so we actually show the selection indicator
                        //                NavigationView.UpdateLayout();
                        //                // Set selected item
                        //                NavigationView.SelectedItem = child;
                        //                child.StartBringIntoView();
                        //            }
                        //            // Set to true to also skip out of outer for loop
                        //            changedSelection = true;
                        //            // Break out of child iteration for loop
                        //            break;
                        //        }
                        //    }
                        //}
                    }
                    // We updated selection, break here!
                    if (changedSelection)
                    {
                        break;
                    }
                }
                NavigationRootPage.RootFrame.Navigate(typeof(ItemPage), itemId);
            }
            else if (!string.IsNullOrEmpty(args.QueryText))
            {
                NavigationRootPage.RootFrame.Navigate(typeof(SearchResultsPage), args.QueryText);
            }
        }

        private void KeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            controlsSearchBox.Focus(FocusState.Keyboard);
        }

        private void NavigationViewControl_PaneClosing(Windows.UI.Xaml.Controls.NavigationView sender, Windows.UI.Xaml.Controls.NavigationViewPaneClosingEventArgs args)
        {
            AppTitle.Visibility = Visibility.Collapsed;
        }

        private void NavigationViewControl_PaneOpened(Windows.UI.Xaml.Controls.NavigationView sender, object args)
        {
            AppTitle.Visibility = Visibility.Visible;
            if (sender.DisplayMode == Windows.UI.Xaml.Controls.NavigationViewDisplayMode.Expanded)
            {
                AppTitleBar.Margin = new Thickness(40, 0, 0, 0);
            }
            else
            {
                AppTitleBar.Margin = new Thickness();
            }
        }

        private void NavigationViewControl_DisplayModeChanged(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewDisplayModeChangedEventArgs args)
        {
            Thickness currMargin = AppTitleBar.Margin;
            if (sender.DisplayMode == Microsoft.UI.Xaml.Controls.NavigationViewDisplayMode.Minimal)
            {
                AppTitleBar.Margin = new Thickness((sender.CompactPaneLength * 2), currMargin.Top, currMargin.Right, currMargin.Bottom);

            }
            else
            {
                AppTitleBar.Margin = new Thickness(sender.CompactPaneLength, currMargin.Top, currMargin.Right, currMargin.Bottom);
            }

            UpdateAppTitleMargin(sender);
            UpdateHeaderMargin(sender);
        }

        private void UpdateAppTitleMargin(Microsoft.UI.Xaml.Controls.NavigationView sender)
        {
            const int smallLeftIndent = 4, largeLeftIndent = 24;

            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 7))
            {
                AppTitle.TranslationTransition = new Vector3Transition();

                if ((sender.DisplayMode == Microsoft.UI.Xaml.Controls.NavigationViewDisplayMode.Expanded && sender.IsPaneOpen) ||
                         sender.DisplayMode == Microsoft.UI.Xaml.Controls.NavigationViewDisplayMode.Minimal)
                {
                    AppTitle.Translation = new System.Numerics.Vector3(smallLeftIndent, 0, 0);
                }
                else
                {
                    AppTitle.Translation = new System.Numerics.Vector3(largeLeftIndent, 0, 0);
                }
            }
            else
            {
                Thickness currMargin = AppTitle.Margin;

                if ((sender.DisplayMode == Microsoft.UI.Xaml.Controls.NavigationViewDisplayMode.Expanded && sender.IsPaneOpen) ||
                         sender.DisplayMode == Microsoft.UI.Xaml.Controls.NavigationViewDisplayMode.Minimal)
                {
                    AppTitle.Margin = new Thickness(smallLeftIndent, currMargin.Top, currMargin.Right, currMargin.Bottom);
                }
                else
                {
                    AppTitle.Margin = new Thickness(largeLeftIndent, currMargin.Top, currMargin.Right, currMargin.Bottom);
                }
            }
        }

        private void UpdateHeaderMargin(Microsoft.UI.Xaml.Controls.NavigationView sender)
        {
            if (PageHeader != null)
            {
                if (sender.DisplayMode == Microsoft.UI.Xaml.Controls.NavigationViewDisplayMode.Minimal)
                {
                    Current.PageHeader.HeaderPadding = (Thickness)App.Current.Resources["PageHeaderMinimalPadding"];
                }
                else
                {
                    Current.PageHeader.HeaderPadding = (Thickness)App.Current.Resources["PageHeaderDefaultPadding"];
                }
            }
        }

        private void CtrlF_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            controlsSearchBox.Focus(FocusState.Programmatic);
        }
    }


    public enum DeviceType
    {
        Desktop,
        Mobile,
        Other,
        Xbox
    }
}
