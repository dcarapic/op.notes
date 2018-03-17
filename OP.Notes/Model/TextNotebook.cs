using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OP.Notes.Model
{
    /// <summary>
    /// Represents a notebook
    /// </summary>
    public class TextNotebook : NotifyPropertyChanged, INotebook
    {
        private string _fileName;
        private string _folder;
        private HashSet<string> _allTags = new HashSet<string>();
        private List<TextNote> _allNotes = new List<TextNote>();
        private DateTime _createdDate = DateTime.Now;
        private NotebookState _state;
        private static readonly char[] _invalidFileCharacters = System.IO.Path.GetInvalidFileNameChars();

        private static readonly string _noteSeparator = new String('-', 50);
        private static readonly string _titleLineStart = "Title:";
        private static readonly string _tagsLineStart = "Tags:";
        private static readonly string _createdLineStart = "Created:";
        private static readonly string _updatedLineStart = "Updated:";


        public TextNotebook(string notebookFolder, string fileName)
        {
            if (string.IsNullOrEmpty(notebookFolder))
                throw new ArgumentException("notebookFolder", "notebookFolder may not be null or empty string");
            _folder = notebookFolder;
            _fileName = fileName;
            State = NotebookState.New;
            Load();
        }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        public string Title
        {
            get { return Path.GetFileNameWithoutExtension(_fileName); }
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentException("value", "Title can not be empty");
                var fileName = value + ".txt";
                if (_fileName == fileName)
                    return;

                if (value.Any(c => _invalidFileCharacters.Contains(c)))
                    throw new NoteException("Invalid notebook title: " + value + ". Following characters are not allowed: " + string.Join("  ", _invalidFileCharacters));

                if (State != NotebookState.New)
                {
                    Save();
                    var fullOldFileName = Path.Combine(_folder, _fileName);
                    var fullNewFileName = Path.Combine(_folder, fileName);
                    File.Move(fullOldFileName, fullNewFileName);
                    SetField(ref _fileName, fileName);
                }
                else
                {
                    SetField(ref _fileName, fileName);
                    Save();
                }
            }
        }


        /// <summary>
        /// Gets the state.
        /// </summary>
        public NotebookState State
        {
            get { return _state; }
            set
            {
                if (_state == value)
                    return;
                SetField(ref _state, value);
            }
        }


        /// <summary>
        /// Gets the created date.
        /// </summary>
        public DateTime CreatedDate
        {
            get { return _createdDate; }
        }


        /// <summary>
        /// Gets all tags from all notes.
        /// </summary>
        public IQueryable<string> AllTags
        {
            get { return _allTags.AsQueryable(); }
        }

        /// <summary>
        /// Gets all notes in the notebook.
        /// </summary>
        public IQueryable<INote> AllNotes
        {
            get { return _allNotes.AsQueryable(); }
        }



        /// <summary>
        /// Occurs when a new tag gets added to any note.
        /// </summary>
        public event EventHandler<NoteTagEventArgs> TagAdded;

        /// <summary>
        /// Occurs when tag is no longer present in any note.
        /// </summary>
        public event EventHandler<NoteTagEventArgs> TagRemoved;


        /// <summary>
        /// Occurs when a new note is added
        /// </summary>
        public event EventHandler<NoteEventArgs> NoteAdded;

        /// <summary>
        /// Occurs when note is removed/deleted.
        /// </summary>
        public event EventHandler<NoteEventArgs> NoteRemoved;

        /// <summary>
        /// Occurs when notebook is about to be saved.
        /// </summary>
        public event EventHandler<NotebookEventArgs> BeforeSave;

        /// <summary>
        /// Occurs when notebook has been saved.
        /// </summary>
        public event EventHandler<NotebookEventArgs> AfterSave;

        /// <summary>
        /// Saves the notebook.
        /// This will save all unsaved notes depending on the note persistance model.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Set the notebook title before calling save!</exception>
        public void Save()
        {
            if (string.IsNullOrEmpty(_fileName))
                return; // wait until filename is set
                //throw new InvalidOperationException("Set the notebook title before calling save!");

            if (State == NotebookState.Unmodified)
                return;

            var handler = BeforeSave;
            if (handler != null)
                handler(this, new NotebookEventArgs(this));

            // create backup first
            var fullFileName = Path.Combine(_folder, _fileName);
            if (File.Exists(fullFileName))
            {
                File.Copy(fullFileName, fullFileName + ".bak", true);
            }
            using (FileStream fs = new FileStream(fullFileName, FileMode.Create))
            using (StreamWriter tw = new StreamWriter(fs))
            {
                for (int i = 0; i < _allNotes.Count; i++)
                {
                    var note = _allNotes[i];
                    tw.WriteLine(_noteSeparator);
                    tw.WriteLine(_titleLineStart + note.Title);
                    tw.WriteLine(_createdLineStart + note.CreatedDate.ToString("s", CultureInfo.InvariantCulture));
                    tw.WriteLine(_updatedLineStart + note.LastUpdatedDate.ToString("s", CultureInfo.InvariantCulture));
                    tw.Write("Tags:");
                    foreach (var tag in note.Tags)
                        tw.Write(tag.Replace(",", "") + ",");
                    tw.WriteLine();
                    tw.Write(note.Content);
                    if (!note.Content.EndsWith(Environment.NewLine))
                        tw.WriteLine();
                    note.State = NoteState.Unmodified;
                }
            }
            State = NotebookState.Unmodified;
            handler = AfterSave;
            if (handler != null)
                handler(this, new NotebookEventArgs(this));
        }

        /// <summary>
        /// Loads the notebook
        /// </summary>
        private void Load()
        {
            var fullFileName = Path.Combine(_folder, _fileName);
            if (!File.Exists(fullFileName))
                return;
            var fi = new FileInfo(fullFileName);
            _createdDate = fi.CreationTime;
            string[] lines = File.ReadAllLines(fullFileName);
            var lastIndex = -1;
            var i = 0;
            for (i = 0; i < lines.Length - 1; i++)
            {
                if (lines[i] == _noteSeparator && i == 0) // skip first separator
                {
                    lastIndex = 1;
                    continue;
                }
                if(lines[i] == _noteSeparator)
                {
                    if(lastIndex != -1)
                    {
                        var note = ParseNote(lines.Skip(lastIndex).Take(i - lastIndex));
                        if (note != null)
                        {
                            _allNotes.Add(note);
                            note.TagAdded += Note_TagAdded;
                            note.TagRemoved += Note_TagRemoved;
                            note.PropertyChanged += Note_PropertyChanged;
                            foreach (var tag in note.Tags)
                                _allTags.Add(tag);
                        }
                    }
                    lastIndex = i+1;
                }
            }
            if (lastIndex != -1 && lastIndex != i)
            {
                var note = ParseNote(lines.Skip(lastIndex));
                if (note != null)
                {
                    _allNotes.Add(note);
                    note.TagAdded += Note_TagAdded;
                    note.TagRemoved += Note_TagRemoved;
                    foreach (var tag in note.Tags)
                        _allTags.Add(tag);
                }
            }
            State = NotebookState.Unmodified;
        }

        private TextNote ParseNote(IEnumerable<string> lines)
        {
            int index = 0;
            string titleLine = null;
            string tagLine = null;
            string createdDateLine = null;
            string lastUpdateDateLine = null;
            StringBuilder c = new StringBuilder();
            foreach (var line in lines)
            {
                if (index == 0 && line.StartsWith(_titleLineStart))
                {
                    titleLine = line.Substring(_titleLineStart.Length).Trim();
                    continue;
                }
                if (index == 0 && line.StartsWith(_tagsLineStart))
                {
                    tagLine = line.Substring(_tagsLineStart.Length).Trim();
                    continue;
                }
                if (index == 0 && line.StartsWith(_createdLineStart))
                {
                    createdDateLine = line.Substring(_createdLineStart.Length).Trim();
                    continue;
                }
                if (index == 0 && line.StartsWith(_updatedLineStart))
                {
                    lastUpdateDateLine = line.Substring(_updatedLineStart.Length).Trim();
                    continue;
                }
                c.AppendLine(line);
                index++;
            }
            IEnumerable<string> tags = Enumerable.Empty<string>();
            if(tagLine != null)
                tags = tagLine.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            DateTime createdDate = DateTime.Now;
            DateTime lastUpdateDate = DateTime.Now;
            try
            {
                if (createdDateLine != null)
                    createdDate = DateTime.ParseExact(createdDateLine, "s", CultureInfo.InvariantCulture);
            }
            catch { }
            try
            {
                if (lastUpdateDate != null)
                    lastUpdateDate = DateTime.ParseExact(lastUpdateDateLine, "s", CultureInfo.InvariantCulture);
            }
            catch { }

            return new TextNote(this, createdDate, lastUpdateDate, titleLine, c.ToString(), tags) { State = NoteState.Unmodified };
        }

        public INote NewNote()
        {
            TextNote note = new TextNote(this);
            note.CreatedDate = DateTime.Now;
            note.LastUpdatedDate = DateTime.Now;
            note.TagAdded += Note_TagAdded;
            note.TagRemoved += Note_TagRemoved;
            note.PropertyChanged += Note_PropertyChanged;
            if (State != NotebookState.New)
                State = NotebookState.Modified;
            _allNotes.Add(note);
            var handler = NoteAdded;
            if (handler != null)
                handler(this, new NoteEventArgs(this, note));
            return note;
        }


        /// <summary>
        /// Creates a new note. Note is immediately present in the note list
        /// </summary>
        /// <param name="title"></param>
        /// <param name="content"></param>
        /// <param name="tags"></param>
        /// <returns></returns>
        public INote NewNote(string title, string content, string[] tags)
        {
            TextNote note = new TextNote(this);
            note.Title = title;
            note.Content = content;
            note.CreatedDate = DateTime.Now;
            note.LastUpdatedDate = DateTime.Now;
            note.TagAdded += Note_TagAdded;
            note.TagRemoved += Note_TagRemoved;
            note.PropertyChanged += Note_PropertyChanged;
            if (tags != null)
                foreach (var tag in tags)
                {
                    note.AddTag(tag);
                }

            if (State != NotebookState.New)
                State = NotebookState.Modified;
            _allNotes.Add(note);
            var handler = NoteAdded;
            if (handler != null)
                handler(this, new NoteEventArgs(this, note));
            return note;
        }

        /// <summary>
        /// Deletes the note.
        /// </summary>
        /// <param name="note">The note.</param>
        public void DeleteNote(INote note)
        {
            foreach (var tag in note.Tags)
            {
                Note_TagRemoved(note, new NoteTagEventArgs(this, note, tag));
            }
            _allNotes.Remove((TextNote)note);
            note.TagAdded -= Note_TagAdded;
            note.TagRemoved -= Note_TagRemoved;
            note.PropertyChanged -= Note_PropertyChanged;
            if(State != NotebookState.New)
                State = NotebookState.Modified;
            var handler = NoteRemoved;
            if (handler != null)
                handler(this, new NoteEventArgs(this, note));
        }

        private void Note_TagAdded(object sender, NoteTagEventArgs e)
        {
            if (_allTags.Contains(e.Tag))
                return;

            _allTags.Add(e.Tag);
            var handler = TagAdded;
            if (handler != null)
                handler(this, new NoteTagEventArgs(this, e.Note, e.Tag));
            if (State != NotebookState.New)
                State = NotebookState.Modified;
        }

        private void Note_TagRemoved(object sender, NoteTagEventArgs e)
        {
            if (_allNotes.Any(n => n.Tags.Contains(e.Tag)))
                return;

            _allTags.Remove(e.Tag);
            var handler = TagRemoved;
            if (handler != null)
                handler(this, new NoteTagEventArgs(this, e.Note, e.Tag));
            if (State != NotebookState.New)
                State = NotebookState.Modified;
        }

        private void Note_PropertyChanged(object sender, EventArgs e)
        {
            if (State != NotebookState.New)
                State = NotebookState.Modified;
        }

        public void Delete()
        {
            if (State == NotebookState.New)
                return;

            var fullFileName = Path.Combine(_folder, _fileName);
            if (!File.Exists(fullFileName))
                return;
            Save(); // save will create backup
            File.Delete(fullFileName);
            State = NotebookState.Deleted;
        }



    }
}
