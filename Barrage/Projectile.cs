using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Barrage
{
    class Projectile
    {
        public Image img;
        public Vector Position;
        public Vector Velocity;
        public Vector VelDir = new Vector(1, 1);
        public MainWindow.TAGS Tags;
        public object[] Speed;
        public object[] Angle;
        public object[] XPos;
        public object[] YPos;
        public object[] XVel;
        public object[] YVel;
        public object[] Radius;
        public int RadiusSqr;
        public object[] Duration;
        public object[] TagCount;
        public int TagUses;
        public object[] ActDelay;
        public object[] File;
        public bool IsAlive;
        public bool enabled;
        public int Age;
        public object[] state;

        public static readonly Dictionary<MainWindow.PROPERTIES, object[]> defaultProps = new Dictionary<MainWindow.PROPERTIES, object[]>()
        {
            { MainWindow.PROPERTIES.TAGS,     new object[] { MainWindow.TAGS.NONE } },
            { MainWindow.PROPERTIES.SPEED,    null },
            { MainWindow.PROPERTIES.ANGLE,    null },
            { MainWindow.PROPERTIES.XPOS,     null },
            { MainWindow.PROPERTIES.YPOS,     null },
            { MainWindow.PROPERTIES.XVEL,     null },
            { MainWindow.PROPERTIES.YVEL,     null },
            { MainWindow.PROPERTIES.SIZE,     new object[]{ 7.0 } },
            { MainWindow.PROPERTIES.STARTX,   null },
            { MainWindow.PROPERTIES.STARTY,   new object[]{ 100.0 } },
            { MainWindow.PROPERTIES.DURATION, new object[]{ -1.0 } },
            { MainWindow.PROPERTIES.TAGCOUNT, new object[]{ -1.0 } },
            { MainWindow.PROPERTIES.ACTDELAY, null },
            { MainWindow.PROPERTIES.FILE,     null },
            { MainWindow.PROPERTIES.STATE,    null },
        };

        public double[] projVars;
        public Dictionary<int, double> numValues;

        public Path path;

        public Projectile(object[] radius, double[] projVars)
        {
            Radius = radius;
            this.projVars = projVars;
            projVars[(int)MainWindow.PROJVARS.T] = Age;
            projVars[(int)MainWindow.PROJVARS.LXPOS] = Position.X;
            projVars[(int)MainWindow.PROJVARS.LYPOS] = Position.Y;
            ReadString.projVars = projVars;
            int r = (int)ReadString.Interpret(Radius);
            RadiusSqr = r * r;
            IsAlive = true;
        }

        public void Render()
        {
            int y1 = 0;
            projVars[(int)MainWindow.PROJVARS.T] = Age;
            projVars[(int)MainWindow.PROJVARS.LXPOS] = Position.X;
            projVars[(int)MainWindow.PROJVARS.LYPOS] = Position.Y;
            ReadString.projVars = projVars;
            TransformGroup TG = new TransformGroup();
            if (Tags.HasFlag(MainWindow.TAGS.LASER))
            {
                y1 = 50;    //add 50 to y because ...
                TG.Children.Add(new ScaleTransform(1, 6));
                img.Source = MainWindow.GetProjectileImage((int)ReadString.Interpret(File), true);
            }

            TG.Children.Add(new RotateTransform((double)ReadString.Interpret(Angle) - 90));
            if (Tags.HasFlag(MainWindow.TAGS.CIRCLE))
            {
                TG.Children.Add(new ScaleTransform(VelDir.X, VelDir.Y));
                img.Source = MainWindow.GetProjectileImage((int)ReadString.Interpret(File), false);
            }
            TG.Children.Add(new TranslateTransform(Position.X, Position.Y + y1));
            img.RenderTransform = TG;

            if (enabled)
                img.Opacity = 1;
            else
                img.Opacity = 0.3;

            double dr = ReadString.Interpret(Radius);
            if (double.IsNaN(dr))
                dr = 0;
            int r = Math.Abs((int)dr);
            if (img.Width / 2 != r)
            {
                if (Tags.HasFlag(MainWindow.TAGS.CIRCLE))
                {
                    img.Width = r * 2;
                    img.Height = r * 2;
                }
                else if (Tags.HasFlag(MainWindow.TAGS.LASER))
                {
                    img.Width = r * 2;
                }
                RadiusSqr = r * r;
            }
        }

        public void Move()
        {
            projVars[(int)MainWindow.PROJVARS.T] = Age;
            ReadString.projVars = projVars;
            ReadString.numVals = numValues;

            //ActDelay
            int temp = (int)ReadString.Interpret(ActDelay);
            if (temp > Age || temp == -1)
                enabled = false;
            else
                enabled = true;

            //increase age (for parameters with t)
            Age++;

            //projectiles that have duration have a limited lifespan
            temp = (int)ReadString.Interpret(Duration);
            if (temp != -1 && temp < Age)
                IsAlive = false;

            //radius
            int r = Math.Abs((int)ReadString.Interpret(Radius));

            //checks if offscreen (x)
            temp = (int)ReadString.Interpret(TagCount);
            if (Math.Abs(Position.X) > 200)
            {
                if (Tags.HasFlag(MainWindow.TAGS.WALLBOUNCE) && (temp == -1 || temp > TagUses))
                {
                    Position.X = 400 * Math.Sign(Position.X) - Position.X;
                    VelDir.X *= -1;
                    TagUses++;
                }
                else if (Tags.HasFlag(MainWindow.TAGS.SCREENWRAP) && (temp == -1 || temp > TagUses))
                {
                    Position.X -= 400 * Math.Sign(Position.X);
                    TagUses++;
                }
                else if (Math.Abs(Position.X) > 200 + r)
                    if (Tags.HasFlag(MainWindow.TAGS.OUTSIDE) && (temp == -1 || temp > TagUses))
                        TagUses++;
                    else
                        IsAlive = false;
            }
            //checks if offscreen (y)
            if (Math.Abs(Position.Y) > 200)
            {
                if (Tags.HasFlag(MainWindow.TAGS.WALLBOUNCE) && (temp == -1 || temp > TagUses))
                {
                    Position.Y = 400 * Math.Sign(Position.Y) - Position.Y;
                    VelDir.Y *= -1;
                    TagUses++;
                }
                else if (Tags.HasFlag(MainWindow.TAGS.SCREENWRAP) && (temp == -1 || temp > TagUses))
                {
                    Position.Y -= 400 * Math.Sign(Position.Y);
                    TagUses++;
                }
                else if (Math.Abs(Position.Y) > 200 + r)
                    if (Tags.HasFlag(MainWindow.TAGS.OUTSIDE) && (temp == -1 || temp > TagUses))
                        TagUses++;
                    else
                        IsAlive = false;
            }

            RadiusSqr = r * r;

            double ang, spd;
            //xyVel
            if (XVel != null && YVel != null)
            {
                Velocity = new Vector(ReadString.Interpret(XVel), ReadString.Interpret(YVel));
                spd = Velocity.Length;
                ang = Math.Atan2(Velocity.Y, Velocity.X);
            }
            //xyPos
            else if (XPos != null && YPos != null)
            {
                Velocity = new Vector(ReadString.Interpret(XPos), ReadString.Interpret(YPos)) - Position;
                spd = Velocity.Length;
                ang = Math.Atan2(Velocity.Y, Velocity.X);
            }
            //speed and angle
            else
            {
                ang = ReadString.Interpret(Angle);
                spd = ReadString.Interpret(Speed);
                double radians = ang * Math.PI / 180;
                Velocity = new Vector(Math.Cos(radians), Math.Sin(radians)) * spd;
            }

            //moves projectile by velocity
            Position += Velocity.Scale(VelDir);

            Render();

            double lstate = ReadString.Interpret(state);

            //sets last projVals
            projVars[(int)MainWindow.PROJVARS.T] = Age;
            projVars[(int)MainWindow.PROJVARS.LXPOS] = Position.X;
            projVars[(int)MainWindow.PROJVARS.LYPOS] = Position.Y;
            projVars[(int)MainWindow.PROJVARS.LXVEL] = Velocity.X;
            projVars[(int)MainWindow.PROJVARS.LYVEL] = Velocity.Y;
            projVars[(int)MainWindow.PROJVARS.LSPD] = spd;
            projVars[(int)MainWindow.PROJVARS.LANG] = ang;
            projVars[(int)MainWindow.PROJVARS.LSTATE] = lstate;
        }
    }
}
