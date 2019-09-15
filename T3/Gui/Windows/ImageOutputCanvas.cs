using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using T3.Gui.Graph;
using T3.Gui.Selection;
using UiHelpers;

namespace T3.Gui.Windows
{
    public class ImageOutputCanvas : ScalableCanvas
    {
        public override IEnumerable<ISelectable> SelectableChildren => throw new NotImplementedException();

        public override SelectionHandler SelectionHandler => throw new NotImplementedException();
    }
}
