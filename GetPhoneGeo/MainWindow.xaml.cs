using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using GetPhoneGeo.Properties;
using Microsoft.Win32;
using PhoneGeoLib;

namespace GetPhoneGeo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private static string XlsPath { get; set; }
        public static ObservableCollection<string> DataItemsLog { get; set; }

        public MainWindow()
        {
            DataContext = this;
            DataItemsLog = new ObservableCollection<string>();

            Informer.OnResultStr +=
                async result =>
                    await Application.Current.Dispatcher.BeginInvoke(
                        new Action(() => DataItemsLog.Insert(0, result)));

            InitializeComponent();
            //Height = SystemParameters.WorkArea.Height;
        }

        private void LaunchGetPhoneGeoOnGitHub(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/mazanuj/GetPhoneGeo/");
        }

        private async void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            ButtonIsEnable(false);
            await Task.Run(async () =>
            {
                Utils.ApiKey = Settings.Default.ApiKey;
                await Initialize.ParseXLS(XlsPath);
            });
            ButtonIsEnable(true);
        }

        private void ButtonXls_OnClick(object sender, RoutedEventArgs e)
        {
            ButtonIsEnable(false);
            var sfd = new OpenFileDialog()
            {
                Filter = "Excel 2003 (*.xls)|*.xls|Excel 2007 (*.xlsx)|*.xlsx",
                FilterIndex = 2,
                InitialDirectory = AppDomain.CurrentDomain.BaseDirectory,
                RestoreDirectory = true,
            };

            if (sfd.ShowDialog() == false)
                return;

            XlsPath = sfd.FileName;
            ButtonIsEnable(true);
        }

        private void ButtonIsEnable(bool value)
        {
            ButtonStart.IsEnabled = value;
            ButtonXls.IsEnabled = value;
        }
    }
}