using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AdsUtilities;

namespace AdsUtilitiesUI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public ObservableCollection<StaticRoutesInfo> StaticRoutes { get; set; }
    private ObservableCollection<FileSystemItem> _rootItems;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        StaticRoutes = new ObservableCollection<StaticRoutesInfo>();
        

        InitLogger();

        LoadRoutesAsync();

        LoadRootDirectories();
    }

    private void InitLogger()
    {

    }
    public struct FileInfoStruct
    {
        public string Name { get; set; }
        public bool IsDirectory { get; set; }
    }

    private void LoadRootDirectories()
    {
        string rootDirectory = "C:\\";  // adjust for different operating systems
        FileSystemItem rootFolder = new(
            Name: rootDirectory,
            IsDirectory: true,
            ParentDirectory: string.Empty,
            FileSize: 0,
            CreationTime: DateTime.Now,
            LastAccessTime: DateTime.Now,
            IsSystemFile: true,
            IsCompressed: false,
            IsRoot: true);
        rootFolder.LoadChildren();
        _rootItems = new ObservableCollection<FileSystemItem> { rootFolder };
        FileTreeView.ItemsSource = _rootItems;
        
    }

    private async void LoadRoutesAsync()
    {
        using AdsRoutingClient adsRoutingClient = new();
        adsRoutingClient.ConnectLocal();
        List<StaticRoutesInfo> routes = await adsRoutingClient.GetRoutesListAsync();

        foreach (var route in routes)
        {
            StaticRoutes.Add(route);
        }
    }

    private void CmbBx_SelectRoute_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

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
    private void TreeViewItem_RightClick(object sender, MouseButtonEventArgs e)
    {
        TreeViewItem item = sender as TreeViewItem;
        if (item != null)
        {
            item.Focus();
            e.Handled = true;

            ContextMenu contextMenu = new ContextMenu();
            MenuItem menuItem = new MenuItem();
            menuItem.Header = "Show Info";
            menuItem.Click += (s, args) => ShowItemInfo(item.DataContext);
            contextMenu.Items.Add(menuItem);
            contextMenu.IsOpen = true;
        }
    }

    private static void ShowItemInfo(object dataContext)
    {
        if (dataContext is FileSystemItem fileItem)
        {
            string message =
                $"File size: {FormatFileSize(fileItem.FileSize)}\n" +
                $"Created: {fileItem.CreationTime}\n" +
                $"Accessed: {fileItem.LastAccessTime}";
                
            MessageBox.Show(message, "File Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        
    }

    private static string FormatFileSize(long byteSize)
    {
        const long KiloByte = 1024;
        const long MegaByte = KiloByte * 1024;
        const long GigaByte = MegaByte * 1024;

        if (byteSize >= GigaByte)
        {
            return $"{(byteSize / (double)GigaByte):F2} GB";
        }
        else if (byteSize >= MegaByte)
        {
            return $"{(byteSize / (double)MegaByte):F2} MB";
        }
        else if (byteSize >= KiloByte)
        {
            return $"{(byteSize / (double)KiloByte):F2} KB";
        }
        else
        {
            return $"{byteSize} Bytes";
        }
    }
}

public class FileSystemItem
{
    public string Name { get; }
    public bool IsDirectory { get; }
    public string ParentDirectory { get; }
    public bool IsHidden { get; }
    public long FileSize { get; }
    public DateTime CreationTime { get; }
    public DateTime LastAccessTime { get; }
    public bool IsSystemFile { get; }
    public bool IsCompressed { get; }

    public BitmapImage Image { get; }
    public bool IsRoot { get; }

    public ObservableCollection<FileSystemItem> Children { get; set; } = new ObservableCollection<FileSystemItem>();

    public FileSystemItem(string Name, bool IsDirectory, string ParentDirectory, long FileSize, DateTime CreationTime, DateTime LastAccessTime, bool IsSystemFile, bool IsCompressed, bool IsRoot)
    {
        this.Name = Name;
        this.ParentDirectory = ParentDirectory;
        this.FileSize = FileSize;
        this.CreationTime = CreationTime;
        this.LastAccessTime = LastAccessTime;
        this.IsSystemFile = IsSystemFile;
        this.IsCompressed = IsCompressed;
        this.IsRoot = IsRoot;
        this.IsDirectory = IsDirectory;
        if (IsRoot)
        {
            this.Image = LoadBitmapImage("Images/harddrive.png");
        }
        else if (IsDirectory)
        {
            if (IsHidden)
                this.Image = LoadBitmapImage("Images/folderdrive_hidden.png");
            else
                this.Image = LoadBitmapImage("Images/folderdrive.png");
        }
        else
        {
            if (IsHidden)
                this.Image = LoadBitmapImage("Images/file_simple_hidden.png");
            else
                this.Image = LoadBitmapImage("Images/file_simple.png");
        }
    }

    public async void LoadChildren()
    {
        using AdsFileClient fileClient = new();
        fileClient.ConnectLocal();
        string fullPath = System.IO.Path.Combine(ParentDirectory, Name);
        var content = await fileClient.GetFolderContentListAsync(fullPath);
        foreach (var file in content)
        {
            FileSystemItem item = new 
            (
                Name : file.fileName,
                IsDirectory: file.isDirectory,
                ParentDirectory: fullPath,
                FileSize: file.fileSize,
                CreationTime: file.creationTime,
                LastAccessTime: file.lastAccessTime,
                IsSystemFile: file.isSystemFile,
                IsCompressed: file.isCompressed,
                IsRoot: false
            );
            if (item.IsDirectory)
            {
                item.Children.Add(null); // Placeholder for Lazy Loading
            }
            Children.Add(item);
        }
    }
    private static BitmapImage LoadBitmapImage(string relativePath)
    {
        var uri = new Uri($"pack://application:,,,/{relativePath}");
        return new BitmapImage(uri);
    }

    
}
