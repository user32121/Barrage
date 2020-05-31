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
        public int TagCount;
        public string ActDelay;
        public bool IsAlive;
        public bool enabled;
        public int Age;

        public readonly double[] projVals = new double[8];
        public enum VI /*Value Index*/ { XPOS, YPOS, LXPOS, LYPOS, LXVEL, LYVEL, LSPD, LANG }

        public Projectile() { }

        public Projectile(string radius)
        {
            Radius = radius;
            ReadString.t = 0;
            projVals[(int)VI.XPOS] = Position.X;
            projVals[(int)VI.YPOS] = Position.Y;
            ReadString.projVals = projVals;
            int r = (int)ReadString.Interpret(radius, typeof(int));
            RadiusSqr = r * r;
            IsAlive = true;
        }

        public Projectile Clone()
        {
            Projectile p = new Projectile()
            {
                ActDelay = ActDelay,
                Age = Age,
                Angle = Angle,
                Duration = Duration,
                IsAlive = IsAlive,
                Sprite = Sprite,
                Position = Position,
                Radius = Radius,
                RadiusSqr = RadiusSqr,
                Speed = Speed,
                TagCount = TagCount,
                Tags = Tags,
                VelDir = VelDir,
                Velocity = Velocity,
                XyPos = XyPos,
                XyVel = XyVel,
            };
            for (int i = 0; i < projVals.Length; i++)
                p.projVals[i] = projVals[i];

            return p;
        }

        public void Render()
        {
            int y1 = 0;
            ReadString.t = Age;
            projVals[(int)VI.XPOS] = Position.X;
            projVals[(int)VI.YPOS] = Position.Y;
            ReadString.projVals = projVals;
            TransformGroup TG = new TransformGroup();
            if (Tags.Contains("laser"))
            {
                y1 = 50;    //add 50 to y because ...
                TG.Children.Add(new ScaleTransform(1, 6));
            }

            TG.Children.Add(new RotateTransform((double)ReadString.Interpret(Angle, typeof(double)) - 90));
            if (Tags.Contains("circle"))
                TG.Children.Add(new ScaleTransform(VelDir.X, VelDir.Y));
            TG.Children.Add(new TranslateTransform(Position.X, Position.Y + y1));
            Sprite.RenderTransform = TG;
        }

        public void Move()
        {
            ReadString.t = Age;
            projVals[(int)VI.XPOS] = Position.X;
            projVals[(int)VI.YPOS] = Position.Y;
            ReadString.projVals = projVals;

            //ActDelay
            int temp = (int)ReadString.Interpret(ActDelay, typeof(int));
            if (temp < Age && temp != -1)
            {
                enabled = false;
                Sprite.Opacity = 0.3;
            }
            else
            {
                enabled = true;
                Sprite.Opacity = 1;
            }

            //increase age (for parameters with t)
            Age++;

            //projectiles that have duration have a limited lifespan
            temp = (int)ReadString.Interpret(Duration, typeof(int));
            if (temp != -1)
                if (temp < Age)
                    IsAlive = false;

            //radius
            int r = Math.Abs((int)ReadString.Interpret(Radius, typeof(int)));

            //checks if offscreen (x)
            if (Math.Abs(Position.X) > 200)
            {
                if (Tags.Contains("wallBounce") && TagCount != 0)
                {
                    Position.X = 400 * Math.Sign(Position.X) - Position.X;
                    VelDir.X *= -1;
                    TagCount--;
                }
                else if (Tags.Contains("screenWrap") && TagCount != 0)
                {
                    Position.X -= 400 * Math.Sign(Position.X);
                    TagCount--;
                }
                else if (!Tags.Contains("outside") && Math.Abs(Position.X) > 200 + r)
                    IsAlive = false;
            }
            //checks if offscreen (y)
            if (Math.Abs(Position.Y) > 200)
            {
                if (Tags.Contains("wallBounce") && TagCount != 0)
                {
                    Position.Y = 400 * Math.Sign(Position.Y) - Position.Y;
                    VelDir.Y *= -1;
                    TagCount--;
                }
                else if (Tags.Contains("screenWrap") && TagCount != 0)
                {
                    Position.Y -= 400 * Math.Sign(Position.Y);
                    TagCount--;
                }
                else if (!Tags.Contains("outside") && Math.Abs(Position.Y) > 200 + r)
                    IsAlive = false;
            }

            //radius
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

            //sets last projVals
            projVals[(int)VI.LXPOS] = Position.X;
            projVals[(int)VI.LYPOS] = Position.Y;
            projVals[(int)VI.LXVEL] = Velocity.X;
            projVals[(int)VI.LYVEL] = Velocity.Y;
            projVals[(int)VI.LSPD] = spd;
            projVals[(int)VI.LANG] = ang;
        }
    }
}
