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
        public Vector velDir = new Vector(1, 1);
        public List<string> Tags;
        public string Speed;
        public string Angle;
        public string XyPos;
        public string XyVel;
        public string Radius;
        public int RadiusSqr;
        public int Duration;
        public int TagCount;
        public int ActDelay;
        public bool IsAlive;
        public int Age;

        public readonly double[] lastVals = new double[6];
        public enum LVI /*Last Value Index*/ { x, y, xVel, yVel, spd, ang }
        public static int LVIL = 6; //Last Value Index Length

        public Projectile(string radius)
        {
            Radius = radius;
            ReadString.t = 0;
            ReadString.lastVals = lastVals;
            int r = (int)ReadString.Interpret(radius, typeof(int));
            RadiusSqr = r * r;
            IsAlive = true;
        }

        public void SetPos(double x, double y)
        {
            int y1 = 0;
            TransformGroup TG = new TransformGroup();
            if (Tags.Contains("laser"))
            {
                y1 = 50;    //add 50 to y because ...
                TG.Children.Add(new ScaleTransform(1, 6));
                //rotate laser
                ReadString.t = Age;
                ReadString.lastVals = lastVals;
                TG.Children.Add(new RotateTransform((double)ReadString.Interpret(Angle, typeof(double)) - 90));
            }

            Position = new Vector(x, y);
            TG.Children.Add(new TranslateTransform(x, y + y1));
            Sprite.RenderTransform = TG;
        }

        public void Move()
        {
            //ActDelay
            if (ActDelay > 0)
            {
                ActDelay--;
                if (ActDelay == 0)
                    Sprite.Opacity = 1;
            }

            //increase age (for parameters with t)
            Age++;

            //projectiles that have duration have a limited lifespan
            if (Duration != -1)
            {
                Duration--;
                if (Duration <= 0)
                    IsAlive = false;
            }

            //radius
            ReadString.t = Age;
            ReadString.lastVals = lastVals;
            int r = Math.Abs((int)ReadString.Interpret(Radius, typeof(int)));

            //checks if offscreen (x)
            if (Math.Abs(Position.X) > 200)
            {
                if (Tags.Contains("wallBounce") && TagCount != 0)
                {
                    SetPos(Position.X - 2 * (Position.X - 200 * Math.Sign(Position.X)), Position.Y);
                    velDir.X *= -1;
                    TagCount--;
                }
                else if (Tags.Contains("screenWrap") && TagCount != 0)
                {
                    SetPos(Position.X - 400 * Math.Sign(Position.X), Position.Y);
                    TagCount--;
                }
                else if (!Tags.Contains("outside") && Math.Abs(Position.X) > 400 + r)
                    IsAlive = false;
            }
            //checks if offscreen (y)
            if (Math.Abs(Position.Y) > 200)
            {
                if (Tags.Contains("wallBounce") && TagCount != 0)
                {
                    SetPos(Position.X, Position.Y - 2 * (Position.Y - 200 * Math.Sign(Position.Y)));
                    velDir.Y *= -1;
                    TagCount--;
                }
                else if (Tags.Contains("screenWrap") && TagCount != 0)
                {
                    SetPos(Position.X, Position.Y - 400 * Math.Sign(Position.Y));
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
            SetPos(Position.X + Velocity.X * velDir.X, Position.Y + Velocity.Y * velDir.Y);

            //sets lastVals
            lastVals[(int)LVI.x] = Position.X;
            lastVals[(int)LVI.y] = Position.Y;
            lastVals[(int)LVI.xVel] = Velocity.X;
            lastVals[(int)LVI.yVel] = Velocity.Y;
            lastVals[(int)LVI.spd] = spd;
            lastVals[(int)LVI.ang] = ang;
        }
    }
}
