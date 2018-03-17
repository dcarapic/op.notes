using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OP.Notes.Model
{
    /// <summary>
    /// Represents a notebook
    /// </summary>
    public interface INotebook : INotifyPropertyChanged
    {

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        string Title { get; set; }

        /// <summary>
        /// Gets the state.
        /// </summary>
        NotebookState State { get;  }

        /// <summary>
        /// Gets the created date.
        /// </summary>
        DateTime CreatedDate { get; }

        /// <summary>
        /// Gets all tags.
        /// </summary>
        IQueryable<string> AllTags { get; }

        /// <summary>
        /// Gets all notes.
        /// </summary>
        IQueryable<INote> AllNotes { get; }

        /// <summary>
        /// Occurs when a new tag gets added to any note and the tag never existed up to that point.
        /// </summary>
        event EventHandler<NoteTagEventArgs> TagAdded;

        /// <summary>
        /// Occurs when tag is no longer present in any note because note or tag on note was removed.
        /// </summary>
        event EventHandler<NoteTagEventArgs> TagRemoved;

        /// <summary>
        /// Occurs when a new note is added
        /// </summary>
        event EventHandler<NoteEventArgs> NoteAdded;

        /// <summary>
        /// Occurs when note is removed/deleted.
        /// </summary>
        event EventHandler<NoteEventArgs> NoteRemoved;

        /// <summary>
        /// Occurs when notebook is about to be saved.
        /// </summary>
        event EventHandler<NotebookEventArgs> BeforeSave;

        /// <summary>
        /// Occurs when notebook has been saved.
        /// </summary>
        event EventHandler<NotebookEventArgs> AfterSave;

        /// <summary>
        /// Saves the notebook.
        /// This will save all unsaved notes depending on the note persistance model.
        /// </summary>
        void Save();

        /// <summary>
        /// Creates a new note. Note is immediately present in the note list
        /// </summary>
        INote NewNote();

        /// <summary>
        /// Creates a new note. Note is immediately present in the note list
        /// </summary>
        INote NewNote(string title, string content, string[] tags);

        /// <summary>
        /// Deletes the notebook.
        /// </summary>
        void Delete();
    }

    /// <summary>
    /// Possible INotebook states
    /// </summary>
    public enum NotebookState
    {
        /// <summary>
        /// New, unsaved notebook
        /// </summary>
        New,
        /// <summary>
        /// Notebook was not modified after the last save
        /// </summary>
        Unmodified,
        /// <summary>
        /// Notebook was modified after the last save
        /// </summary>
        Modified,
        /// <summary>
        /// Notebook is deleted
        /// </summary>
        Deleted
    }
}
