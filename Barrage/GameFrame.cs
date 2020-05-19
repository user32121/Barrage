using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Barrage
{
    class GameFrame
    {
        Projectile[] projectiles;
        Vector plyrPos;
        Vector bossPos;
        double bossAngle;
        string bossTarget = "0,0";
        string bossMvSpd = "0";
        string bossAngSpd = "0";

        int time;
        int readIndex = 0;
        double wait;
        readonly Dictionary<int, int> repeatVals = new Dictionary<int, int>();    //(line,repeats left)
        int spwnInd;
        readonly List<double> spwnVals = new List<double>();
    }
}
