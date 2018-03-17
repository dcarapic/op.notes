using OP.Notes.Model;
using System;
using System.Collections.Generic;
using System.Linq;
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

namespace OP.Notes.Controls
{
    /// <summary>
    /// Interaction logic for ucNotebookTitle.xaml
    /// </summary>
    public partial class ucNotebookTitle : ListBoxItem
    {

        private INotebook _notebook;
        private bool _isEditingTitle;

        /// <summary>
        /// Gets a value indicating whether [is editing title].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [is editing title]; otherwise, <c>false</c>.
        /// </value>
        public bool IsEditingTitle
        {
            get { return _isEditingTitle; }
        }

        /// <summary>
        /// Gets or sets the notebook.
        /// </summary>
        /// <value>
        /// The notebook.
        /// </value>
        public INotebook Notebook
        {
            get { return _notebook; }
            set
            {
                if (_notebook == value)
                    return;
                _notebook = value;
                OnNotebookSet();
            }
        }

        /// <summary>
        /// Occurs when [notebook delete requested].
        /// </summary>
        public event EventHandler NotebookDeleteRequested;

        public ucNotebookTitle()
        {
            InitializeComponent();
        }

        private void OnNotebookSet()
        {
            txtTitle.Text = _notebook.Title;
            SetTitleEditable(_notebook.State == NotebookState.New);
            if (_notebook.State == NotebookState.New)
            {
                System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback((object i) =>
                {
                    System.Threading.Thread.Sleep(100);
                    txtTitle.Dispatcher.Invoke(new Action(() => { txtTitle.Focus(); }));
                }));
            }
            _notebook.PropertyChanged += _notebook_PropertyChanged;
            if (_notebook.State == NotebookState.Unmodified)
                lblModified.Visibility = System.Windows.Visibility.Collapsed;
            else
                lblModified.Visibility = System.Windows.Visibility.Visible;
        }

        void _notebook_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(e.PropertyName == "State")
            {

                if (_notebook.State == NotebookState.Unmodified)
                    lblModified.Visibility = System.Windows.Visibility.Collapsed;
                else
                    lblModified.Visibility = System.Windows.Visibility.Visible;
            }
        }

        void SetTitleEditable(bool editable)
        {
            if (editable)
            {
                txtTitle.Cursor = Cursors.IBeam;
                txtTitle.IsReadOnly = false;
                txtTitle.SelectAll();
                _isEditingTitle = true;
            }
            else
            {
                txtTitle.Cursor = Cursors.Hand;
                txtTitle.IsReadOnly = true;
                txtTitle.Select(0, 0);
                _isEditingTitle = false;
            }
        }

        private void OnNotebookDeleteRequested()
        {
            var handler = NotebookDeleteRequested;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        private void imgDelete_MouseUp(object sender, MouseButtonEventArgs e)
        {
            OnNotebookDeleteRequested();
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            imgDelete.Visibility = System.Windows.Visibility.Visible;
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            imgDelete.Visibility = System.Windows.Visibility.Hidden;
        }

        private void txtTitle_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (txtTitle.SelectionLength == 0)
                return;
            if (txtTitle.IsReadOnly)
                txtTitle.Select(0, 0);
        }

        private void txtTitle_GotFocus(object sender, RoutedEventArgs e)
        {
            this.IsSelected = true;
        }

        private void txtTitle_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTitle.Text))
            {
                Dispatcher.BeginInvoke(new Action(() => txtTitle.Focus()));
                return;
            }

            try
            {
                _notebook.Title = txtTitle.Text.Trim();
            }
            catch (NoteException ex)
            {
                MessageBox.Show(ex.Message, "OP.Notes", MessageBoxButton.OK, MessageBoxImage.Error);
                txtTitle.Text = _notebook.Title;
                Dispatcher.BeginInvoke(new Action(() => txtTitle.Focus()));
                return;
            }
            SetTitleEditable(false);
        }

        private void txtTitle_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (_notebook.State == NotebookState.New)
                {
                    OnNotebookDeleteRequested();
                }
                else
                {
                    txtTitle.Text = _notebook.Title;
                    SetTitleEditable(false);
                }
            }
            else if (e.Key == Key.Enter)
            {
                SetTitleEditable(false);
            }
        }

        private void txtTitle_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!_isEditingTitle)
                SetTitleEditable(true);
        }

    }
}
