#undef TAS

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Barrage
{
    public partial class MainWindow
    {
#if TAS
        (int, int, int, int)[] TASInputs = new (int, int, int, int)[]
        {
            (0,0,0, 200),
            (-1,0,1, 10),
            (0,1,1, 5),
            (0,0,0, 1000),
        };
#endif
    }
}
