using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OP.Notes.Model
{
    public class NotebookProvider
    {

        private string _notebookFolder = Properties.Settings.Default.NotebookFolder;

        private static NotebookProvider _current = new NotebookProvider();
        public static NotebookProvider Current
        {
            get { return _current; }
        }

        public IEnumerable<INotebook> GetNotebooks()
        {
            if (!System.IO.Path.IsPathRooted(_notebookFolder))
            {
                var location = System.Reflection.Assembly.GetEntryAssembly().Location;
                location = System.IO.Path.GetDirectoryName(location);
                _notebookFolder = System.IO.Path.Combine(location, _notebookFolder);
            }
            if (!System.IO.Directory.Exists(_notebookFolder))
                System.IO.Directory.CreateDirectory(_notebookFolder);
            var notebookFiles = System.IO.Directory.GetFiles(_notebookFolder, "*.txt");
            foreach (var nbFile in notebookFiles)
            {
                yield return new TextNotebook(_notebookFolder, System.IO.Path.GetFileName(nbFile));
            }
        }

        public string ImageFilePath
        {
            get 
            {  
                var path = System.IO.Path.Combine(_notebookFolder, "Images");
                return path;
            }
        }

        public INotebook NewNotebook()
        {
            var nb = new TextNotebook(_notebookFolder, string.Empty);
            var note = nb.NewNote();
            note.Title = "[New note]";
            note.Content = "[Note content]";
            return nb;
        }

    }
}
