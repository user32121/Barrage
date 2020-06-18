using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Barrage
{
    class Projectile
    {
        public Image Sprite;
        public Vector Position;
        public Vector Velocity;
        public Vector VelDir = new Vector(1, 1);
        public List<string> Tags;
        public string Speed;
        public string Angle;
        public string XyPos;
        public string XyVel;
        public string Radius;
        public int RadiusSqr;
        public string Duration;
        public string TagCount;
        public int TagUses;
        public string ActDelay;
        public string File;
        public bool IsAlive;
        public bool enabled;
        public int Age;
        public string state;

        public double[] projVals = new double[(int)VI.Count];
        public enum VI /*Value Index*/
        {
            LXPOS, LYPOS,
            LXVEL, LYVEL,
            LSPD,
            LANG,
            LSTATE,
            Count
        }

        public Projectile() { }

        public Projectile(string radius)
        {
            Radius = radius;
            ReadString.t = 0;
            projVals[(int)VI.LXPOS] = Position.X;
            projVals[(int)VI.LYPOS] = Position.Y;
            ReadString.projVals = projVals;
            int r = (int)ReadString.Interpret(radius, typeof(int));
            RadiusSqr = r * r;
            IsAlive = true;
        }

        public Projectile Clone()
        {
            Projectile p = (Projectile)MemberwiseClone();
            p.projVals = new double[(int)VI.Count];
            for (int i = 0; i < projVals.Length; i++)
                p.projVals[i] = projVals[i];

            return p;
        }

        public void Render()
        {
            int y1 = 0;
            ReadString.t = Age;
            projVals[(int)VI.LXPOS] = Position.X;
            projVals[(int)VI.LYPOS] = Position.Y;
            ReadString.projVals = projVals;
            TransformGroup TG = new TransformGroup();
            if (Tags.Contains("laser"))
            {
                y1 = 50;    //add 50 to y because ...
                TG.Children.Add(new ScaleTransform(1, 6));
                Sprite.Source = MainWindow.GetProjectileImage((int)ReadString.Interpret(File, typeof(int)), true);
            }

            TG.Children.Add(new RotateTransform((double)ReadString.Interpret(Angle, typeof(double)) - 90));
            if (Tags.Contains("circle"))
            {
                TG.Children.Add(new ScaleTransform(VelDir.X, VelDir.Y));
                Sprite.Source = MainWindow.GetProjectileImage((int)ReadString.Interpret(File, typeof(int)), false);
            }
            TG.Children.Add(new TranslateTransform(Position.X, Position.Y + y1));
            Sprite.RenderTransform = TG;

            if (enabled)
                Sprite.Opacity = 1;
            else
                Sprite.Opacity = 0.3;

            int r = (int)ReadString.Interpret(Radius, typeof(int));
            if (Sprite.Width / 2 != r)
            {
                if (Tags.Contains("circle"))
                {
                    Sprite.Width = r * 2;
                    Sprite.Height = r * 2;
                }
                else if (Tags.Contains("laser"))
                {
                    Sprite.Width = r * 2;
                }
                RadiusSqr = r * r;
            }
        }

        public void Move()
        {
            ReadString.t = Age;
            ReadString.projVals = projVals;

            //ActDelay
            int temp = (int)ReadString.Interpret(ActDelay, typeof(int));
            if (temp > Age || temp == -1)
                enabled = false;
            else
                enabled = true;

            //increase age (for parameters with t)
            Age++;

            //projectiles that have duration have a limited lifespan
            temp = (int)ReadString.Interpret(Duration, typeof(int));
            if (temp != -1 && temp < Age)
                IsAlive = false;

            //radius
            int r = Math.Abs((int)ReadString.Interpret(Radius, typeof(int)));

            //checks if offscreen (x)
            temp = (int)ReadString.Interpret(TagCount, typeof(int));
            if (Math.Abs(Position.X) > 200)
            {
                if (Tags.Contains("wallBounce") && (temp == -1 || temp > TagUses))
                {
                    Position.X = 400 * Math.Sign(Position.X) - Position.X;
                    VelDir.X *= -1;
                    TagUses++;
                }
                else if (Tags.Contains("screenWrap") && (temp == -1 || temp > TagUses))
                {
                    Position.X -= 400 * Math.Sign(Position.X);
                    TagUses++;
                }
                else if (Math.Abs(Position.X) > 200 + r)
                    if (Tags.Contains("outside") && (temp == -1 || temp > TagUses))
                        TagUses++;
                    else
                        IsAlive = false;
            }
            //checks if offscreen (y)
            if (Math.Abs(Position.Y) > 200)
            {
                if (Tags.Contains("wallBounce") && (temp == -1 || temp > TagUses))
                {
                    Position.Y = 400 * Math.Sign(Position.Y) - Position.Y;
                    VelDir.Y *= -1;
                    TagUses++;
                }
                else if (Tags.Contains("screenWrap") && (temp == -1 || temp > TagUses))
                {
                    Position.Y -= 400 * Math.Sign(Position.Y);
                    TagUses++;
                }
                else if (!Tags.Contains("outside") && Math.Abs(Position.Y) > 200 + r)
                    if (Tags.Contains("outside") && (temp == -1 || temp > TagUses))
                        TagUses++;
                    else
                        IsAlive = false;
            }

            RadiusSqr = r * r;

            double ang, spd;
            //xyVel
            if (XyVel != "")
            {
                Velocity = (Vector)ReadString.Interpret(XyVel, typeof(Vector));
                spd = Velocity.Length;
                ang = Math.Atan2(Velocity.Y, Velocity.X);
            }
            //xyPos
            else if (XyPos != "")
            {
                Velocity = (Vector)ReadString.Interpret(XyPos, typeof(Vector)) - Position;
                spd = Velocity.Length;
                ang = Math.Atan2(Velocity.Y, Velocity.X);
            }
            //speed and angle
            else
            {
                ang = (double)ReadString.Interpret(Angle, typeof(double));
                spd = (double)ReadString.Interpret(Speed, typeof(double));
                double radians = ang * Math.PI / 180;
                Velocity = new Vector(Math.Cos(radians), Math.Sin(radians)) * spd;
            }

            //moves projectile by velocity
            Position += Velocity.Scale(VelDir);

            Render();

            double lstate = (double)ReadString.Interpret(state, typeof(double));

            //sets last projVals
            projVals[(int)VI.LXPOS] = Position.X;
            projVals[(int)VI.LYPOS] = Position.Y;
            projVals[(int)VI.LXVEL] = Velocity.X;
            projVals[(int)VI.LYVEL] = Velocity.Y;
            projVals[(int)VI.LSPD] = spd;
            projVals[(int)VI.LANG] = ang;
            projVals[(int)VI.LSTATE] = lstate;
        }
    }
}
