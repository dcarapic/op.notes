using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace OP.Notes.Controls
{
    public class ExtScrollViewer : ScrollViewer
    {

        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
        {
            base.OnPreviewMouseWheel(e);
            OnMouseWheel(e);
            e.Handled = true;
        }

        
        
    }
}
