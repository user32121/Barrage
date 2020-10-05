using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Barrage
{
    class ReadString
    {
        public static Random rng = new Random();
        static readonly Dictionary<string, OPERATORS> strToOp = new Dictionary<string, OPERATORS>() {
            { "==", OPERATORS.EQUAL }, { "!=", OPERATORS.NOTEQUAL }, { ">", OPERATORS.GREATER }, { ">=", OPERATORS.GREATEREQUAL }, { "<", OPERATORS.LESS }, { "<=", OPERATORS.LESSEQUAL },
            { "+", OPERATORS.ADD }, { "-", OPERATORS.SUBTRACT },
            { "*", OPERATORS.MULTIPLY }, { "/", OPERATORS.DIVIDE }, { "MOD", OPERATORS.MOD },
            { "^", OPERATORS.POW }, { "SQRT", OPERATORS.SQRT },
            { "SIGN", OPERATORS.SIGN }, { "ABS", OPERATORS.ABS },
            { "FLR", OPERATORS.FLR }, { "CEIL", OPERATORS.CEIL }, { "ROUND", OPERATORS.ROUND },
            { "RNG", OPERATORS.RNG }, { "MIN", OPERATORS.MIN }, { "MAX", OPERATORS.MAX },
            { "SIN", OPERATORS.SIN }, { "COS", OPERATORS.COS }, { "TAN", OPERATORS.TAN }, { "ASIN", OPERATORS.ASIN }, { "ACOS", OPERATORS.ACOS }, { "ATAN", OPERATORS.ATAN },
        };
        static readonly Dictionary<OPERATORS, double> opPrecedence = new Dictionary<OPERATORS, double>() {
            //lower number means lower precedence
            //.1 means it is left assosciative (calculation is read left to right)
            //inline operators
            {OPERATORS.EQUAL, 0.1}, {OPERATORS.NOTEQUAL, 0.1}, {OPERATORS.GREATER, 0.1}, {OPERATORS.GREATEREQUAL, 0.1}, {OPERATORS.LESS, 0.1}, {OPERATORS.LESSEQUAL, 0.1},
            {OPERATORS.ADD, 1.1}, {OPERATORS.SUBTRACT, 1.1},
            {OPERATORS.MULTIPLY, 2.1}, {OPERATORS.DIVIDE, 2.1}, {OPERATORS.MOD, 10},
            {OPERATORS.POW, 3}, {OPERATORS.SQRT, 10},
            {OPERATORS.SIGN, 10}, {OPERATORS.ABS, 10},
            {OPERATORS.FLR, 10}, {OPERATORS.CEIL, 10}, {OPERATORS.ROUND, 10},
            {OPERATORS.RNG, 10}, {OPERATORS.MIN, 10}, {OPERATORS.MAX, 10},
            {OPERATORS.SIN, 10}, {OPERATORS.COS, 10}, {OPERATORS.TAN, 10}, {OPERATORS.ASIN, 10}, {OPERATORS.ACOS, 10}, {OPERATORS.ATAN,10},
        };
        enum OPERATORS
        {
            PARENLEFT = -1, PARENRIGHT = -2,
            EQUAL = 0, NOTEQUAL, GREATER, GREATEREQUAL, LESS, LESSEQUAL,
            ADD, SUBTRACT,
            MULTIPLY, DIVIDE, MOD,
            POW, SQRT,
            SIGN, ABS,
            FLR, CEIL, ROUND,
            RNG, MIN, MAX,
            SIN, COS, TAN, ASIN, ACOS, ATAN
        }

        //global values
        public static int n;
        public static List<double> numVals;

        //projectile specific values
        public static int t;
        public static double[] projVals;

        //debug info
        public static int line;
        public static string lineStr;

        //converts from infix/prefix notation to postfix notation
        public static object[] ToPostfix(string inp)
        {
            Queue<string> input = new Queue<string>(inp.Split(MainWindow.charSpace, StringSplitOptions.RemoveEmptyEntries));
            Stack<OPERATORS> opStack = new Stack<OPERATORS>();
            Queue<object> output = new Queue<object>();

            while (input.Count > 0)
            {
                string token = input.Dequeue(); //get token            
                if (strToOp.TryGetValue(token, out OPERATORS op1))   //function or operator
                {
                    double prec1 = opPrecedence[op1];
                    while (opStack.Count > 0 && opStack.Peek() != OPERATORS.PARENLEFT &&    //not empty or left parentheses
                        opPrecedence.TryGetValue(opStack.Peek(), out double prec2) && (prec2 > prec1 ||    //compare precedence
                        prec2 == prec1 && (int)prec2 != prec2))    //check left assosciative
                        output.Enqueue(opStack.Pop());  //add to output queue
                    opStack.Push(op1);
                }
                else if (token == "(")  //left parentheses
                    opStack.Push(OPERATORS.PARENLEFT);
                else if (token == ")")  //right parentheses
                {
                    while (opStack.Peek() != OPERATORS.PARENLEFT)   //move from operator stack to output until close parentheses
                        output.Enqueue(opStack.Pop());
                    opStack.Pop();
                }
                else if (double.TryParse(token, out double val))   //token is a number
                    output.Enqueue(val);
                else    //token must be a string
                    output.Enqueue(token);
            }
            while (opStack.Count > 0)
                output.Enqueue(opStack.Pop());  //move remaining operators to ouput

            return output.ToArray();
        }

        //inserts n and val#
        public static object[] InsertValues(object[] input)
        {
            //null check
            if (input == null)
                return null;

            input = (object[])input.Clone();

            for (int i = 0; i < input.Length && !MainWindow.closeRequested; i++)
            {
                {
                    //replace n
                    if (input[i] is string s && s == "n")
                        input[i] = n;
                }
                {
                    //replace vals
                    if (input[i] is string s && s.StartsWith("val"))
                        if (int.TryParse(s.Substring(3), out int ind))
                            if (ind >= 0 && ind < numVals.Count)
                                input[i] = numVals[ind];
                            else
                                input[i] = 0;
                        else
                            MainWindow.MessageIssue(s, line);
                }
            }

            return input;
        }
        public static (object[], object[]) InsertValues((object[], object[]) input)
        {
            return (InsertValues(input.Item1), InsertValues(input.Item2));
        }

        //evaluates the expression
        public static double Interpret(object[] inp)
        {
            //null value check (returns default value of 0)
            if (inp == null)
                return 0;

            List<object> input = new List<object>(inp);

            if (input.Count == 0)
                return 0;

            for (int i = 0; i >= 0 && i < input.Count && !MainWindow.stopGameRequested; i++)
            {
                switch (input[i])
                {
                    case string s:
                        switch (s)
                        {
                            case "t":
                                input[i] = t;
                                break;
                            case "PLYRX":
                                input[i] = MainWindow.plyrPos.X;
                                break;
                            case "PLYRY":
                                input[i] = MainWindow.plyrPos.Y;
                                break;
                            case "BOSSX":
                                input[i] = MainWindow.bossPos.X;
                                break;
                            case "BOSSY":
                                input[i] = MainWindow.bossPos.Y;
                                break;
                            case "LPOSX":
                                input[i] = projVals == null ? 0 : projVals[(int)Projectile.VI.LXPOS];
                                break;
                            case "LPOSY":
                                input[i] = projVals == null ? 0 : projVals[(int)Projectile.VI.LYPOS];
                                break;
                            case "LVELX":
                                input[i] = projVals == null ? 0 : projVals[(int)Projectile.VI.LXVEL];
                                break;
                            case "LVELY":
                                input[i] = projVals == null ? 0 : projVals[(int)Projectile.VI.LYVEL];
                                break;
                            case "LSPD":
                                input[i] = projVals == null ? 0 : projVals[(int)Projectile.VI.LSPD];
                                break;
                            case "LANG":
                                input[i] = projVals == null ? 0 : projVals[(int)Projectile.VI.LANG];
                                break;
                            case "LSTATE":
                                input[i] = projVals == null ? 0 : projVals[(int)Projectile.VI.LSTATE];
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                        break;

                    case OPERATORS op:
                        switch (op)
                        {
                            //1 value operators
                            case OPERATORS.SQRT:
                                double[] nums = GetVals(ref input, i - 1, 1);
                                input[i] = Math.Sqrt(nums[0]);
                                input.RemoveAt(Math.Max(i - 1, 0)); i--;
                                break;
                            case OPERATORS.SIGN:
                                nums = GetVals(ref input, i - 1, 1);
                                input[i] = (double)Math.Sign(nums[0]);
                                input.RemoveAt(Math.Max(i - 1, 0)); i--;
                                break;
                            case OPERATORS.SIN:
                                nums = GetVals(ref input, i - 1, 1);
                                input[i] = Math.Sin(nums[0] * Math.PI / 180);
                                input.RemoveAt(Math.Max(i - 1, 0)); i--;
                                break;
                            case OPERATORS.COS:
                                nums = GetVals(ref input, i - 1, 1);
                                input[i] = Math.Cos(nums[0] * Math.PI / 180);
                                input.RemoveAt(Math.Max(i - 1, 0)); i--;
                                break;
                            case OPERATORS.TAN:
                                nums = GetVals(ref input, i - 1, 1);
                                input[i] = Math.Tan(nums[0] * Math.PI / 180);
                                input.RemoveAt(Math.Max(i - 1, 0)); i--;
                                break;
                            case OPERATORS.ASIN:
                                nums = GetVals(ref input, i - 1, 1);
                                input[i] = Math.Asin(nums[0]) / Math.PI * 180;
                                input.RemoveAt(Math.Max(i - 1, 0)); i--;
                                break;
                            case OPERATORS.ACOS:
                                nums = GetVals(ref input, i - 1, 1);
                                input[i] = Math.Acos(nums[0]) / Math.PI * 180;
                                input.RemoveAt(Math.Max(i - 1, 0)); i--;
                                break;
                            case OPERATORS.ABS:
                                nums = GetVals(ref input, i - 1, 1);
                                input[i] = Math.Abs(nums[0]);
                                input.RemoveAt(Math.Max(i - 1, 0)); i--;
                                break;
                            case OPERATORS.FLR:
                                nums = GetVals(ref input, i - 1, 1);
                                input[i] = Math.Floor(nums[0]);
                                input.RemoveAt(Math.Max(i - 1, 0)); i--;
                                break;
                            case OPERATORS.CEIL:
                                nums = GetVals(ref input, i - 1, 1);
                                input[i] = Math.Ceiling(nums[0]);
                                input.RemoveAt(Math.Max(i - 1, 0)); i--;
                                break;
                            case OPERATORS.ROUND:
                                nums = GetVals(ref input, i - 1, 1);
                                input[i] = Math.Round(nums[0]);
                                input.RemoveAt(Math.Max(i - 1, 0)); i--;
                                break;
                            //2 value operators
                            case OPERATORS.EQUAL:
                                nums = GetVals(ref input, i - 2, 2);
                                input[i] = nums[0] == nums[1] ? 1 : 0;
                                input.RemoveRange(Math.Max(i - 2, 0), Math.Min(2, input.Count - 1)); i -= 2;
                                break;
                            case OPERATORS.NOTEQUAL:
                                nums = GetVals(ref input, i - 2, 2);
                                input[i] = nums[0] != nums[1] ? 1 : 0;
                                input.RemoveRange(Math.Max(i - 2, 0), Math.Min(2, input.Count - 1)); i -= 2;
                                break;
                            case OPERATORS.GREATER:
                                nums = GetVals(ref input, i - 2, 2);
                                input[i] = nums[0] > nums[1] ? 1 : 0;
                                input.RemoveRange(Math.Max(i - 2, 0), Math.Min(2, input.Count - 1)); i -= 2;
                                break;
                            case OPERATORS.GREATEREQUAL:
                                nums = GetVals(ref input, i - 2, 2);
                                input[i] = nums[0] >= nums[1] ? 1 : 0;
                                input.RemoveRange(Math.Max(i - 2, 0), Math.Min(2, input.Count - 1)); i -= 2;
                                break;
                            case OPERATORS.LESS:
                                nums = GetVals(ref input, i - 2, 2);
                                input[i] = nums[0] < nums[1] ? 1 : 0;
                                input.RemoveRange(Math.Max(i - 2, 0), Math.Min(2, input.Count - 1)); i -= 2;
                                break;
                            case OPERATORS.LESSEQUAL:
                                nums = GetVals(ref input, i - 2, 2);
                                input[i] = nums[0] <= nums[1] ? 1 : 0;
                                input.RemoveRange(Math.Max(i - 2, 0), Math.Min(2, input.Count - 1)); i -= 2;
                                break;
                            case OPERATORS.ADD:
                                nums = GetVals(ref input, i - 2, 2);
                                input[i] = nums[0] + nums[1];
                                input.RemoveRange(Math.Max(i - 2, 0), Math.Min(2, input.Count - 1)); i -= 2;
                                break;
                            case OPERATORS.SUBTRACT:
                                nums = GetVals(ref input, i - 2, 2);
                                input[i] = nums[0] - nums[1];
                                input.RemoveRange(Math.Max(i - 2, 0), Math.Min(2, input.Count - 1)); i -= 2;
                                break;
                            case OPERATORS.MULTIPLY:
                                nums = GetVals(ref input, i - 2, 2);
                                input[i] = nums[0] * nums[1];
                                input.RemoveRange(Math.Max(i - 2, 0), Math.Min(2, input.Count - 1)); i -= 2;
                                break;
                            case OPERATORS.DIVIDE:
                                nums = GetVals(ref input, i - 2, 2);
                                input[i] = nums[0] / nums[1];
                                input.RemoveRange(Math.Max(i - 2, 0), Math.Min(2, input.Count - 1)); i -= 2;
                                break;
                            case OPERATORS.POW:
                                nums = GetVals(ref input, i - 2, 2);
                                input[i] = Math.Pow(nums[0], nums[1]);
                                input.RemoveRange(Math.Max(i - 2, 0), Math.Min(2, input.Count - 1)); i -= 2;
                                break;
                            case OPERATORS.MOD:
                                nums = GetVals(ref input, i - 2, 2);
                                input[i] = nums[0] % nums[1];
                                input.RemoveRange(Math.Max(i - 2, 0), Math.Min(2, input.Count - 1)); i -= 2;
                                break;
                            case OPERATORS.MIN:
                                nums = GetVals(ref input, i - 2, 2);
                                input[i] = Math.Min(nums[0], nums[1]);
                                input.RemoveRange(Math.Max(i - 2, 0), Math.Min(2, input.Count - 1)); i -= 2;
                                break;
                            case OPERATORS.MAX:
                                nums = GetVals(ref input, i - 2, 2);
                                input[i] = Math.Max(nums[0], nums[1]);
                                input.RemoveRange(Math.Max(i - 2, 0), Math.Min(2, input.Count - 1)); i -= 2;
                                break;
                            case OPERATORS.RNG:
                                nums = GetVals(ref input, i - 2, 2);
                                input[i] = nums[0] + rng.NextDouble() * (nums[1] - nums[0]);
                                input.RemoveRange(Math.Max(i - 2, 0), Math.Min(2, input.Count - 1)); i -= 2;
                                break;
                            case OPERATORS.ATAN:
                                nums = GetVals(ref input, i - 2, 2);
                                input[i] = Math.Atan2(nums[0], nums[1]) / Math.PI * 180;
                                input.RemoveRange(Math.Max(i - 2, 0), Math.Min(2, input.Count - 1)); i -= 2;
                                break;
                        }
                        break;
                }
            }
            if (input.Count < 1)
                return 0;
            else if (input[0] is double) //double
                return (double)input[0];
            else if (input[0] is int) //integer
                return (int)input[0];
            else
                MainWindow.MessageIssue(lineStr, line);
            return 0;
        }

        private static double[] GetVals(ref List<object> input, int start, int count)
        {
            //gets a number of values from the input list
            double[] output = new double[count];

            if (start < 0)
                MainWindow.MessageIssue(lineStr, line);

            for (int i = 0; i < count; i++)
            {
                if (i + start < 0)  //out of range
                    output[i] = 0;
                else if (input[i + start] is double) //double
                    output[i] = (double)input[i + start];
                else if (input[i + start] is int) //integer
                    output[i] = (int)input[i + start];
                else  //something went wrong
                    MainWindow.MessageIssue(lineStr, line);
            }

            return output;
        }
    }
}
