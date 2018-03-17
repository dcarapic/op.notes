using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
namespace OP.Notes.Model
{
    /// <summary>
    /// Represents a note
    /// </summary>
    public interface INote : INotifyPropertyChanged
    {

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        string Title { get; set; }

        /// <summary>
        /// Gets the state.
        /// </summary>
        NoteState State { get; }

        /// <summary>
        /// Gets or sets the content.
        /// </summary>
        string Content { get; set; }

        /// <summary>
        /// Gets the created date.
        /// </summary>
        DateTime CreatedDate { get; }

        /// <summary>
        /// Gets the last updated date.
        /// </summary>
        DateTime LastUpdatedDate { get; }

        /// <summary>
        /// Gets the tags.
        /// </summary>
        IQueryable<string> Tags { get; }

        /// <summary>
        /// Adds the tag.
        /// </summary>
        /// <param name="tag">The tag.</param>
        void AddTag(string tag);

        /// <summary>
        /// Removes the tag.
        /// </summary>
        /// <param name="tag">The tag.</param>
        void RemoveTag(string tag);

        /// <summary>
        /// Occurs when a new Tag gets added to the Tags collection
        /// </summary>
        event EventHandler<NoteTagEventArgs> TagAdded;

        /// <summary>
        /// Occurs when a Tag is removed from the Tags collection
        /// </summary>
        event EventHandler<NoteTagEventArgs> TagRemoved;

        /// <summary>
        /// Occurs when a note is deleted
        /// </summary>
        event EventHandler<NoteEventArgs> NoteDeleted;

        /// <summary>
        /// Deletes the note.
        /// </summary>
        void Delete();
    }




    /// <summary>
    /// Possible INote states
    /// </summary>
    public enum NoteState
    {
        /// <summary>
        /// New, unsaved note
        /// </summary>
        New,
        /// <summary>
        /// Note was not modified after the last save
        /// </summary>
        Unmodified,
        /// <summary>
        /// Note was modified after the last save
        /// </summary>
        Modified,
        /// <summary>
        /// Note is deleted
        /// </summary>
        Deleted
    }
}
