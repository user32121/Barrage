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
        public Projectile[] projectiles;
        public Vector plyrPos;
        public Vector bossPos;
        public double bossAngle;
        public (object[], object[]) bossTarget;
        public object[] bossMvSpd;
        public object[] bossAngSpd;

        public int time;
        public int readIndex;
        public double wait;
        public Dictionary<int, int> repeatVals;    //(line,repeats left)
        public int spwnInd;
        public double[] spwnVals;
    }
}
