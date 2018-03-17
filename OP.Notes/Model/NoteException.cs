using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OP.Notes.Model
{
    [Serializable]
    public class NoteException : Exception
    {
        public NoteException() { }
        public NoteException(string message) : base(message) { }
        public NoteException(string message, Exception inner) : base(message, inner) { }
        protected NoteException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
