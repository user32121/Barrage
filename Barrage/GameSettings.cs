using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

namespace Barrage
{
    //used for saving information
    struct GameSettings
    {
        //game
        public static bool useMouse;
        //editor
        public static bool useGrid;

        public static void Save()
        {
            BinaryWriter bw = new BinaryWriter(new FileStream("files/settings.dat", FileMode.Create));
            bw.Write(useMouse);
            bw.Write(useGrid);
        }

        public static bool TryLoad()
        {
            if (File.Exists("files/settings.dat"))
            {
                BinaryReader br = new BinaryReader(new FileStream("files/settings.dat", FileMode.Open));
                useMouse = br.ReadBoolean();
                useGrid = br.ReadBoolean();
                return true;
            }
            else 
                return false;
        }
    }
}
