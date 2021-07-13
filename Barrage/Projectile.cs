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
        public object[] SpeedRS;  //RS postfix means it needs to be interpreted by ReadString
        public object[] AngleRS;
        public double Angle;  //angle in degrees
        public object[] XPosRS;
        public object[] YPosRS;
        public object[] XVelRS;
        public object[] YVelRS;
        public object[] RadiusRS;
        public int Radius;  //radius of circle or half-width of laser
        public int RadiusSqr;
        public object[] DurationRS;
        public object[] TagCountRS;
        public int TagUses;
        public object[] ActDelayRS;
        public object[] FileRS;
        public bool IsAlive;
        public bool enabled;  //whether or not the projectile is active and can collide with player
        public int Age;
        public object[] stateRS;

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
            { MainWindow.PROPERTIES.STARTY,   new object[]{ -100.0 } },
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
            RadiusRS = radius;
            this.projVars = projVars;
            projVars[(int)MainWindow.PROJVARS.T] = Age;
            projVars[(int)MainWindow.PROJVARS.LXPOS] = Position.X;
            projVars[(int)MainWindow.PROJVARS.LYPOS] = Position.Y;
            ReadString.projVars = projVars;
            Radius = (int)ReadString.Interpret(RadiusRS, MainWindow.PARAMETERS.SIZE);
            RadiusSqr = Radius * Radius;
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
                y1 = 50;    //add 50 to y because ... ?
                img.Source = MainWindow.GetProjectileImage((int)ReadString.Interpret(FileRS, MainWindow.PARAMETERS.FILE), true);
                TG.Children.Add(new ScaleTransform(1, MainWindow.laserImgScale));
            }

            TG.Children.Add(new RotateTransform((Angle = ReadString.Interpret(AngleRS, MainWindow.PARAMETERS.ANGLE)) - 90));
            if (Tags.HasFlag(MainWindow.TAGS.CIRCLE))
            {
                TG.Children.Add(new ScaleTransform(VelDir.X, VelDir.Y));
                img.Source = MainWindow.GetProjectileImage((int)ReadString.Interpret(FileRS, MainWindow.PARAMETERS.FILE), false);
            }
            TG.Children.Add(new TranslateTransform(Position.X, Position.Y + y1));
            img.RenderTransform = TG;

            if (enabled)
                img.Opacity = 1;
            else
                img.Opacity = 0.3;

            double dr = ReadString.Interpret(RadiusRS, MainWindow.PARAMETERS.SIZE);
            if (double.IsNaN(dr))
                dr = 0;
            Radius = Math.Abs((int)dr);
            const double imgScale = 3;  //radius factor to increase appearance
            if (img.Width / imgScale != Radius)
            {
                if (Tags.HasFlag(MainWindow.TAGS.CIRCLE))
                {
                    img.Width = Radius * imgScale;
                    img.Height = Radius * imgScale;
                }
                if (Tags.HasFlag(MainWindow.TAGS.LASER))
                {
                    img.Width = Radius * imgScale;
                }
                RadiusSqr = Radius * Radius;
            }
        }

        public void Move()
        {
            projVars[(int)MainWindow.PROJVARS.T] = Age;
            ReadString.projVars = projVars;
            ReadString.numVals = numValues;

            //ActDelay
            int temp = (int)ReadString.Interpret(ActDelayRS, MainWindow.PARAMETERS.ACTDELAY);
            if (temp > Age || temp == -1)
                enabled = false;
            else
                enabled = true;

            //increase age (for parameters with t)
            Age++;

            //projectiles that have duration have a limited lifespan
            temp = (int)ReadString.Interpret(DurationRS, MainWindow.PARAMETERS.DURATION);
            if (temp != -1 && temp < Age)
                IsAlive = false;

            //radius
            Radius = Math.Abs((int)ReadString.Interpret(RadiusRS, MainWindow.PARAMETERS.SIZE));

            //checks if offscreen (x)
            temp = (int)ReadString.Interpret(TagCountRS, MainWindow.PARAMETERS.TAGCOUNT);
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
                else if (Math.Abs(Position.X) > 200 + Radius)
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
                else if (Math.Abs(Position.Y) > 200 + Radius)
                    if (Tags.HasFlag(MainWindow.TAGS.OUTSIDE) && (temp == -1 || temp > TagUses))
                        TagUses++;
                    else
                        IsAlive = false;
            }

            RadiusSqr = Radius * Radius;

            double ang, spd;
            //xyVel
            if (XVelRS != null && YVelRS != null)
            {
                Velocity = new Vector(ReadString.Interpret(XVelRS, MainWindow.PARAMETERS.XVEL), ReadString.Interpret(YVelRS, MainWindow.PARAMETERS.YVEL));
                spd = Velocity.Length;
                ang = Math.Atan2(Velocity.Y, Velocity.X);
            }
            //xyPos
            else if (XPosRS != null && YPosRS != null)
            {
                Velocity = new Vector(ReadString.Interpret(XPosRS, MainWindow.PARAMETERS.XPOS), ReadString.Interpret(YPosRS, MainWindow.PARAMETERS.YPOS)) - Position;
                spd = Velocity.Length;
                ang = Math.Atan2(Velocity.Y, Velocity.X);
            }
            //speed and angle
            else
            {
                ang = ReadString.Interpret(AngleRS, MainWindow.PARAMETERS.ANGLE);
                spd = ReadString.Interpret(SpeedRS, MainWindow.PARAMETERS.SPEED);
                double radians = ang * Math.PI / 180;
                Velocity = new Vector(Math.Cos(radians), Math.Sin(radians)) * spd;
            }

            //moves projectile by velocity
            Position += Velocity.Scale(VelDir);

            Render();

            double lstate = ReadString.Interpret(stateRS, MainWindow.PARAMETERS.STATE);

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
