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
        private readonly MainWindow m_parent;
        public List<string> Tags;
        public string Speed;
        public string Angle;
        public string XyPos;
        public string XyVel;
        public string Radius;
        public int RadiusSqr;
        public int Duration;
        public int ActDelay;
        public bool IsAlive;
        public int Age;

        public readonly double[] lastVals = new double[6];
        public enum LVI /*Last Value Index*/ { x, y, xVel, yVel, spd, ang }
        public static int LVIL = 6; //Last Value Index Length

        public Projectile(string radius, MainWindow parent)
        {
            Radius = radius;
            int r = (int)ReadString.Interpret(radius, typeof(int), 0, lastVals);
            RadiusSqr = r * r;
            m_parent = parent;
            IsAlive = true;
        }

        public void SetPos(double x, double y, double r)
        {
            int y1 = 0;
            TransformGroup TG = new TransformGroup();
            if (Tags.Contains("laser"))
            {
                y1 = 50;
                TG.Children.Add(new ScaleTransform(1, 6));
                //rotate laser
                TG.Children.Add(new RotateTransform((double)ReadString.Interpret(Angle, typeof(double), Age, lastVals) - 90, r / 2, 0));
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

            //lasers have a limited lifespan
            if (Tags.Contains("laser"))
                Duration--;

            //radius
            int r = Math.Abs((int)ReadString.Interpret(Radius, typeof(int), Age, lastVals));

            //checks if offscreen (x)
            if (Math.Abs(Position.X) > m_parent.mainGrid.ActualWidth / 2)
            {
                if (Tags.Contains("wallBounce") && Duration > 0)
                {
                    SetPos(Position.X - 2 * (Position.X - m_parent.mainGrid.ActualWidth / 2 * Math.Sign(Position.X)), Position.Y, r);
                    velDir.X *= -1;
                    Duration--;
                }
                else if (Tags.Contains("screenWrap") && Duration > 0)
                {
                    SetPos(Position.X - m_parent.mainGrid.ActualWidth * Math.Sign(Position.X), Position.Y, r);
                    Duration--;
                }
                else if (Math.Abs(Position.X) > m_parent.mainGrid.ActualWidth / 2 + r)
                    IsAlive = false;
            }
            //checks if offscreen (y)
            else if (Math.Abs(Position.Y) > m_parent.mainGrid.ActualHeight / 2 + r)
            {
                if (Tags.Contains("wallBounce") && Duration > 0)
                {
                    SetPos(Position.X, Position.Y - 2 * (Position.Y - m_parent.mainGrid.ActualHeight / 2 * Math.Sign(Position.Y)), r);
                    velDir.Y *= -1;
                    Duration--;
                }
                else if (Tags.Contains("screenWrap") && Duration > 0)
                {
                    SetPos(Position.X, Position.Y - m_parent.mainGrid.ActualHeight * Math.Sign(Position.Y), r);
                    Duration--;
                }
                else if (Math.Abs(Position.X) > m_parent.mainGrid.ActualWidth / 2 + r)
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
                Velocity = (Vector)ReadString.Interpret(XyVel, typeof(Vector), Age, lastVals);
                spd = Velocity.Length;
                ang = Math.Atan2(Velocity.Y, Velocity.X);
            }
            //xyPos
            else if (XyPos != "")
            {
                Velocity = (Vector)ReadString.Interpret(XyPos, typeof(Vector), Age, lastVals) - Position;
                spd = Velocity.Length;
                ang = Math.Atan2(Velocity.Y, Velocity.X);
            }
            //speed and angle
            else
            {
                ang = (double)ReadString.Interpret(Angle, typeof(double), Age, lastVals);
                spd = (double)ReadString.Interpret(Speed, typeof(double), Age, lastVals);
                double radians = ang * Math.PI / 180;
                Velocity = new Vector(Math.Cos(radians), Math.Sin(radians)) * spd;
            }

            //moves projectile by velocity
            SetPos(Position.X + Velocity.X * velDir.X, Position.Y + Velocity.Y * velDir.Y, r);

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
