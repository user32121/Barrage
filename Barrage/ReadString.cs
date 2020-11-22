using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Barrage
{
    public class ReadString
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
        public enum OPERATORS
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
        public static double[] gameVars;

        //projectile specific values
        public static double[] projVars;
        public static Dictionary<int, double> numVals;

        //debug info
        public static int line;

        //converts from infix/prefix notation (string) to postfix notation (object[])
        public static object[] ToPostfix(string inp)
        {
            Queue<string> input = new Queue<string>(inp.Split(' '));
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
                    if (opStack.Count == 0)
                        MainWindow.MessageIssue(inp, line, "Unbalanced parentheses");
                    else
                    {
                        while (opStack.Peek() != OPERATORS.PARENLEFT)   //move from operator stack to output until close parentheses
                        {
                            output.Enqueue(opStack.Pop());
                            if (opStack.Count == 0)
                            {
                                MainWindow.MessageIssue(inp, line, "Unbalanced parentheses");
                                goto BREAKOPSTACKLOOP;
                            }
                        }
                        opStack.Pop();
                    BREAKOPSTACKLOOP:;
                    }
                }
                else if (MainWindow.strToPVar.TryGetValue(token, out MainWindow.PROJVARS pVar))   //token is a projectile variable
                    output.Enqueue(pVar);
                else if (MainWindow.strToGVar.TryGetValue(token, out MainWindow.GLOBALVARS gVar))   //token is a global variable
                    output.Enqueue(gVar);
                else if (token.StartsWith("val"))   //token is a num value
                    if (int.TryParse(token.Substring(3), out int index))
                        output.Enqueue(new MainWindow.ValIndex(index));
                    else
                        MainWindow.MessageIssue(token, line, "Invalid val index");
                else if (double.TryParse(token, out double val))   //token is a number
                    output.Enqueue(val);
                else
                    MainWindow.MessageIssue(token, line, "Not a number or a variable");
            }
            while (opStack.Count > 0)
                output.Enqueue(opStack.Pop());  //move remaining operators to ouput

            return output.ToArray();
        }

        //converts from a string of tags separated by commas to a TAGS enum
        public static MainWindow.TAGS ToTags(string s)
        {
            string[] tagsStr = s.Split(',');
            MainWindow.TAGS tags = MainWindow.TAGS.NONE;
            for (int i = 0; i < tagsStr.Length; i++)
            {
                if (MainWindow.strToTag.TryGetValue(tagsStr[i], out MainWindow.TAGS tag))
                    tags |= tag;
                else
                    MainWindow.MessageIssue(tagsStr[i], line, "Not a tag");
            }
            return tags;
        }

        //evaluates the expression
        //also inserts variable values
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
                    case MainWindow.GLOBALVARS gVar:
                        switch (gVar)
                        {
                            case MainWindow.GLOBALVARS.N:
                                input[i] = projVars == null ? gameVars[(int)MainWindow.GLOBALVARS.N] : projVars[(int)MainWindow.PROJVARS.N];
                                break;
                            case MainWindow.GLOBALVARS.T:
                                input[i] = projVars == null ? gameVars[(int)MainWindow.GLOBALVARS.T] : projVars[(int)MainWindow.PROJVARS.T];
                                break;
                            case MainWindow.GLOBALVARS.PLYRX:
                                input[i] = MainWindow.plyrPos.X;
                                break;
                            case MainWindow.GLOBALVARS.PLYRY:
                                input[i] = MainWindow.plyrPos.Y;
                                break;
                            case MainWindow.GLOBALVARS.BOSSX:
                                input[i] = MainWindow.bossPos.X;
                                break;
                            case MainWindow.GLOBALVARS.BOSSY:
                                input[i] = MainWindow.bossPos.Y;
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                        break;

                    case MainWindow.PROJVARS pVar:
                        switch (pVar)
                        {
                            case MainWindow.PROJVARS.N:
                                input[i] = projVars == null ? gameVars[(int)MainWindow.GLOBALVARS.N] : projVars[(int)MainWindow.PROJVARS.N];
                                break;
                            case MainWindow.PROJVARS.T:
                                input[i] = projVars == null ? gameVars[(int)MainWindow.GLOBALVARS.T] : projVars[(int)MainWindow.PROJVARS.T];
                                break;
                            case MainWindow.PROJVARS.LXPOS:
                                input[i] = projVars == null ? 0 : projVars[(int)MainWindow.PROJVARS.LXPOS];
                                break;
                            case MainWindow.PROJVARS.LYPOS:
                                input[i] = projVars == null ? 0 : projVars[(int)MainWindow.PROJVARS.LYPOS];
                                break;
                            case MainWindow.PROJVARS.LXVEL:
                                input[i] = projVars == null ? 0 : projVars[(int)MainWindow.PROJVARS.LXVEL];
                                break;
                            case MainWindow.PROJVARS.LYVEL:
                                input[i] = projVars == null ? 0 : projVars[(int)MainWindow.PROJVARS.LYVEL];
                                break;
                            case MainWindow.PROJVARS.LSPD:
                                input[i] = projVars == null ? 0 : projVars[(int)MainWindow.PROJVARS.LSPD];
                                break;
                            case MainWindow.PROJVARS.LANG:
                                input[i] = projVars == null ? 0 : projVars[(int)MainWindow.PROJVARS.LANG];
                                break;
                            case MainWindow.PROJVARS.LSTATE:
                                input[i] = projVars == null ? 0 : projVars[(int)MainWindow.PROJVARS.LSTATE];
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                        break;

                    case MainWindow.ValIndex valIndex:
                        input[i] = numVals[valIndex.index];
                        break;

                    case ReadString.OPERATORS op:
                        double num1, num2;

                        switch (op)
                        {
                            //1 value operators
                            case OPERATORS.SQRT:
                                num1 = GetVal(ref input, i -= 1);
                                input[i] = Math.Sqrt(num1);
                                break;
                            case OPERATORS.SIGN:
                                num1 = GetVal(ref input, i -= 1);
                                input[i] = (double)Math.Sign(num1);
                                break;
                            case OPERATORS.SIN:
                                num1 = GetVal(ref input, i -= 1);
                                input[i] = Math.Sin(num1 * Math.PI / 180);
                                break;
                            case OPERATORS.COS:
                                num1 = GetVal(ref input, i -= 1);
                                input[i] = Math.Cos(num1 * Math.PI / 180);
                                break;
                            case OPERATORS.TAN:
                                num1 = GetVal(ref input, i -= 1);
                                input[i] = Math.Tan(num1 * Math.PI / 180);
                                break;
                            case OPERATORS.ASIN:
                                num1 = GetVal(ref input, i -= 1);
                                input[i] = Math.Asin(num1) / Math.PI * 180;
                                break;
                            case OPERATORS.ACOS:
                                num1 = GetVal(ref input, i -= 1);
                                input[i] = Math.Acos(num1) / Math.PI * 180;
                                break;
                            case OPERATORS.ABS:
                                num1 = GetVal(ref input, i -= 1);
                                input[i] = Math.Abs(num1);
                                break;
                            case OPERATORS.FLR:
                                num1 = GetVal(ref input, i -= 1);
                                input[i] = Math.Floor(num1);
                                break;
                            case OPERATORS.CEIL:
                                num1 = GetVal(ref input, i -= 1);
                                input[i] = Math.Ceiling(num1);
                                break;
                            case OPERATORS.ROUND:
                                num1 = GetVal(ref input, i -= 1);
                                input[i] = Math.Round(num1);
                                break;
                            //2 value operators
                            case OPERATORS.EQUAL:
                                (num1, num2) = Get2Vals(ref input, i -= 2);
                                input[i] = num1 == num2 ? 1 : 0;
                                break;
                            case OPERATORS.NOTEQUAL:
                                (num1, num2) = Get2Vals(ref input, i -= 2);
                                input[i] = num1 != num2 ? 1 : 0;
                                break;
                            case OPERATORS.GREATER:
                                (num1, num2) = Get2Vals(ref input, i -= 2);
                                input[i] = num1 > num2 ? 1 : 0;
                                break;
                            case OPERATORS.GREATEREQUAL:
                                (num1, num2) = Get2Vals(ref input, i -= 2);
                                input[i] = num1 >= num2 ? 1 : 0;
                                break;
                            case OPERATORS.LESS:
                                (num1, num2) = Get2Vals(ref input, i -= 2);
                                input[i] = num1 < num2 ? 1 : 0;
                                break;
                            case OPERATORS.LESSEQUAL:
                                (num1, num2) = Get2Vals(ref input, i -= 2);
                                input[i] = num1 <= num2 ? 1 : 0;
                                break;
                            case OPERATORS.ADD:
                                (num1, num2) = Get2Vals(ref input, i -= 2);
                                input[i] = num1 + num2;
                                break;
                            case OPERATORS.SUBTRACT:
                                (num1, num2) = Get2Vals(ref input, i -= 2);
                                input[i] = num1 - num2;
                                break;
                            case OPERATORS.MULTIPLY:
                                (num1, num2) = Get2Vals(ref input, i -= 2);
                                input[i] = num1 * num2;
                                break;
                            case OPERATORS.DIVIDE:
                                (num1, num2) = Get2Vals(ref input, i -= 2);
                                input[i] = num1 / num2;
                                break;
                            case OPERATORS.POW:
                                (num1, num2) = Get2Vals(ref input, i -= 2);
                                input[i] = Math.Pow(num1, num2);
                                break;
                            case OPERATORS.MOD:
                                (num1, num2) = Get2Vals(ref input, i -= 2);
                                input[i] = num1 % num2;
                                break;
                            case OPERATORS.MIN:
                                (num1, num2) = Get2Vals(ref input, i -= 2);
                                input[i] = Math.Min(num1, num2);
                                break;
                            case OPERATORS.MAX:
                                (num1, num2) = Get2Vals(ref input, i -= 2);
                                input[i] = Math.Max(num1, num2);
                                break;
                            case OPERATORS.RNG:
                                (num1, num2) = Get2Vals(ref input, i -= 2);
                                input[i] = num1 + rng.NextDouble() * (num2 - num1);
                                break;
                            case OPERATORS.ATAN:
                                (num1, num2) = Get2Vals(ref input, i -= 2);
                                input[i] = Math.Atan2(num1, num2) / Math.PI * 180;
                                break;
                        }
                        break;
                }
            }
            if (input.Count < 1)
                return 0;
            else if (input[0] is double) //double
                return (double)input[0];
            else
                MainWindow.MessageIssue(MainWindow.spText[line], line, "Equation resulted in a value of " + input[0].GetType() + "\n Expecting a double");
            return 0;
        }

        private static (double, double) Get2Vals(ref List<object> input, int start)
        {
            return (GetVal(ref input, start), GetVal(ref input, start));
        }
        private static double GetVal(ref List<object> input, int index)
        {
            //gets a number of values from the input list
            double output = 0;

            if (index < 0)
                MainWindow.MessageIssue(MainWindow.spText[line], line, "Not enough operands for operators");

            if (index < 0)  //out of range
                output = 0;
            else
            {
                if (input[index] is double) //double
                    output = (double)input[index];
                else
                    MainWindow.MessageIssue(MainWindow.spText[line], line, "Cannot perform math on an operator");
                input.RemoveAt(index);
            }

            return output;
        }
    }
}
