using OP.Notes.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    /// Interaction logic for ucNote.xaml
    /// </summary>
    public partial class ucNote : UserControl
    {

        private INote _note;
        private bool _loading;

        public INote Note
        {
            get { return _note; }
            set
            {
                if (_note == value)
                    return;
                _note = value;
                OnNoteSet();
            }
        }

        public bool WordWrap
        {
            get { return txtContent.WordWrap; }
            set { txtContent.WordWrap = value; }
        }

        public Regex HiglightWord
        {
            get { return txtContent.HighlightWords; }
            set { txtContent.HighlightWords = value;  }
        }

        public ucNote()
        {
            InitializeComponent();
            if (!System.IO.Directory.Exists(NotebookProvider.Current.ImageFilePath))
                System.IO.Directory.CreateDirectory(NotebookProvider.Current.ImageFilePath);
            txtContent.ImageFolder = NotebookProvider.Current.ImageFilePath;
        }

        /// <summary>
        /// Updates the note from contents of the editors.
        /// </summary>
        public void UpdateNote()
        {
            if (txtTags.IsFocused)
                txtTags_LostFocus(txtTags, null);
            if (txtTitle.IsFocused)
                txtTitle_LostFocus(txtTitle, null);
            if (txtContent.TextArea.IsFocused)
                txtContent_LostFocus(txtContent, null);
        }

        private void OnNoteSet()
        {
            if (_note == null)
                return;

            txtTitle.Text = _note.Title;
            txtContent.Text = _note.Content;
            LoadTags();
            _note.PropertyChanged += note_PropertyChanged;
            _note.TagAdded += note_TagAdded;
            _note.TagRemoved += note_TagRemoved;
            CheckNoteStatuses();
        }

        void note_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (_loading)
                return;
            if (e.PropertyName == "Title")
                txtTitle.Text = _note.Title;

            else if (e.PropertyName == "Content")
            {
                txtContent.Text = _note.Content;
                CheckNoteStatuses();
            }
        }

        void note_TagAdded(object sender, EventArgs e)
        {
            if (_loading)
                return;
            LoadTags();
        }

        void note_TagRemoved(object sender, EventArgs e)
        {
            if (_loading)
                return;
            LoadTags();
        }

        private void txtTags_LostFocus(object sender, RoutedEventArgs e)
        {
            List<string> tags = new List<string>();
            if (!string.IsNullOrWhiteSpace(txtTags.Text))
                tags.AddRange(txtTags.Text.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).Distinct());
            _loading = true;
            try
            {
                foreach (var tag in tags.Where(t=>!_note.Tags.Contains(t)))
                {
                    _note.AddTag(tag);
                }
                foreach (var tag in _note.Tags.Where(t => !tags.Contains(t)).ToList())
                {
                    _note.RemoveTag(tag);
                }
                LoadTags();
            }
            finally
            {
                _loading = false;
            }
        }

        private void txtTitle_LostFocus(object sender, RoutedEventArgs e)
        {
            _loading = true;
            try
            {
                _note.Title = txtTitle.Text;
            }
            finally
            {
                _loading = false;
            }
        }

        private void txtContent_LostFocus(object sender, RoutedEventArgs e)
        {
            _loading = true;
            try
            {
                //if (txtContent.IsFocused)
                //    return;
                //var carret = txtContent.CaretOffset;
                _note.Content = txtContent.Text;
                CheckNoteStatuses();
                //txtContent.CaretOffset = carret;
            }
            finally
            {
                _loading = false;
            }
        }

        private void LoadTags()
        {
            StringBuilder tags = new StringBuilder();
            foreach (var tag in _note.Tags)
            {
                if (tags.Length > 0)
                    tags.Append(", ");
                tags.Append(tag);
            }
            txtTags.Text = tags.ToString();
        }

        private void imgDelete_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_note.State == NoteState.New || System.Windows.MessageBox.Show("Are you sure you wish to remove the note: '" + _note.Title + "'?", "OP.Notes", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                _note.Delete();
            }
        }

        private void CheckNoteStatuses()
        {
            if (_note.Content.Contains("[]"))
            {
                imgTodoChecked.Visibility = System.Windows.Visibility.Collapsed;
                imgTodoUnhecked.Visibility = System.Windows.Visibility.Visible;
            } 
            else if (_note.Content.Contains("[x]"))
            {
                imgTodoChecked.Visibility = System.Windows.Visibility.Visible;
                imgTodoUnhecked.Visibility = System.Windows.Visibility.Collapsed;
            }
            else
            {
                imgTodoChecked.Visibility = System.Windows.Visibility.Collapsed;
                imgTodoUnhecked.Visibility = System.Windows.Visibility.Collapsed;
            }
        }


    }
}
