using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Rendering;
using OP.Notes.Controls;
using OP.Notes.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace OP.Notes
{


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private List<INotebook> _notebooks = new List<INotebook>();
        private bool _loading = true;
        private DispatcherTimer _timer;
        private ucNotebook _currentEditedNotebookControl;

        private IEnumerable<ucNotebookTitle> AllNotebookTabControls
        {
            get { return lbNotebooks.Items.OfType<ucNotebookTitle>(); }
        }

        private IEnumerable<ucNotebook> AllNotebookControls
        {
            get { return AllNotebookTabControls.Select(li => (ucNotebook)li.Tag); }
        }

        private IEnumerable<INotebook> AllNotebooks
        {
            get { return AllNotebookControls.Select(uc => uc.Notebook); }
        }

        public MainWindow()
        {
            InitializeComponent();
            LoadNotebooks();
            this.WindowState = (System.Windows.WindowState)Enum.Parse(typeof(System.Windows.WindowState), Properties.Settings.Default.MainWindow_State);
            if (this.WindowState == System.Windows.WindowState.Normal)
            {
                if(Properties.Settings.Default.MainWindow_Bounds != System.Drawing.Rectangle.Empty)
                {
                    // restore position only if the position overlaps available screen
                    var virtScreen = new System.Drawing.Rectangle(
                        x: (int)SystemParameters.VirtualScreenLeft,
                        y: (int)SystemParameters.VirtualScreenTop,
                        width: (int)SystemParameters.VirtualScreenWidth,
                        height: (int)SystemParameters.VirtualScreenHeight);

                    virtScreen.Intersect(Properties.Settings.Default.MainWindow_Bounds);
                    if (virtScreen.Width > 50 && virtScreen.Height > 50)
                    {
                        this.Left = Properties.Settings.Default.MainWindow_Bounds.Left;
                        this.Top = Properties.Settings.Default.MainWindow_Bounds.Top;
                        this.Width = Properties.Settings.Default.MainWindow_Bounds.Width;
                        this.Height = Properties.Settings.Default.MainWindow_Bounds.Height;
                    }

                }
            }
            toggleAutosave.IsChecked = Properties.Settings.Default.AutoSave;
            toggleWrap.IsChecked = Properties.Settings.Default.WordWrap;
            toggleOnTop.IsChecked = Properties.Settings.Default.OnTop;
            toggleTags.IsChecked = Properties.Settings.Default.ShowTags;
            toggleTagsOnTop.IsChecked = Properties.Settings.Default.TagsOnTop;
            DetermineWindowIcons();


            _timer = new DispatcherTimer(TimeSpan.FromSeconds(10), DispatcherPriority.ApplicationIdle, OnSaveTimer, Dispatcher.CurrentDispatcher);
            this.Title = this.Title + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        private void LoadNotebooks()
        {
            try
            {
                _loading = true;
                foreach (var nb in NotebookProvider.Current.GetNotebooks())
                {
                    AddNotebook(nb, false);
                }
                if (!string.IsNullOrEmpty(Properties.Settings.Default.LastNotebook))
                {
                    var lastTag = AllNotebookTabControls.FirstOrDefault(t => t.Notebook.Title == Properties.Settings.Default.LastNotebook);
                    if (lastTag != null)
                        lastTag.IsSelected = true;
                }
                _loading = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occured while trying to load available notebooks: " + ex.ToString(), "OP.Notes", MessageBoxButton.OK, MessageBoxImage.Error);
                this.IsEnabled = false;
            }
        }

        private void AddNotebook(INotebook nb, bool editTitle)
        {
            _notebooks.Add(nb);
            var li = new ucNotebookTitle()
            {
                Notebook = nb,
                Tag = new ucNotebook()
                {
                    RenderTransform = new TranslateTransform(),
                    Notebook = nb,
                    WordWrap = toggleWrap.IsChecked.GetValueOrDefault(),
                    ShowTags = toggleTags.IsChecked.GetValueOrDefault(),
                    TagsOnTop = toggleTagsOnTop.IsChecked.GetValueOrDefault()
                }
            };
            li.NotebookDeleteRequested += (s, e) => DeleteNotebook(li.Notebook);
            lbNotebooks.Items.Add(li);
            lbNotebooks.SelectedItem = li;
            nb.BeforeSave += notebook_BeforeSave;
            nb.AfterSave += notebook_AfterSave;
        }

        private void DeleteNotebook(INotebook nb)
        {
            if (nb.State == NotebookState.New || System.Windows.MessageBox.Show("Are you sure you wish to remove the notebook: '" + nb.Title + "'?", "OP.Notes", MessageBoxButton.YesNo) ==
                MessageBoxResult.Yes)
            {
                nb.BeforeSave -= notebook_BeforeSave;
                nb.AfterSave -= notebook_AfterSave;
                _notebooks.Remove(nb);
                nb.Delete();
                var li = lbNotebooks.Items.OfType<ListBoxItem>().First(ti => ((ucNotebook)ti.Tag).Notebook == nb);
                var ctrlNb = (ucNotebook)li.Tag;
                if (pnlNotebooks.Children.Contains(ctrlNb))
                    pnlNotebooks.Children.Remove(ctrlNb);
                lbNotebooks.Items.Remove(li);

            }
        }

        private void OnSaveTimer(object sender, EventArgs e)
        {
            Save();
        }

        private void Save()
        {
            foreach (ucNotebook ctrlNotebook in AllNotebookControls)
            {
                if (ctrlNotebook.Notebook.State != NotebookState.New)
                    ctrlNotebook.Save();
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!(Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
                return;
            if (e.Key == Key.S)
            {
                Save();
            }
            else if (e.Key == Key.N && _currentEditedNotebookControl != null)
            {
                _currentEditedNotebookControl.Notebook.NewNote("[New note]", "[Note content]", null);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.MainWindow_State = this.WindowState.ToString();
            if (this.WindowState == System.Windows.WindowState.Normal)
            {
                var bounds = new System.Drawing.Rectangle(
                    x: (int)this.Left,
                    y: (int)this.Top,
                    width: (int)this.Width,
                    height: (int)this.Height);
                Properties.Settings.Default.MainWindow_Bounds = bounds;
            }
            Properties.Settings.Default.AutoSave = toggleAutosave.IsChecked.GetValueOrDefault();
            Properties.Settings.Default.WordWrap = toggleWrap.IsChecked.GetValueOrDefault();
            Properties.Settings.Default.OnTop = toggleOnTop.IsChecked.GetValueOrDefault();
            Properties.Settings.Default.ShowTags = toggleTags.IsChecked.GetValueOrDefault();
            Properties.Settings.Default.TagsOnTop = toggleTagsOnTop.IsChecked.GetValueOrDefault();

            var openedTab = AllNotebookTabControls.FirstOrDefault(t => t.IsSelected);
            if (openedTab != null)
                Properties.Settings.Default.LastNotebook = openedTab.Notebook.Title;
            Properties.Settings.Default.Save();
            Save();
        }

        private void lbNotebooks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedTab = AllNotebookTabControls.FirstOrDefault(li => li.IsSelected);
            if (selectedTab == null)
                return;

            var nbControl = (ucNotebook)selectedTab.Tag;
            var liUnselected = e.RemovedItems.OfType<ucNotebookTitle>().FirstOrDefault();
            var liSelected = e.AddedItems.OfType<ucNotebookTitle>().FirstOrDefault();
            int moveDirection = -1;
            if (liUnselected != null && liSelected != null)
            {
                if (lbNotebooks.Items.IndexOf(liUnselected) > lbNotebooks.Items.IndexOf(liSelected))
                    moveDirection = 1;
                else
                    moveDirection = -1;
            }
            if (liUnselected != null && liUnselected.IsEditingTitle)
                return;

            if (nbControl == _currentEditedNotebookControl)
                return;

            var currentControl = _currentEditedNotebookControl;
            _currentEditedNotebookControl = nbControl;
            if (_loading)
            {
                if (currentControl != null)
                    currentControl.Visibility = System.Windows.Visibility.Collapsed;
                if (!pnlNotebooks.Children.Contains(nbControl))
                    pnlNotebooks.Children.Add(nbControl);
                nbControl.Visibility = System.Windows.Visibility.Visible;
                return;
            }

            if (currentControl != null)
            {
                currentControl.AnimateTranslateTransform(new Point(0, 0), new Point(moveDirection * ActualWidth, 0), TimeSpan.FromMilliseconds(200), true).Then(() =>
                {
                    currentControl.Visibility = System.Windows.Visibility.Collapsed;
                });
            }
            ((TranslateTransform)nbControl.RenderTransform).X = -moveDirection * ActualWidth;
            nbControl.Visibility = System.Windows.Visibility.Visible;
            if (!pnlNotebooks.Children.Contains(nbControl))
                pnlNotebooks.Children.Add(nbControl);
            nbControl.AnimateTranslateTransform(new Point(-moveDirection * ActualWidth, 0), new Point(0, 0), TimeSpan.FromMilliseconds(200), true).Then(() =>
            {
                ((TranslateTransform)nbControl.RenderTransform).X = 0;
            });
        }


        void notebook_BeforeSave(object sender, NotebookEventArgs e)
        {
            imgStatusSave.ApplyAnimationClock(Image.OpacityProperty, null);
            imgStatusSave.Visibility = System.Windows.Visibility.Visible;
        }

        void notebook_AfterSave(object sender, NotebookEventArgs e)
        {
            DoubleAnimation anim = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromMilliseconds(200)));
            anim.Completed += (s, e1) =>
            {
                imgStatusSave.Visibility = System.Windows.Visibility.Hidden;
            };
            imgStatusSave.BeginAnimation(Image.OpacityProperty, anim);
        }

        private void toggleAutosave_Checked(object sender, RoutedEventArgs e)
        {
            if (_loading)
                return;
            _timer.IsEnabled = toggleAutosave.IsChecked.GetValueOrDefault();
        }

        private void toggleWrap_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var ctrlNb in AllNotebookControls)
            {
                ctrlNb.WordWrap = toggleWrap.IsChecked.GetValueOrDefault();
            }
        }

        private void toggleOnTop_Checked(object sender, RoutedEventArgs e)
        {
            this.Topmost = toggleOnTop.IsChecked.GetValueOrDefault();
        }

        private void toggleTags_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var ctrlNb in AllNotebookControls)
            {
                ctrlNb.ShowTags = toggleTags.IsChecked.GetValueOrDefault();
            }
        }

        private void toggleTagsOnTop_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var ctrlNb in AllNotebookControls)
            {
                ctrlNb.TagsOnTop = toggleTagsOnTop.IsChecked.GetValueOrDefault();
            }
        }


        private void btnNewNotebook_Click(object sender, RoutedEventArgs e)
        {
            var nb = NotebookProvider.Current.NewNotebook();
            AddNotebook(nb, true);
        }

        #region Window handling

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var hwndSource = (HwndSource)HwndSource.FromVisual(this);
            hwndSource.AddHook(WndProcHook);
            base.OnSourceInitialized(e);
        }

        private IntPtr WndProcHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            var result = IntPtr.Zero;
            if (msg == DwmApiInterop.WM_NCHITTEST)
            {

                int x = lParam.ToInt32() << 16 >> 16, y = lParam.ToInt32() >> 16;
                var point = PointFromScreen(new Point(x, y));
                var ht = VisualTreeHelper.HitTest(this, point);
                if (ht != null)
                {
                    var dp = ht.VisualHit;
                    while (dp != null) // possible drag
                    {
                        if (point.X < 4)
                        {
                            if (point.Y < 4)
                            {
                                result = new IntPtr(DwmApiInterop.HTTOPLEFT);
                                break;
                            }
                            else if (point.Y > (this.ActualHeight - 4))
                            {
                                result = new IntPtr(DwmApiInterop.HTBOTTOMLEFT);
                                break;
                            }
                            else
                            {
                                result = new IntPtr(DwmApiInterop.HTLEFT);
                                break;
                            }
                        }
                        else if (point.X > (this.ActualWidth - 4))
                        {
                            if (point.Y < 4)
                            {
                                result = new IntPtr(DwmApiInterop.HTTOPRIGHT);
                                break;
                            }
                            else if (point.Y > (this.ActualHeight - 4))
                            {
                                result = new IntPtr(DwmApiInterop.HTBOTTOMRIGHT);
                                break;
                            }
                            else
                            {
                                result = new IntPtr(DwmApiInterop.HTRIGHT);
                                break;
                            }
                        }
                        else
                        {
                            if (point.Y < 4)
                            {
                                result = new IntPtr(DwmApiInterop.HTTOP);
                                break;
                            }
                            else if (point.Y > (this.ActualHeight - 4))
                            {
                                result = new IntPtr(DwmApiInterop.HTBOTTOM);
                                break;
                            }
                        }

                        //System.Diagnostics.Debug.Print(lbNotebooks.Items.Count.ToString());
                        if (dp == borderCaption)
                        {
                            result = new IntPtr(DwmApiInterop.HTCAPTION);
                            break;
                        }
                        else if (lbNotebooks.Items.Cast<ListBoxItem>().Any(i => i == dp))
                        {
                            break;
                        } 
                        else if(dp == lbNotebooks)
                        {
                            result = new IntPtr(DwmApiInterop.HTCAPTION);
                            break;
                        }
                        dp = VisualTreeHelper.GetParent(dp);
                    }
                }
            }
            if(result != IntPtr.Zero)
                handled = true;
            return result;
        }
        

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnMaximize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = System.Windows.WindowState.Maximized;
        }

        private void btnRestore_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = System.Windows.WindowState.Normal;
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = System.Windows.WindowState.Minimized;
        }



        private void mySelf_StateChanged(object sender, EventArgs e)
        {
            DetermineWindowIcons();
        }

        private void DetermineWindowIcons()
        {
            if (this.WindowState == System.Windows.WindowState.Maximized)
            {
                btnMaximize.Visibility = System.Windows.Visibility.Collapsed;
                btnRestore.Visibility = System.Windows.Visibility.Visible;
                btnMinimize.Visibility = System.Windows.Visibility.Visible;
                borderCaption.Visibility = System.Windows.Visibility.Collapsed;
            }
            else if (this.WindowState == System.Windows.WindowState.Minimized)
            {
                btnMaximize.Visibility = System.Windows.Visibility.Visible;
                btnRestore.Visibility = System.Windows.Visibility.Visible;
                btnMinimize.Visibility = System.Windows.Visibility.Collapsed;
                borderCaption.Visibility = System.Windows.Visibility.Collapsed;
            }
            else
            {
                btnMaximize.Visibility = System.Windows.Visibility.Visible;
                btnRestore.Visibility = System.Windows.Visibility.Collapsed;
                btnMinimize.Visibility = System.Windows.Visibility.Visible;
                borderCaption.Visibility = System.Windows.Visibility.Visible;
            }
        }

        #endregion



    }


}
