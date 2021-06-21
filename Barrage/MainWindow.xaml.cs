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
using SPUpdater;
using Path = System.IO.Path;
using System.Net;

namespace Barrage
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static MainWindow Main;
        public static readonly string filesFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Barrage\\files");

        #region

        #region game
        List<Projectile> projectiles = new List<Projectile>();
        bool paused;
        bool gameOver;
        bool isVisual;
        int time;  //age of the game in frames
        private static Random gameRng = new Random();  //separate random for non interpreter uses
        private const int laserImgLength = 100;
        public const int laserImgScale = 6;
        private const int laserLength = laserImgLength * laserImgScale;
        private static int msgBarDuration;  //frames left for the warning bar
        #endregion

        #region player
        public static Vector plyrPos;
        double plyrSpeed = 5;
        const double plyrFast = 5;
        const double plyrSlow = 2;
        #endregion

        #region boss
        public static Vector bossPos = new Vector(300, -300);
        double bossAngle = 0;
        object[] bossTargetX = null;
        object[] bossTargetY = null;
        object[] bossMvSpd = null;
        object[] bossAngSpd = null;
        #endregion

        #region enums
        public enum COMMANDS
        {
            NONE,
            PROJ,
            WAIT,
            GOTOIF,
            REPEAT,
            BOSS,
            VAL,
            RNG,
            VISUAL,
            FREEZE,
            TEXT,
        }
        public static readonly Dictionary<string, COMMANDS> strToCmd = new Dictionary<string, COMMANDS>()
        {
            { "proj",   COMMANDS.PROJ   },
            { "wait",   COMMANDS.WAIT   },
            { "gotoIf", COMMANDS.GOTOIF },
            { "repeat", COMMANDS.REPEAT },
            { "boss",   COMMANDS.BOSS   },
            { "val",    COMMANDS.VAL    },
            { "rng",    COMMANDS.RNG    },
            { "visual", COMMANDS.VISUAL },
            { "freeze", COMMANDS.FREEZE },
            { "text", COMMANDS.TEXT },
        };
        public enum GLOBALVARS
        {
            N,
            T,
            PLYRX, PLYRY,
            BOSSX, BOSSY,
            Count
        }
        public static readonly Dictionary<string, GLOBALVARS> strToGVar = new Dictionary<string, GLOBALVARS>()
        {
            { "n",      GLOBALVARS.N      },
            { "t",      GLOBALVARS.T      },
            { "PLYRX",  GLOBALVARS.PLYRX  },
            { "PLYRY",  GLOBALVARS.PLYRY  },
            { "BOSSX",  GLOBALVARS.BOSSX  },
            { "BOSSY",  GLOBALVARS.BOSSY  },
        };
        public enum PROJVARS
        {
            N,
            T,
            LANG,
            LSPD,
            LXPOS, LYPOS,
            LXVEL, LYVEL,
            LSTATE,
            Count
        }
        public static readonly Dictionary<string, PROJVARS> strToPVar = new Dictionary<string, PROJVARS>()
        {
            { "n",      PROJVARS.N      },
            { "t",      PROJVARS.T      },
            { "LPOSX",  PROJVARS.LXPOS  },
            { "LPOSY",  PROJVARS.LYPOS  },
            { "LVELX",  PROJVARS.LXVEL  },
            { "LVELY",  PROJVARS.LYVEL  },
            { "LSPD",   PROJVARS.LSPD   },
            { "LANG",   PROJVARS.LANG   },
            { "LSTATE", PROJVARS.LSTATE },
        };
        public struct ValIndex
        {
            public int index;
            public ValIndex(int index)
            {
                this.index = index;
            }
        }
        public enum PROPERTIES
        {
            TAGS,
            SPEED,
            ANGLE,
            XPOS,
            YPOS,
            XVEL,
            YVEL,
            SIZE,
            STARTX,
            STARTY,
            DURATION,
            TAGCOUNT,
            ACTDELAY,
            FILE,
            STATE,
        }
        public enum PARAMETERS
        {
            FIRST,
            SECOND,
            THIRD,
            FOURTH,
            TAGS,
            SPEED,
            ANGLE,
            XPOS,
            YPOS,
            XVEL,
            YVEL,
            SIZE,
            STARTX,
            STARTY,
            DURATION,
            TAGCOUNT,
            ACTDELAY,
            FILE,
            STATE,
        }
        public static readonly Dictionary<string, PROPERTIES> strToProp = new Dictionary<string, PROPERTIES>()
        {
            { "tags",       PROPERTIES.TAGS     },
            { "speed",      PROPERTIES.SPEED    },
            { "angle",      PROPERTIES.ANGLE    },
            { "xPos",       PROPERTIES.XPOS     },
            { "yPos",       PROPERTIES.YPOS     },
            { "xVel",       PROPERTIES.XVEL     },
            { "yVel",       PROPERTIES.YVEL     },
            { "size",       PROPERTIES.SIZE     },
            { "startX",     PROPERTIES.STARTX   },
            { "startY",     PROPERTIES.STARTY   },
            { "duration",   PROPERTIES.DURATION },
            { "tagCount",   PROPERTIES.TAGCOUNT },
            { "actDelay",   PROPERTIES.ACTDELAY },
            { "file",       PROPERTIES.FILE     },
            { "state",      PROPERTIES.STATE    },
        };
        public enum TAGS
        {
            NONE = 0b00000000,
            CIRCLE = 0b00000001,
            LASER = 0b00000010,
            WALLBOUNCE = 0b00000100,
            SCREENWRAP = 0b00001000,
            OUTSIDE = 0b00010000,
        };
        public static readonly Dictionary<string, TAGS> strToTag = new Dictionary<string, TAGS>()
        {
            { "circle",     TAGS.CIRCLE     },
            { "laser",      TAGS.LASER      },
            { "wallBounce", TAGS.WALLBOUNCE },
            { "screenWrap", TAGS.SCREENWRAP },
            { "outside",    TAGS.OUTSIDE    },
        };
        #endregion

        #region spawn pattern
        static string selectedScript = "";
        List<(COMMANDS, object)> spawnPattern = new List<(COMMANDS, object)>();  //object is either (object[],...) or (property, object[])[]
        public static List<string> spText = new List<string>();
        int readIndex = 0;
        double wait;
        Dictionary<int, int> repeatVals = new Dictionary<int, int>();    //(line,repeats left)
        int spawnInd;
        Dictionary<int, double> spawnVals = new Dictionary<int, double>();
        public static Dictionary<string, int> labelToInt = new Dictionary<string, int>();    //label, line #
        double[] spawnVars = new double[(int)GLOBALVARS.Count];
        Stopwatch SPTimeout = new Stopwatch();
        public static bool stopGameRequested;
        double plyrFreeze = 0;
        bool attemptReadSP = true;
        #endregion

        #region frame moderation
        DispatcherTimer kickStart;
        readonly Stopwatch stopwatch = new Stopwatch();
        readonly long frameLength;
        long nextFrame;
        long nextSecond;
        readonly int[] fps = new int[5];
        int fpsIndex;
        const int fpsMeasureRate = 5;
        public static bool closeRequested;
        #endregion

        #region  gamestate
        enum GAMESTATE
        {
            MENU,
            SELECTFORPLAY,
            SELECTFOREDIT,
            PLAY,
            EDITOR,
            OPTIONS,
            DOWNLOAD,
        }
        static GAMESTATE gamestate = GAMESTATE.MENU;
        #endregion

        #region image storage
        static readonly ImageSource[] playPauseImgs = new ImageSource[]
        {
            new BitmapImage(new Uri("files/play.png", UriKind.Relative)),
            new BitmapImage(new Uri("files/pause.png", UriKind.Relative)),
        };
        static readonly Dictionary<int, ImageSource> projectileImgs = new Dictionary<int, ImageSource>();  //cache gloabl skin files to avoid needing to load them again
        static readonly Dictionary<int, ImageSource> laserImgs = new Dictionary<int, ImageSource>();
        static readonly Dictionary<int, ImageSource> projectileLocalImgs = new Dictionary<int, ImageSource>();  //local skin files
        static readonly Dictionary<int, ImageSource> laserLocalImgs = new Dictionary<int, ImageSource>();
        static ImageSource defaultProjImg = new BitmapImage(new Uri("files/projectile.png", UriKind.Relative));
        static ImageSource defaultLaserImg = new BitmapImage(new Uri("files/laser.png", UriKind.Relative));
        static ImageSource defaultPreview = new BitmapImage(new Uri("files/preview.png", UriKind.Relative));
        static ImageSource defaultBossImg;
        static ImageSource defaultPlayerImg;
        #endregion

        #region editor
        bool playing;
        bool stepForwards;
        Size minSize;
        readonly LinearGradientBrush hitIndicatorBrush = new LinearGradientBrush(Colors.Transparent, Colors.Transparent, new Point(0, 0.5), new Point(1, 0.5));
        int textEditKeyPresses;
        DispatcherTimer autosaveTimer;
        readonly ImageBrush gridUnderlay = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/files/Grid.png")));
        bool isSPSaved;
        private const int projStepsAhead = 30;
        #endregion

        #endregion

        public MainWindow()
        {
            InitializeComponent();

            frameLength = Stopwatch.Frequency / 60;  //framerate

            plyrPos.Y = 100;

            if (File.Exists(Path.Combine(filesFolderPath, "boss.png")))
                Boss.Source = defaultBossImg = new BitmapImage(new Uri(Path.Combine(filesFolderPath, "boss.png")));
            else
                defaultBossImg = Boss.Source;
            if (File.Exists(Path.Combine(filesFolderPath, "player.png")))
                Player.Source = defaultPlayerImg = new BitmapImage(new Uri(Path.Combine(filesFolderPath, "player.png")));
            else
                defaultPlayerImg = Player.Source;

            if (File.Exists(Path.Combine(filesFolderPath, "play.png")))
                imageEditorPlay.Source = playPauseImgs[0] = new BitmapImage(new Uri(Path.Combine(filesFolderPath, "play.png")));
            if (File.Exists(Path.Combine(filesFolderPath, "pause.png")))
                playPauseImgs[1] = new BitmapImage(new Uri(Path.Combine(filesFolderPath, "pause.png")));
            if (File.Exists(Path.Combine(filesFolderPath, "step.png")))
                imageEditorStepForwards.Source = new BitmapImage(new Uri(Path.Combine(filesFolderPath, "step.png")));
            if (File.Exists(Path.Combine(filesFolderPath, "grid.png")))
                gridUnderlay = new ImageBrush(new BitmapImage(new Uri(Path.Combine(filesFolderPath, "grid.png"))));
            if (File.Exists(Path.Combine(filesFolderPath, "preview.png")))
                defaultPreview = new BitmapImage(new Uri(Path.Combine(filesFolderPath, "preview.png")));

            if (System.Deployment.Application.ApplicationDeployment.IsNetworkDeployed)
                labelVersion.Content = "v" + System.Deployment.Application.ApplicationDeployment.CurrentDeployment.CurrentVersion;

            //load settings
            GameSettings.TryLoad();
            checkMouse.IsChecked = GameSettings.useMouse;
            checkInfiniteLoop.IsChecked = GameSettings.checkForInfiniteLoop;
            dropDownErrorMode.SelectedIndex = (int)GameSettings.errorMode;
            checkUseGrid.IsChecked = GameSettings.useGrid;
            checkPredict.IsChecked = GameSettings.predictProjectile;

            LoadScripts();
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
            while (!closeRequested)
            {
                RunFrame();

                this.Refresh(DispatcherPriority.Input);
                ModerateFrames();
            }
        }

        void RunFrame()
        {
            if (gamestate == GAMESTATE.PLAY || playing || stepForwards)
            {
                if (!paused)
                {
                    msgBarDuration--;
                    labelWarning.Opacity = msgBarDuration / 100.0;

                    time++;
                    spawnVars[(int)GLOBALVARS.T] = time;

                    if (!gameOver)
                    {
                        PlayerMove();
                        SPStep();
                    }

                    MoveProjectiles();

                    if (!gameOver && !isVisual)
                        CheckPlayerHit();
                    RenderPlayerAndBoss();

                    DisplayArrow();

                    stepForwards = false;
                }
            }
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
                spawnInd = 0;
                for (int i = 0; i < spawnVals.Count; i++)
                    spawnVals[i] = 0;
                for (int i = 0; i < repeatVals.Count; i++)
                    repeatVals[i] = 0;
                wait = 0;
                time = 0;

                isVisual = false;
                labelVisual.Visibility = Visibility.Collapsed;

                if (attemptReadSP)
                    ReadSPTxt();

                gameOver = false;
                paused = false;
                gridField.Effect = null;
                gridPause.Visibility = Visibility.Collapsed;

                for (int i = 0; i < projectiles.Count; i++)
                {
                    gridField.Children.Remove(projectiles[i].img);
                    if (projectiles[i].path != null)
                        gridField.Children.Remove(projectiles[i].path);
                }
                projectiles.Clear();
                projCount.Content = "0";

                plyrPos = new Vector(0, 100);
                Player.RenderTransform = new TranslateTransform(plyrPos.X, plyrPos.Y);

                bossPos = new Vector(300, -300);
                Boss.RenderTransform = new TranslateTransform(bossPos.X, bossPos.Y);
                bossTargetX = null;
                bossTargetY = null;
                bossMvSpd = null;
                bossAngSpd = null;
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
                        paused = true;
                    }
                    else
                    {
                        gridField.Effect = null;
                        gridPause.Visibility = Visibility.Collapsed;
                        paused = false;
                    }
                }
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
                else if (e.Key == Key.S && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
                {
                    ReadSPTxt();
                    if (!stopGameRequested)
                        SaveSP();
                    stopGameRequested = false;
                    e.Handled = true;
                }
            }
            else if (gamestate == GAMESTATE.MENU)
            {
                if (e.Key == Key.P)
                {
                    LabelMenu_MouseUp(labelMenuPlay, null);
                    LabelMenu_MouseLeave(labelMenuPlay, null);
                }
                else if (e.Key == Key.E)
                {
                    LabelMenu_MouseUp(labelMenuEditor, null);
                    LabelMenu_MouseLeave(labelMenuEditor, null);
                }
                else if (e.Key == Key.O)
                {
                    LabelMenu_MouseUp(labelMenuOptions, null);
                    LabelMenu_MouseLeave(labelMenuOptions, null);
                }
            }
            else if (gamestate == GAMESTATE.SELECTFORPLAY || gamestate == GAMESTATE.SELECTFOREDIT)
            {
                if (e.Key == Key.Return)
                    GetSelectedScriptAndSetup();
            }
        }

        void PlayerMove()
        {
            if (plyrFreeze > 0)
            {
                plyrFreeze--;
                return;
            }

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
                if (GameSettings.autoPlay)
                {
                    Vector mov = new Vector((gameRng.NextDouble() - 0.5) / 10000, (gameRng.NextDouble() - 0.5) / 10000), dir;
                    for (int p = 0; p < projectiles.Count; p++)
                    {
                        if (projectiles[p].Tags.HasFlag(TAGS.CIRCLE))
                        {
                            dir = plyrPos - projectiles[p].Position;
                            mov += dir / Math.Abs(Math.Pow(dir.Length - Math.Sqrt(projectiles[p].RadiusSqr), 3));
                        }
                        if (projectiles[p].Tags.HasFlag(TAGS.LASER))
                        {
                            //rotation of axes method
                            double ang = projectiles[p].Angle;
                            Matrix origToTrans, transToOrig;  //rotation matrices to move between original axes and transformed (to rectangle) axes
                            origToTrans = transToOrig = Matrix.Identity;
                            origToTrans.Rotate(-ang);
                            transToOrig.Rotate(ang);

                            //find transformed positions
                            Vector tfRectPos = origToTrans.Transform(projectiles[p].Position) + new Vector(laserLength / 2, 0);  //center of rectangle
                            Vector tfPlyrPos = origToTrans.Transform(plyrPos);

                            //find closest point on rectangle
                            Vector closePos = transToOrig.Transform(new Vector(Math.Max(Math.Min(tfPlyrPos.X, tfRectPos.X + laserLength / 2), tfRectPos.X - laserLength / 2), Math.Max(Math.Min(tfPlyrPos.Y, tfRectPos.Y + projectiles[p].Radius), tfRectPos.Y - projectiles[p].Radius)) * 0.9999 + tfRectPos * 0.0001);

                            dir = plyrPos - closePos;
                            mov += dir / Math.Abs(Math.Pow(dir.Length, 3));
                        }
                    }
                    double d = plyrPos.X + 200.1;
                    mov.X += 10 / d / d;
                    d = plyrPos.X - 200.1;
                    mov.X -= 10 / d / d;
                    d = plyrPos.Y + 200.1;
                    mov.Y += 10 / d / d;
                    d = plyrPos.Y - 200.1;
                    mov.Y -= 10 / d / d;
                    if (mov.LengthSquared > 0)
                    {
                        mov.Normalize();
                        mov *= plyrSpeed;
                        plyrPos += mov;
                        moved = true;
                    }
                }

                if ((Keyboard.IsKeyDown(Key.LeftShift)) && plyrSpeed == plyrFast)
                {
                    plyrSpeed = plyrSlow;
                }
                if (Keyboard.IsKeyUp(Key.LeftShift) && plyrSpeed == plyrSlow)
                {
                    plyrSpeed = plyrFast;
                }
                if (Keyboard.IsKeyDown(Key.Left))
                {
                    plyrPos.X -= plyrSpeed;
                    moved = true;
                }
                if (Keyboard.IsKeyDown(Key.Right))
                {
                    plyrPos.X += plyrSpeed;
                    moved = true;
                }
                if (Keyboard.IsKeyDown(Key.Up))
                {
                    plyrPos.Y -= plyrSpeed;
                    moved = true;
                }
                if (Keyboard.IsKeyDown(Key.Down))
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
                if (!item.enabled)
                    continue;

                //collision detection
                if (item.Tags.HasFlag(TAGS.CIRCLE))
                {
                    //distance is less than radius
                    if (Math.Pow(plyrPos.X - item.Position.X, 2) + Math.Pow(plyrPos.Y - item.Position.Y, 2) < item.RadiusSqr)
                        hit = true;
                }
                else if (item.Tags.HasFlag(TAGS.LASER))
                {
                    //rotation of axes method
                    Matrix origToTrans;  //rotation matrices to move between original axes and transformed (to rectangle) axes
                    origToTrans = Matrix.Identity;
                    origToTrans.Rotate(-item.Angle);

                    //find transformed positions
                    Vector tfRectPos = origToTrans.Transform(item.Position) + new Vector(laserLength / 2, 0);  //center of rectangle
                    Vector tfPlyrPos = origToTrans.Transform(plyrPos);

                    if (Math.Abs(tfRectPos.X - tfPlyrPos.X) <= laserLength / 2 && Math.Abs(tfRectPos.Y - tfPlyrPos.Y) <= item.Radius)
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

        void SPStep()
        {
            if ((bool)checkInfiniteLoop.IsChecked)
                SPTimeout.Restart();
            while (wait <= 0 && readIndex < spawnPattern.Count && !closeRequested && !stopGameRequested)
            {
                if ((bool)checkInfiniteLoop.IsChecked && SPTimeout.ElapsedMilliseconds > 3000)
                {
                    MessageIssue("The SP might be stuck in a infinite loop\nContinue?");
                    SPTimeout.Restart();
                }

                ReadString.gameVars = spawnVars;
                ReadString.numVals = spawnVals;
                ReadString.projVars = null;
                ReadString.curLine = readIndex;

                //keeps reading lines until text says to wait
                (COMMANDS cmd, object args) = spawnPattern[readIndex];

                switch (cmd)
                {
                    case COMMANDS.NONE:
                        break;
                    case COMMANDS.PROJ:
                        Dictionary<PROPERTIES, object> oldProps = (Dictionary<PROPERTIES, object>)args;
                        Dictionary<PROPERTIES, object[]> newProps = new Dictionary<PROPERTIES, object[]>();
                        TAGS tags = TAGS.NONE;

                        foreach (var prop in oldProps)
                            switch (prop.Key)
                            {
                                case PROPERTIES.TAGS:
                                    tags = (TAGS)prop.Value;
                                    break;
                                default:
                                    newProps[prop.Key] = (object[])prop.Value;
                                    break;
                            }

                        CreateProj(newProps, tags, spawnInd, spawnVals);
                        spawnInd++;
                        spawnVars[(int)GLOBALVARS.N] = spawnInd;
                        break;
                    case COMMANDS.WAIT:
                        //waits # of frames untill spawns again
                        wait += ReadString.Interpret(args as object[], PARAMETERS.FIRST);
                        break;
                    case COMMANDS.GOTOIF:
                        {
                            (object[] line, object[] condition) = ((object[], object[]))args;

                            if (ReadString.Interpret(condition, PARAMETERS.SECOND) != 0)
                            {
                                int lineNum = (int)ReadString.Interpret(line, PARAMETERS.FIRST) - 1;
                                //(-1 because there is ++ later on)

                                if (lineNum < -1)
                                {
                                    MessageIssue(string.Join(" ", line[1]), spawnInd, "Line number cannot be negative.");
                                    readIndex = -1;
                                }
                                else
                                    readIndex = lineNum;
                            }
                        }
                        break;
                    case COMMANDS.REPEAT:
                        {
                            (object[] line, object[] times) = ((object[], object[]))args;

                            //sets repeats left
                            if (repeatVals[readIndex] <= 0)
                                repeatVals[readIndex] = (int)ReadString.Interpret(times, PARAMETERS.SECOND);

                            //repeats (stops at 1 since that will be the last repeat)
                            repeatVals[readIndex]--;
                            if (repeatVals[readIndex] >= 1)
                            {
                                int lineNum = (int)ReadString.Interpret(line, PARAMETERS.FIRST) - 1;
                                //(-1 because there is ++ later on)

                                if (lineNum < -1)
                                    MessageIssue(string.Join(" ", line), readIndex, "Line number cannot be negative.");
                                else
                                    readIndex = lineNum;
                            }
                        }
                        break;
                    case COMMANDS.BOSS:
                        //set movement and rotation of boss
                        (bossTargetX, bossTargetY, bossMvSpd, bossAngSpd) = ((object[], object[], object[], object[]))args;
                        break;
                    case COMMANDS.VAL:
                        (object[] indexArr, object[] value) = ((object[], object[]))args;

                        //sets a value to spwnVals
                        int index = (int)ReadString.Interpret(indexArr, PARAMETERS.FIRST);
                        if (index < 0)
                            MessageIssue(spText[readIndex], spawnInd, "Val# cannot be negative.");
                        else if (!stopGameRequested)
                            spawnVals[index] = ReadString.Interpret(value, PARAMETERS.SECOND);
                        break;
                    case COMMANDS.RNG:
                        //set rng seed
                        ReadString.rng = new Random((int)ReadString.Interpret(args as object[], PARAMETERS.FIRST));
                        break;
                    case COMMANDS.VISUAL:
                        isVisual = true;
                        break;
                    case COMMANDS.FREEZE:
                        //freeze player
                        plyrFreeze += ReadString.Interpret(args as object[], PARAMETERS.FIRST);
                        break;
                    case COMMANDS.TEXT:
                        //display text
                        labelSPText.Content = args;
                        break;
                    default:
                        throw new NotImplementedException();
                }

                //next line
                readIndex++;
            }

            stopGameRequested = false;
            wait--;
        }

        public void CreateProj(Dictionary<PROPERTIES, object[]> props, TAGS tags, int projIndex, Dictionary<int, double> numValues)
        {
            //add properties that don't exist
            foreach (KeyValuePair<PROPERTIES, object[]> defProp in Projectile.defaultProps)
                if (!props.ContainsKey(defProp.Key))
                    props.Add(defProp.Key, defProp.Value);

            double[] projVars = new double[(int)MainWindow.PROJVARS.Count];
            projVars[(int)PROJVARS.T] = 0;
            projVars[(int)PROJVARS.N] = projIndex;
            ReadString.projVars = projVars;

            //displays projectile
            int r = Math.Abs((int)ReadString.Interpret(props[PROPERTIES.SIZE], PARAMETERS.SIZE));
            Image projImage = new Image();
            if (GameSettings.predictProjectile)
                projImage.MouseEnter += ProjImage_MouseEnter;
            if (tags.HasFlag(TAGS.CIRCLE))
            {
                projImage.Width = r * 2;
                projImage.Height = r * 2;
                projImage.Source = GetProjectileImage((int)ReadString.Interpret(props[PROPERTIES.FILE], PARAMETERS.FILE), false);
                projImage.RenderTransformOrigin = new Point(0.5, 0.5);
            }
            else if (tags.HasFlag(TAGS.LASER))
            {
                projImage.Stretch = Stretch.Fill;
                projImage.Width = r * 2;
                projImage.Height = laserImgLength;
                projImage.Source = GetProjectileImage((int)ReadString.Interpret(props[PROPERTIES.FILE], PARAMETERS.FILE), true);
                projImage.RenderTransformOrigin = new Point(0.5, 0);
            }
            int temp = (int)ReadString.Interpret(props[PROPERTIES.ACTDELAY], PARAMETERS.ACTDELAY);
            if (temp >= 0 || temp == -1)
                projImage.Opacity = 0.3;
            gridField.Children.Add(projImage);

            //creates projectile
            double radians = ReadString.Interpret(props[PROPERTIES.ANGLE], PARAMETERS.ANGLE) * Math.PI / 180;
            Projectile tempProjectile = new Projectile(props[PROPERTIES.SIZE], projVars)
            {
                img = projImage,
                DurationRS = props[PROPERTIES.DURATION],
                FileRS = props[PROPERTIES.FILE],
                Position = new Vector(ReadString.Interpret(props[PROPERTIES.STARTX], PARAMETERS.STARTX), ReadString.Interpret(props[PROPERTIES.STARTY], PARAMETERS.STARTY)),
                SpeedRS = props[PROPERTIES.SPEED],
                AngleRS = props[PROPERTIES.ANGLE],
                XPosRS = props[PROPERTIES.XPOS],
                YPosRS = props[PROPERTIES.YPOS],
                XVelRS = props[PROPERTIES.XVEL],
                YVelRS = props[PROPERTIES.YVEL],
                Tags = tags,
                TagCountRS = props[PROPERTIES.TAGCOUNT],
                Velocity = new Vector(Math.Cos(radians), Math.Sin(radians)) * ReadString.Interpret(props[PROPERTIES.SPEED], PARAMETERS.SPEED),
                ActDelayRS = props[PROPERTIES.ACTDELAY],
                stateRS = props[PROPERTIES.STATE],
                numValues = new Dictionary<int, double>(spawnVals),
            };

            projectiles.Add(tempProjectile);
        }

        private void ProjImage_MouseEnter(object sender, MouseEventArgs e)
        {
            (sender as Image).Tag = projStepsAhead * 10;
            DisplayArrow();
        }

        //display a message box with the error message, also gives an option to stop the program. text is the message to be displayed
        public static void MessageIssue(string text)
        {
            if (GameSettings.errorMode == GameSettings.ErrorMode.Interrupt)
            {
                if (!stopGameRequested && MessageBox.Show(text, "", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                {
                    stopGameRequested = true;
                    if (gamestate == GAMESTATE.EDITOR)
                        Main.MainWindow_KeyDown(Main, new KeyEventArgs(Keyboard.PrimaryDevice, new HwndSource(0, 0, 0, 0, 0, "", IntPtr.Zero), 0, Key.P));
                    else
                        Main.LabelBack_MouseUp(Main.labelPauseBack, new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left));
                }
            }
            else if (GameSettings.errorMode == GameSettings.ErrorMode.Continue)
            {
                WarnIssue(text);
            }
        }
        //similar to above function, provides a template for incorrect text, line number, and error message
        public static void MessageIssue(string errorText, int line, string issue)
        {
            string text = string.Format("There was an issue with \"{0}\" at line {1}\n{2}\n Continue?", errorText, line, issue);
            if (GameSettings.errorMode == GameSettings.ErrorMode.Interrupt)
            {
                if (!stopGameRequested && MessageBox.Show(text, "An error occurred", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                {
                    stopGameRequested = true;
                    if (Main != null)
                        if (gamestate == GAMESTATE.EDITOR)
                            Main.MainWindow_KeyDown(Main, new KeyEventArgs(Keyboard.PrimaryDevice, new HwndSource(0, 0, 0, 0, 0, "", IntPtr.Zero), 0, Key.P));
                        else
                            Main.LabelBack_MouseUp(Main.labelPauseBack, new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left));
                }
            }
            else if (GameSettings.errorMode == GameSettings.ErrorMode.Continue)
            {
                WarnIssue(text);
            }
        }
        //display the message in the message/warning bar
        public static void WarnIssue(string text)
        {
            Main.labelWarning.Content = text;
            msgBarDuration = 300;
        }

        void MoveProjectiles()
        {
            //move projectiles
            for (int i = 0; i < projectiles.Count && !stopGameRequested; i++)
            {
                projectiles[i].Move();
                if (i < projectiles.Count && !projectiles[i].IsAlive)
                {
                    if (projectiles[i].path != null)
                        gridField.Children.Remove(projectiles[i].path);
                    gridField.Children.Remove(projectiles[i].img);
                    projectiles.RemoveAt(i);
                    i--;
                }
            }

            //counts projectiles for monitoring lag
            projCount.Content = projectiles.Count.ToString();

            //move the boss
            ReadString.projVars = null;
            Vector target = new Vector(ReadString.Interpret(bossTargetX, PARAMETERS.FIRST), ReadString.Interpret(bossTargetY, PARAMETERS.SECOND));
            Vector offset = target - bossPos;
            double mvSpd = ReadString.Interpret(bossMvSpd, PARAMETERS.THIRD);
            double angSpd = ReadString.Interpret(bossAngSpd, PARAMETERS.FOURTH);

            if (offset.LengthSquared > mvSpd * mvSpd)
            {
                offset.Normalize();
                offset *= mvSpd;
            }
            bossPos += offset;
            bossAngle += angSpd;
        }

        void ReadSPTxt()
        {
            string spPath = Path.Combine(filesFolderPath, "scripts", selectedScript, "SP.txt");
            if (!File.Exists(spPath))
            {
                MessageBox.Show(spPath + " not found");
                return;
            }

            string[] lines;
            if (gamestate == GAMESTATE.EDITOR)
                lines = textEditor.Text.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            else
            {
                StreamReader sr = new StreamReader(spPath);
                string temp = sr.ReadToEnd();

                //check for update
                if (GameSettings.errorMode == GameSettings.ErrorMode.Interrupt && !temp.StartsWith(SPUpdater.SPUpdater.versionText) && MessageBox.Show("The script was made in an older version.\nUpdate?", "", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    sr.Close();
                    sr.Dispose();

                    try
                    {
                        Process.Start(Path.Combine(filesFolderPath, "scripts\\SPUpdater.exe"), "\"" + Path.Combine(filesFolderPath, "scripts", selectedScript) + "\"").WaitForExit();
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.Message, "An issue occurred while converting script");
                    }

                    sr = new StreamReader(Path.Combine(filesFolderPath, "scripts", selectedScript, "SP.txt"));
                    temp = sr.ReadToEnd();
                }

                textEditor.Text = temp;
                isSPSaved = true;
                Title = "Barrage";
                lines = temp.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
                sr.Close();
                sr.Dispose();
            }

            spText.Clear();
            spawnPattern.Clear();
            labelToInt.Clear();

            //scan and look for labels
            for (int i = 0; i < lines.Length; i++)
                if (lines[i].Length > 0 && lines[i][0] == ':')  //label
                {
                    if (GameSettings.errorMode == GameSettings.ErrorMode.Interrupt)
                        if (labelToInt.ContainsKey(lines[i]))
                            MessageBox.Show("\"" + lines[i] + "\" is already a label", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                        else
                            labelToInt.Add(lines[i], i);
                    else if (GameSettings.errorMode == GameSettings.ErrorMode.Continue)
                    {
                        WarnIssue("\"" + lines[i] + "\" is already a label");
                    }
                }

            //parse each line
            for (int i = 0; i < lines.Length; i++)
            {
                spText.Add(lines[i]);

                //remove comments
                int commentInd = lines[i].IndexOf('#');
                if (commentInd != -1)
                    lines[i] = lines[i].Substring(0, commentInd);

                //split into command and arguments
                string[] lineSplt = lines[i].Split('|');

                if (lines[i].Length == 0 || lineSplt[0][0] == ':')  // empty(/comment)/label, ignored
                    spawnPattern.Add((COMMANDS.NONE, null));
                else if (strToCmd.TryGetValue(lineSplt[0], out COMMANDS cmd))
                {
                    ReadString.curLine = i;

                    switch (cmd)
                    {
                        case COMMANDS.PROJ:
                            Dictionary<PROPERTIES, object> props = new Dictionary<PROPERTIES, object>();
                            string propName;
                            string propVal;
                            for (int p = 1; p < lineSplt.Length; p++)
                            {
                                if (lineSplt[p].Trim().Length == 0)
                                    continue;

                                int index = lineSplt[p].IndexOf('=');
                                if (index == -1)
                                {
                                    MessageIssue(lineSplt[p], i, "Proj parameter not in PROPERTY=EQUATION format.");
                                    continue;
                                }

                                propName = lineSplt[p].Substring(0, index);
                                propVal = lineSplt[p].Substring(index + 1);
                                if (strToProp.TryGetValue(propName, out PROPERTIES prop))
                                    switch (prop)
                                    {
                                        case PROPERTIES.TAGS:
                                            props[prop] = ReadString.ToTags(propVal);
                                            break;
                                        default:
                                            props[prop] = ReadString.ToPostfix(propVal);
                                            break;
                                    }
                                else
                                    MessageIssue(propName, spawnInd, "Not a property name.");
                            }
                            spawnPattern.Add((cmd, props));
                            break;
                        case COMMANDS.REPEAT:
                            repeatVals[i] = 0;
                            goto case COMMANDS.GOTOIF;
                        case COMMANDS.GOTOIF:
                        case COMMANDS.VAL:
                            if (lineSplt.Length < 3)
                                MessageIssue(lines[i], i, "Not enough parameters. (Needs 2)");
                            else
                                spawnPattern.Add((cmd, (ReadString.ToPostfix(lineSplt[1]), ReadString.ToPostfix(lineSplt[2]))));
                            break;
                        case COMMANDS.BOSS:
                            if (lineSplt.Length < 5)
                                MessageIssue(lines[i], i, "Not enough parameters. (Needs 4)");
                            else
                                spawnPattern.Add((cmd, (ReadString.ToPostfix(lineSplt[1]), ReadString.ToPostfix(lineSplt[2]), ReadString.ToPostfix(lineSplt[3]), ReadString.ToPostfix(lineSplt[4]))));
                            break;
                        case COMMANDS.WAIT:
                        case COMMANDS.RNG:
                        case COMMANDS.FREEZE:
                            if (lineSplt.Length < 2)
                                MessageIssue(lines[i], i, "Not enough parameters. (Needs 1)");
                            else
                                spawnPattern.Add((cmd, ReadString.ToPostfix(lineSplt[1])));
                            break;
                        case COMMANDS.VISUAL:
                            spawnPattern.Add((cmd, null));
                            break;
                        case COMMANDS.TEXT:
                            if (lineSplt.Length < 2)
                                MessageIssue(lines[i], i, "Not enough parameters. (Needs 1)");
                            else
                                spawnPattern.Add((cmd, lineSplt[1]));
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }
                else
                {
                    MessageIssue(lineSplt[0], spawnInd, "Not a command.");
                    spawnPattern.Add((COMMANDS.NONE, null));
                }
            }
        }

        void ModerateFrames()
        {
            //moderate
            while (stopwatch.ElapsedTicks < nextFrame) ;
            long ticksPassed = stopwatch.ElapsedTicks;
            nextFrame = ticksPassed / frameLength * frameLength + frameLength;
            //nextFrame += frameLength;

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

        void DisplayArrow()
        {
            if (gamestate == GAMESTATE.EDITOR)
            {
                for (int i = 0; i < projectiles.Count; i++)
                    if (projectiles[i].img.Tag is int ticks)
                    {
                        if (ticks > 0)
                        {
                            GeometryGroup gg;
                            System.Windows.Shapes.Path path;
                            //create geometry group if path is null
                            if (projectiles[i].path == null)
                            {
                                //add prediction line points
                                gg = new GeometryGroup();
                                for (int s = 0; s < projStepsAhead; s++)
                                {
                                    gg.Children.Add(new LineGeometry());
                                    gg.Children.Add(new EllipseGeometry(new Point(), 1, 1));
                                }
                                gridField.Children.Add(path = projectiles[i].path = new System.Windows.Shapes.Path()
                                {
                                    Data = gg,
                                    Fill = Brushes.Black,
                                    Stroke = Brushes.Black,
                                    RenderTransform = new TranslateTransform(gridGame.ActualWidth / 2, gridGame.ActualHeight / 2),
                                });
                                Panel.SetZIndex(path, -1);
                            }
                            else
                            {
                                path = projectiles[i].path;
                                gg = path.Data as GeometryGroup;
                            }

                            path.Visibility = Visibility.Visible;

                            //store current values
                            Vector pos = projectiles[i].Position;
                            (gg.Children[0] as LineGeometry).StartPoint = (Point)pos;

                            ReadString.projVars = projectiles[i].projVars;
                            ReadString.numVals = projectiles[i].numValues;

                            //predict steps ahead
                            for (int t = 0; t < projStepsAhead; t++)
                            {
                                ReadString.projVars[(int)PROJVARS.T] = projectiles[i].Age + t;

                                Vector vel;
                                double spd, ang;
                                if (projectiles[i].XVelRS != null && projectiles[i].YVelRS != null)
                                {
                                    vel = new Vector(ReadString.Interpret(projectiles[i].XVelRS, PARAMETERS.XVEL), ReadString.Interpret(projectiles[i].YVelRS, PARAMETERS.YVEL));
                                    spd = vel.Length;
                                    ang = Math.Atan2(vel.Y, vel.X);
                                }
                                //xyPos
                                else if (projectiles[i].XPosRS != null && projectiles[i].YPosRS != null)
                                {
                                    vel = new Vector(ReadString.Interpret(projectiles[i].XPosRS, PARAMETERS.XPOS), ReadString.Interpret(projectiles[i].YPosRS, PARAMETERS.YPOS)) - pos;
                                    spd = vel.Length;
                                    ang = Math.Atan2(vel.Y, vel.X);
                                }
                                //speed and angle
                                else
                                {
                                    ang = ReadString.Interpret(projectiles[i].AngleRS, PARAMETERS.ANGLE);
                                    spd = ReadString.Interpret(projectiles[i].SpeedRS, PARAMETERS.SPEED);
                                    double radians = ang * Math.PI / 180;
                                    vel = new Vector(Math.Cos(radians), Math.Sin(radians)) * spd;
                                }

                                //moves pos by vel
                                pos += vel.Scale(projectiles[i].VelDir);

                                //set values to geometry
                                (gg.Children[t * 2] as LineGeometry).EndPoint = (Point)pos;
                                (gg.Children[t * 2 + 1] as EllipseGeometry).Center = (Point)pos;
                                if (t < projStepsAhead - 1)
                                    (gg.Children[t * 2 + 2] as LineGeometry).StartPoint = (Point)pos;

                                //set last values
                                ReadString.projVars[(int)PROJVARS.LXPOS] = pos.X;
                                ReadString.projVars[(int)PROJVARS.LYPOS] = pos.Y;
                                ReadString.projVars[(int)PROJVARS.LXVEL] = vel.X;
                                ReadString.projVars[(int)PROJVARS.LYVEL] = vel.Y;
                                ReadString.projVars[(int)PROJVARS.LSPD] = spd;
                                ReadString.projVars[(int)PROJVARS.LANG] = ang;
                            }
                            projectiles[i].img.Tag = ticks - 1;
                        }
                        else
                        {
                            projectiles[i].path.Visibility = Visibility.Collapsed;
                        }
                    }
            }
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
            closeRequested = true;
        }

        void SaveSP()
        {
            StreamWriter sw = new StreamWriter(Path.Combine(filesFolderPath, "scripts", selectedScript, "SP.txt"));
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
                gamestate = GAMESTATE.SELECTFORPLAY;
                gridMenu.Visibility = Visibility.Collapsed;
                gridScriptDisplay.Visibility = Visibility.Visible;
                gridScriptSelect.Visibility = Visibility.Visible;
                scriptSelectExButtons.Visibility = Visibility.Collapsed;
                LBScripts.Focus();
            }
            else if (sender == labelMenuEditor)
            {
                gamestate = GAMESTATE.SELECTFOREDIT;
                gridMenu.Visibility = Visibility.Collapsed;
                gridScriptDisplay.Visibility = Visibility.Visible;
                gridScriptSelect.Visibility = Visibility.Visible;
                scriptSelectExButtons.Visibility = Visibility.Visible;
                LBScripts.Focus();
            }
            else if (sender == labelMenuOptions)
            {
                gamestate = GAMESTATE.OPTIONS;
                gridMenu.Visibility = Visibility.Collapsed;
                gridOptions.Visibility = Visibility.Visible;
            }
            else if (sender == labelMenuQuit)
            {
                Application.Current.Shutdown();
            }
            else if (sender == labelRefreshScripts)
            {
                LoadScripts();
            }
            else if (sender == labelPlayScript)
            {
                GetSelectedScriptAndSetup();
            }
            else if (sender == labelNewScript)
            {
                MakeNewScript();
                return;
            }
            else if (sender == labelRenameScript)
            {
                RenameScript();
                return;
            }
            else if (sender == labelDeleteScript)
            {
                DeleteScript();
                return;
            }
            else if (sender == labelOnlineMenu)
            {
                gamestate = GAMESTATE.DOWNLOAD;
                gridScriptSelect.Visibility = Visibility.Collapsed;
                gridOnline.Visibility = Visibility.Visible;
                gridScriptDisplay.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);
                LBOnlineScripts.Visibility = Visibility.Visible;
                ListWebScripts();
                return;
            }
            else if (sender == labelOnlineRefresh)
            {
                LoadScripts();
                ListWebScripts();
                return;
            }
            else if (sender == labelOnlineUpload)
            {
                UploadSelectedScriptToWeb();
                return;
            }
            else if (sender == labelOnlineUpdate)
            {
                UpdateSelectedScriptOnWeb();
                return;
            }
            else if (sender == labelOnlineDownload)
            {
                DownloadSelectedWebScript();
                return;
            }
            else if (sender == labelOnlineDelete)
            {
                DeleteSelectedWebScript();
                return;
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
            if (gamestate == GAMESTATE.PLAY)
            {
                gamestate = GAMESTATE.SELECTFORPLAY;
                gridScriptDisplay.Visibility = Visibility.Visible;
                gridScriptSelect.Visibility = Visibility.Visible;
                gridGame.Visibility = Visibility.Collapsed;
            }
            else if (gamestate == GAMESTATE.EDITOR)
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

                    gamestate = GAMESTATE.SELECTFOREDIT;
                    gridScriptDisplay.Visibility = Visibility.Visible;
                    gridScriptSelect.Visibility = Visibility.Visible;
                    gridEditor.Visibility = Visibility.Collapsed;
                    gridGame.Visibility = Visibility.Collapsed;
                }
                else
                    canceled = true;
            }
            else if (gamestate == GAMESTATE.OPTIONS)
            {
                GameSettings.useMouse = (bool)checkMouse.IsChecked;
                GameSettings.checkForInfiniteLoop = (bool)checkInfiniteLoop.IsChecked;
                GameSettings.useGrid = (bool)checkUseGrid.IsChecked;
                GameSettings.errorMode = (GameSettings.ErrorMode)dropDownErrorMode.SelectedIndex;
                GameSettings.predictProjectile = (bool)checkPredict.IsChecked;
                GameSettings.Save();

                gamestate = GAMESTATE.MENU;
                gridMenu.Visibility = Visibility.Visible;
                gridOptions.Visibility = Visibility.Collapsed;
            }
            else if (gamestate == GAMESTATE.SELECTFORPLAY || gamestate == GAMESTATE.SELECTFOREDIT)
            {
                gridScriptDisplay.Visibility = Visibility.Collapsed;
                gridScriptSelect.Visibility = Visibility.Collapsed;
                gridMenu.Visibility = Visibility.Visible;
                gamestate = GAMESTATE.MENU;
            }
            else if (gamestate == GAMESTATE.DOWNLOAD)
            {
                gridOnline.Visibility = Visibility.Collapsed;
                gridScriptDisplay.ColumnDefinitions[1].Width = new GridLength(0, GridUnitType.Star);
                LBOnlineScripts.Visibility = Visibility.Collapsed;
                gridScriptSelect.Visibility = Visibility.Visible;
                gamestate = GAMESTATE.SELECTFOREDIT;
            }
            if (!canceled)
            {
                attemptReadSP = false;
                MainWindow_KeyDown(this, new KeyEventArgs(Keyboard.PrimaryDevice, new HwndSource(0, 0, 0, 0, 0, "", IntPtr.Zero), 0, Key.R));
                attemptReadSP = true;
            }
        }
        private void LabelRetry_MouseUp(object sender, MouseButtonEventArgs e)
        {
            attemptReadSP = false;
            MainWindow_KeyDown(this, new KeyEventArgs(Keyboard.PrimaryDevice, new HwndSource(0, 0, 0, 0, 0, "", IntPtr.Zero), 0, Key.R));
            attemptReadSP = true;
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
                labelEditorFocus.Focus();
            if (e.Key != Key.S || e.KeyboardDevice.Modifiers != ModifierKeys.Control)
                textEditKeyPresses++;
        }
        private void TextEditor_TextChanged(object sender, TextChangedEventArgs e)
        {
            isSPSaved = false;
            Title = "*Barrage";
        }

        private void AutosaveTimer_Tick(object sender, EventArgs e)
        {
            if (gamestate == GAMESTATE.EDITOR)
            {
                StreamWriter sw = new StreamWriter(Path.Combine(filesFolderPath, "scripts", selectedScript, "SP(autosave).txt"));
                sw.Write(textEditor.Text);
                sw.Close();
                sw.Dispose();
            }
        }

        private void CheckOptions_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (sender == checkUseGrid)
                if ((bool)checkUseGrid.IsChecked)
                    gridField.Background = gridUnderlay;
                else
                    gridField.Background = Brushes.Transparent;
        }

        public static ImageSource GetProjectileImage(int index, bool laserImg)
        {
            if (laserImg)
            {
                if (!laserLocalImgs.ContainsKey(index))
                {
                    //try load local skin
                    if (File.Exists(Path.Combine(filesFolderPath, "scripts", selectedScript, "laser" + index + ".png")))
                        laserLocalImgs[index] = new BitmapImage(new Uri(Path.Combine(filesFolderPath, "scripts", selectedScript, "laser" + index + ".png")));
                    else
                    {
                        if (!laserImgs.ContainsKey(index))
                            //try load global skin
                            if (File.Exists(Path.Combine(filesFolderPath, "laser" + index + ".png")))
                                laserImgs[index] = new BitmapImage(new Uri(Path.Combine(filesFolderPath, "laser" + index + ".png")));
                            else  //use default
                                laserImgs[index] = defaultLaserImg;
                        //transfer global skin to cache
                        laserLocalImgs[index] = laserImgs[index];
                    }
                }
                return laserLocalImgs[index];
            }
            else
            {
                if (!projectileLocalImgs.ContainsKey(index))
                {
                    //try load local skin
                    if (File.Exists(Path.Combine(filesFolderPath, "scripts", selectedScript, "projectile" + index + ".png")))
                        projectileLocalImgs[index] = new BitmapImage(new Uri(Path.Combine(filesFolderPath, "scripts", selectedScript, "projectile" + index + ".png")));
                    else
                    {
                        if (!projectileImgs.ContainsKey(index))
                            //try load global skin
                            if (File.Exists(Path.Combine(filesFolderPath, "projectile" + index + ".png")))
                                projectileImgs[index] = new BitmapImage(new Uri(Path.Combine(filesFolderPath, "projectile" + index + ".png")));
                            else  //use default
                                projectileImgs[index] = defaultProjImg;
                        //transfer global skin to cache
                        projectileLocalImgs[index] = projectileImgs[index];
                    }
                }
                return projectileLocalImgs[index];
            }
        }

        private void LabelOpenFiles_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Process.Start("explorer", filesFolderPath);
        }

        void LoadScripts()
        {
            LBScripts.Items.Clear();

            string[] scripts = Directory.GetDirectories(Path.Combine(filesFolderPath, "scripts"));
            for (int i = 0; i < scripts.Length; i++)
            {
                StackPanel SP = new StackPanel() { Tag = Path.GetFileName(scripts[i]), Orientation = Orientation.Horizontal };
                if (File.Exists(Path.Combine(scripts[i], "preview.png")))
                {
                    BitmapImage img = new BitmapImage();
                    img.BeginInit();
                    img.CacheOption = BitmapCacheOption.OnLoad;
                    img.UriSource = new Uri(Path.Combine(scripts[i], "preview.png"), UriKind.Absolute);
                    img.EndInit();
                    SP.Children.Add(new Image() { Source = img, Width = 50, Height = 50 });
                }
                else
                    SP.Children.Add(new Image() { Source = defaultPreview, Width = 50, Height = 50 });
                SP.Children.Add(new Label() { Content = Path.GetFileName(scripts[i]), VerticalAlignment = VerticalAlignment.Center });
                SP.Tag = Path.GetFileName(scripts[i]);

                LBScripts.Items.Add(SP);
            }

            LBScripts.SelectedIndex = 0;
        }
        void GetSelectedScriptAndSetup()
        {
            if (LBScripts.SelectedIndex == -1)
                return;

            selectedScript = (LBScripts.SelectedItem as StackPanel).Tag as string;

            //SP.txt
            ReadSPTxt();
            if (stopGameRequested)
            {
                stopGameRequested = false;
                return;
            }

            if (gamestate == GAMESTATE.SELECTFORPLAY)
            {
                gamestate = GAMESTATE.PLAY;
                gridScriptDisplay.Visibility = Visibility.Collapsed;
                gridScriptSelect.Visibility = Visibility.Collapsed;
                gridGame.Visibility = Visibility.Visible;
                gridField.Background = Brushes.Transparent;
            }
            else if (gamestate == GAMESTATE.SELECTFOREDIT)
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

                gridScriptDisplay.Visibility = Visibility.Collapsed;
                gridScriptSelect.Visibility = Visibility.Collapsed;
                gridEditor.Visibility = Visibility.Visible;
                gridGame.Visibility = Visibility.Visible;
                if ((bool)checkUseGrid.IsChecked)
                    gridField.Background = gridUnderlay;
                else
                    gridField.Background = Brushes.Transparent;
            }
            else
                throw new NotImplementedException();

            //images
            projectileLocalImgs.Clear();

            if (File.Exists(Path.Combine(filesFolderPath, "scripts", selectedScript, "boss.png")))
                Boss.Source = new BitmapImage(new Uri(Path.Combine(filesFolderPath, "scripts", selectedScript, "boss.png")));
            else
                Boss.Source = defaultBossImg;
            if (File.Exists(Path.Combine(filesFolderPath, "scripts", selectedScript, "player.png")))
                Player.Source = new BitmapImage(new Uri(Path.Combine(filesFolderPath, "scripts", selectedScript, "player.png")));
            else
                Player.Source = defaultPlayerImg;
        }
        void MakeNewScript()
        {
            string name = "";
            while (true)
            {
                name = InputDialog.Prompt("Enter new script name:", name);
                if (name == null)
                    return;

                int index;
                if (name == "")
                    MessageBox.Show("script name cannot be empty", "invalid script name");
                else if ((index = name.IndexOfAny(Path.GetInvalidFileNameChars())) != -1)
                    MessageBox.Show("script name contains an invalid character at position " + index, "invalid script name");
                else if (Directory.Exists(Path.Combine(filesFolderPath, "scripts", name)))
                    MessageBox.Show("a script with the same name already exists", "invalid script name");
                else
                    break;
            }

            try
            {
                Directory.CreateDirectory(Path.Combine(Path.Combine(filesFolderPath, "scripts", name)));
                StreamWriter sw = File.CreateText(Path.Combine(filesFolderPath, "scripts", name, "SP.txt"));
                sw.WriteLine(SPUpdater.SPUpdater.versionText);
                sw.Close();
            }
            catch (Exception e)
            {
                MessageBox.Show(string.Format("could not create new script: {0}\n{1}", e.GetType(), e.Message), "", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            LoadScripts();
        }
        void RenameScript()
        {
            if (LBScripts.SelectedIndex == -1)
                return;

            selectedScript = (LBScripts.SelectedItem as StackPanel).Tag as string;

            string newName = selectedScript;
            while (true)
            {
                newName = InputDialog.Prompt("Enter new script name:", newName);
                if (newName == null)
                    return;

                int index;
                if (newName == "")
                    MessageBox.Show("script name cannot be empty", "invalid script name");
                else if ((index = newName.IndexOfAny(Path.GetInvalidFileNameChars())) != -1)
                    MessageBox.Show("script name contains an invalid character at position " + index, "invalid script name");
                else if (Directory.Exists(Path.Combine(filesFolderPath, "scripts", newName)))
                    MessageBox.Show("a script with the same name already exists", "invalid script name");
                else
                    break;
            }

            try
            {
                Directory.Move(Path.Combine(filesFolderPath, "scripts", selectedScript), Path.Combine(filesFolderPath, "scripts", newName));
            }
            catch (Exception e)
            {
                MessageBox.Show(string.Format("could not rename script: {0}\n{1}", e.GetType(), e.Message), "", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }

            LoadScripts();
        }
        void DeleteScript()
        {
            if (LBScripts.SelectedIndex == -1)
                return;

            selectedScript = (LBScripts.SelectedItem as StackPanel).Tag as string;

            if (MessageBox.Show("Are you sure you want to delete this script forever?", "", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                try
                {
                    Directory.Delete(Path.Combine(filesFolderPath, "scripts", selectedScript), true);
                }
                catch (Exception e)
                {
                    MessageBox.Show(string.Format("could not delete script: {0}\n{1}", e.GetType(), e.Message), "", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }

            LoadScripts();
        }

        void ListWebScripts()
        {
            string[] scripts = MHBClient.ListScripts().Split('\n', '\r');
            LBOnlineScripts.Items.Clear();
            for (int i = 0; i < scripts.Length; i++)
                if (scripts[i].Length > 0)
                    LBOnlineScripts.Items.Add(Path.GetFileName(scripts[i]));

            LBOnlineScripts.SelectedIndex = 0;
            LBOnlineScripts.Focus();
        }
        void UploadSelectedScriptToWeb()
        {
            if (LBScripts.SelectedIndex == -1)
                return;

            string folderName = (LBScripts.SelectedItem as StackPanel).Tag as string;

            //get script password
            string password = InputDialog.Prompt("Please set a script password to allow editing access");
            if (password == null)
                return;

            MHBClient.Upload(Path.Combine(filesFolderPath, "scripts"), folderName, password);

            ListWebScripts();
        }
        void UpdateSelectedScriptOnWeb()
        {
            if (LBScripts.SelectedIndex == -1)
                return;

            string folderName = (LBScripts.SelectedItem as StackPanel).Tag as string;

            //get script password
            string password = InputDialog.Prompt("Please enter the script password to update it");
            if (password == null)
                return;

            MHBClient.UploadWithPassword(Path.Combine(filesFolderPath, "scripts"), folderName, password);

            ListWebScripts();
        }
        void DownloadSelectedWebScript()
        {
            if (LBOnlineScripts.SelectedIndex == -1)
                return;

            string folderName = LBOnlineScripts.SelectedItem as string;

            MHBClient.Download(Path.Combine(filesFolderPath, "scripts"), folderName);

            LoadScripts();
        }
        void DeleteSelectedWebScript()
        {
            if (LBOnlineScripts.SelectedIndex == -1)
                return;

            string folderName = LBOnlineScripts.SelectedItem as string;

            //get script password
            string password = InputDialog.Prompt("Please enter the script password to update it");
            if (password == null)
                return;

            MHBClient.Delete(folderName, password);

            ListWebScripts();
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

        //attempt to pop from the stack, will call message issue if no items
        public static double AttemptPop(this Stack<double> stack)
        {
            if (stack.Count > 0)
                return stack.Pop();
            else
            {
                MainWindow.MessageIssue(MainWindow.spText[ReadString.curLine], ReadString.curLine, "Not enough operands for operators for " + ReadString.curParam.ToString().ToLower() + " parameter");
                return 0;
            }
        }
    }
}
