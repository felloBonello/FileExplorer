
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Runtime.Remoting.Messaging;

namespace FileExplorer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private delegate Node ParseDirDelegate();

        //tree display source
        ObservableCollection<Node> treeCtx = new ObservableCollection<Node>();
        Node firstNode;

        //Dependancy properties for all labels
        public string parseDir
        {
            get { return (string)GetValue(parseDirProp); }
            set { SetValue(parseDirProp, value); }
        }

        public static readonly DependencyProperty parseDirProp =
            DependencyProperty.Register("parseDir", typeof(string), typeof(MainWindow), new PropertyMetadata(""));

        public int folders
        {
            get { return (int)GetValue(foldersProp); }
            set { SetValue(foldersProp, value); }
        }

        public static readonly DependencyProperty foldersProp =
            DependencyProperty.Register("folders", typeof(int), typeof(MainWindow), new PropertyMetadata(0));

        public int files
        {
            get { return (int)GetValue(filesProp); }
            set { SetValue(filesProp, value); }
        }

        public static readonly DependencyProperty filesProp =
            DependencyProperty.Register("files", typeof(int), typeof(MainWindow), new PropertyMetadata(0));

        public int selectedFolders
        {
            get { return (int)GetValue(selectedFoldersProp); }
            set { SetValue(selectedFoldersProp, value); }
        }

        public static readonly DependencyProperty selectedFoldersProp =
            DependencyProperty.Register("selectedFolders", typeof(int), typeof(MainWindow), new PropertyMetadata(0));

        public int selectedFiles
        {
            get { return (int)GetValue(selectedFilesProp); }
            set { SetValue(selectedFilesProp, value); }
        }

        public static readonly DependencyProperty selectedFilesProp =
            DependencyProperty.Register("selectedFiles", typeof(int), typeof(MainWindow), new PropertyMetadata(0));

        public long sizeInBytes
        {
            get { return (long)GetValue(sizeInBytesProp); }
            set { SetValue(sizeInBytesProp, value); }
        }

        public static readonly DependencyProperty sizeInBytesProp =
            DependencyProperty.Register("sizeInBytes", typeof(long), typeof(MainWindow), new PropertyMetadata((long)0));

        public MainWindow()
        {
            InitializeComponent();
            //load up last file used
            LoadPathFile();
            DataContext = this;           
        }

        public void LoadPathFile()
        {
            //if saved path exists load else parse current dir
            if (File.Exists("./path.txt"))
                try
                {
                    using (StreamReader inputFile = new StreamReader("path.txt", true))
                    {
                        parseDir = inputFile.ReadLine();
                    }
                }
                catch(Exception err)
                {
                    System.Windows.MessageBox.Show("There has been an error loading the previously parsed directory.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            else
            {
                //Get full path of current directory
                parseDir = Path.GetFullPath(".");
            }               
        }

        private void createFirstNode()
        {
            //initialize first node to hold all other nodes
            DirectoryInfo dirInfo = new DirectoryInfo(parseDir);
            firstNode = new Node()
            {
                name = dirInfo.Name,
                fullPath = parseDir,
                byteSize = 0,
                parent = null,
                iconLoc = Node.folderIcon,
                isFile = false,
            };
            ++Node.folders;
            //add first node to display         
            treeCtx = new ObservableCollection<Node>()
            {
                firstNode
            };         
        }

        //update all dependacy properties to current static values in Node class
        private void UpdateCounts()
        {
            files = Node.files;
            folders = Node.folders;
            selectedFolders = Node.selectedFolders;
            selectedFiles = Node.selectedFiles;
            sizeInBytes = Node.selectedBytes;
        }

        private void ParseNewDir()
        {
            //resets all counts and displays to 0
            Node.resetCounts();
            UpdateCounts();

            //create first node, empty tree display, display parsing msg
            createFirstNode();
            fileDisplay.ItemsSource = null;
            parseMsg.Visibility = Visibility.Visible;

            //recursively parse directory asynchronously 
            ParseDirDelegate parseDelegate = new ParseDirDelegate(firstNode.DirParse);
            parseDelegate.BeginInvoke(theCallback, this);
        }

        public void theCallback(IAsyncResult theResults)
        {
            AsyncResult result = (AsyncResult)theResults;
            ParseDirDelegate parseDelegate = (ParseDirDelegate)result.AsyncDelegate;
            Node node = parseDelegate.EndInvoke(theResults);
            //Back to the GUI thread to update tree display and counts with newly parsed directory
            //hide parsing msg and display complete msg
            this.Dispatcher.Invoke(DispatcherPriority.Background, ((Action)(() => {
                parseMsg.Visibility = Visibility.Hidden;
                firstNode = node;
                firstNode.isChecked = false;
                UpdateCounts();
                fileDisplay.ItemsSource = treeCtx;
                System.Windows.MessageBox.Show(Properties.Resources.parseSuccess);
            })));
        }

        //directory changed -> parse the new directory
        private void dirDisplay_TextChanged(object sender, TextChangedEventArgs e)
        {
            ParseNewDir();
        }

        //checkbox clicked update counts display
        private void chk_clicked(object sender, RoutedEventArgs e)
        {
            UpdateCounts();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //display folder dialog so user can select directory to parse
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                    parseDir = fbd.SelectedPath;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //save current directory to text file on window close
            try
            {
                System.IO.File.WriteAllText(@"path.txt", parseDir);
            }
            catch(Exception err)
            {
                System.Windows.MessageBox.Show("There has been an error saving the current directory.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
