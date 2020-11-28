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

namespace Barrage
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static MainWindow Main;

        #region game
        List<Projectile> projectiles = new List<Projectile>();
        bool paused;
        bool gameOver;
        bool isVisual;
        int time;  //age of the game in frames
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
        List<(COMMANDS, object)> spawnPattern = new List<(COMMANDS, object)>();  //object is either (object[],...) or (property, object[])[]
        public static List<string> spText = new List<string>();
        int readIndex = 0;
        double wait;
        Dictionary<int, int> repeatVals = new Dictionary<int, int>();    //(line,repeats left)
        int spawnInd;
        Dictionary<int, double> spawnVals = new Dictionary<int, double>();
        Dictionary<string, int> labelToInt = new Dictionary<string, int>();    //label, line #
        double[] spawnVars = new double[(int)GLOBALVARS.Count];
        Stopwatch SPTimeout = new Stopwatch();
        public static bool stopGameRequested;
        double plyrFreeze = 0;
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
            PLAY,
            EDITOR,
            OPTIONS,
        }
        static GAMESTATE gamestate = GAMESTATE.MENU;
        #endregion

        #region image storage
        static readonly BitmapImage[] playPauseImgs = new BitmapImage[]
        {
            new BitmapImage(new Uri("files/Play.png", UriKind.Relative)),
            new BitmapImage(new Uri("files/Pause.png", UriKind.Relative)),
        };
        static readonly List<BitmapImage> projectileImgs = new List<BitmapImage>();
        static int laserImgsIndex;
        #endregion

        #region editor
        bool playing;
        bool stepForwards;
        Size minSize;
        readonly LinearGradientBrush hitIndicatorBrush = new LinearGradientBrush(Colors.Transparent, Colors.Transparent, new Point(0, 0.5), new Point(1, 0.5));
        Point projStartPos;
        Point projEndPos;
        int textEditKeyPresses;
        DispatcherTimer autosaveTimer;
        readonly ImageBrush gridUnderlay = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/files/Grid.png")));
        bool isSPSaved;
        private const int projStepsAhead = 30;
        #endregion

        public MainWindow()
        {
            InitializeComponent();

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
                imageEditorStepForwards.Source = new BitmapImage(new Uri(Directory.GetCurrentDirectory() + "/files/Step.png"));
            if (File.Exists("files/Arrow.png"))
                Arrow.Source = new BitmapImage(new Uri(Directory.GetCurrentDirectory() + "/files/Arrow.png"));
            if (File.Exists("files/Grid.png"))
                gridUnderlay = new ImageBrush(new BitmapImage(new Uri(Directory.GetCurrentDirectory() + "/files/Grid.png")));
            if (System.Deployment.Application.ApplicationDeployment.IsNetworkDeployed)
                labelVersion.Content = "v" + System.Deployment.Application.ApplicationDeployment.CurrentDeployment.CurrentVersion;

            //load settings
            GameSettings.TryLoad();
            checkMouse.IsChecked = GameSettings.useMouse;
            checkInfiniteLoop.IsChecked = GameSettings.checkForInfiniteLoop;
            checkError.IsChecked = GameSettings.checkForErrors;
            checkUseGrid.IsChecked = GameSettings.useGrid;
            checkPredict.IsChecked = GameSettings.predictProjectile;

            ReadSPTxt();
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
                    time++;

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
                labelVisual.Visibility = Visibility.Hidden;

                ReadSPTxt();

                gameOver = false;
                paused = false;
                gridField.Effect = null;
                gridPause.Visibility = Visibility.Hidden;

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
                        gridPause.Visibility = Visibility.Hidden;
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
                    Vector mov = new Vector(), dir;
                    for (int p = 0; p < projectiles.Count; p++)
                    {
                        dir = plyrPos - projectiles[p].Position;
                        mov += dir / Math.Pow(dir.LengthSquared - projectiles[p].RadiusSqr, 1.5);
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
                    //dist to line is less than radius, also checks if plyr is behind laser
                    ReadString.projVars = item.projVars;
                    ReadString.numVals = item.numValues;
                    double ang = ReadString.Interpret(item.Angle),
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

                spawnVars[(int)GLOBALVARS.T] = time;
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
                        wait += ReadString.Interpret(args as object[]);
                        break;
                    case COMMANDS.GOTOIF:
                        {
                            (object[] line, object[] condition) = ((object[], object[]))args;

                            if (ReadString.Interpret(condition) != 0)
                            {
                                int lineNum = (int)ReadString.Interpret(line) - 1;
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
                                repeatVals[readIndex] = (int)ReadString.Interpret(times);

                            //repeats (stops at 1 since that will be the last repeat)
                            repeatVals[readIndex]--;
                            if (repeatVals[readIndex] >= 1)
                            {
                                int lineNum = (int)ReadString.Interpret(line) - 1;
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
                        int index = (int)ReadString.Interpret(indexArr);
                        if (index < 0)
                            MessageIssue(spText[readIndex], spawnInd, "Val# cannot be negative.");
                        else if (!stopGameRequested)
                            spawnVals[index] = ReadString.Interpret(value);
                        break;
                    case COMMANDS.RNG:
                        //set rng seed
                        ReadString.rng = new Random((int)ReadString.Interpret(args as object[]));
                        break;
                    case COMMANDS.VISUAL:
                        isVisual = true;
                        break;
                    case COMMANDS.FREEZE:
                        //freeze player
                        plyrFreeze += ReadString.Interpret(args as object[]);
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
            int r = Math.Abs((int)ReadString.Interpret(props[PROPERTIES.SIZE]));
            Image projImage = new Image();
            if (GameSettings.predictProjectile)
                projImage.MouseEnter += ProjImage_MouseEnter;
            if (tags.HasFlag(TAGS.CIRCLE))
            {
                projImage.Width = r * 2;
                projImage.Height = r * 2;
                projImage.Source = GetProjectileImage((int)ReadString.Interpret(props[PROPERTIES.FILE]), false);
                projImage.RenderTransformOrigin = new Point(0.5, 0.5);
            }
            else if (tags.HasFlag(TAGS.LASER))
            {
                projImage.Stretch = Stretch.Fill;
                projImage.Width = r * 2;
                projImage.Height = 100;
                projImage.Source = GetProjectileImage((int)ReadString.Interpret(props[PROPERTIES.FILE]), true);
                projImage.RenderTransformOrigin = new Point(0.5, 0);
            }
            int temp = (int)ReadString.Interpret(props[PROPERTIES.ACTDELAY]);
            if (temp >= 0 || temp == -1)
                projImage.Opacity = 0.3;
            gridField.Children.Add(projImage);

            //creates projectile
            double radians = ReadString.Interpret(props[PROPERTIES.ANGLE]) * Math.PI / 180;
            Projectile tempProjectile = new Projectile(props[PROPERTIES.SIZE], projVars)
            {
                img = projImage,
                Duration = props[PROPERTIES.DURATION],
                File = props[PROPERTIES.FILE],
                Position = new Vector(ReadString.Interpret(props[PROPERTIES.STARTX]), ReadString.Interpret(props[PROPERTIES.STARTY])),
                Speed = props[PROPERTIES.SPEED],
                Angle = props[PROPERTIES.ANGLE],
                XPos = props[PROPERTIES.XPOS],
                YPos = props[PROPERTIES.YPOS],
                XVel = props[PROPERTIES.XVEL],
                YVel = props[PROPERTIES.YVEL],
                Tags = tags,
                TagCount = props[PROPERTIES.TAGCOUNT],
                Velocity = new Vector(Math.Cos(radians), Math.Sin(radians)) * ReadString.Interpret(props[PROPERTIES.SPEED]),
                ActDelay = props[PROPERTIES.ACTDELAY],
                state = props[PROPERTIES.STATE],
                numValues = new Dictionary<int, double>(spawnVals),
            };

            projectiles.Add(tempProjectile);
        }

        private void ProjImage_MouseEnter(object sender, MouseEventArgs e)
        {
            (sender as Image).Tag = projStepsAhead * 10;
            DisplayArrow();
        }

        //display a message box with the error message, also gives an option to stop the program
        public static void MessageIssue(string text)
        {
            if (GameSettings.checkForErrors && !stopGameRequested && MessageBox.Show(text, "", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
            {
                stopGameRequested = true;
                if (gamestate == GAMESTATE.EDITOR)
                    Main.MainWindow_KeyDown(Main, new KeyEventArgs(Keyboard.PrimaryDevice, new HwndSource(0, 0, 0, 0, 0, "", IntPtr.Zero), 0, Key.P));
                else
                    Main.LabelBack_MouseUp(Main.labelPauseBack, new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left));
            }
        }
        //similar to above function, can be called from anywhere, provides a template for incorrect text, line number, and error message
        public static void MessageIssue(string text, int line, string issue)
        {
            if (GameSettings.checkForErrors && !stopGameRequested && MessageBox.Show(string.Format("There was an issue with \"{0}\" at line {1}\n{2}\n Continue?", text, line, issue),
                "An error occurred", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
            {
                stopGameRequested = true;
                if (Main != null)
                    if (gamestate == GAMESTATE.EDITOR)
                        Main.MainWindow_KeyDown(Main, new KeyEventArgs(Keyboard.PrimaryDevice, new HwndSource(0, 0, 0, 0, 0, "", IntPtr.Zero), 0, Key.P));
                    else
                        Main.LabelBack_MouseUp(Main.labelPauseBack, new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left));
            }
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
            Vector target = new Vector(ReadString.Interpret(bossTargetX), ReadString.Interpret(bossTargetY));
            Vector offset = target - bossPos;
            double mvSpd = ReadString.Interpret(bossMvSpd);
            double angSpd = ReadString.Interpret(bossAngSpd);

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
            if (!File.Exists("files/SP.txt"))
                MessageBox.Show("files/SP.txt not found");

            string[] lines;
            if (gamestate == GAMESTATE.EDITOR)
                lines = textEditor.Text.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            else
            {
                StreamReader sr = new StreamReader("files/SP.txt");
                string temp = sr.ReadToEnd();

                //check for update
                if (!temp.StartsWith(SPUpdater.SPUpdater.versionText) && MessageBox.Show("The script was made in an older version.\nUpdate?", "", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    sr.Close();
                    sr.Dispose();

                    Process.Start(Directory.GetCurrentDirectory() + "/files/SPUpdater.exe").WaitForExit();

                    sr = new StreamReader("files/SP.txt");
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
                    if (labelToInt.ContainsKey(lines[i]))
                        MessageBox.Show("\"" + lines[i] + "\" is already a label", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    else
                        labelToInt.Add(lines[i], i);
                }

            //loads files into readFile and removes comments and empty spaces
            for (int i = 0; i < lines.Length; i++)
            {
                spText.Add(lines[i]);

                //substitute labels
                if (lines[i].Length > 1 && lines[i][0] != ':')
                    foreach (var label in labelToInt)
                        lines[i] = lines[i].Replace(label.Key, label.Value.ToString());

                //split into command and arguments
                string[] lineSplt = lines[i].Split('|');

                if (lines[i].Length == 0 || lineSplt[0][0] == '#' || lineSplt[0][0] == ':')  // empty/comment/label, ignored
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
                                int index = lineSplt[p].IndexOf('=');
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
                            spawnPattern.Add((cmd, (ReadString.ToPostfix(lineSplt[1]), ReadString.ToPostfix(lineSplt[2]))));
                            break;
                        case COMMANDS.BOSS:
                            spawnPattern.Add((cmd, (ReadString.ToPostfix(lineSplt[1]), ReadString.ToPostfix(lineSplt[2]), ReadString.ToPostfix(lineSplt[3]), ReadString.ToPostfix(lineSplt[4]))));
                            break;
                        case COMMANDS.WAIT:
                        case COMMANDS.RNG:
                        case COMMANDS.FREEZE:
                            spawnPattern.Add((cmd, ReadString.ToPostfix(lineSplt[1])));
                            break;
                        case COMMANDS.VISUAL:
                            spawnPattern.Add((cmd, null));
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }
                else
                    MessageIssue(lineSplt[0], spawnInd, "Not a command.");
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
                            Vector pos = projectiles[i].Position;
                            (gg.Children[0] as LineGeometry).StartPoint = (Point)pos;

                            ReadString.projVars = (new double[(int)PROJVARS.Count]);
                            Array.Copy(projectiles[i].projVars, ReadString.projVars, (int)PROJVARS.Count);

                            //predict steps ahead
                            for (int t = 0; t < projStepsAhead; t++)
                            {
                                ReadString.projVars[(int)PROJVARS.T] = projectiles[i].Age + t;

                                Vector vel;
                                double spd, ang;
                                if (projectiles[i].XVel != null && projectiles[i].YVel != null)
                                {
                                    vel = new Vector(ReadString.Interpret(projectiles[i].XVel), ReadString.Interpret(projectiles[i].YVel));
                                    spd = vel.Length;
                                    ang = Math.Atan2(vel.Y, vel.X);
                                }
                                //xyPos
                                else if (projectiles[i].XPos != null && projectiles[i].YPos != null)
                                {
                                    vel = new Vector(ReadString.Interpret(projectiles[i].XPos), ReadString.Interpret(projectiles[i].YPos)) - pos;
                                    spd = vel.Length;
                                    ang = Math.Atan2(vel.Y, vel.X);
                                }
                                //speed and angle
                                else
                                {
                                    ang = ReadString.Interpret(projectiles[i].Angle);
                                    spd = ReadString.Interpret(projectiles[i].Speed);
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
                    gridField.Background = gridUnderlay;
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
            else if (gamestate == GAMESTATE.OPTIONS)
            {
                GameSettings.useMouse = (bool)checkMouse.IsChecked;
                GameSettings.checkForInfiniteLoop = (bool)checkInfiniteLoop.IsChecked;
                GameSettings.useGrid = (bool)checkUseGrid.IsChecked;
                GameSettings.checkForErrors = (bool)checkError.IsChecked;
                GameSettings.predictProjectile = (bool)checkPredict.IsChecked;
                GameSettings.Save();
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
                        while (labelToInt.ContainsKey(':' + labelIndex.SetWidth(4)))
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
                    gridField.Background = gridUnderlay;
                else
                    gridField.Background = Brushes.Transparent;
        }

        public static BitmapImage GetProjectileImage(int index, bool laserImg)
        {
            if (laserImg)
            {
                while (index + laserImgsIndex >= projectileImgs.Count)
                    projectileImgs.Add(null);

                if (projectileImgs[index + laserImgsIndex] == null)
                    if (File.Exists("files/Laser" + index + ".png"))
                        projectileImgs[index + laserImgsIndex] = new BitmapImage(new Uri(Directory.GetCurrentDirectory() + "/files/Laser" + index + ".png"));
                    else
                        projectileImgs[index + laserImgsIndex] = new BitmapImage(new Uri("files/Laser.png", UriKind.Relative));
                return projectileImgs[index + laserImgsIndex];
            }
            else
            {
                while (index >= laserImgsIndex)
                {
                    projectileImgs.Insert(laserImgsIndex, null);
                    laserImgsIndex++;
                }
                if (projectileImgs[index] == null)
                    if (File.Exists("files/Projectile" + index + ".png"))
                        projectileImgs[index] = new BitmapImage(new Uri(Directory.GetCurrentDirectory() + "/files/Projectile" + index + ".png"));
                    else
                        projectileImgs[index] = new BitmapImage(new Uri("files/Projectile.png", UriKind.Relative));
                return projectileImgs[index];
            }
        }

        private void LabelOpenFiles_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Process.Start("explorer", "files");
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
        public static double AttPop(this Stack<double> stack)
        {
            if (stack.Count > 0)
                return stack.Pop();
            else
            {
                MainWindow.MessageIssue(MainWindow.spText[ReadString.curLine], ReadString.curLine, "Not enough operands for operators.");
                return 0;
            }
        }
    }
}
