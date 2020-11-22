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

                    //replace obsolete variables with relevant versions
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

                    //change val# with val|# format
                    if (s.StartsWith("val") && char.IsDigit(s[3]))
                        s = s.Insert(3, "|");
                    else if (s.StartsWith("proj"))
                    {
                        string[] props = s.Split('|');
                        for (int i = 0; i < props.Length; i++)
                            if (props[i].StartsWith("xyPos"))
                                props[i] = props[i].Replace("xyPos", "xPos").Replace(",", "|yPos=");
                            else if (props[i].StartsWith("xyVel"))
                                props[i] = props[i].Replace("xyVel", "xVel").Replace(",", "|yVel=");
                            else if (props[i].StartsWith("startPos"))
                                props[i] = props[i].Replace("startPos", "startX").Replace(",", "|startY=");
                        s = string.Join("|", props);
                    }
                    else if(s.StartsWith("ifGoto"))
                    {
                        string[] spltLine = s.Split('|');
                        //swap item 1 and 2 to change to gotoIf
                        string t = spltLine[1];
                        spltLine[1] = spltLine[2];
                        spltLine[2] = t;
                        spltLine[0] = "gotoIf";
                        s = string.Join("|", spltLine);
                    }

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
