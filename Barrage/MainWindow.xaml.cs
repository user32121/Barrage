#define song

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
using System.Threading;

namespace Barrage
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly int extraUI;
        readonly List<Projectile> projectiles = new List<Projectile>();
        bool paused;
        bool gameOver;
        bool isVisual;
        int time;

        DispatcherTimer kickStart;
        readonly Stopwatch stopwatch = new Stopwatch();
        readonly long frameLength;
        long nextFrame;
        long nextSecond;
        readonly int[] fps = new int[5];
        int fpsIndex;
        const int fpsMeasureRate = 5;
        public static bool stopRequested;

#if song
        MediaPlayer song = new MediaPlayer();
#endif

        public MainWindow()
        {
            InitializeComponent();

            this.KeyDown += new KeyEventHandler(MainWindow_KeyDown);
            extraUI = mainGrid.Children.Count;

            frameLength = Stopwatch.Frequency / 60;  //framerate

            plyrY = 100;

#if song
            song.Open(new Uri("files/song.mp3", UriKind.Relative));
#endif

            ReadSpawnTxt();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            kickStart = new DispatcherTimer();
            kickStart.Tick += KickStart_Tick;
            kickStart.Start();
        }

        private void KickStart_Tick(object sender, EventArgs e)
        {
            kickStart.Stop();
            Start();
        }

        private void Start()
        {
            stopwatch.Start();
            while (!stopRequested)
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

                    if (!(gameOver || isVisual))
                        CheckPlayerHit();
                }
                this.Refresh(DispatcherPriority.Input);
                ModerateFrames();
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
                labels.Clear();
                wait = 0;
                time = 0;

                ReadSpawnTxt();

                gameOver = false;
                paused = false;
                isVisual = false;
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
        double plyrSpeed = 5;
        const double plyrFast = 5;
        const double plyrSlow = 2;

        void PlayerMove()
        {
            bool moved = false;

            if (checkMouse.IsChecked == true && IsMouseOver)
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
                if (Keyboard.IsKeyDown(Key.LeftShift) && plyrSpeed == plyrFast)
                {
                    plyrSpeed = plyrSlow;
                }
                if (Keyboard.IsKeyUp(Key.LeftShift) && plyrSpeed == plyrSlow)
                {
                    plyrSpeed = plyrFast;
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
                    ReadString.t = item.Age; ReadString.lastVals = item.lastVals;
                    double ang = (double)ReadString.Interpret(item.Angle, typeof(double)),
                        radians = ang * Math.PI / 180,
                        m1 = Math.Sin(radians) / Math.Cos(radians), m2 = -1 / m1;
                    if (m1 > 1000) m1 = 1000; else if (m1 < -1000) m1 = -1000;
                    if (m2 > 1000) m2 = 1000; else if (m2 < -1000) m2 = -1000;

                    double b1 = item.Position.Y - m1 * item.Position.X, b2 = plyrY - m2 * plyrX,
                        ix = (b2 - b1) / (m1 - m2), iy = m1 * ix + b1;
                    if (Math.Pow(plyrX - ix, 2) + Math.Pow(plyrY - iy, 2) < item.RadiusSqr && ((Math.Abs(ang % 360) <= 90 || Math.Abs(ang % 360) > 270) ? ix >= item.Position.X : ix <= item.Position.X))
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
        double wait;
        readonly Dictionary<int, int> repeatVals = new Dictionary<int, int>();    //(line,repeats left)
        readonly Dictionary<string, int> labels = new Dictionary<string, int>();    //label, line
        int spwnInd;
        readonly List<double> spwnVals = new List<double>();

        string bossTarget = "0,0";
        string bossMvSpd = "0";
        string bossAngSpd = "0";
        Vector bossPos = new Vector(300, -300);
        double bossAngle = 0;

        void SpawnProjectiles()
        {
            ReadString.n = spwnInd;
            ReadString.numVals = spwnVals;
            ReadString.t = time;
            ReadString.lastVals = null;

            while (wait <= 0 && readIndex < spawnPattern.Length && !stopRequested)
            {
                ReadString.line = readIndex;

                //keeps reading lines untill text says to wait
                string[] line = spawnPattern[readIndex].Split('|');
                for (int i = 0; i < line.Length; i++)
                    line[i] = line[i].Trim();

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
                    int duration = -1;
                    int actDelay = 1;

                    for (int i = 1; i < line.Length; i++)
                    {
                        if (line[i].Contains("size"))
                            size = ReadString.ToEquation(line[i].Substring(line[i].IndexOf('=') + 1));
                        else if (line[i].Contains("startPos"))
                        {
                            string[] str = line[i].Substring(line[i].IndexOf('=') + 1).Split(',');
                            if (str.Length < 2)
                            {
                                MessageIssue(line[i]);
                                str = new string[2] { "", "" };
                            }
                            startPos = ReadString.ToEquation(str[0]) + "," + ReadString.ToEquation(str[1]);
                        }
                        else if (line[i].Contains("speed"))
                            speed = ReadString.ToEquation(line[i].Substring(line[i].IndexOf('=') + 1));
                        else if (line[i].Contains("angle"))
                            angle = ReadString.ToEquation(line[i].Substring(line[i].IndexOf('=') + 1));
                        else if (line[i].Contains("xyPos"))
                        {
                            string[] str = line[i].Substring(line[i].IndexOf('=') + 1).Split(',');
                            xyPos = ReadString.ToEquation(str[0]) + "," + ReadString.ToEquation(str[1]);
                        }
                        else if (line[i].Contains("xyVel"))
                        {
                            string[] str = line[i].Substring(line[i].IndexOf('=') + 1).Split(',');
                            xyVel = ReadString.ToEquation(str[0]) + "," + ReadString.ToEquation(str[1]);
                        }
                        else if (line[i].Contains("tags"))
                        {
                            tags = line[i].Substring(line[i].IndexOf('=') + 1).Split(',').ToList();
                            for (int t = 0; t < tags.Count; t++)
                                tags[t] = tags[t].Trim();
                        }
                        else if (line[i].Contains("duration"))
                            duration = (int)ReadString.Interpret(ReadString.ToEquation(line[i].Substring(line[i].IndexOf('=') + 1)), typeof(int));
                        else if (line[i].Contains("actDelay"))
                            actDelay = (int)ReadString.Interpret(ReadString.ToEquation(line[i].Substring(line[i].IndexOf('=') + 1)), typeof(int));
                    }

                    CreateProj(size, startPos, speed, angle, xyPos, xyVel, tags, duration, actDelay);
                    spwnInd++;
                    ReadString.n = spwnInd;
                }
                else if (line[0] == "boss")
                {
                    //set movement and rotation of boss
                    bossTarget = ReadString.ToEquation(line[1]) + "," + ReadString.ToEquation(line[2]);
                    bossMvSpd = ReadString.ToEquation(line[3]);
                    bossAngSpd = ReadString.ToEquation(line[4]);
                }
                else if (line[0] == "wait")
                {
                    //waits # of frames untill spawns again
                    wait += (double)ReadString.Interpret(ReadString.ToEquation(line[1]), typeof(double));
                }
                else if (line[0] == "repeat")
                {
                    //sets repeats left
                    if (repeatVals[readIndex] <= 0)
                    {
                        if (line.Length < 3)
                            MessageIssue(spawnPattern[readIndex], "repeat requires 3 inputs");
                        else
                            repeatVals[readIndex] = (int)ReadString.Interpret(ReadString.ToEquation(line[2]), typeof(int));
                    }

                    //repeats (stops at 1 since that will be the last repeat)
                    repeatVals[readIndex]--;
                    if (repeatVals[readIndex] >= 1)
                    {
                        int lineNum = (int)ReadString.Interpret(ReadString.ToEquation(line[1]), typeof(int)) - 1;
                        //(-1 because there is ++ later on)

                        if (lineNum < -1)
                            MessageIssue(line[1], "line number cannot be negative");
                        else
                            readIndex = lineNum;
                    }
                }
                else if (line[0] == "ifGoto")
                {
                    if ((double)ReadString.Interpret(ReadString.ToEquation(line[1]), typeof(double)) == 1)
                    {
                        int lineNum = (int)ReadString.Interpret(ReadString.ToEquation(line[2]), typeof(int)) - 1;
                        //(-1 because there is ++ later on)

                        if (lineNum < -1)
                        {
                            MessageIssue(line[2], "line number cannot be negative");
                            readIndex = -1;
                        }
                        else
                            readIndex = lineNum;
                    }
                }
                else if (line[0].Contains("val"))
                {
                    //sets a value to spwnVals
                    int ind = int.Parse(line[0].Substring(3));

                    while (ind >= spwnVals.Count)
                        spwnVals.Add(0);

                    spwnVals[ind] = (double)ReadString.Interpret(ReadString.ToEquation(line[1]), typeof(double));
                }
#if song
                else if (line[0] == "music")
                {
                    song.Stop();
                    song.Play();
                }
#endif

                //next line
                readIndex++;
            }

            wait--;
        }

        public void CreateProj(string size, string startPos, string speed, string angle, string xyPos, string xyVel, List<string> tags, int duration, int actDelay)
        {
            ReadString.t = 0;
            ReadString.lastVals = null;

            //displays projectile
            int r = Math.Abs((int)ReadString.Interpret(size, typeof(int)));
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
                projImage.RenderTransformOrigin = new Point(0.5, 0);
            }
            if (actDelay > 0)
                projImage.Opacity = 0.3;
            Grid.SetColumn(projImage, 0);
            Grid.SetRow(projImage, 0);
            mainGrid.Children.Add(projImage);

            //creates projectile
            double radians = (double)ReadString.Interpret(angle, typeof(double)) * Math.PI / 180;
            Projectile tempProjectile = new Projectile(size, this)
            {
                Sprite = projImage,
                Duration = duration,
                Position = (Vector)ReadString.Interpret(startPos, typeof(Vector)),
                Speed = speed,
                Angle = angle,
                XyPos = xyPos,
                XyVel = xyVel,
                Tags = tags,
                Velocity = new Vector(Math.Cos(radians), Math.Sin(radians)) * (double)ReadString.Interpret(speed, typeof(double)),
                ActDelay = actDelay
            };

            projectiles.Add(tempProjectile);
        }

        void MessageIssue(string text)
        {
            if (MessageBox.Show(string.Format("There was an issue with \"{0}\" at line {1}\n Continue?", text, readIndex),
                "", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
            {
                Application.Current.Shutdown();
                stopRequested = true;
            }
        }
        void MessageIssue(string text, string issue)
        {
            if (MessageBox.Show(string.Format("There was an issue with \"{0}\" at line {1} because {2}\n Continue?", text, readIndex, issue),
                "", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
            {
                Application.Current.Shutdown();
                stopRequested = true;
            }
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
            ReadString.t = time;
            ReadString.lastVals = null;
            Vector target = (Vector)ReadString.Interpret(bossTarget, typeof(Vector));
            Vector offset = target - bossPos;
            double mvSpd = (double)ReadString.Interpret(bossMvSpd, typeof(double));
            double angSpd = (double)ReadString.Interpret(bossAngSpd, typeof(double));

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
                if (line == "visual")
                {
                    isVisual = true;
                    labelVisual.Visibility = Visibility.Visible;

                }
                else if (line == "" || line.Substring(0, 1) == "#")
                {
                    readFile.Add("");
                }
                else
                {
                    readFile.Add(line);

                    //repeat
                    if (line.Contains("repeat"))
                        repeatVals.Add(readFile.Count - 1, 0);

                    //label
                    if (line[0] == ':')
                        if (labels.ContainsKey(line))
                            MessageBox.Show("\"" + line + "\" is already a label", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                        else
                            labels.Add(line, readFile.Count - 1);
                }
            }
            sr.Dispose();

            //insert labels
            string[] keys = labels.Keys.ToArray();
            for (int i = 0; i < readFile.Count; i++)
                for (int j = 0; j < labels.Count; j++)
                    readFile[i] = readFile[i].Replace(keys[j], labels[keys[j]].ToString());

            //transfers lines into spawnPattern
            spawnPattern = readFile.ToArray<string>();
        }

        void ModerateFrames()
        {
            //moderate
            while (stopwatch.ElapsedTicks < nextFrame) ;
            long ticksPassed = stopwatch.ElapsedTicks;
            nextFrame = ticksPassed / frameLength * frameLength + frameLength;

            //display fps
            ticksPassed = stopwatch.ElapsedTicks;
            if (ticksPassed > nextSecond)
            {
                while (nextSecond < ticksPassed)
                    nextSecond += Stopwatch.Frequency / fpsMeasureRate;
                fpsIndex = (fpsIndex + 1) % fps.Length;
                fps[fpsIndex] = 0;
            }
            fps[fpsIndex]++;
            double avg = 0;
            for (int i = 0; i < fps.Length; i++)
                if (i != fpsIndex)
                    avg += fps[i];
            avg /= fps.Length - 1;
            avg *= fpsMeasureRate;
            avg = Math.Round(avg, 1);
            labelFps.Content = (int)avg == avg ? avg + ".0" : avg.ToString();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            stopRequested = true;
        }
    }
    public static class ExtensionMethods
    {
        private static Action EmptyDelegate = delegate () { };

        public static void Refresh(this UIElement uiElement, DispatcherPriority priority)
        {
            uiElement.Dispatcher.Invoke(priority, EmptyDelegate);
        }
    }
}
