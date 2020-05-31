#define SONG
#undef TAS

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
using System.Windows.Interop;

namespace Barrage
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static MainWindow Main;

        //game
        readonly int extraUI;
        List<Projectile> projectiles = new List<Projectile>();
        bool paused;
        bool gameOver;
        bool isVisual;
        int time;
        //player
        public static Vector plyrPos;
        double plyrSpeed = 5;
        const double plyrFast = 5;
        const double plyrSlow = 2;
        //boss
        public static Vector bossPos = new Vector(300, -300);
        double bossAngle = 0;
        string bossTarget = "0,0";
        string bossMvSpd = "0";
        string bossAngSpd = "0";
        //spawn pattern
        List<string> spawnPattern;
        int readIndex = 0;
        double wait;
        Dictionary<int, int> repeatVals = new Dictionary<int, int>();    //(line,repeats left)
        int spwnInd;
        List<double> spwnVals = new List<double>();
        readonly Dictionary<string, int> labels = new Dictionary<string, int>();    //label, line

        //frame moderation
        DispatcherTimer kickStart;
        readonly Stopwatch stopwatch = new Stopwatch();
        readonly long frameLength;
        long nextFrame;
        long nextSecond;
        readonly int[] fps = new int[5];
        int fpsIndex;
        const int fpsMeasureRate = 5;
        public static bool stopRequested;

        //gamestate
        enum GAMESTATE
        {
            MENU,
            PLAY,
            EDITOR,
            OPTIONS,
        }
        static GAMESTATE gamestate = GAMESTATE.MENU;

        //image storage
        readonly BitmapImage[] playPauseImgs = new BitmapImage[]
        {
            new BitmapImage(new Uri("files/Play.png", UriKind.Relative)),
            new BitmapImage(new Uri("files/Pause.png", UriKind.Relative)),
        };
        readonly List<BitmapImage> projectileImgs = new List<BitmapImage>();
        int laserImgsIndex;

        //editor
        bool playing;
        bool stepForwards;
        Size minSize;
        readonly LinearGradientBrush hitIndicatorBrush = new LinearGradientBrush(Colors.Transparent, Colors.Transparent, new Point(0, 0.5), new Point(1, 0.5));
        readonly GameFrame[] hist = new GameFrame[1000];
        int histIndex = 0;
        int histIndexMin = 0;
        Point projStartPos;
        Point projEndPos;
        int textEditKeyPresses;
        DispatcherTimer autosaveTimer;
        readonly ImageBrush gridOverlay = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/files/Grid.png")));
        bool isSPSaved;

#if SONG
        readonly MediaPlayer song = new MediaPlayer();
        bool songPlaying;
#endif
#if TAS
        int TASIndex;
#endif

        public MainWindow()
        {
            InitializeComponent();

            KeyDown += new KeyEventHandler(MainWindow_KeyDown);
            extraUI = gridField.Children.Count;

            frameLength = Stopwatch.Frequency / 60;  //framerate

            plyrPos.Y = 100;

            if (File.Exists("files/Boss.png"))
                Boss.Source = new BitmapImage(new Uri(Directory.GetCurrentDirectory() + "/files/Boss.png"));
            if (File.Exists("files/Player.png"))
                Player.Source = new BitmapImage(new Uri(Directory.GetCurrentDirectory() + "/files/Player.png"));

            if (File.Exists("files/Play.png"))
            {
                playPauseImgs[0] = new BitmapImage(new Uri(Directory.GetCurrentDirectory() + "/files/Play.png"));
                imageEditorPlay.Source = playPauseImgs[0];
            }
            if (File.Exists("files/Pause.png"))
                playPauseImgs[1] = new BitmapImage(new Uri(Directory.GetCurrentDirectory() + "/files/Pause.png"));
            if (File.Exists("files/Step.png"))
            {
                imageEditorStepForwards.Source = new BitmapImage(new Uri(Directory.GetCurrentDirectory() + "/files/Step.png"));
                imageEditorStepBackwards.Source = imageEditorStepForwards.Source;
            }
            if (File.Exists("files/Arrow.png"))
                Arrow.Source = new BitmapImage(new Uri(Directory.GetCurrentDirectory() + "/files/Arrow.png"));
            if (File.Exists("files/Grid.png"))
                gridOverlay = new ImageBrush(new BitmapImage(new Uri(Directory.GetCurrentDirectory() + "/files/Grid.png")));
#if SONG
            song.Open(new Uri("files/song.mp3", UriKind.Relative));
#endif

            //load settings
            if (GameSettings.TryLoad())
            {
                checkMouse.IsChecked = GameSettings.useMouse;
                checkUseGrid.IsChecked = GameSettings.useGrid;
            }

            hist[0] = new GameFrame()
            {
                bossAngle = bossAngle,
                bossAngSpd = bossAngSpd,
                bossMvSpd = bossMvSpd,
                bossPos = bossPos,
                bossTarget = bossTarget,
                plyrPos = plyrPos,
                projectiles = new Projectile[0],
                readIndex = readIndex,
                repeatVals = repeatVals,
                spwnInd = spwnInd,
                spwnVals = new double[0],
                time = time,
                wait = wait,
            };

            ReadSpawnTxt();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Main = this;

            Width += 400 - gridSize.ActualWidth;
            Height += 400 - gridSize.ActualHeight;
            MinWidth = Width;
            MinHeight = Height;
            minSize = new Size(Width, Height);

            kickStart = new DispatcherTimer();
            kickStart.Tick += KickStart_Tick;
            kickStart.Start();
            autosaveTimer = new DispatcherTimer() { Interval = TimeSpan.FromSeconds(60) };
            autosaveTimer.Tick += AutosaveTimer_Tick;
            autosaveTimer.Start();
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
                if (gamestate == GAMESTATE.PLAY || playing || stepForwards)
                {
                    if (!paused)
                    {
                        time++;

                        if (!gameOver)
                        {
                            PlayerMove();
                            ReadNextLine();
                        }

                        MoveProjectiles();

                        if (!gameOver && !isVisual)
                            CheckPlayerHit();
                        RenderPlayerAndBoss();

                        if (gamestate == GAMESTATE.EDITOR)
                        {
                            if (++histIndex >= hist.Length)
                                histIndex = 0;
                            hist[histIndex] = new GameFrame()
                            {
                                bossAngle = bossAngle,
                                bossAngSpd = bossAngSpd,
                                bossMvSpd = bossMvSpd,
                                bossPos = bossPos,
                                bossTarget = bossTarget,
                                plyrPos = plyrPos,
                                projectiles = new Projectile[projectiles.Count],
                                readIndex = readIndex,
                                repeatVals = new Dictionary<int, int>(repeatVals),
                                spwnInd = spwnInd,
                                spwnVals = spwnVals.ToArray(),
                                time = time,
                                wait = wait,
                            };
                            for (int i = 0; i < projectiles.Count; i++)
                                hist[histIndex].projectiles[i] = projectiles[i].Clone();
                            if (histIndex == histIndexMin)
                                histIndexMin++;
                        }
                        stepForwards = false;
                    }
                }
                this.Refresh(DispatcherPriority.Input);
                ModerateFrames();
            }

            GameSettings.useMouse = (bool)checkMouse.IsChecked;
            GameSettings.useGrid = (bool)checkUseGrid.IsChecked;
            GameSettings.Save();
        }

        void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (textEditKeyPresses > 0)
            {
                textEditKeyPresses--;
                return;
            }

            if (e.Key == Key.R)
            {
                readIndex = 0;
                spwnInd = 0;
                spwnVals.Clear();
                repeatVals.Clear();
                wait = 0;
                time = 0;

                isVisual = false;
                labelVisual.Visibility = Visibility.Hidden;
#if SONG
                song.Stop();
                songPlaying = false;
#endif
#if TAS
                TASIndex = 0;
#endif
                ReadSpawnTxt();

                gameOver = false;
                paused = false;
                gridField.Effect = null;
                gridPause.Visibility = Visibility.Hidden;

                projectiles.Clear();
                gridField.Children.RemoveRange(extraUI, gridField.Children.Count);

                plyrPos = new Vector(0, 100);
                Player.RenderTransform = new TranslateTransform(plyrPos.X, plyrPos.Y);

                bossPos = new Vector(300, -300);
                Boss.RenderTransform = new TranslateTransform(bossPos.X, bossPos.Y);
                bossTarget = "0,0";
                bossMvSpd = "0";
                bossAngSpd = "0";
                bossAngle = 0;
            }
            else if (gamestate == GAMESTATE.PLAY)
            {
                if ((e.Key == Key.Escape || e.Key == Key.P && !paused) && !gameOver)
                {
                    if (!paused)
                    {
                        gridField.Effect = new BlurEffect { Radius = 10 };
                        gridPause.Visibility = Visibility.Visible;
                        labelPause.Content = "Paused";
#if SONG
                        song.Pause();
#endif
                        paused = true;
                    }
                    else
                    {
                        gridField.Effect = null;
                        gridPause.Visibility = Visibility.Hidden;
#if SONG
                        if (songPlaying)
                            song.Play();
#endif
                        paused = false;
                    }
                }
#if SONG
                else if (e.Key == Key.Q)
                    song.Position -= TimeSpan.FromSeconds(0.1);
                else if (e.Key == Key.W)
                    song.Position += TimeSpan.FromSeconds(0.1);
#endif
            }
            else if (gamestate == GAMESTATE.EDITOR)
            {
                if (e.Key == Key.Escape || e.Key == Key.Space || e.Key == Key.K || e.Key == Key.P && playing)
                {
                    ImageEditor_MouseUp(imageEditorPlay, null);
                    ImageEditor_MouseLeave(imageEditorPlay, null);
                }
                else if (e.Key == Key.L)
                {
                    ImageEditor_MouseUp(imageEditorStepForwards, null);
                    ImageEditor_MouseLeave(imageEditorStepForwards, null);
                }
                else if (e.Key == Key.J)
                {
                    ImageEditor_MouseUp(imageEditorStepBackwards, new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left));
                    ImageEditor_MouseLeave(imageEditorStepBackwards, null);
                }
                else if (e.Key == Key.H)
                {
                    ImageEditor_MouseUp(imageEditorStepBackwards, new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Right));
                    ImageEditor_MouseLeave(imageEditorStepBackwards, null);
                }
                else if (e.Key == Key.S && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
                    SaveSP();
            }
        }

        void PlayerMove()
        {
            bool moved = false;

            if (checkMouse.IsChecked == true && IsMouseOver)
            {
                Point mousePos = Mouse.GetPosition(gridField);
                plyrPos.X = mousePos.X - gridField.ActualWidth / 2;
                plyrPos.Y = mousePos.Y - gridField.ActualHeight / 2;
                moved = true;
            }
            else
            {
#if TAS
                if (TASIndex < TASInputs.GetLength(0) - 1)
                    if (TASInputs[TASIndex, 3] > 0)
                        TASInputs[TASIndex, 3]--;
                    else
                        TASIndex += 1;
#endif
                if ((Keyboard.IsKeyDown(Key.LeftShift)
#if TAS
                    || TASInputs[TASIndex, 2] == 1
#endif
                    ) && plyrSpeed == plyrFast)
                {
                    plyrSpeed = plyrSlow;
                }
                if (Keyboard.IsKeyUp(Key.LeftShift)
#if TAS
                    && TASInputs[TASIndex, 2] != 1
#endif
                     && plyrSpeed == plyrSlow)
                {
                    plyrSpeed = plyrFast;
                }
                if (Keyboard.IsKeyDown(Key.Left)
#if TAS
                    || TASInputs[TASIndex, 0] == -1
#endif 
                    )
                {
                    plyrPos.X -= plyrSpeed;
                    moved = true;
                }
                if (Keyboard.IsKeyDown(Key.Right)
#if TAS
                    || TASInputs[TASIndex, 0] == 1
#endif 
                    )
                {
                    plyrPos.X += plyrSpeed;
                    moved = true;
                }
                if (Keyboard.IsKeyDown(Key.Up)
#if TAS
                    || TASInputs[TASIndex, 1] == -1
#endif
                    )
                {
                    plyrPos.Y -= plyrSpeed;
                    moved = true;
                }
                if (Keyboard.IsKeyDown(Key.Down)
#if TAS
                    || TASInputs[TASIndex, 1] == 1
#endif
                    )
                {
                    plyrPos.Y += plyrSpeed;
                    moved = true;
                }
            }

            if (moved)
            {
                if (plyrPos.X < -200)
                    plyrPos.X = -200;
                else if (plyrPos.X > 200)
                    plyrPos.X = 200;
                if (plyrPos.Y < -200)
                    plyrPos.Y = -200;
                else if (plyrPos.Y > 200)
                    plyrPos.Y = 200;
            }
        }

        void RenderPlayerAndBoss()
        {
            Player.RenderTransform = new TranslateTransform(plyrPos.X, plyrPos.Y);

            TransformGroup TG = new TransformGroup();
            TG.Children.Add(new RotateTransform(bossAngle));
            TG.Children.Add(new TranslateTransform(bossPos.X, bossPos.Y));
            Boss.RenderTransform = TG;
        }

        void CheckPlayerHit()
        {
            bool hit = false;

            foreach (Projectile item in projectiles)
            {
                if (item.ActDelay > 0 || item.ActDelay == -1)
                    continue;

                //collision detection
                if (item.Tags.Contains("circle"))
                {
                    //distance is less than radius
                    if (Math.Pow(plyrPos.X - item.Position.X, 2) + Math.Pow(plyrPos.Y - item.Position.Y, 2) < item.RadiusSqr)
                        hit = true;
                }
                else if (item.Tags.Contains("laser"))
                {
                    //dist to line is less than radius, also checks if plyr is behind laser
                    ReadString.t = item.Age; ReadString.projVals = item.projVals;
                    double ang = (double)ReadString.Interpret(item.Angle, typeof(double)),
                        radians = ang * Math.PI / 180,
                        m1 = Math.Sin(radians) / Math.Cos(radians), m2 = -1 / m1;
                    if (m1 > 1000) m1 = 1000; else if (m1 < -1000) m1 = -1000;
                    if (m2 > 1000) m2 = 1000; else if (m2 < -1000) m2 = -1000;

                    double b1 = item.Position.Y - m1 * item.Position.X, b2 = plyrPos.Y - m2 * plyrPos.X,
                        ix = (b2 - b1) / (m1 - m2), iy = m1 * ix + b1;
                    if (Math.Pow(plyrPos.X - ix, 2) + Math.Pow(plyrPos.Y - iy, 2) < item.RadiusSqr && ((Math.Abs(ang % 360) <= 90 || Math.Abs(ang % 360) > 270) ? ix >= item.Position.X : ix <= item.Position.X))
                        hit = true;
                }
            }

            if (gamestate == GAMESTATE.PLAY)
            {
                if (hit)
                {
                    gameOver = true;
                    gridField.Effect = new BlurEffect { Radius = 10 };
                    gridPause.Visibility = Visibility.Visible;
                    labelPause.Content = "Game Over";
                }
            }
            else if (gamestate == GAMESTATE.EDITOR)
            {
                if (hit)
                {
                    hitIndicatorBrush.GradientStops[0].Color = Colors.White;
                    hitIndicatorBrush.GradientStops[1].Color = Colors.White;
                }
                else
                {
                    Color col = hitIndicatorBrush.GradientStops[0].Color;
                    if (col.A > 20)
                        col.A -= 20;
                    else
                        col.A = 0;
                    hitIndicatorBrush.GradientStops[0].Color = col;
                    hitIndicatorBrush.GradientStops[1].Color = Colors.Transparent;
                }
                labelEditorHitIndicator.OpacityMask = hitIndicatorBrush;
            }
        }

        void ReadNextLine()
        {
            while (wait <= 0 && readIndex < spawnPattern.Count && !stopRequested)
            {
                if (Keyboard.IsKeyDown(Key.P))
                    return;

                ReadString.n = spwnInd;
                ReadString.numVals = spwnVals;
                ReadString.t = time;
                ReadString.projVals = null;
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
                    string duration = "-1";
                    int tagCount = -1;
                    int actDelay = 1;
                    int file = 0;

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
                            if (str.Length < 2)
                            {
                                MessageIssue(line[i]);
                                str = new string[2] { "", "" };
                            }
                            xyPos = ReadString.ToEquation(str[0]) + "," + ReadString.ToEquation(str[1]);
                        }
                        else if (line[i].Contains("xyVel"))
                        {
                            string[] str = line[i].Substring(line[i].IndexOf('=') + 1).Split(',');
                            if (str.Length < 2)
                            {
                                MessageIssue(line[i]);
                                str = new string[2] { "", "" };
                            }
                            xyVel = ReadString.ToEquation(str[0]) + "," + ReadString.ToEquation(str[1]);
                        }
                        else if (line[i].Contains("tags"))
                        {
                            tags = line[i].Substring(line[i].IndexOf('=') + 1).Split(',').ToList();
                            for (int t = 0; t < tags.Count; t++)
                                tags[t] = tags[t].Trim();
                        }
                        else if (line[i].Contains("duration"))
                            duration = ReadString.ToEquation(line[i].Substring(line[i].IndexOf('=') + 1));
                        else if (line[i].Contains("tagCount"))
                            tagCount = (int)ReadString.Interpret(ReadString.ToEquation(line[i].Substring(line[i].IndexOf('=') + 1)), typeof(int));
                        else if (line[i].Contains("actDelay"))
                            actDelay = (int)ReadString.Interpret(ReadString.ToEquation(line[i].Substring(line[i].IndexOf('=') + 1)), typeof(int));
                        else if (line[i].Contains("file"))
                            file = (int)ReadString.Interpret(ReadString.ToEquation(line[i].Substring(line[i].IndexOf('=') + 1)), typeof(int));
                    }

                    CreateProj(size, startPos, speed, angle, xyPos, xyVel, tags, duration, tagCount, actDelay, file);
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
                            MessageIssue(spawnPattern[readIndex], "repeat requires 2 inputs");
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
                else if (line[0].Length >= 3 && line[0].Substring(0, 3) == "val")
                {
                    //sets a value to spwnVals
                    if (int.TryParse(line[0].Substring(3), out int ind))
                    {
                        while (ind >= spwnVals.Count)
                            spwnVals.Add(0);

                        if (line.Length < 2)
                            MessageIssue(spawnPattern[readIndex], "val requires an input");
                        else if (ind < 0)
                            MessageIssue(spawnPattern[readIndex], "val# cannot be negative");
                        else
                            spwnVals[ind] = (double)ReadString.Interpret(ReadString.ToEquation(line[1]), typeof(double));
                    }
                    else
                        MessageIssue(line[0]);
                }
#if SONG
                else if (line[0] == "music")
                {
                    if (line.Length > 1)
                    {
                        if (double.TryParse(line[1], out double result) && result >= 0)
                            song.Position = TimeSpan.FromMilliseconds(result);
                    }
                    else
                        song.Stop();
                    song.Play();
                    songPlaying = true;
                }
#endif
                else if (line[0] == "rng")
                {
                    if (int.TryParse(line[1], out int num))
                        ReadString.rng = new Random(num);
                    else
                        MessageIssue(line[1]);
                }

                //next line
                readIndex++;
            }

            wait--;
        }

        public void CreateProj(string size, string startPos, string speed, string angle, string xyPos,
            string xyVel, List<string> tags, string duration, int tagCount, int actDelay, int file)
        {
            ReadString.t = 0;
            ReadString.projVals = null;

            //displays projectile
            int r = Math.Abs((int)ReadString.Interpret(size, typeof(int)));
            Image projImage = new Image();
            if (tags.Contains("circle"))
            {
                projImage.Width = r * 2;
                projImage.Height = r * 2;
                if (file >= laserImgsIndex || projectileImgs[file] == null)
                {
                    while (file >= laserImgsIndex)
                    {
                        projectileImgs.Insert(laserImgsIndex, null);
                        laserImgsIndex++;
                    }
                    if (File.Exists("files/Projectile" + file + ".png"))
                        projectileImgs[file] = new BitmapImage(new Uri(Directory.GetCurrentDirectory() + "/files/Projectile" + file + ".png"));
                    else
                        projectileImgs[file] = new BitmapImage(new Uri("files/Projectile.png", UriKind.Relative));
                }
                projImage.Source = projectileImgs[file];

                projImage.RenderTransformOrigin = new Point(0.5, 0.5);
            }
            else if (tags.Contains("laser"))
            {
                projImage.Stretch = Stretch.Fill;
                projImage.Width = r * 2;
                projImage.Height = 100;
                if (file + laserImgsIndex >= projectileImgs.Count || projectileImgs[file + laserImgsIndex] == null)
                {
                    while (file + laserImgsIndex >= projectileImgs.Count)
                        projectileImgs.Add(null);

                    if (File.Exists("files/Laser" + file + ".png"))
                        projectileImgs[file + laserImgsIndex] = new BitmapImage(new Uri(Directory.GetCurrentDirectory() + "/files/Laser" + file + ".png"));
                    else
                        projectileImgs[file + laserImgsIndex] = new BitmapImage(new Uri("files/Laser.png", UriKind.Relative));
                }
                projImage.RenderTransformOrigin = new Point(0.5, 0);
                projImage.Source = projectileImgs[file + laserImgsIndex];
            }
            if (actDelay > 0 || actDelay == -1)
                projImage.Opacity = 0.3;
            Grid.SetColumn(projImage, 0);
            Grid.SetRow(projImage, 0);
            gridField.Children.Add(projImage);

            //creates projectile
            double radians = (double)ReadString.Interpret(angle, typeof(double)) * Math.PI / 180;
            Projectile tempProjectile = new Projectile(size)
            {
                Sprite = projImage,
                Duration = duration,
                Position = (Vector)ReadString.Interpret(startPos, typeof(Vector)),
                Speed = speed,
                Angle = angle,
                XyPos = xyPos,
                XyVel = xyVel,
                Tags = tags,
                TagCount = tagCount,
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
                if (gamestate == GAMESTATE.EDITOR)
                    MainWindow_KeyDown(this, new KeyEventArgs(Keyboard.PrimaryDevice, new HwndSource(0, 0, 0, 0, 0, "", IntPtr.Zero), 0, Key.P));
                else
                    LabelBack_MouseUp(labelPauseBack, new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left));
            }
        }
        public static void MessageIssue(string text, int line)
        {
            if (MessageBox.Show(string.Format("There was an issue with \"{0}\" at line {1}\n Continue?", text, line),
                "", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
            {
                if (gamestate == GAMESTATE.EDITOR)
                    Main.MainWindow_KeyDown(Main, new KeyEventArgs(Keyboard.PrimaryDevice, new HwndSource(0, 0, 0, 0, 0, "", IntPtr.Zero), 0, Key.P));
                else
                    Main.LabelBack_MouseUp(Main.labelPauseBack, new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left));
            }
        }
        void MessageIssue(string text, string issue)
        {
            if (MessageBox.Show(string.Format("There was an issue with \"{0}\" at line {1} because {2}\n Continue?", text, readIndex, issue),
                "", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
            {
                if (gamestate == GAMESTATE.EDITOR)
                    MainWindow_KeyDown(this, new KeyEventArgs(Keyboard.PrimaryDevice, new HwndSource(0, 0, 0, 0, 0, "", IntPtr.Zero), 0, Key.P));
                else
                    LabelBack_MouseUp(labelPauseBack, new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left));
            }
        }

        void MoveProjectiles()
        {
            //move projectiles
            for (int i = 0; i < projectiles.Count; i++)
            {
                projectiles[i].Move();
                if (i < projectiles.Count && !projectiles[i].IsAlive)
                {
                    gridField.Children.Remove(projectiles[i].Sprite);
                    projectiles.RemoveAt(i);
                    i--;
                }
            }

            //counts projectiles for monitoring lag
            projCount.Content = projectiles.Count.ToString();

            //move the boss
            ReadString.t = time;
            ReadString.projVals = null;
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
        }

        void ReadSpawnTxt()
        {
            StreamReader sr = new StreamReader("files/SP.txt");
            string[] lines;
            if (gamestate == GAMESTATE.EDITOR)
                lines = textEditor.Text.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            else
            {
                string temp = sr.ReadToEnd();
                textEditor.Text = temp;
                isSPSaved = true;
                Title = "Barrage";
                lines = temp.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            }
            sr.Close();
            sr.Dispose();

            List<string> readFile = new List<string>();
            labels.Clear();

            //loads files into readFile and removes comments and empty spaces
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i] == "visual")
                {
                    isVisual = true;
                    labelVisual.Visibility = Visibility.Visible;

                }
                else if (lines[i] == "" || lines[i].Substring(0, 1) == "#")
                {
                    readFile.Add("");
                }
                else
                {
                    readFile.Add(lines[i]);

                    //repeat
                    if (lines[i].Contains("repeat") && !repeatVals.ContainsKey(readFile.Count - 1))
                        repeatVals.Add(readFile.Count - 1, 0);

                    //label
                    if (lines[i][0] == ':')
                        if (labels.ContainsKey(lines[i]))
                            MessageBox.Show("\"" + lines[i] + "\" is already a label", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                        else
                            labels.Add(lines[i], readFile.Count - 1);
                }
            }

            //insert labels
            string[] keys = labels.Keys.ToArray();
            for (int i = 0; i < readFile.Count; i++)
                for (int j = 0; j < labels.Count; j++)
                    readFile[i] = readFile[i].Replace(keys[j], labels[keys[j]].ToString());

            //transfers lines into spawnPattern
            spawnPattern = readFile;
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
            if (gamestate == GAMESTATE.EDITOR && !isSPSaved)
            {
                MessageBoxResult result = MessageBox.Show("Do you want to save changes?", "", MessageBoxButton.YesNoCancel);

                if (result == MessageBoxResult.Yes)
                    SaveSP();
                else if (result == MessageBoxResult.Cancel)
                    e.Cancel = true;
            }
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            stopRequested = true;
        }

        void SaveSP()
        {
            StreamWriter sw = new StreamWriter("files/SP.txt");
            sw.Write(textEditor.Text);
            sw.Close();
            sw.Dispose();
            isSPSaved = true;
            Title = "Barrage";
        }

        private void LabelMenu_MouseEnter(object sender, MouseEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed || Mouse.RightButton == MouseButtonState.Pressed || Mouse.MiddleButton == MouseButtonState.Pressed)
                ((Label)sender).Background = new SolidColorBrush(Color.FromRgb(150, 150, 150));
            else
                ((Label)sender).Background = new SolidColorBrush(Color.FromRgb(230, 230, 230));
        }
        private void LabelMenu_MouseLeave(object sender, MouseEventArgs e)
        {
            ((Label)sender).Background = new SolidColorBrush(Color.FromRgb(200, 200, 200));
        }
        private void LabelMenu_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ((Label)sender).Background = new SolidColorBrush(Color.FromRgb(150, 150, 150));
        }
        private void LabelMenu_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (sender == labelMenuPlay)
            {
                gamestate = GAMESTATE.PLAY;
                gridMenu.Visibility = Visibility.Hidden;
                gridGame.Visibility = Visibility.Visible;
                gridField.Background = Brushes.Transparent;
            }
            else if (sender == labelMenuEditor)
            {
                gamestate = GAMESTATE.EDITOR;
                gridMain.Width = 800;
                gridMain.Height = 450;
                gridGame.HorizontalAlignment = HorizontalAlignment.Left;
                gridGame.VerticalAlignment = VerticalAlignment.Top;
                gridGameBorder.BorderThickness = new Thickness(0.5);
                labelFps.Margin = new Thickness(0, 0, 400, 0);
                double sX = gridSize.ActualWidth - rightBlack.Width * 4,
                    sY = (gridSize.ActualHeight - downBlack.Height * 2) / 8 * 9 - gridSize.ActualHeight;
                if (sX > 0)
                    Width += sX;
                if (sY > 0)
                    Height += sY;
                Window_SizeChanged(this, null);
                MinWidth = minSize.Width + 400;
                MinHeight = minSize.Height + 50;

                gridMenu.Visibility = Visibility.Hidden;
                gridEditor.Visibility = Visibility.Visible;
                gridGame.Visibility = Visibility.Visible;
                if ((bool)checkUseGrid.IsChecked)
                    gridField.Background = gridOverlay;
                else
                    gridField.Background = Brushes.Transparent;
            }
            else if (sender == labelMenuOptions)
            {
                gamestate = GAMESTATE.OPTIONS;
                gridMenu.Visibility = Visibility.Hidden;
                gridOptions.Visibility = Visibility.Visible;
            }
            else if (sender == labelMenuQuit)
            {
                Application.Current.Shutdown();
            }

            ((Label)sender).Background = new SolidColorBrush(Color.FromRgb(230, 230, 230));
        }
        private void LabelBack_MouseEnter(object sender, MouseEventArgs e)
        {
            ((Label)sender).Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150));
        }
        private void LabelBack_MouseLeave(object sender, MouseEventArgs e)
        {
            ((Label)sender).Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
        }
        private void LabelBack_MouseUp(object sender, MouseButtonEventArgs e)
        {
            bool canceled = false;
            if (gamestate == GAMESTATE.EDITOR)
            {
                MessageBoxResult result;
                if (isSPSaved)
                    result = MessageBox.Show("Leaving editor.", "", MessageBoxButton.OKCancel);
                else
                    result = MessageBox.Show("Do you want to save changes?", "", MessageBoxButton.YesNoCancel);

                if (result != MessageBoxResult.Cancel)
                {
                    gridMain.Width = 400;
                    gridMain.Height = 400;
                    gridGame.HorizontalAlignment = HorizontalAlignment.Center;
                    gridGameBorder.BorderThickness = new Thickness();
                    labelFps.Margin = new Thickness();
                    imageEditorPlay.Source = playPauseImgs[0];
                    MinWidth = minSize.Width;
                    MinHeight = minSize.Height;
                    playing = false;
                    if (WindowState != WindowState.Maximized)
                    {
                        double s = gridSize.ActualHeight / 9 - downBlack.Height / 4;
                        Width -= gridSize.ActualWidth / 2 - rightBlack.Width;
                        Height -= s;
                    }
                    Window_SizeChanged(this, null);

                    if (result == MessageBoxResult.Yes)
                        SaveSP();
                }
                else
                    canceled = true;
            }
            if (!canceled)
            {
                gamestate = GAMESTATE.MENU;
                gridMenu.Visibility = Visibility.Visible;
                gridGame.Visibility = Visibility.Hidden;
                gridEditor.Visibility = Visibility.Hidden;
                gridOptions.Visibility = Visibility.Hidden;

                MainWindow_KeyDown(this, new KeyEventArgs(Keyboard.PrimaryDevice, new HwndSource(0, 0, 0, 0, 0, "", IntPtr.Zero), 0, Key.R));
            }
        }
        private void LabelRetry_MouseUp(object sender, MouseButtonEventArgs e)
        {
            MainWindow_KeyDown(this, new KeyEventArgs(Keyboard.PrimaryDevice, new HwndSource(0, 0, 0, 0, 0, "", IntPtr.Zero), 0, Key.R));
        }
        private void ImageEditor_MouseEnter(object sender, MouseEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed || Mouse.RightButton == MouseButtonState.Pressed || Mouse.MiddleButton == MouseButtonState.Pressed)
                ((Image)sender).Opacity = 1;
            else
                ((Image)sender).Opacity = 0.5;
        }
        private void ImageEditor_MouseLeave(object sender, MouseEventArgs e)
        {
            ((Image)sender).Opacity = 0.8;
        }
        private void ImageEditor_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ((Image)sender).Opacity = 1;
        }
        private void ImageEditor_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ((Image)sender).Opacity = 0.5;

            if (sender == imageEditorPlay)
            {
                playing = !playing;
                if (playing)
                    imageEditorPlay.Source = playPauseImgs[1];
                else
                    imageEditorPlay.Source = playPauseImgs[0];
            }
            else if (sender == imageEditorStepForwards)
                stepForwards = true;
            else if (sender == imageEditorStepBackwards)
            {
                if (e.ChangedButton == MouseButton.Left)
                    histIndex--;
                else if (e.ChangedButton == MouseButton.Right)
                    histIndex -= 5;
                if (histIndex < histIndexMin)
                    histIndex = histIndexMin;
                if (histIndex < 0)
                    histIndex += hist.Length;
                while (hist[histIndex] == null)
                    if (--histIndex < 0)
                        histIndex += hist.Length;

                for (int i = 0; i < projectiles.Count; i++)
                    gridField.Children.Remove(projectiles[i].Sprite);
                projectiles = new List<Projectile>(hist[histIndex].projectiles);
                for (int i = 0; i < projectiles.Count; i++)
                {
                    projectiles[i] = hist[histIndex].projectiles[i].Clone();
                    gridField.Children.Add(projectiles[i].Sprite);
                    projectiles[i].Render();
                }
                plyrPos = hist[histIndex].plyrPos;
                bossPos = hist[histIndex].bossPos;
                bossAngle = hist[histIndex].bossAngle;
                bossTarget = hist[histIndex].bossTarget;
                bossMvSpd = hist[histIndex].bossMvSpd;
                bossAngSpd = hist[histIndex].bossAngSpd;
                time = hist[histIndex].time;
                readIndex = hist[histIndex].readIndex;
                wait = hist[histIndex].wait;
                repeatVals = hist[histIndex].repeatVals;
                spwnInd = hist[histIndex].spwnInd;
                spwnVals = new List<double>(hist[histIndex].spwnVals);

                RenderPlayerAndBoss();
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double min = Math.Min(gridSize.ActualWidth / gridMain.Width, gridSize.ActualHeight / gridMain.Height);
            gridMain.RenderTransform = new ScaleTransform(min, min);
            leftBlack.Width = Math.Max(gridSize.ActualWidth - min * gridMain.Width, 0) / 2;
            rightBlack.Width = Math.Max(gridSize.ActualWidth - min * gridMain.Width, 0) / 2;
            upBlack.Height = Math.Max(gridSize.ActualHeight - min * gridMain.Height, 0) / 2;
            downBlack.Height = Math.Max(gridSize.ActualHeight - min * gridMain.Height, 0) / 2;
        }

        private void TextEditor_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                textEditor.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            textEditKeyPresses++;
        }
        private void TextEditor_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (gamestate == GAMESTATE.EDITOR)
                ReadSpawnTxt();
            isSPSaved = false;
            Title = "*Barrage";
        }

        private void GridField_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (gamestate == GAMESTATE.EDITOR)
            {
                projStartPos = e.GetPosition((Grid)sender);
                if ((bool)checkUseGrid.IsChecked)
                {
                    projStartPos.X = Math.Round(projStartPos.X / 20) * 20;
                    projStartPos.Y = Math.Round(projStartPos.Y / 20) * 20;
                }

                Arrow.RenderTransform = new TransformGroup()
                {
                    Children = new TransformCollection() {
                        new ScaleTransform(0, 1),
                        new TranslateTransform(projStartPos.X, projStartPos.Y),
                    }
                };
                Arrow.Visibility = Visibility.Visible;
            }
        }
        private void GridField_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (gamestate == GAMESTATE.EDITOR && Arrow.Visibility == Visibility.Visible)
            {
                projEndPos = e.GetPosition((Grid)sender);
                if ((bool)checkUseGrid.IsChecked)
                {
                    projEndPos.X = Math.Round(projEndPos.X / 20) * 20;
                    projEndPos.Y = Math.Round(projEndPos.Y / 20) * 20;
                }

                List<string> lines = new List<string>(textEditor.Text.Split(new string[] { Environment.NewLine }, StringSplitOptions.None));
                string[] statement;
                bool inLoop = false;

                //check if in a loop
                for (int i = readIndex; i < lines.Count; i++)
                {
                    statement = lines[i].Split('|');
                    if (statement.Length >= 1)
                    {
                        int index = -1;
                        if (statement[0] == "repeat" && statement.Length >= 2)
                            index = 1;
                        else if (statement[0] == "ifGoto" && statement.Length >= 3)
                            index = 2;
                        if (index != -1 && int.TryParse(statement[index], out int num) && num < readIndex)
                            inLoop = true;
                    }
                }

                //shift repeat and ifGoto
                for (int i = 0; i < lines.Count; i++)
                {
                    statement = lines[i].Split('|');
                    if (statement.Length >= 1)
                    {
                        int index = -1;
                        if (statement[0] == "repeat" && statement.Length >= 2)
                            index = 1;
                        else if (statement[0] == "ifGoto" && statement.Length >= 3)
                            index = 2;
                        if (index != -1 && int.TryParse(statement[index], out int num) && num >= readIndex)
                        {
                            statement[index] = (num + (inLoop ? 3 : 1)).ToString();
                            lines[i] = string.Join("|", statement);
                        }
                    }
                }

                //insert new projectile
                lines.Insert(readIndex, string.Format("proj|tags=circle|startPos={0},{1}|speed={2}|angle={3}",
                    projStartPos.X - 200, projStartPos.Y - 200,
                    Math.Sqrt(Math.Pow(projStartPos.X - projEndPos.X, 2) + Math.Pow(projStartPos.Y - projEndPos.Y, 2)) / 20,
                    Math.Atan2(projEndPos.Y - projStartPos.Y, projEndPos.X - projStartPos.X) / Math.PI * 180));

                //insert statements
                //check if at end
                if (readIndex == spawnPattern.Count && wait < 0)
                {
                    lines.Insert(readIndex, "wait|" + (int)-wait);
                    readIndex++;
                    wait += (int)-wait;
                }
                else
                {
                    //split wait
                    if (wait > 0)
                    {
                        statement = lines[readIndex - 1].Split('|');
                        if (statement.Length >= 1 && statement[0] == "wait" && int.TryParse(statement[1], out int num))
                        {
                            lines[readIndex - 1] = "wait|" + (num - wait);
                            lines.Insert(readIndex + 1, "wait|" + wait);
                            wait = 0;
                        }
                    }

                    //time checker in loop
                    if (inLoop)
                    {
                        //find next available label for ifGoto
                        int labelIndex = 0;
                        while (labels.ContainsKey(':' + labelIndex.SetWidth(4)))
                            labelIndex++;
                        lines.Insert(readIndex, string.Format("ifGoto|t != {0}|{1}", time + 1, ":" + labelIndex.SetWidth(4)));
                        lines.Insert(readIndex + 2, ":" + labelIndex.SetWidth(4));
                    }
                }
                textEditor.Text = string.Join(Environment.NewLine, lines);

                Arrow.Visibility = Visibility.Hidden;
            }
        }

        private void GridField_MouseMove(object sender, MouseEventArgs e)
        {
            if (gamestate == GAMESTATE.EDITOR && Arrow.Visibility == Visibility.Visible)
            {
                if (e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed || e.MiddleButton == MouseButtonState.Pressed)
                {
                    projEndPos = e.GetPosition((Grid)sender);
                    Arrow.RenderTransform = new TransformGroup()
                    {
                        Children = new TransformCollection() {
                            new ScaleTransform(Math.Sqrt(Math.Pow(projStartPos.X - projEndPos.X, 2) + Math.Pow(projStartPos.Y - projEndPos.Y, 2)), 1),
                            new RotateTransform(Math.Atan2(projEndPos.Y - projStartPos.Y, projEndPos.X - projStartPos.X) / Math.PI * 180),
                            new TranslateTransform(projStartPos.X, projStartPos.Y),
                       }
                    };
                }
                else
                    GridField_MouseUp(sender, new MouseButtonEventArgs(e.MouseDevice, e.Timestamp, MouseButton.Left));
            }
        }

        private void AutosaveTimer_Tick(object sender, EventArgs e)
        {
            if (gamestate == GAMESTATE.EDITOR)
            {
                StreamWriter sw = new StreamWriter("files/SP(autosave).txt");
                sw.Write(textEditor.Text);
                sw.Close();
                sw.Dispose();
            }
        }

        private void CheckOptions_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (sender == checkUseGrid)
                if ((bool)checkUseGrid.IsChecked)
                    gridField.Background = gridOverlay;
                else
                    gridField.Background = Brushes.Transparent;
        }
    }
    public static class ExtensionMethods
    {
        private static readonly Action EmptyDelegate = delegate () { };

        public static void Refresh(this UIElement uiElement, DispatcherPriority priority)
        {
            uiElement.Dispatcher.Invoke(priority, EmptyDelegate);
        }

        public static Vector Scale(this Vector v1, Vector v2)
        {
            v1.X *= v2.X;
            v1.Y *= v2.Y;
            return v1;
        }

        public static string SetWidth(this int n, int width)
        {
            string s = n.ToString();
            return new string('0', width - s.Length) + s;
        }
    }
}
