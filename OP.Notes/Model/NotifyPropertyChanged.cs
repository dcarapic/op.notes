using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace OP.Notes.Model
{
    public abstract class NotifyPropertyChanged : INotifyPropertyChanged
    {



        protected void SetField<T>(ref T field, T value, [CallerMemberName]string caller = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return;
            field = value;
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(caller));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

    }
}
