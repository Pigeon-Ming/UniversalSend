using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Pickers.Provider;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Threading.Tasks;
using UniversalSend.Models;
using System.Diagnostics;
using UniversalSend.Controls.ContentDialogControls;
using Windows.UI;
using Windows.System;
using Windows.ApplicationModel.DataTransfer;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace UniversalSend.Views
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class ExplorerPage : Page
    {


        public ExplorerPage()
        {
            this.InitializeComponent();
            string ViewMode = Settings.GetSettingContentAsString(Settings.ExplorerPage_ViewMode);
            if (ViewMode == "List")
                CurrentViewMode = ExplorerPage.ViewMode.List;
            else
                CurrentViewMode = ExplorerPage.ViewMode.Grid;
            UpdateViewMode();
        }

        ContentDialogManager ContentDialogManager { get; set; } = new ContentDialogManager();

        FileOpenPickerUI fileOpenPickerUI;

        FileSavePickerUI fileSavePickerUI;

        List<StorageFolder> folderStack = new List<StorageFolder>();

        List<string> AllowedFileTypes;

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            Settings.InitUserSettings();
            UpdateViewMode();
            if (e.Parameter is FileOpenPickerUI)
            {
                // 获取参数
                fileOpenPickerUI = e.Parameter as FileOpenPickerUI;
                // 获取本地文件列表
                AllowedFileTypes = fileOpenPickerUI.AllowedFileTypes.ToList();



            }
            else if(e.Parameter is FileSavePickerUI)
            {
                fileSavePickerUI = e.Parameter as FileSavePickerUI;
                AllowedFileTypes = fileSavePickerUI.AllowedFileTypes.ToList();
                fileSavePickerUI.TargetFileRequested += FileSavePickerUI_TargetFileRequested;
            }
            //List<IStorageItem> items = (await (await StorageHelper.GetReceiveStoageFolderAsync()).GetItemsAsync()).ToList();
            await UpdateViewAsync(await StorageHelper.GetReceiveStoageFolderAsync());
        }

        List<ViewStorageItem> viewItems = new List<ViewStorageItem>();

        async Task UpdateViewAsync(StorageFolder folder)
        {
            LoadingProgressBar.Visibility = Visibility.Visible;
            lvFiles.IsEnabled = false;
            UpdateFolderPathTextBlock();
            List<IStorageItem> items = (await folder.GetItemsAsync()).ToList();
            BackButton.IsEnabled = folderStack.Count > 0 ? true : false;

            viewItems = new List<ViewStorageItem>();
            EmptyFolderTip.Visibility = Visibility.Collapsed;
            if (AllowedFileTypes == null || AllowedFileTypes.Contains("*"))
                foreach (var item in items)
                {
                    viewItems.Add(new ViewStorageItem(item));
                }
            else
                foreach (var item in items)
                {
                    if (item is StorageFile && !AllowedFileTypes.Contains(((StorageFile)item).FileType.ToLower()))
                    {
                        continue;
                    }
                    viewItems.Add(new ViewStorageItem(item));
                }
            if (viewItems.Count == 0)
            {
                EmptyFolderTip.Visibility = Visibility.Visible;
            }

            await Task.Delay(100);
            lvFiles.ItemsSource = viewItems;
            lvFiles.IsEnabled = true;
            LoadingProgressBar.Visibility = Visibility.Collapsed;
        }

        void UpdateViewMode()
        {
            if (CurrentViewMode == ViewMode.List)
            {
                lvFiles.ItemTemplate = ListViewModeItemTemplate;
                lvFiles.ItemContainerStyle = ListViewModeListViewItemStyle;
                lvFiles.ItemsPanel = ListViewModeItemsPanelTemplate;
                CurrentViewMode = ViewMode.Grid;
                ViewModeButtonIcon.Glyph = "\uF0E2";
                ViewModeButton.Label = "网格视图";
            }
            else
            {
                lvFiles.ItemTemplate = GridViewModeItemTemplate;
                lvFiles.ItemContainerStyle = GridViewModeListViewItemStyle;
                lvFiles.ItemsPanel = GridViewModeItemsPanelTemplate;
                CurrentViewMode = ViewMode.List;
                ViewModeButtonIcon.Glyph = "\uEA37";
                ViewModeButton.Label = "列表视图";
            }
        }

        void UpdateFolderPathTextBlock()
        {
            string str = "根目录";
            foreach(var item in folderStack)
            {
                str += $"/{item.Name}";
            }
            FolderPathTextBlock.Text = str;
        }

        private async void FileSavePickerUI_TargetFileRequested(FileSavePickerUI sender, TargetFileRequestedEventArgs args)
        {
            var deferral = args.Request.GetDeferral();
            StorageFolder folder = null;
            if (folderStack.Count != 0)
                folder = folderStack.Last();
            else
                folder = await StorageHelper.GetReceiveStoageFolderAsync();

            try
            {
                // 在指定的地址新建一个没有任何内容的空白文件
                StorageFile file = await folder.CreateFileAsync(sender.FileName, CreationCollisionOption.GenerateUniqueName);

                // 设置 TargetFile，“自定义文件保存选取器”的调用端会收到此对象
                args.Request.TargetFile = file;
            }
            catch (Exception ex)
            {
                // 输出异常信息
                Debug.WriteLine(ex.ToString());
            }
            finally
            {
                // 完成异步操作
                deferral.Complete();
            }
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(fileOpenPickerUI != null)
            {
                // 移除列表
                if (e.RemovedItems.Count > 0)
                {
                    if (fileOpenPickerUI.SelectionMode == FileSelectionMode.Multiple)
                    {
                        for (int i = 0; i < e.RemovedItems.Count; i++)
                        {
                            ViewStorageItem item = e.RemovedItems[i] as ViewStorageItem;
                            // 移除前先判断是否存在目标项
                            if (fileOpenPickerUI.ContainsFile(item.Name))
                            {
                                fileOpenPickerUI.RemoveFile(item.Name);
                            }
                        }
                    }
                    else
                    {
                        ViewStorageItem item = e.RemovedItems[0] as ViewStorageItem;
                        if (fileOpenPickerUI.ContainsFile(item.Name))
                        {
                            fileOpenPickerUI.RemoveFile(item.Name);
                        }
                    }
                }

                // 添加列表
                if (e.AddedItems.Count > 0)
                {
                    // 如果是多选
                    if (fileOpenPickerUI.SelectionMode == FileSelectionMode.Multiple)
                    {
                        for (int i = 0; i < e.AddedItems.Count; i++)
                        {
                            ViewStorageItem item = e.AddedItems[i] as ViewStorageItem;
                            if (item.Item is StorageFile)
                            {
                                StorageFile file = (StorageFile)item.Item;
                                if (fileOpenPickerUI.CanAddFile(file))
                                {
                                    fileOpenPickerUI.AddFile(item.Name, file);
                                }
                            }
                            else if (item.Item is StorageFolder)
                            {
                                /*To-Do:进入子文件夹*/
                            }
                        }
                    }
                    else //如果是单选
                    {
                        ViewStorageItem item = e.AddedItems[0] as ViewStorageItem;
                        if (item.Item is StorageFile)
                        {
                            StorageFile file = (StorageFile)item.Item;
                            if (fileOpenPickerUI.CanAddFile(file))
                            {
                                fileOpenPickerUI.AddFile(item.Name, file);
                            }
                        }
                        else if (item.Item is StorageFolder)
                        {
                            /*To-Do:进入子文件夹*/
                        }

                    }
                }
            }else if(fileSavePickerUI != null)
            {
                
                //// 移除列表
                //if (e.RemovedItems.Count > 0)
                //{
                //    if (fileSavePickerUI. == FileSelectionMode.Multiple)
                //    {
                //        for (int i = 0; i < e.RemovedItems.Count; i++)
                //        {
                //            ViewStorageItem item = e.RemovedItems[i] as ViewStorageItem;
                //            // 移除前先判断是否存在目标项
                //            if (fileSavePickerUI.ContainsFile(item.Name))
                //            {
                //                fileSavePickerUI.RemoveFile(item.Name);
                //            }
                //        }
                //    }
                //    else
                //    {
                //        ViewStorageItem item = e.RemovedItems[0] as ViewStorageItem;
                //        if (fileSavePickerUI.ContainsFile(item.Name))
                //        {
                //            fileSavePickerUI.RemoveFile(item.Name);
                //        }
                //    }
                //}

                //// 添加列表
                //if (e.AddedItems.Count > 0)
                //{
                //    // 如果是多选
                //    if (fileSavePickerUI.SelectionMode == FileSelectionMode.Multiple)
                //    {
                //        for (int i = 0; i < e.AddedItems.Count; i++)
                //        {
                //            ViewStorageItem item = e.AddedItems[i] as ViewStorageItem;
                //            if (item.Item is StorageFile)
                //            {
                //                StorageFile file = (StorageFile)item.Item;
                //                if (fileSavePickerUI.CanAddFile(file))
                //                {
                //                    fileSavePickerUI.AddFile(item.Name, file);
                //                }
                //            }
                //            else if (item.Item is StorageFolder)
                //            {
                //                /*To-Do:进入子文件夹*/
                //            }
                //        }
                //    }
                //    else //如果是单选
                //    {
                //        ViewStorageItem item = e.AddedItems[0] as ViewStorageItem;
                //        if (item.Item is StorageFile)
                //        {
                //            StorageFile file = (StorageFile)item.Item;
                //            if (fileSavePickerUI.CanAddFile(file))
                //            {
                //                fileSavePickerUI.AddFile(item.Name, file);
                //            }
                //        }
                //        else if (item.Item is StorageFolder)
                //        {
                //            /*To-Do:进入子文件夹*/
                //        }

                //    }
                //}
            }
        }

        private async void lvFiles_ItemClick(object sender, ItemClickEventArgs e)
        {
            ViewStorageItem item = e.ClickedItem as ViewStorageItem;
            if (item.Item is StorageFolder)
            {
                StorageFolder folder = item.Item as StorageFolder;
                folderStack.Add(folder);
                await UpdateViewAsync(folder);
            }
            else if(item.Item is StorageFile && fileOpenPickerUI == null && fileSavePickerUI == null)
            {
                await Launcher.LaunchFileAsync(((ViewStorageItem)e.ClickedItem).Item as StorageFile);
            }
        }

        private async void BackButton_Click(object sender, RoutedEventArgs e)
        {
            folderStack.Remove(folderStack.Last());
            
            await UpdateViewAsync(await GetFolderOfCurrentViewAsync());
            
        }

        public async Task<StorageFolder> GetFolderOfCurrentViewAsync()
        {
            if (folderStack.Count == 0)
            {
                return await StorageHelper.GetReceiveStoageFolderAsync();
            }
            StorageFolder folder = folderStack[folderStack.Count - 1];
            return folder;
        }

        private async void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            folderStack.Clear();
            await UpdateViewAsync(await StorageHelper.GetReceiveStoageFolderAsync());
        }

        private async void CreateFolderButton_Click(object sender, RoutedEventArgs e)
        {
            CreateNewFolderControl control = new CreateNewFolderControl();
            control.CancelButton.Click += (a, b) =>
            {
                ContentDialogManager.HideContentDialog();
            };
            control.OKButton.Click += async (a, b) =>
            {
                if (StringHelper.IsValidFileName(control.NameTextBox.Text))
                {
                    StorageFolder folder = null;
                    if (folderStack.Count != 0)
                        folder = folderStack.Last();
                    else
                        folder = await StorageHelper.GetReceiveStoageFolderAsync();

                    await folder.CreateFolderAsync(control.NameTextBox.Text);
                    ContentDialogManager.HideContentDialog();
                    await UpdateViewAsync(folder);
                }
                else
                {
                    control.NameTextBox.BorderBrush = new SolidColorBrush(Colors.Red);
                }

            };
            await ContentDialogManager.ShowContentDialogAsync(control);
        }

        //private void Flyout_Opened(object sender, object e)
        //{
        //    CreateFolderFlyoutGrid.Children.Clear();
            
        //    CreateNewFolderControl control = new CreateNewFolderControl();
        //    control.OKButton.Click += async (a, b) =>
        //    {
        //        if (StringHelper.IsValidFileName(control.NameTextBox.Text))
        //        {
        //            StorageFolder folder = null;
        //            if (folderStack.Count != 0)
        //                folder = folderStack.Last();
        //            else
        //                folder = await StorageHelper.GetReceiveStoageFolderAsync();

        //            await folder.CreateFolderAsync(control.NameTextBox.Text);
        //            CreateFolderFlyout.Hide();
        //            await UpdateViewAsync(folder);
        //        }
        //        else
        //        {
        //            control.NameTextBox.BorderBrush = new SolidColorBrush(Colors.Red);
        //        }

        //    };
        //    CreateFolderFlyoutGrid.Children.Add(control);
            
        //}

        IStorageItem RightTabedItem = null;

        private void lvFiles_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            ViewStorageItem item = (e.OriginalSource as FrameworkElement).DataContext as ViewStorageItem;
            if (item == null)
                RightTabedItem = null;
            else
                RightTabedItem = item.Item;
        }

        private async void ListViewFlyout_Open_Click(object sender, RoutedEventArgs e)
        {
            if (RightTabedItem is StorageFile)
            {
                await Launcher.LaunchFileAsync((StorageFile)RightTabedItem);
            }
            else if (RightTabedItem is StorageFolder)
            {
                StorageFolder folder = (StorageFolder)RightTabedItem;
                folderStack.Add(folder);
                await UpdateViewAsync(folder);
            }
            
        }

        private async void ListViewFlyout_OpenFilePath_Click(object sender, RoutedEventArgs e)
        {
            if (RightTabedItem is StorageFile)
            {
                StorageFile file = ((StorageFile)RightTabedItem);
                string str = file.Path.Substring(0, file.Path.Length - file.Name.Length);
                await Launcher.LaunchFolderPathAsync(str);
            }
            else if (RightTabedItem is StorageFolder)
            {

                await Launcher.LaunchFolderAsync((StorageFolder)RightTabedItem);
            }
        }

        private void ListViewFlyout_CopyFilePath_Click(object sender, RoutedEventArgs e)
        {
            DataPackage dataPackage = new DataPackage();
            if (RightTabedItem is StorageFile)
            {
                dataPackage.SetText(((StorageFile)RightTabedItem).Path);
                Clipboard.SetContent(dataPackage);
            }
            else if (RightTabedItem is StorageFolder)
            {
                dataPackage.SetText(((StorageFolder)RightTabedItem).Path);
                Clipboard.SetContent(dataPackage);
            }
        }

        private void ListViewFlyout_Opened(object sender, object e)
        {
            if (RightTabedItem == null)
            {
                if (ClipboardItem == null)
                    ListViewFlyout.Hide();
                else
                    ListViewFlyout_Open.Visibility = Visibility.Collapsed;
            }
            else
            {
                    ListViewFlyout_Open.Visibility = Visibility.Visible;
            }
            if (RightTabedItem is StorageFile)
            {
                ListViewFlyout_OpenFilePath.Text = "打开文件位置";
            }
            else if (RightTabedItem is StorageFolder)
            {
                ListViewFlyout_OpenFilePath.Text = "打开文件夹位置";
            }

            if (ClipboardItem == null)
            {
                ListViewFlyout_Paste.Visibility = Visibility.Collapsed;
            }
            else
            {
                ListViewFlyout_Paste.Visibility = Visibility.Visible;
            }
        }

        private void ListViewFlyout_Cut_Click(object sender, RoutedEventArgs e)
        {
            CutMode = true;
            ClipboardItem = RightTabedItem;
        }

        IStorageItem ClipboardItem { get; set; }

        bool CutMode = false;

        private void ListViewFlyout_Copy_Click(object sender, RoutedEventArgs e)
        {
            CutMode = false;
            ClipboardItem = RightTabedItem;
        }

        private async void ListViewFlyout_Paste_Click(object sender, RoutedEventArgs e)
        {
            StorageFolder currentFolder = await GetFolderOfCurrentViewAsync();
            if (ClipboardItem is StorageFile)
            {
                StorageFile file = (StorageFile)ClipboardItem;
                if (CutMode)
                {
                    await file.MoveAsync(currentFolder);
                }
                else
                {
                    await file.CopyAsync(currentFolder);
                }
                await UpdateViewAsync(await GetFolderOfCurrentViewAsync());
            }
            else
            {
                ContentDialog contentDialog = new ContentDialog();
                contentDialog.CloseButtonText = "关闭";
                contentDialog.Title = $"不支持的操作";
                contentDialog.Content = "文件夹暂不支持剪切与复制操作";
                await contentDialog.ShowAsync();
            }
        }

        private async void ListViewFlyout_Delete_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog contentDialog = new ContentDialog();
            contentDialog.Title = $"确定要删除项目“{RightTabedItem.Name}”吗？";
            contentDialog.PrimaryButtonText = "否";
            contentDialog.SecondaryButtonText = "是";
            var result = await contentDialog.ShowAsync();
            if (result == ContentDialogResult.Secondary)
            {
                if (RightTabedItem is StorageFile)
                {
                    await ((StorageFile)RightTabedItem).DeleteAsync();
                }
                else if (RightTabedItem is StorageFolder)
                {
                    await ((StorageFolder)RightTabedItem).DeleteAsync();
                }
                await UpdateViewAsync(await GetFolderOfCurrentViewAsync());
            }
                
        }

        private async void ListViewFlyout_Rename_Click(object sender, RoutedEventArgs e)
        {
            RenameStorageItemControl control = new RenameStorageItemControl();
            control.NameTextBox.Text = RightTabedItem.Name;
            control.CancelButton.Click += (a, b) =>
            {
                ContentDialogManager.HideContentDialog();
            };
            control.OKButton.Click += async (a, b) =>
            {
                if (StringHelper.IsValidFileName(control.NameTextBox.Text))
                {
                    if(RightTabedItem is StorageFile)
                    {
                        StorageFile storageFile = (StorageFile)RightTabedItem;
                        await storageFile.RenameAsync(control.NameTextBox.Text);
                    }
                    else if(RightTabedItem is StorageFolder)
                    {
                        StorageFolder storageFolder = (StorageFolder)RightTabedItem;
                        await storageFolder.RenameAsync(control.NameTextBox.Text);
                    }
                    await UpdateViewAsync(await GetFolderOfCurrentViewAsync());
                    ContentDialogManager.HideContentDialog();
                }
                else
                {
                    control.NameTextBox.BorderBrush = new SolidColorBrush(Colors.Red);
                }

            };
            await ContentDialogManager.ShowContentDialogAsync(control);
            await UpdateViewAsync(await GetFolderOfCurrentViewAsync());
        }

        private async void ListViewFlyout_Properties_Click(object sender, RoutedEventArgs e)
        {
            StorageItemPropertiesControl control = new StorageItemPropertiesControl(RightTabedItem);
            control.CancelButton.Click += (a, b) =>
            {
                ContentDialogManager.HideContentDialog();
            };
            await ContentDialogManager.ShowContentDialogAsync(control);
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await UpdateViewAsync(await GetFolderOfCurrentViewAsync());
        }

        enum ViewMode
        {
            List,
            Grid
        }

        ViewMode CurrentViewMode = ViewMode.Grid;

        private void ViewModeButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateViewMode();
            Debug.WriteLine(CurrentViewMode.ToString());
            Settings.SetSetting(Settings.ExplorerPage_ViewMode, CurrentViewMode.ToString());

        }
    }
}
