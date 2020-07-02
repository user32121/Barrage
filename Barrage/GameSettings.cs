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
        public static bool useMouse = false;
        public static bool checkForInfiniteLoop = true;
        public static bool useGrid = true;
        public static int maxHistFrames = 1000;
        public static bool autoPlay = false;
        public static bool checkForErrors = true;

        public static void Save()
        {
            BinaryWriter bw = new BinaryWriter(new FileStream("files/settings.dat", FileMode.Create));
            bw.Write(useMouse);
            bw.Write(checkForInfiniteLoop);
            bw.Write(useGrid);
            bw.Write(maxHistFrames);
            bw.Write(autoPlay);
            bw.Write(checkForErrors);
            bw.Close();
        }

        public static bool TryLoad()
        {
            if (File.Exists("files/settings.dat"))
            {
                BinaryReader br = new BinaryReader(new FileStream("files/settings.dat", FileMode.Open));
                try
                {
                    useMouse = br.ReadBoolean();
                    checkForInfiniteLoop = br.ReadBoolean();
                    useGrid = br.ReadBoolean();
                    maxHistFrames = br.ReadInt32();
                    autoPlay = br.ReadBoolean();
                    checkForErrors = br.ReadBoolean();
                }
                catch (EndOfStreamException) { }
                br.Close();
                return true;
            }
            else
                return false;
        }
    }
}
