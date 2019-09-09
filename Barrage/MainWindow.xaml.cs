using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Windows.Threading;
using System.Windows.Media.Effects;

namespace Barrage
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly int extraUI;
        readonly DispatcherTimer timer = new DispatcherTimer();
        readonly List<Projectile> projectiles = new List<Projectile>();
        bool paused;
        bool gameOver;
        int time;

        public MainWindow()
        {
            InitializeComponent();

            this.KeyDown += new KeyEventHandler(MainWindow_KeyDown);
            extraUI = mainGrid.Children.Count;
            timer.Tick += new EventHandler(Timer_Tick);
            timer.Interval = new TimeSpan(0, 0, 0, 0, 17);
            timer.Start();

            plyrY = 100;

            ReadSpawnTxt();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (!paused)
            {
                time++;

                if (!gameOver)
                {
                    PlayerMove();
                    SpawnProjectiles();
                }

                MoveProjectiles();

                if (!gameOver)
                    CheckPlayerHit();
            }
        }

        void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.R)
            {
                readIndex = 0;
                spwnInd = 0;
                spwnVals.Clear();
                repeatVals.Clear();
                wait = 0;
                time = 0;

                ReadSpawnTxt();

                gameOver = false;
                paused = false;
                mainGrid.Effect = new BlurEffect { Radius = 0 };
                PauseText.Content = "";

                projectiles.Clear();
                mainGrid.Children.RemoveRange(extraUI, mainGrid.Children.Count);

                plyrX = 0;
                plyrY = 100;
                Player.RenderTransform = new TranslateTransform(plyrX, plyrY);

                bossPos = new Vector(300, -300);
                bossTarget = "0,0";
                bossMvSpd = "0";
                bossAngSpd = "0";
                bossAngle = 0;
            }
            if (e.Key == Key.Escape && !gameOver)
            {
                if (!paused)
                {
                    mainGrid.Effect = new BlurEffect { Radius = 10 };
                    PauseText.Content = "Paused";
                    paused = true;
                }
                else
                {
                    mainGrid.Effect = new BlurEffect { Radius = 0 };
                    PauseText.Content = "";
                    paused = false;
                }
            }
        }

        public static double plyrX;
        public static double plyrY;
        double plyrSpeed = 10;

        void PlayerMove()
        {
            bool moved = false;

            if (checkBox.IsChecked == true && IsMouseOver)
            {
                Point mousePos = Mouse.GetPosition(this);
                plyrX = mousePos.X - 191;
                plyrY = mousePos.Y - 205;
                moved = true;
            }
            else
            {
                if (Keyboard.IsKeyDown(Key.Left))
                {
                    plyrX -= plyrSpeed;
                    moved = true;
                }
                if (Keyboard.IsKeyDown(Key.Right))
                {
                    plyrX += plyrSpeed;
                    moved = true;
                }
                if (Keyboard.IsKeyDown(Key.Up))
                {
                    plyrY -= plyrSpeed;
                    moved = true;
                }
                if (Keyboard.IsKeyDown(Key.Down))
                {
                    plyrY += plyrSpeed;
                    moved = true;
                }
                if (Keyboard.IsKeyDown(Key.LeftShift) && plyrSpeed == 10)
                {
                    plyrSpeed = 4;
                }
                if (Keyboard.IsKeyUp(Key.LeftShift) && plyrSpeed == 4)
                {
                    plyrSpeed = 10;
                }
            }

            if (moved)
            {
                if (plyrX < mainGrid.ActualWidth / -2)
                    plyrX = mainGrid.ActualWidth / -2;
                else if (plyrX > mainGrid.ActualWidth / 2)
                    plyrX = mainGrid.ActualWidth / 2;
                if (plyrY < mainGrid.ActualHeight / -2)
                    plyrY = mainGrid.ActualHeight / -2;
                else if (plyrY > mainGrid.ActualHeight / 2)
                    plyrY = mainGrid.ActualHeight / 2;

                Player.RenderTransform = new TranslateTransform(plyrX, plyrY);
            }
        }

        void CheckPlayerHit()
        {
            bool hit = false;

            foreach (Projectile item in projectiles)
            {
                if (item.ActDelay > 0)
                    continue;

                //collision detection
                if (item.Tags.Contains("circle"))
                {
                    //distance is less than radius
                    if (Math.Pow(plyrX - item.Position.X, 2) + Math.Pow(plyrY - item.Position.Y, 2) < item.RadiusSqr)
                        hit = true;
                }
                else if (item.Tags.Contains("laser"))
                {
                    //dist to line is less than radius, also checks if plyr is behind laser
                    double ang = (double)ReadString.Interpret(item.Angle, typeof(double), item.Age, item.lastVals), radians = ang * Math.PI / 180,
                        m1 = Math.Sin(radians) / Math.Cos(radians), m2 = -1 / m1;
                    if (double.IsInfinity(m1)) m1 = 1; if (double.IsInfinity(m2)) m2 = 1;

                    double b1 = item.Position.Y - m1 * item.Position.X, b2 = plyrY - m2 * plyrX,
                        ix = (b2 - b1) / (m1 - m2), iy = m1 * ix + b1;
                    if (Math.Pow(plyrX - ix, 2) + Math.Pow(plyrY - iy, 2) < item.RadiusSqr && ((Math.Abs(ang % 360) < 90 || Math.Abs(ang % 360) > 270) ? plyrX >= item.Position.X : plyrX <= item.Position.X))
                        hit = true;
                }
            }

            if (hit)
            {
                gameOver = true;
                mainGrid.Effect = new BlurEffect { Radius = 10 };
                PauseText.Content = "Game Over";
            }
        }

        string[] spawnPattern;
        int readIndex = 0;
        int wait;
        readonly Dictionary<int, int> repeatVals = new Dictionary<int, int>();    //(line,repeats left)
        int spwnInd;
        readonly List<double> spwnVals = new List<double>();

        string bossTarget = "0,0";
        string bossMvSpd = "0";
        string bossAngSpd = "0";
        Vector bossPos = new Vector(300,-300);
        double bossAngle = 0;

        void SpawnProjectiles()
        {
            while (wait <= 0 && readIndex < spawnPattern.Length)
            {
                //keeps reading lines untill text says to wait
                string[] line = spawnPattern[readIndex].Split('|');
                if (line[0] == "proj")
                {
                    //finds parameters
                    List<string> tags = new List<string>();
                    string size = "7";
                    string speed = "0";
                    string angle = "0";
                    string xyPos = "";
                    string xyVel = "";
                    string startPos = "0,-100";
                    int duration = 1;
                    int actDelay = 1;

                    for (int i = 1; i < line.Length; i++)
                    {
                        if (line[i].Contains("size"))
                            size = ReadString.AddVals(line[i].Split('=')[1], spwnInd, spwnVals);
                        else if (line[i].Contains("startPos"))
                            startPos = ReadString.AddVals(line[i].Split('=')[1], spwnInd, spwnVals);
                        else if (line[i].Contains("speed"))
                            speed = ReadString.AddVals(line[i].Split('=')[1], spwnInd, spwnVals);
                        else if (line[i].Contains("angle"))
                            angle = ReadString.AddVals(line[i].Split('=')[1], spwnInd, spwnVals);
                        else if (line[i].Contains("xyPos"))
                            xyPos = ReadString.AddVals(line[i].Split('=')[1], spwnInd, spwnVals);
                        else if (line[i].Contains("xyVel"))
                            xyVel = ReadString.AddVals(line[i].Split('=')[1], spwnInd, spwnVals);
                        else if (line[i].Contains("tags"))
                            tags = line[i].Split('=')[1].Split(',').ToList();
                        else if (line[i].Contains("duration"))
                            duration = (int)ReadString.Interpret(ReadString.AddVals(line[i].Split('=')[1], spwnInd, spwnVals), typeof(int), 0, new double[Projectile.LVIL]);
                        else if (line[i].Contains("actDelay"))
                            actDelay = (int)ReadString.Interpret(ReadString.AddVals(line[i].Split('=')[1], spwnInd, spwnVals), typeof(int), 0, new double[Projectile.LVIL]);
                    }

                    CreateProj(size, startPos, speed, angle, xyPos, xyVel, tags, duration, actDelay);
                    spwnInd++;
                }
                else if (line[0] == "boss")
                {
                    //set movement and rotation of boss
                    bossTarget = ReadString.AddVals(line[1] + "," + line[2], spwnInd, spwnVals);
                    bossMvSpd = ReadString.AddVals(line[3], spwnInd, spwnVals);
                    bossAngSpd = ReadString.AddVals(line[4], spwnInd, spwnVals);
                }
                else if (line[0] == "wait")
                {
                    //waits # of frames untill spawns again
                    wait = (int)ReadString.Interpret(ReadString.AddVals(line[1], spwnInd, spwnVals), typeof(int), 0, new double[Projectile.LVIL]);
                }
                else if (line[0] == "repeat")
                {
                    //sets repeats left
                    if (repeatVals[readIndex] <= 0)
                    {
                        repeatVals[readIndex] = (int)ReadString.Interpret(ReadString.AddVals(line[2], spwnInd, spwnVals), typeof(int), 0, new double[Projectile.LVIL]);
                    }

                    //repeats (stops at 1 since that will be the last repeat)
                    repeatVals[readIndex]--;
                    if (repeatVals[readIndex] >= 1)
                    {
                        readIndex = (int)ReadString.Interpret(ReadString.AddVals(line[1], spwnInd, spwnVals), typeof(int), 0, new double[Projectile.LVIL]) - 1;
                        //(-1 because there is ++ later on)
                    }
                }
                else if (line[0].Contains("val"))
                {
                    //sets a value to spwnVals
                    int ind = int.Parse(line[0].Substring(3));

                    while (ind >= spwnVals.Count)
                        spwnVals.Add(0);

                    spwnVals[ind] = (double)ReadString.Interpret(ReadString.AddVals(line[1], spwnInd, spwnVals), typeof(double), 0, new double[Projectile.LVIL]);
                }

                //next line
                readIndex++;
            }

            wait--;
        }

        public void CreateProj(string size, string startPos, string speed, string angle, string xyPos, string xyVel, List<string> tags, int duration, int actDelay)
        {
            //displays projectile
            int r = Math.Abs((int)ReadString.Interpret(size, typeof(int), 0, new double[Projectile.LVIL]));
            Image projImage = new Image();
            if (tags.Contains("circle"))
            {
                projImage.Width = r * 2;
                projImage.Height = r * 2;
                projImage.Source = new BitmapImage(new Uri("files/Projectile1.png", UriKind.Relative));
            }
            else if (tags.Contains("laser"))
            {
                projImage.Stretch = Stretch.Fill;
                projImage.Width = r * 2;
                projImage.Height = 100;
                projImage.Source = new BitmapImage(new Uri("files/Laser1.png", UriKind.Relative));
            }
            if (actDelay > 0)
                projImage.Opacity = 0.3;
            Grid.SetColumn(projImage, 0);
            Grid.SetRow(projImage, 0);
            mainGrid.Children.Add(projImage);

            //creates projectile
            double radians = (double)ReadString.Interpret(angle, typeof(double), 0, new double[Projectile.LVIL]) * Math.PI / 180;
            Projectile tempProjectile = new Projectile(size, this)
            {
                Sprite = projImage,
                Duration = duration,
                Position = (Vector)ReadString.Interpret(startPos, typeof(Vector), 0, new double[Projectile.LVIL]),
                Speed = speed,
                Angle = angle,
                XyPos = xyPos,
                XyVel = xyVel,
                Tags = tags,
                Velocity = new Vector(Math.Cos(radians), Math.Sin(radians)) * (double)ReadString.Interpret(speed, typeof(double), 0, new double[Projectile.LVIL]),
                ActDelay = actDelay
            };

            projectiles.Add(tempProjectile);
        }

        void MoveProjectiles()
        {
            //move projectiles
            List<Projectile> toRemove = new List<Projectile>();
            foreach (Projectile P in projectiles)
            {
                P.Move();
                if (!P.IsAlive)
                {
                    toRemove.Add(P);
                }
            }
            //remove projectiles
            foreach (Projectile P in toRemove)
            {
                projectiles.Remove(P);
                mainGrid.Children.Remove(P.Sprite);
            }

            //counts projectiles for monitoring lag
            projCount.Content = projectiles.Count.ToString();

            //move the boss
            Vector target = (Vector)ReadString.Interpret(bossTarget, typeof(Vector), time, new double[Projectile.LVIL]);
            Vector offset = target - bossPos;
            double mvSpd = (double)ReadString.Interpret(bossMvSpd, typeof(double), time, new double[Projectile.LVIL]);
            double angSpd = (double)ReadString.Interpret(bossAngSpd, typeof(double), time, new double[Projectile.LVIL]);

            if (offset.LengthSquared > mvSpd * mvSpd)
            {
                offset.Normalize();
                offset *= mvSpd;
            }
            bossPos += offset;
            bossAngle += angSpd;

            TransformGroup TG = new TransformGroup();
            TG.Children.Add(new RotateTransform(bossAngle));
            TG.Children.Add(new TranslateTransform(bossPos.X, bossPos.Y));
            Boss.RenderTransform = TG;
        }

        void ReadSpawnTxt()
        {
            StreamReader sr = new StreamReader("files/SP.txt");
            List<string> readFile = new List<string>();

            //loads files into readFile and removes comments and empty spaces
            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();
                if (line != "" && line.Substring(0, 1) != "#")
                {
                    readFile.Add(line);

                    if (line.Contains("repeat"))
                        repeatVals.Add(readFile.Count - 1, 0);
                }
            }
            sr.Dispose();

            //transfers lines into spawnPattern
            spawnPattern = readFile.ToArray<string>();
        }
    }
}
