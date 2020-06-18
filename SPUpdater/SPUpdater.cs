using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPUpdater
{
    class SPUpdater
    {
        static void Main(string[] args)
        {
            if (File.Exists("files/SP.txt"))
            {
                int oldIndex = 1;
                while (File.Exists("files/SP(old)" + oldIndex + ".txt"))
                    oldIndex++;

                File.Move("files/SP.txt", "files/SP(old)" + oldIndex + ".txt");
                StreamReader sr = new StreamReader("files/SP(old)" + oldIndex + ".txt");
                StreamWriter sw = new StreamWriter("files/SP.txt");

                string s;
                while (!sr.EndOfStream)
                {
                    s = sr.ReadLine();
                    s = s.
                        Replace("plyrx", "PLYRX").
                        Replace("plyry", "PLYRY").
                        Replace("bossx", "BOSSX").
                        Replace("bossy", "BOSSY").
                        Replace("lxPOS", "LPOSX").
                        Replace("lyPOS", "LPOSY").
                        Replace("lxVEL", "LVELX").
                        Replace("lyVEL", "LVELY").
                        Replace("lSPD", "LSPD").
                        Replace("lANG", "LANG").

                        Replace("XPOS", "POSX").
                        Replace("YPOS", "POSY").
                        Replace("LXPOS", "LPOSY").
                        Replace("LYPOS", "LPOSY").
                        Replace("LXVEL", "LVELX").
                        Replace("LYVEL", "LVELY").

                        Replace("POSX", "LPOSX").
                        Replace("POSY", "LPOSY").
                        Replace("LLLPOSX", "LPOSX").
                        Replace("LLLPOSY", "LPOSY").
                        Replace("LLPOSX", "LPOSX").
                        Replace("LLPOSY", "LPOSY");
                    sw.WriteLine(s);
                }
                sr.Close();
                sw.Close();
                Console.WriteLine("Finished converting SP.txt");
                Console.WriteLine("Unconverted version can be found as SP(old)" + oldIndex + ".txt");
            }
            else
            {
                Console.WriteLine("Could not find SP.txt in " + Path.GetFullPath("files"));
            }
            Console.ReadLine();
        }
    }
}
