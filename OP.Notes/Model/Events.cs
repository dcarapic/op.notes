using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OP.Notes.Model
{
    public class NoteTagEventArgs : NoteEventArgs
    {
        public string Tag { get; private set; }

        public NoteTagEventArgs(INotebook notebook, INote note, string tag)
            : base(notebook, note)
        {
            Tag = tag;
        }

    }

    public class NoteEventArgs : NotebookEventArgs
    {
        public INote Note { get; private set; }

        public NoteEventArgs(INotebook notebook, INote note)
            : base(notebook)
        {
            Note = note;
        }

    }

    public class NotebookEventArgs : EventArgs
    {
        public INotebook Notebook { get; private set; }

        public NotebookEventArgs(INotebook notebook)
        {
            Notebook = notebook;
        }

    }

}
