using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPUpdater
{
    public class SPUpdater
    {
        public const string currentVer = "2.1";
        public const string versionText = "# Barrage Script v" + currentVer;

        static void Main(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
                Console.WriteLine(args[i]);

            string folder = "";
            if (args.Length > 0)
                folder = args[0];

            string SPPathPrefix = Path.Combine(folder, "SP");

            if (File.Exists(SPPathPrefix + ".txt"))
            {
                int oldIndex = 1;
                while (File.Exists(SPPathPrefix + "(old)" + oldIndex + ".txt"))
                    oldIndex++;

                File.Move(SPPathPrefix + ".txt", SPPathPrefix + "(old)" + oldIndex + ".txt");
                StreamReader sr = new StreamReader(SPPathPrefix + "(old)" + oldIndex + ".txt");
                StreamWriter sw = new StreamWriter(SPPathPrefix + ".txt");

                sw.Write(UpdateScript(sr.ReadToEnd()));

                sr.Close();
                sw.Close();
                Console.WriteLine("Finished converting SP.txt");
                Console.WriteLine("Unconverted version can be found at " + SPPathPrefix + "(old)" + oldIndex + ".txt");
            }
            else
            {
                Console.WriteLine("Could not find " + SPPathPrefix + ".txt");
            }
            Console.Write("Press enter to continue...");
            Console.ReadLine();
        }

        static string UpdateScript(string text)
        {
            List<string> lines = new List<string>(text.Split(new string[] { Environment.NewLine }, StringSplitOptions.None));

            for (int i = 0; i < lines.Count; i++)
            {
                //replace obsolete variables with relevant versions
                lines[i] = lines[i].
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
                if (lines[i].StartsWith("val") && char.IsDigit(lines[i][3]))
                    lines[i] = lines[i].Insert(3, "|");
                else if (lines[i].StartsWith("proj"))
                {
                    string[] props = lines[i].Split('|');
                    for (int j = 0; j < props.Length; j++)
                        if (props[j].StartsWith("xyPos"))
                            props[j] = props[j].Replace("xyPos", "xPos").Replace(",", "|yPos=");
                        else if (props[j].StartsWith("xyVel"))
                            props[j] = props[j].Replace("xyVel", "xVel").Replace(",", "|yVel=");
                        else if (props[j].StartsWith("startPos"))
                            props[j] = props[j].Replace("startPos", "startX").Replace(",", "|startY=");
                    lines[i] = string.Join("|", props);
                }
                else if (lines[i].StartsWith("ifGoto"))
                {
                    string[] spltLine = lines[i].Split('|');
                    //swap item 1 and 2 to change to gotoIf
                    string t = spltLine[1];
                    spltLine[1] = spltLine[2];
                    spltLine[2] = t;
                    spltLine[0] = "gotoIf";
                    lines[i] = string.Join("|", spltLine);
                }
            }

            //file format version
            if (!lines[0].StartsWith("# Barrage Script v"))
                lines.Insert(0, versionText);
            else
                lines[0] = versionText;

            return string.Join(Environment.NewLine, lines);
        }
    }
}
