﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using UniversalSend.Models;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

//https://go.microsoft.com/fwlink/?LinkId=234236 上介绍了“用户控件”项模板

namespace UniversalSend.Controls
{
    public sealed partial class QuickSaveOptionsControl : UserControl
    {
        public QuickSaveOptionsControl()
        {
            this.InitializeComponent();
        }

        private void OffButton_Click(object sender, RoutedEventArgs e)
        {
            FavoritesButton.IsChecked = OnButton.IsChecked = false;
            OffButton.IsChecked = true;
            ReceiveManager.QuickSave = ReceiveManager.QuickSaveMode.Off;
        }

        private void OnButton_Click(object sender, RoutedEventArgs e)
        {
            FavoritesButton.IsChecked = OffButton.IsChecked = false;
            OnButton.IsChecked = true;
            ReceiveManager.QuickSave = ReceiveManager.QuickSaveMode.On;
        }

        private void FavoritesButton_Click(object sender, RoutedEventArgs e)
        {
            OffButton.IsChecked = OnButton.IsChecked = false;
            FavoritesButton.IsChecked = true;
            ReceiveManager.QuickSave = ReceiveManager.QuickSaveMode.Favorites;
        }
    }
}
