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
    /// Interaction logic for ucNotebook.xaml
    /// </summary>
    public partial class ucNotebook : UserControl
    {

        private bool _loading;
        private bool _wordWrap = true;

        private INotebook _notebook;

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

        public bool WordWrap
        {
            get { return _wordWrap; }
            set
            {
                _wordWrap = value;
                foreach (var ctrlNote in AllNoteControls)
                {
                    ctrlNote.WordWrap = value;
                }
            }
        }

        public bool ShowTags
        {
            get { return lbTags.Visibility == System.Windows.Visibility.Visible; }
            set { lbTags.Visibility = value ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed; }
        }

        public bool TagsOnTop
        {
            get { return DockPanel.GetDock(lbTags) == Dock.Top; }
            set
            {
                if (value == TagsOnTop)
                    return;
                DockPanel.SetDock(lbTags, value ? Dock.Top : Dock.Left);
                if (value)
                    lbTags.ItemsPanel = (ItemsPanelTemplate)Application.Current.FindResource("NbTagContainerHorizontalContainerStyle");
                else
                    lbTags.ItemsPanel = (ItemsPanelTemplate)Application.Current.FindResource("NbTagContainerVerticalContainerStyle");
            }
        }

        private IEnumerable<ListBoxItem> NotebookTagControls
        {
            get { return lbTags.Items.OfType<ListBoxItem>().Where(c => c != liAll && c != liTodo); }
        }

        private IEnumerable<ListBoxItem> AllTagControls
        {
            get { return lbTags.Items.OfType<ListBoxItem>(); }
        }

        private IEnumerable<ucNote> AllNoteControls
        {
            get { return pnlNotes.Children.OfType<ucNote>(); }
        }


        public ucNotebook()
        {
            InitializeComponent();
            ConfigureTabCheckbox(liAll);
            ConfigureTabCheckbox(liTodo);
        }

        private void OnNotebookSet()
        {
            _loading = true;
            try
            {
                foreach (var liTag in NotebookTagControls.ToList())
                {
                    lbTags.Items.Remove(liTag);
                }
                liAll.IsSelected = true;
                pnlNotes.Children.Clear();

                if (_notebook == null)
                    return;

                _notebook.TagAdded += notebook_TagAdded;
                _notebook.TagRemoved += notebook_TagRemoved;
                _notebook.NoteAdded += notebook_NoteAdded;
                _notebook.NoteRemoved += notebook_NoteRemoved;

                LoadTags();
                LoadLastTags();
                LoadNotes();
            }
            finally
            {
                _loading = false;
            }
        }

        public void Save()
        {
            foreach (var ctrlNote in AllNoteControls)
            {
                ctrlNote.UpdateNote(); // persist current editor changes
            }
            _notebook.Save();
        }

        private void SaveLastTags()
        {
            OP.Notes.Properties.Settings.Default.LastTags = null;
            String lastTags = string.Empty;
            var tagSegments = from ListBoxItem liTag in lbTags.SelectedItems
                              let chkFixedTag = (CheckBox)liTag.Template.FindName("chkFixedTag", liTag)
                              let tag = liTag.Tag == null ? liTag.Name : liTag.Tag.ToString()
                              select tag + "|" + (chkFixedTag.IsChecked.GetValueOrDefault() ? "1" : "0");
            lastTags = String.Join(";", tagSegments);
            OP.Notes.Properties.Settings.Default.LastTags = lastTags;
            OP.Notes.Properties.Settings.Default.Save();
        }

        private void LoadLastTags()
        {
            if (string.IsNullOrEmpty(OP.Notes.Properties.Settings.Default.LastTags))
                return;
            lbTags.SelectedItems.Clear();
            var tagSegments = OP.Notes.Properties.Settings.Default.LastTags.Split(new char[] { ';' });
            foreach(var tagSegment in tagSegments)
            {
                var parts = tagSegment.Split(new char[] { '|' });
                var tag = parts[0];
                var isChecked = parts[1] == "1" ? true : false;
                var liTag = lbTags.Items.Cast<ListBoxItem>().FirstOrDefault(li => ((string)li.Tag) == tag);
                if (liTag == null)
                    liTag = lbTags.Items.Cast<ListBoxItem>().FirstOrDefault(li => li.Name == tag);
                if (liTag == null)
                    continue;

                CheckBox chkFixedTag = (CheckBox)liTag.Template.FindName("chkFixedTag", liTag);
                chkFixedTag.IsChecked = isChecked;
                lbTags.SelectedItems.Add(liTag);
            }
        }


        private void lbTags_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loading) return;
            foreach (ListBoxItem liTag in e.RemovedItems)
            {
                CheckBox chkFixedTag = (CheckBox)liTag.Template.FindName("chkFixedTag", liTag);
                if (chkFixedTag.IsChecked.GetValueOrDefault())
                    lbTags.SelectedItems.Add(liTag);
            }
            SaveLastTags();
            LoadNotes();
        }

        void notebook_TagAdded(object sender, NoteTagEventArgs e)
        {
            LoadTags();
        }

        void notebook_TagRemoved(object sender, NoteTagEventArgs e)
        {
            LoadTags();
        }


        void notebook_NoteAdded(object sender, NoteEventArgs e)
        {
            if (AllNoteControls.Any(c => c.Note == e.Note))
                return;
            var ctrlNote = new ucNote()
            {
                Note = e.Note,
                WordWrap = WordWrap
            };
            foreach (var otherCtrlNote in AllNoteControls)
            {
                if (otherCtrlNote.Note.CreatedDate > ctrlNote.Note.CreatedDate)
                {
                    pnlNotes.Children.Insert(pnlNotes.Children.IndexOf(otherCtrlNote), ctrlNote);
                    break;
                }
            }
            if (!pnlNotes.Children.Contains(ctrlNote))
                pnlNotes.Children.Add(ctrlNote);
            //pnlNotes.UpdateLayout();
            scrollNotes.UpdateLayout();
            scrollNotes.ScrollToVerticalOffset(ctrlNote.TranslatePoint(new Point(0, 0), pnlNotes).Y);
        }

        void notebook_NoteRemoved(object sender, NoteEventArgs e)
        {
            var ctrlNote = pnlNotes.Children.OfType<ucNote>().FirstOrDefault(c => c.Note == e.Note);
            if (ctrlNote != null)
                pnlNotes.Children.Remove(ctrlNote);
        }


        private void LoadTags()
        {
            var allTags = _notebook.AllTags.ToList();
            var liTags = NotebookTagControls.ToList();
            foreach (var tag in allTags)
            {
                var liTag = liTags.FirstOrDefault(c => ((string)c.Tag) == tag);
                if (liTag == null)
                {
                    liTag = new ListBoxItem()
                    {
                        Tag = tag,
                        Style = (Style)Application.Current.FindResource("NbTagStyle"),
                        Content = tag
                    };
                    ConfigureTabCheckbox(liTag);
                    lbTags.Items.Add(liTag);
                }
            }
            foreach (var liTag in liTags)
            {
                if (!allTags.Any(t => t == ((string)liTag.Tag)))
                {
                    lbTags.Items.Remove(liTag);
                }
            }
        }

        private void ConfigureTabCheckbox(ListBoxItem liTag)
        {
            liTag.ApplyTemplate();
            CheckBox chkFixedTag = (CheckBox)liTag.Template.FindName("chkFixedTag", liTag);
            chkFixedTag.Checked += (s, e) =>
            {
                lbTags.SelectedItems.Add(liTag);
            };
            chkFixedTag.Unchecked += (s, e) =>
            {
                lbTags.SelectedItems.Remove(liTag);
            };

        }

        private void LoadNotes()
        {
            Save();
            var filter = CreateNoteFilter();
            foreach (var ctrlNote in AllNoteControls.ToList())
            {
                ctrlNote.UpdateNote(); // persist current editor changes
                if (!filter(ctrlNote.Note))
                    pnlNotes.Children.Remove(ctrlNote);
            }
            var matchingNotes = _notebook.AllNotes.Where(filter);
            foreach (var note in matchingNotes)
            {
                if (AllNoteControls.Any(nc => nc.Note.Equals(note)))
                    continue;
                var ctrlNote = new ucNote()
                {
                    Note = note,
                    WordWrap = WordWrap
                };
                foreach (var otherCtrlNote in AllNoteControls)
                {
                    if (otherCtrlNote.Note.CreatedDate > ctrlNote.Note.CreatedDate)
                    {
                        pnlNotes.Children.Insert(pnlNotes.Children.IndexOf(otherCtrlNote), ctrlNote);
                        break;
                    }
                }
                if (!pnlNotes.Children.Contains(ctrlNote))
                    pnlNotes.Children.Add(ctrlNote);
            }
        }

        private Func<INote, bool> CreateNoteFilter()
        {
            var filterPattern = txtFilterNotes.Text ?? txtFilterNotes.Text.Trim();
            var tags = NotebookTagControls.Where(c => c.IsSelected).Select(c => (string)c.Tag).ToList();
            List<Func<INote, bool>> filterChain = new List<Func<INote, bool>>();
            if (!liAll.IsSelected)
            {
                if (tags.Count > 0)
                    filterChain.Add(note => tags.All(t => note.Tags.Contains(t)));
                if (liTodo.IsSelected)
                    filterChain.Add(note => note.Content.Contains("[]"));
            }
            if (filterPattern != null)
                filterChain.Add(note => Regex.IsMatch(note.Title, filterPattern, RegexOptions.IgnoreCase) || Regex.IsMatch(note.Content, filterPattern, RegexOptions.IgnoreCase));

            return new Func<INote, bool>(note =>
            {
                return filterChain.All(f => f(note));
            });
        }

        private void txtFilterNotes_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && !string.IsNullOrWhiteSpace(txtFilterNotes.Text))
            {
                txtFilterNotes.Text = null;
            }
        }

        private void imgClearFilter_MouseUp(object sender, MouseButtonEventArgs e)
        {
            txtFilterNotes.Text = null;
            LoadNotes();
        }

        private void txtFilterNotes_TextChanged(object sender, TextChangedEventArgs e)
        {
            imgClearFilter.Visibility = String.IsNullOrWhiteSpace(txtFilterNotes.Text) ? Visibility.Hidden : Visibility.Visible;
            LoadNotes();
            Regex regex = null;
            if (!string.IsNullOrWhiteSpace(txtFilterNotes.Text))
                regex = new Regex(txtFilterNotes.Text.Trim(), RegexOptions.IgnoreCase);

            foreach (var ctrlNote in AllNoteControls)
            {
                ctrlNote.HiglightWord = regex;
            }
        }

        protected override void OnPreviewKeyUp(KeyEventArgs e)
        {
            base.OnPreviewKeyUp(e);
            if (e.Key == Key.F && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
                txtFilterNotes.Focus();
        }

    }
}
