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
        int extraUI;
        DispatcherTimer timer = new DispatcherTimer();
        List<Projectile> projectiles = new List<Projectile>();
        bool paused;
        bool gameOver;

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
                if (!gameOver)
                {
                    PlayerMove();
                    SpawnProjectiles();
                }

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

                CheckPlayerHit();

                //counts projectiles for lag monitoring
                projCount.Content = projectiles.Count.ToString();
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
                if (item.Tags.Contains("circle"))
                {
                    if (Math.Pow(plyrX - item.Position.X, 2) + Math.Pow(plyrY - item.Position.Y, 2) < item.RadiusSqr)
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
        Dictionary<int, int> repeatVals = new Dictionary<int, int>();    //(line,repeats left)
        int spwnInd;
        List<double> spwnVals = new List<double>();

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
                    string speed = "1";
                    string angle = "0";
                    string startPos = "0,-100";
                    int duration = 0;

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
                        else if (line[i].Contains("tags"))
                            tags = line[i].Split('=')[1].Split(',').ToList();
                        else if (line[i].Contains("duration"))
                            duration = (int)ReadString.Interpret(ReadString.AddVals(line[i].Split('=')[1], spwnInd, spwnVals), typeof(int), 0, new double[4]);
                    }

                    CreateProj(size, startPos, speed, angle, tags, duration);
                    spwnInd++;
                }
                else if (line[0] == "wait")
                {
                    //waits # of frames untill spawns again
                    wait = (int)ReadString.Interpret(ReadString.AddVals(line[1], spwnInd, spwnVals), typeof(int), 0, new double[4]);
                }
                else if (line[0] == "repeat")
                {
                    //repeats
                    if (repeatVals[readIndex] > 0)
                    {
                        repeatVals[readIndex]--;
                        readIndex = (int)ReadString.Interpret(ReadString.AddVals(line[1], spwnInd, spwnVals), typeof(int), 0, new double[4]) - 1;
                        //(-1 because there is ++ later on)
                    }
                    //resets repeats left (and moves to next line)
                    else
                    {
                        repeatVals[readIndex] = (int)ReadString.Interpret(ReadString.AddVals(line[2], spwnInd, spwnVals), typeof(int), 0, new double[4]) - 1;
                    }
                }
                else if (line[0].Contains("val"))
                {
                    //sets a value to spwnVals
                    int ind = int.Parse(line[0].Substring(3));

                    while (ind >= spwnVals.Count)
                        spwnVals.Add(0);

                    spwnVals[ind] = (double)ReadString.Interpret(ReadString.AddVals(line[1], spwnInd, spwnVals), typeof(double), 0, new double[4]);
                }

                //next line
                readIndex++;
            }

            wait--;
        }

        public void CreateProj(string size, string startPos, string speed, string angle, List<string> tags, int duration)
        {
            //displays projectile
            int r = Math.Abs((int)ReadString.Interpret(size, typeof(int), 0, new double[4]));
            Image projImage = new Image() { Height = r * 2, Width = r * 2 };
            if (tags.Contains("circle"))
                projImage.Source = new BitmapImage(new Uri("files/Projectile1.png", UriKind.Relative));
            Grid.SetColumn(projImage, 0);
            Grid.SetRow(projImage, 0);
            mainGrid.Children.Add(projImage);

            //creates projectile
            double radians = (double)ReadString.Interpret(angle, typeof(double), 0, new double[4]) * Math.PI / 180;
            Projectile tempProjectile = new Projectile(size, this)
            {
                Sprite = projImage,
                Duration = duration,
                Position = (Vector)ReadString.Interpret(startPos, typeof(Vector), 0, new double[4]),
                Speed = speed,
                Angle = angle,
                Tags = tags,
                Velocity = new Vector(Math.Cos(radians), Math.Sin(radians)) * (double)ReadString.Interpret(speed, typeof(double), 0, new double[4])
            };

            projectiles.Add(tempProjectile);
        }

        void ReadSpawnTxt()
        {
            StreamReader sr = new StreamReader("files/spawnPattern.txt");
            List<string> readFile = new List<string>();

            //loads files into readFile and removes comments and empty spaces
            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();
                if (line != "" && line.Substring(0, 1) != "#")
                {
                    readFile.Add(line);

                    if (line.Contains("repeat"))
                    {
                        string[] vals = line.Split('|');
                        repeatVals.Add(readFile.Count - 1, repeatVals[readIndex] = (int)ReadString.Interpret(ReadString.AddVals(vals[2], spwnInd, spwnVals), typeof(int), 0, new double[4]));
                    }
                }
            }
            sr.Dispose();

            //transfers lines into spawnPattern
            spawnPattern = readFile.ToArray<string>();
        }
    }
}
