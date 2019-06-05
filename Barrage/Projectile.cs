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
        private MainWindow m_parent;
        public List<string> Tags;
        public string Speed;
        public string Angle;
        public string XyPos;
        public string XyVel;
        public string Radius;
        public int RadiusSqr;
        public int Duration;
        public bool IsAlive;
        int Age;

        readonly double[] lastVals = new double[6];
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

        public void SetPos(double x, double y)
        {
            Position = new Vector(x, y);
            Sprite.RenderTransform = new TranslateTransform(x, y);
        }

        public void Move()
        {
            //increase age (for parameters with t)
            Age++;

            //checks if offscreen (x)
            if (Math.Abs(Position.X) > m_parent.mainGrid.ActualWidth / 2)
            {
                if (Tags.Contains("wallBounce") && Duration > 0)
                {
                    SetPos(Position.X - 2 * (Position.X - m_parent.mainGrid.ActualWidth / 2 * Math.Sign(Position.X)), Position.Y);
                    velDir.X *= -1;
                    Duration--;
                }
                else if (Tags.Contains("screenWrap") && Duration > 0)
                {
                    SetPos(Position.X - m_parent.mainGrid.ActualWidth * Math.Sign(Position.X), Position.Y);
                    Duration--;
                }
                else
                {
                    IsAlive = false;
                }
            }
            //checks if offscreen (y)
            else if (Math.Abs(Position.Y) > m_parent.mainGrid.ActualHeight / 2)
            {
                if (Tags.Contains("wallBounce") && Duration > 0)
                {
                    SetPos(Position.X, Position.Y - 2 * (Position.Y - m_parent.mainGrid.ActualHeight / 2 * Math.Sign(Position.Y)));
                    velDir.Y *= -1;
                    Duration--;
                }
                else if (Tags.Contains("screenWrap") && Duration > 0)
                {
                    SetPos(Position.X, Position.Y - m_parent.mainGrid.ActualHeight * Math.Sign(Position.Y));
                    Duration--;
                }
                else
                {
                    IsAlive = false;
                }
            }

            //radius
            int r = Math.Abs((int)ReadString.Interpret(Radius, typeof(int), Age, lastVals));
            if (Sprite.Width / 2 != r)
            {
                Sprite.Width = r * 2;
                Sprite.Height = r * 2;
                RadiusSqr = r * r;
            }

            double ang = 0, spd = 0;
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
