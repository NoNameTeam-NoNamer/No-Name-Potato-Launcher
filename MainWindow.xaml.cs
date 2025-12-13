using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Security.Principal;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.ApplicationSettings;
using WinRT.Interop;
using Windows.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace No_Name_Potato_Launcher
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private static MainWindow? current;

        private readonly Microsoft.UI.Windowing.AppWindow m_AppWindow;
        private Microsoft.UI.Windowing.AppWindow GetAppWindowForCurrentWindow()
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            Microsoft.UI.WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            return Microsoft.UI.Windowing.AppWindow.GetFromWindowId(wndId);
        }
        public static IntPtr GetMainWindowHandle()
        {
            if (current is null) return IntPtr.Zero;
            return WindowNative.GetWindowHandle(current);
        }
        private readonly Scighost.WinUILib.Helpers.SystemBackdrop backdropHelper;
        public MainWindow()
        {
            InitializeComponent();
            this.Title = "某个不想起名的土豆烘烤器";
            m_AppWindow = GetAppWindowForCurrentWindow();
            this.SetTitleBar(AppTitleBar);
            UpdateAdminUI();
            var titleBar = m_AppWindow.TitleBar;
            // Hide system title bar.
            titleBar.ExtendsContentIntoTitleBar = true;
            titleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
            titleBar.ButtonBackgroundColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);
            //NavView.Margin = new Thickness(0, titleBar.Height, 0, 0);
            //AppTitleBar.Margin = new Thickness(12, titleBar.Height / 4, 12, titleBar.Height / 4);
            current = this;
            /*backgroundImage = ApplicationBackgroundImage;
            _ = Windows.Storage.ApplicationData.Current.LocalSettings;
            _ = Windows.Storage.ApplicationData.Current.LocalFolder;
            ReadLD();
            ReadBG();
            ReadBGOpacity();*/
            backdropHelper = new Scighost.WinUILib.Helpers.SystemBackdrop(this);
            backdropHelper.TrySetMica(useMicaAlt: false, TintColor: Color.FromArgb(128, 187, 187, 238),TintOpacity:0.5f);
        }
        //private static ImageBrush backgroundImage;

        private void ContentFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        private async void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            // You can also add items in code.


            // Add handler for ContentFrame navigation.
            ContentFrame.Navigated += On_Navigated;
            NavView_Navigate(typeof(StartPage), new DrillInNavigationTransitionInfo());
            // NavView doesn't load any page by default, so load home page.

            // If navigation occurs on SelectionChanged, this isn't needed.
            // Because we use ItemInvoked to navigate, we need to call Navigate
            // here to load the home page.

        }

        private void NavView_ItemInvoked(NavigationView sender,
                                         NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked == true)
            {
                NavView_Navigate(typeof(SettingsPage), args.RecommendedNavigationTransitionInfo);
            }
            else if (args.InvokedItemContainer != null)
            {
                Type navPageType = Type.GetType(args.InvokedItemContainer.Tag.ToString());
                NavView_Navigate(navPageType, args.RecommendedNavigationTransitionInfo);
            }
        }



        private void NavView_Navigate(
            Type navPageType,
            NavigationTransitionInfo transitionInfo)
        {
            ArgumentNullException.ThrowIfNull(transitionInfo);
            // Get the page type before navigation so you can prevent duplicate
            // entries in the backstack.
            Type preNavPageType = ContentFrame.CurrentSourcePageType;

            // Only navigate if the selected page isn't currently loaded.
            if (navPageType is not null && !Type.Equals(preNavPageType, navPageType))
            {
                ContentFrame.Navigate(navPageType, null, new DrillInNavigationTransitionInfo());
            }
        }

        private void On_Navigated(object sender, NavigationEventArgs e)
        {
            NavView.IsBackEnabled = ContentFrame.CanGoBack;

            if (ContentFrame.SourcePageType == typeof(SettingsPage))
            {
                // SettingsItem is not part of NavView.MenuItems, and doesn't have a Tag.
                NavView.SelectedItem = (NavigationViewItem)NavView.SettingsItem;
                NavView.Header = "设置";
            }
            else if (ContentFrame.SourcePageType != null)
            {
                // Select the nav view item that corresponds to the page being navigated to.
                var selectedItem = NavView.MenuItems
        .Cast<object>()
        .Concat(NavView.FooterMenuItems.Cast<object>())
        .OfType<NavigationViewItem>()
        .FirstOrDefault(i => i.Tag != null && i.Tag.Equals(ContentFrame.SourcePageType.FullName));

                if (selectedItem != null)
                {
                    NavView.SelectedItem = selectedItem;
                    NavView.Header = selectedItem.Content?.ToString();
                }
                else
                {
                    NavView.SelectedItem = null;
                    NavView.Header = null;
                }
            }
        }

        private void AppTitleBar_PaneToggleRequested(TitleBar sender, object args)
        {
            NavView.IsPaneOpen = !NavView.IsPaneOpen;
        }

        private void UpdateAdminUI()
        {
            if (AppTitleBar != null && IsRunAsAdmin())
            {
                AppTitleBar.Subtitle = "管理员";
            }
        }

        private static bool IsRunAsAdmin()
        {
            try
            {
                using var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        private void TopButton_Click(object sender, RoutedEventArgs e)
        {
            OverlappedPresenter presenter = (OverlappedPresenter)AppWindow.Presenter;
            presenter.IsAlwaysOnTop = !presenter.IsAlwaysOnTop;
        }
    }
}
