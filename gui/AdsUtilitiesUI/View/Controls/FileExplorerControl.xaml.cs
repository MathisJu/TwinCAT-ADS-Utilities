using AdsUtilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AdsUtilitiesUI
{
    /// <summary>
    /// Interaction logic for FileExplorerControl.xaml
    /// </summary>
    public partial class FileExplorerControl : UserControl  // ToDo: Make this a page to make logging easier
    {
        public FileExplorerControl()
        {
            InitializeComponent();
            _viewModel = new();
            DataContext = _viewModel;
        }

        public FileExplorerViewModel _viewModel;    // ToDo: This dependency should not be set within the control, clean this up

        public static readonly DependencyProperty TargetProperty =
            DependencyProperty.Register("Target", typeof(StaticRoutesInfo), typeof(FileExplorerControl),
                new PropertyMetadata(default(StaticRoutesInfo), OnTargetChanged));

        public StaticRoutesInfo Target
        {
            get => (StaticRoutesInfo)GetValue(TargetProperty);
            set => SetValue(TargetProperty, value);
        }

        private static void OnTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is null)
                return;

            var control = (FileExplorerControl)d;
            if (control.DataContext is FileExplorerViewModel viewModel)
            {
                viewModel.Target = (StaticRoutesInfo)e.NewValue;
            }
        }

        private Point _startPoint;

        private void FileTreeView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Store the mouse position
            _startPoint = e.GetPosition(null);
        }

        private void FileTreeView_MouseMove(object sender, MouseEventArgs e)
        {
            // Get the current mouse position
            Point mousePos = e.GetPosition(null);
            Vector diff = _startPoint - mousePos;

            // Start the drag operation if the mouse has moved far enough
            if (e.LeftButton == MouseButtonState.Pressed &&
                (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                 Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                // Get the TreeView and the clicked TreeViewItem
                if (sender is not TreeView treeView) return;

                TreeViewItem treeViewItem = FindAncestor<TreeViewItem>((DependencyObject)e.OriginalSource);

                if (treeViewItem != null)
                {
                    // Start the drag operation and include the FileExplorerControl instance
                    var data = new DataObject(treeViewItem.DataContext);
                    data.SetData(typeof(FileExplorerControl), this);
                    DragDrop.DoDragDrop(treeViewItem, data, DragDropEffects.Copy);
                }
            }
        }

        private void FileTreeView_Drop(object sender, DragEventArgs e)
        {
            // Get the TreeView and the target TreeViewItem
            if (sender is not TreeView) return;

            TreeViewItem treeViewItem = FindAncestor<TreeViewItem>((DependencyObject)e.OriginalSource);

            if (treeViewItem != null)
            {
                if (treeViewItem.DataContext is FileSystemItem targetFolder)
                {
                    // Get the source FileExplorerControl

                    if (e.Data.GetData(typeof(FileExplorerControl)) is FileExplorerControl sourceControl && sourceControl != this)
                    {
                        // Handle the drop operation
                        if (e.Data.GetData(typeof(FileSystemItem)) is FileSystemItem sourceFile)
                        {
                            if (!sourceFile.IsDirectory && targetFolder.IsDirectory)
                            {
                                (DataContext as FileExplorerViewModel).CopyFile(sourceFile, targetFolder);
                            }
                        }
                    }
                }
            }
        }

        private void TreeViewItem_Expanded(object sender, RoutedEventArgs e)
        {
            if (e.Source is TreeViewItem item)
            {
                item.IsSelected = true;
            }
        }

        private void FileTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is FileSystemItem selectedItem && selectedItem.IsDirectory && selectedItem.Children.Count == 1 && selectedItem.Children[0] is null)
            {
                selectedItem.Children.Clear();
                selectedItem.LoadChildren();
            }
        }

        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T t)
                {
                    return t;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        private FileSystemItem GetSelectedFileSystemItem(object sender)
        {
            // Find the MenuItem that was clicked
            if (sender is MenuItem menuItem)
            {
                // Find the ContextMenu, then the TreeViewItem
                if (menuItem.Parent is ContextMenu contextMenu && contextMenu.PlacementTarget is FrameworkElement placementTarget)
                {
                    // Get the TreeViewItem
                    TreeViewItem treeViewItem = placementTarget.GetVisualParent<TreeViewItem>();

                    // Return the DataContext, which should be the FileSystemItem
                    return treeViewItem?.DataContext as FileSystemItem;
                }
            }
            return null;
        }

        private void Rename_Click(object sender, RoutedEventArgs e)
        {
            FileSystemItem? fileItem = GetSelectedFileSystemItem(sender);
            if (fileItem != null)
            {
                if (fileItem.IsSystemFile || fileItem.IsRoot)   // ToDo: Add log event that system files cannot be renamed
                    return;

                InputDialog inputDialog = new ("Rename File", "Enter new name:", fileItem.Name) { Owner = Application.Current.MainWindow };
                

                if (sender is FrameworkElement element)
                {
                    Point elementScreenPosition = element.PointToScreen(new Point(0, 0));

                    inputDialog.Left = elementScreenPosition.X;
                    inputDialog.Top = elementScreenPosition.Y; 
                }

                if (inputDialog.ShowDialog() == true)
                {
                    RenameFile(fileItem, inputDialog.ResponseText);
                }

            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            FileSystemItem? fileItem = GetSelectedFileSystemItem(sender);
            {
                if (fileItem != null)
                {
                    if (MessageBox.Show("Are you sure you want to delete this file?", "Delete Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        DeleteFile(fileItem);
                    }
                }
                
            }
        }

        private void Properties_Click(object sender, RoutedEventArgs e)
        {
            FileSystemItem? fileItem = GetSelectedFileSystemItem(sender);
            if (fileItem != null)
            {
                PropertiesWindow propertiesWindow = new(fileItem);

                if (sender is FrameworkElement element)
                {
                    Point elementScreenPosition = element.PointToScreen(new Point(0, 0));
                    propertiesWindow.Left = elementScreenPosition.X; 
                    propertiesWindow.Top = elementScreenPosition.Y; 


                }

                propertiesWindow.Show();
            }
        }

        private async Task RenameFile(FileSystemItem fileItem, string newName)
        {
            using AdsFileClient fileClient = new();
            await fileClient.Connect(fileItem.DeviceNetID);
            await fileClient.RenameFileAsync($"{fileItem.ParentDirectory}/{fileItem.Name}", $"{fileItem.ParentDirectory}/{newName}"); 
        }

        private async Task DeleteFile(FileSystemItem fileItem)
        {
            using AdsFileClient fileClient = new();
            await fileClient.Connect(fileItem.DeviceNetID);
            await fileClient.DeleteFileAsync($"{fileItem.ParentDirectory}/{fileItem.Name}");
        }
    }

    public static class VisualTreeHelperExtensions
    {
        public static T GetVisualParent<T>(this DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;

            if (parentObject is T parent)
            {
                return parent;
            }
            else
            {
                return GetVisualParent<T>(parentObject);
            }
        }
    }
}
