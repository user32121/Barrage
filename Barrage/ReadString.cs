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
        static Dictionary<string, double> opPrecedence = new Dictionary<string, double>() {
            //lower number means lower precedence
            //.1 means it is left assosciative (calculation is read left to right)
            { "==", 0.1 }, { "!=", 0.1 }, { ">", 0.1 }, { ">=", 0.1 }, { "<", 0.1 }, { "<=", 0.1 },
            { "+", 1.1 }, { "-", 1.1 },
            { "*", 2.1 }, { "/", 2.1 }, {"MOD", 2.1},
            { "^", 3   },
            //1 parameter functions
            {"SQRT", 10}, {"SIGN", 10}, {"SIN", 10}, {"COS", 10}, {"TAN", 10}, {"ASIN", 10}, {"ACOS", 10},
            {"ABS", 10 }, {"FLR", 10 },
            //2 parameter functions
            {"MIN", 10 }, {"MAX", 10 }, {"RNG", 10}, {"ATAN",10},
        };

        public static int n;
        public static List<double> numVals;

        public static int t;
        public static double[] lastVals;

        public static int line;

        private static string ToPostfix(Queue<string> input)
        {
            Stack<string> opStack = new Stack<string>();
            Queue<string> output = new Queue<string>();

            while (input.Count > 0)
            {
                string token = input.Dequeue(); //get token            
                if (opPrecedence.TryGetValue(token, out double op1))   //function or operator
                {
                    while (opStack.Count > 0 && opStack.Peek() != "(" &&    //not empty or left parentheses
                        opPrecedence.TryGetValue(opStack.Peek(), out double op2) && (op2 > op1 ||    //compare precedence
                        op2 == op1 && (int)op2 != op2))    //check left assosciative
                        output.Enqueue(opStack.Pop());  //add to output queue
                    opStack.Push(token);
                }
                else if (token == "(")  //left parentheses
                    opStack.Push(token);
                else if (token == ")")  //right parentheses
                {
                    while (opStack.Peek() != "(")   //move from operator stack to output until close parentheses
                        output.Enqueue(opStack.Pop());
                    opStack.Pop();
                }
                else    //token must be a number or a value
                    output.Enqueue(token);
            }
            while (opStack.Count > 0)
                output.Enqueue(opStack.Pop());  //move remaining operators to ouput

            //convert to string
            string result = "";
            while (output.Count > 0)
            {
                result += output.Dequeue() + " ";
            }
            return result;
        }

        public static string ToEquation(string input)
        {
            //replaces n
            input = input.Replace("n", n.ToString());

            //replaces vals
            int i = input.IndexOf("val");
            while (i != -1 && !MainWindow.stopRequested)
            {
                //finds the val's index
                int j = 1;
                while (i + 3 + j < input.Length && char.IsDigit(input[i + 3 + j]))
                    j++;

                //substitutes the value
                if (i + 3 + j > input.Length)
                    MainWindow.MessageIssue(input, line);
                else
                {
                    int.TryParse(input.Substring(i + 3, j), out int valId);
                    if (valId < numVals.Count && valId >= 0)
                        input = input.Replace(input.Substring(i, j + 3), numVals[valId].ToString());
                    else
                        input = input.Replace(input.Substring(i, j + 3), "0");
                }

                //next val
                i = input.IndexOf("val");
            }

            //convert to postfix notation
            input = ToPostfix(new Queue<string>(input.Split(' ')));

            return input;
        }

        public static object Interpret(string input, Type Treturn)
        {
            //add values
            input = input.
                Replace("t", t.ToString()).
                Replace("PLYRX", MainWindow.plyrPos.X.ToString()).
                Replace("PLYRY", MainWindow.plyrPos.Y.ToString()).
                Replace("BOSSX", MainWindow.bossPos.X.ToString()).
                Replace("BOSSY", MainWindow.bossPos.Y.ToString());
            if (lastVals == null)
                input = input.
                    Replace("LXPOS", "0").
                    Replace("LYPOS", "0").
                    Replace("LXVEL", "0").
                    Replace("LYVEL", "0").
                    Replace("LSPD", "0").
                    Replace("LANG", "0");
            else
                input = input.
                    Replace("LXPOS", lastVals[(int)Projectile.LVI.x].ToString()).
                    Replace("LYPOS", lastVals[(int)Projectile.LVI.y].ToString()).
                    Replace("LXVEL", lastVals[(int)Projectile.LVI.xVel].ToString()).
                    Replace("LYVEL", lastVals[(int)Projectile.LVI.yVel].ToString()).
                    Replace("LSPD", lastVals[(int)Projectile.LVI.spd].ToString()).
                    Replace("LANG", lastVals[(int)Projectile.LVI.ang].ToString());

            if (Treturn == typeof(int))
            {
                return (int)Solve(input);
            }
            else if (Treturn == typeof(double))
            {
                return Solve(input);
            }
            else if (Treturn == typeof(Vector))
            {
                string[] xy = input.Split(',');
                return new Vector(Solve(xy[0]), Solve(xy[1]));
            }
            else
            {
                throw new NotImplementedException(Treturn.ToString() + " is not implemented");
            }
        }

        private static double Solve(string inp)
        {
            List<object> input = new List<object>(inp.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));

            if (input.Count == 0)
                return 0;

            for (int i = 0; i < input.Count; i++)
            {
                //1 value operators
                if ((string)input[i] == "SQRT")
                {
                    double nums = GetVals(ref input, inp, i - 1, 1)[0];
                    input[i] = Math.Sqrt(nums);
                    input.RemoveAt(Math.Max(i - 1, 0)); i--;
                }
                else if ((string)input[i] == "SIGN")
                {
                    double nums = GetVals(ref input, inp, i - 1, 1)[0];
                    input[i] = (double)Math.Sign(nums);
                    input.RemoveAt(Math.Max(i - 1, 0)); i--;
                }
                else if ((string)input[i] == "SIN")
                {
                    double nums = GetVals(ref input, inp, i - 1, 1)[0];
                    input[i] = Math.Sin(nums * Math.PI / 180);
                    input.RemoveAt(Math.Max(i - 1, 0)); i--;
                }
                else if ((string)input[i] == "COS")
                {
                    double nums = GetVals(ref input, inp, i - 1, 1)[0];
                    input[i] = Math.Cos(nums * Math.PI / 180);
                    input.RemoveAt(Math.Max(i - 1, 0)); i--;
                }
                else if ((string)input[i] == "TAN")
                {
                    double nums = GetVals(ref input, inp, i - 1, 1)[0];
                    input[i] = Math.Tan(nums * Math.PI / 180);
                    input.RemoveAt(Math.Max(i - 1, 0)); i--;
                }
                else if ((string)input[i] == "ASIN")
                {
                    double nums = GetVals(ref input, inp, i - 1, 1)[0];
                    input[i] = Math.Asin(nums) / Math.PI * 180;
                    input.RemoveAt(Math.Max(i - 1, 0)); i--;
                }
                else if ((string)input[i] == "ACOS")
                {
                    double nums = GetVals(ref input, inp, i - 1, 1)[0];
                    input[i] = Math.Acos(nums) / Math.PI * 180;
                    input.RemoveAt(Math.Max(i - 1, 0)); i--;
                }
                else if ((string)input[i] == "ABS")
                {
                    double nums = GetVals(ref input, inp, i - 1, 1)[0];
                    input[i] = Math.Abs(nums);
                    input.RemoveAt(Math.Max(i - 1, 0)); i--;
                }
                else if ((string)input[i] == "FLR")
                {
                    double nums = GetVals(ref input, inp, i - 1, 1)[0];
                    input[i] = Math.Floor(nums);
                    input.RemoveAt(Math.Max(i - 1, 0)); i--;
                }

                //2 value operators
                else if ((string)input[i] == "==")
                {
                    double[] nums = GetVals(ref input, inp, i - 2, 2);
                    input[i] = nums[0] == nums[1] ? 1 : 0;
                    input.RemoveRange(Math.Max(i - 2, 0), 2); i -= 2;
                }
                else if ((string)input[i] == "!=")
                {
                    double[] nums = GetVals(ref input, inp, i - 2, 2);
                    input[i] = nums[0] != nums[1] ? 1 : 0;
                    input.RemoveRange(Math.Max(i - 2, 0), 2); i -= 2;
                }
                else if ((string)input[i] == ">")
                {
                    double[] nums = GetVals(ref input, inp, i - 2, 2);
                    input[i] = nums[0] > nums[1] ? 1 : 0;
                    input.RemoveRange(Math.Max(i - 2, 0), 2); i -= 2;
                }
                else if ((string)input[i] == ">=")
                {
                    double[] nums = GetVals(ref input, inp, i - 2, 2);
                    input[i] = nums[0] >= nums[1] ? 1 : 0;
                    input.RemoveRange(Math.Max(i - 2, 0), 2); i -= 2;
                }
                else if ((string)input[i] == "<")
                {
                    double[] nums = GetVals(ref input, inp, i - 2, 2);
                    input[i] = nums[0] < nums[1] ? 1 : 0;
                    input.RemoveRange(Math.Max(i - 2, 0), 2); i -= 2;
                }
                else if ((string)input[i] == "<=")
                {
                    double[] nums = GetVals(ref input, inp, i - 2, 2);
                    input[i] = nums[0] <= nums[1] ? 1 : 0;
                    input.RemoveRange(Math.Max(i - 2, 0), 2); i -= 2;
                }
                else if ((string)input[i] == "+")
                {
                    double[] nums = GetVals(ref input, inp, i - 2, 2);
                    input[i] = nums[0] + nums[1];
                    input.RemoveRange(Math.Max(i - 2, 0), 2); i -= 2;
                }
                else if ((string)input[i] == "-")
                {
                    double[] nums = GetVals(ref input, inp, i - 2, 2);
                    input[i] = nums[0] - nums[1];
                    input.RemoveRange(Math.Max(i - 2, 0), 2); i -= 2;
                }
                else if ((string)input[i] == "*")
                {
                    double[] nums = GetVals(ref input, inp, i - 2, 2);
                    input[i] = nums[0] * nums[1];
                    input.RemoveRange(Math.Max(i - 2, 0), 2); i -= 2;
                }
                else if ((string)input[i] == "/")
                {
                    double[] nums = GetVals(ref input, inp, i - 2, 2);
                    input[i] = nums[0] / nums[1];
                    input.RemoveRange(Math.Max(i - 2, 0), 2); i -= 2;
                }
                else if ((string)input[i] == "^")
                {
                    double[] nums = GetVals(ref input, inp, i - 2, 2);
                    input[i] = Math.Pow(nums[0], nums[1]);
                    input.RemoveRange(Math.Max(i - 2, 0), 2); i -= 2;
                }
                else if ((string)input[i] == "MOD")
                {
                    double[] nums = GetVals(ref input, inp, i - 2, 2);
                    input[i] = nums[0] % nums[1];
                    input.RemoveRange(Math.Max(i - 2, 0), 2); i -= 2;
                }
                else if ((string)input[i] == "MIN")
                {
                    double[] nums = GetVals(ref input, inp, i - 2, 2);
                    input[i] = Math.Min(nums[0], nums[1]);
                    input.RemoveRange(Math.Max(i - 2, 0), 2); i -= 2;
                }
                else if ((string)input[i] == "MAX")
                {
                    double[] nums = GetVals(ref input, inp, i - 2, 2);
                    input[i] = Math.Max(nums[0], nums[1]);
                    input.RemoveRange(Math.Max(i - 2, 0), 2); i -= 2;
                }
                else if ((string)input[i] == "RNG")
                {
                    double[] nums = GetVals(ref input, inp, i - 2, 2);
                    input[i] = nums[0] + rng.NextDouble() * (nums[1] - nums[0]);
                    input.RemoveRange(Math.Max(i - 2, 0), 2); i -= 2;
                }
                else if ((string)input[i] == "ATAN")
                {
                    double[] nums = GetVals(ref input, inp, i - 2, 2);
                    input[i] = Math.Atan2(nums[0], nums[1]) / Math.PI * 180;
                    input.RemoveRange(Math.Max(i - 2, 0), 2); i -= 2;
                }
            }
            if (input[0] is double) //double
                return (double)input[0];
            else if (input[0] is int) //integer
                return (int)input[0];
            else if (double.TryParse((string)input[0], out double num))    //string
                return num;
            else
                MainWindow.MessageIssue(inp, line);
            return 0;
        }

        private static double[] GetVals(ref List<object> input, string text, int start, int count)
        {
            double[] output = new double[count];

            if (start < 0)
                if (MessageBox.Show(string.Format("There was an issue with \"{0}\" at line {1}\n Continue?", text, line),
                    "", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No && Application.Current != null)
                {
                    Application.Current.Shutdown();
                    MainWindow.stopRequested = true;
                }

            for (int i = 0; i < count; i++)
            {
                if (i + start < 0)
                    output[i] = 0;
                else if (input[i + start] is double) //double
                    output[i] = (double)input[i + start];
                else if (input[i + start] is int) //integer
                    output[i] = (int)input[i + start];
                else if (double.TryParse((string)input[i + start], out double num))    //string
                    output[i] = num;
                else if (MessageBox.Show(string.Format("There was an issue with \"{0}\" at line {1}\n Continue?", text, line),
                    "", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No && Application.Current != null)
                {
                    Application.Current.Shutdown();
                    MainWindow.stopRequested = true;
                }
            }

            return output;
        }
    }
}
