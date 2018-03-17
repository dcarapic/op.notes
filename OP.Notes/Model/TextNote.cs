using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace OP.Notes.Model
{

    /// <summary>
    /// Note for TextNotebook
    /// </summary>
    public class TextNote : NotifyPropertyChanged, INote
    {

        private DateTime _createdDate;
        private DateTime _lastUpdate;
        private string _title;
        private string _content;
        private List<string> _tags = new List<string>();
        private TextNotebook _notebook;
        private NoteState _state;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextNote"/> class.
        /// </summary>
        public TextNote(TextNotebook notebook)
        {
            _notebook = notebook;
            State = NoteState.New;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TextNote" /> class.
        /// </summary>
        /// <param name="notebook">The notebook.</param>
        /// <param name="createdDate">The created date.</param>
        /// <param name="lastUpdateDate">The last update date.</param>
        /// <param name="title">The title.</param>
        /// <param name="content">The content.</param>
        /// <param name="tags">The tags.</param>
        public TextNote(TextNotebook notebook, DateTime createdDate, DateTime lastUpdateDate, string title, string content, IEnumerable<String> tags)
        {
            _notebook = notebook;
            _createdDate = createdDate;
            _lastUpdate = lastUpdateDate;
            _title = title;
            _content = content;
            _tags.AddRange(tags);
            State = NoteState.New;
        }

        /// <summary>
        /// Gets the created date.
        /// </summary>
        public DateTime CreatedDate
        {
            get { return _createdDate; }
            set
            {
                if (_createdDate == value)
                    return;
                SetField(ref _createdDate, value);
            }
        }

        /// <summary>
        /// Gets the last updated date.
        /// </summary>
        public DateTime LastUpdatedDate
        {
            get { return _lastUpdate; }
            set
            {
                if (_lastUpdate == value)
                    return;
                SetField(ref _lastUpdate, value);
                if (State != NoteState.New)
                    State = NoteState.Modified;
            }
        }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        public string Title
        {
            get { return _title; }
            set 
            {
                if (_title == value)
                    return;
                SetField(ref _title, value);
                LastUpdatedDate = DateTime.Now;
                if (State != NoteState.New)
                    State = NoteState.Modified;
            }
        }


        /// <summary>
        /// Gets the state.
        /// </summary>
        public NoteState State
        {
            get { return _state; }
            set
            {
                if (_state == value)
                    return;
                SetField(ref _state, value);
                if (State == NoteState.Modified)
                    _notebook.State = NotebookState.Modified;
            }
        }

        /// <summary>
        /// Gets or sets the content.
        /// </summary>
        public string Content
        {
            get { return _content; }
            set 
            {
                if (_content == value)
                    return;
                SetField(ref _content, value);
                LastUpdatedDate = DateTime.Now;
                if (State != NoteState.New)
                    State = NoteState.Modified;
            }
        }

        /// <summary>
        /// Gets the tags.
        /// </summary>
        public IQueryable<string> Tags
        {
            get { return _tags.AsQueryable();  }
        }

        /// <summary>
        /// Occurs when a new tag gets added to note.
        /// </summary>
        public event EventHandler<NoteTagEventArgs> TagAdded;

        /// <summary>
        /// Occurs when tag is no longer present in note.
        /// </summary>
        public event EventHandler<NoteTagEventArgs> TagRemoved;

        /// <summary>
        /// Occurs when a note is deleted
        /// </summary>
        public event EventHandler<NoteEventArgs> NoteDeleted;

        /// <summary>
        /// Adds the tag.
        /// </summary>
        /// <param name="tag">The tag.</param>
        public void AddTag(string tag)
        {
            if (_tags.Contains(tag))
                return;
            _tags.Add(tag);
            var handler = TagAdded;
            if (handler != null)
                handler(this, new NoteTagEventArgs(_notebook, this, tag));
            if (State != NoteState.New)
                State = NoteState.Modified;
        }

        /// <summary>
        /// Removes the tag.
        /// </summary>
        /// <param name="tag">The tag.</param>
        public void RemoveTag(string tag)
        {
            if (!_tags.Remove(tag))
                return;
            var handler = TagRemoved;
            if (handler != null)
                handler(this, new NoteTagEventArgs(_notebook, this, tag));
            if(State != NoteState.New)
                State = NoteState.Modified;
        }

        public void Delete()
        {
            _notebook.DeleteNote(this);
            var handler = NoteDeleted;
            if (handler != null)
                handler(this, new NoteEventArgs(_notebook, this));

            State = NoteState.Deleted;
        }


    }
}
