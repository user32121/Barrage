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
        static readonly Random rng = new Random();

        public static object Interpret(string input, Type Treturn, int t, double[] lastVals)
        {
            input = input.Replace("t", t.ToString()).
                Replace("plyrx", MainWindow.plyrX.ToString()).
                Replace("plyry", MainWindow.plyrY.ToString()).
                Replace("lxPOS", lastVals[(int)Projectile.LVI.x].ToString()).
                Replace("lyPOS", lastVals[(int)Projectile.LVI.y].ToString()).
                Replace("lxVEL", lastVals[(int)Projectile.LVI.xVel].ToString()).
                Replace("lyVEL", lastVals[(int)Projectile.LVI.yVel].ToString()).
                Replace("lSPD", lastVals[(int)Projectile.LVI.spd].ToString()).
                Replace("lANG", lastVals[(int)Projectile.LVI.ang].ToString());

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

        private static double Solve(string input)
        {
            //separates function into individual parts
            List<string> equation = new List<string> { input[0].ToString() };
            int i;
            for (i = 1; i < input.Length; i++)
            {
                //if (prev char IsDigit)                                and (this char IsDigit                         or letter E)         or (prev char E        and (this char IsDigit      or +               or -))               or (this char IsDIgit     and notlow and prev prev char E)
                if (((char.IsDigit(input[i - 1]) || input[i - 1] == '.') && (char.IsDigit(input[i]) || input[i] == '.' || input[i] == 'E')) || (input[i - 1] == 'E' && (char.IsDigit(input[i]) || input[i] == '+' || input[i] == '-')) || (char.IsDigit(input[i]) && i >= 2 && input[i - 2] == 'E'))
                {
                    //connect
                    equation[equation.Count - 1] += input[i];
                }
                //if(a series of letters)
                else if (char.IsLetter(input[i]) && char.IsLetter(input[i - 1]))
                {
                    //connect
                    equation[equation.Count - 1] += input[i];
                }
                //if(cannot combine into a number)
                else
                {
                    //separate
                    equation.Add(input[i].ToString());
                }
            }

            //calculation (negation)
            for (i = equation.Count - 1; i >= 0; i--)
                if (equation[i] == "-" && (i == 0 || !char.IsDigit(equation[i - 1][0])))
                {
                    equation.RemoveAt(i);
                    equation[i] = equation[i].Insert(0, "-");
                }

            //parentheses
            int p1, p2 = 0, c1, c2 = 0;
            bool oneMore = true;    //loops through one more time for base calculation
            do
            {
                //next parentheses iteration
                p1 = equation.LastIndexOf("("); //right-most opening parentheses
                if (p1 != -1)
                    p2 = equation.IndexOf(")", p1); //its matching closing parentheses

                if (p1 == -1)
                {
                    p1 = 0;
                    p2 = equation.Count - 1;
                    oneMore = false;
                }

                //mathematic functions
                c1 = equation.IndexOf(":", p1, p2 - p1);
                if (c1 != -1)
                    c2 = equation.IndexOf(":", c1 + 1, p2 - c1);
                while (c1 != -1)
                {
                    //ABS (absolute value)
                    if (equation[c1 - 1] == "ABS")
                    {
                        equation[c1 - 1] = Math.Abs(double.Parse(equation[c1 + 1])).ToString();

                        equation.RemoveRange(c1, 2);
                        p2 -= 2;
                    }
                    //FLR (floor)
                    else if (equation[c1 - 1] == "FLR")
                    {
                        equation[c1 - 1] = Math.Floor(double.Parse(equation[c1 + 1])).ToString();

                        equation.RemoveRange(c1, 2);
                        p2 -= 2;
                    }
                    //MOD (modulo)
                    else if (equation[c1 - 1] == "MOD")
                    {
                        equation[c1 - 1] = (double.Parse(equation[c1 + 1]) % double.Parse(equation[c2 + 1])).ToString();

                        equation.RemoveRange(c1, 4);
                        p2 -= 4;
                    }
                    //POW (power or exponent)
                    else if (equation[c1 - 1] == "POW")
                    {
                        equation[c1 - 1] = Math.Pow(double.Parse(equation[c1 + 1]), double.Parse(equation[c2 + 1])).ToString();

                        equation.RemoveRange(c1, 4);
                        p2 -= 4;
                    }
                    //RNG (random number generator)
                    else if (equation[c1 - 1] == "RNG")
                    {
                        //                 random           * (b                              - a                            )  + a
                        equation[c1 - 1] = (rng.NextDouble() * (double.Parse(equation[c2 + 1]) - double.Parse(equation[c1 + 1])) + double.Parse(equation[c1 + 1])).ToString();

                        equation.RemoveRange(c1, 4);
                        p2 -= 4;
                    }
                    //SIGN (sign)
                    else if (equation[c1 - 1] == "SIGN")
                    {
                        equation[c1 - 1] = Math.Sign(double.Parse(equation[c1 + 1])).ToString();

                        equation.RemoveRange(c1, 2);
                        p2 -= 2;
                    }
                    //SQRT (square root)
                    else if (equation[c1 - 1] == "SQRT")
                    {
                        equation[c1 - 1] = Math.Sqrt(double.Parse(equation[c1 + 1])).ToString();

                        equation.RemoveRange(c1, 2);
                        p2 -= 2;
                    }
                    //trigonometric functions
                    //SIN (sine)
                    else if (equation[c1 - 1] == "SIN")
                    {
                        //                 sin      number                         * pi      / 180 (change to radians)
                        equation[c1 - 1] = Math.Sin(double.Parse(equation[c1 + 1]) * Math.PI / 180).ToString();

                        equation.RemoveRange(c1, 2);
                        p2 -= 2;
                    }
                    //COS (cosine)
                    else if (equation[c1 - 1] == "COS")
                    {
                        equation[c1 - 1] = Math.Cos(double.Parse(equation[c1 + 1]) * Math.PI / 180).ToString();

                        equation.RemoveRange(c1, 2);
                        p2 -= 2;
                    }
                    //TAN (tangent)
                    else if (equation[c1 - 1] == "TAN")
                    {
                        equation[c1 - 1] = Math.Tan(double.Parse(equation[c1 + 1]) * Math.PI / 180).ToString();

                        equation.RemoveRange(c1, 2);
                        p2 -= 2;
                    }
                    //ASIN (tangent)
                    else if (equation[c1 - 1] == "ASIN")
                    {
                        equation[c1 - 1] = Math.Asin(double.Parse(equation[c1 + 1]) * Math.PI / 180).ToString();

                        equation.RemoveRange(c1, 2);
                        p2 -= 2;
                    }
                    //ACOS (tangent)
                    else if (equation[c1 - 1] == "ACOS")
                    {
                        equation[c1 - 1] = Math.Acos(double.Parse(equation[c1 + 1]) * Math.PI / 180).ToString();

                        equation.RemoveRange(c1, 2);
                        p2 -= 2;
                    }
                    //ATAN (inverse tangent)
                    else if (equation[c1 - 1] == "ATAN")
                    {
                        if (c2 == -1)
                        {
                            equation[c1 - 1] = (Math.Atan(double.Parse(equation[c1 + 1])) / Math.PI * 180).ToString();
                            equation.RemoveRange(c1, 2);
                            p2 -= 2;
                        }
                        else
                        {
                            equation[c1 - 1] = (Math.Atan2(double.Parse(equation[c1 + 1]), double.Parse(equation[c2 + 1])) / Math.PI * 180).ToString();
                            equation.RemoveRange(c1, 4);
                            p2 -= 4;
                        }
                    }
                    else
                    {
                        //not implemented or empty colon
                        equation.RemoveAt(c1);
                    }

                    c1 = equation.IndexOf(":", p1, p2 - p1);
                    if (c1 != -1)
                        c2 = equation.IndexOf(":", c1 + 1, p2 - c1);
                }

                //arithmetic
                i = equation.IndexOf("/", p1, p2 - p1);
                while (i != -1)
                {
                    equation[i - 1] = (double.Parse(equation[i - 1]) / double.Parse(equation[i + 1])).ToString();//sets value
                    equation.RemoveRange(i, 2);//removes extra charaters
                    p2 -= 2;
                    i = equation.IndexOf("/", p1, p2 - p1);//next division symbol
                }
                i = equation.IndexOf("*", p1, p2 - p1);
                while (i != -1)
                {
                    equation[i - 1] = (double.Parse(equation[i - 1]) * double.Parse(equation[i + 1])).ToString();
                    equation.RemoveRange(i, 2);
                    p2 -= 2;
                    i = equation.IndexOf("*", p1, p2 - p1);
                }
                i = equation.IndexOf("-", p1, p2 - p1);
                while (i != -1)
                {
                    equation[i - 1] = (double.Parse(equation[i - 1]) - double.Parse(equation[i + 1])).ToString();
                    equation.RemoveRange(i, 2);
                    p2 -= 2;
                    i = equation.IndexOf("-", p1, p2 - p1);
                }
                i = equation.IndexOf("+", p1, p2 - p1);
                while (i != -1)
                {
                    equation[i - 1] = (double.Parse(equation[i - 1]) + double.Parse(equation[i + 1])).ToString();
                    equation.RemoveRange(i, 2);
                    p2 -= 2;
                    i = equation.IndexOf("+", p1, p2 - p1);
                }

                //removes parentheses
                if (oneMore)
                {
                    equation.RemoveAt(p1);
                    equation.RemoveAt(p2 - 1);
                }
            }
            while (p1 != -1 && oneMore);

            return double.Parse(equation[0]);
        }

        public static string AddVals(string input, int n, List<double> vals)
        {
            //replaces n
            input = input.Replace("n", n.ToString());

            //replaces vals
            int i = input.IndexOf("val");
            while (i != -1)
            {
                //finds the val's index
                int j = 1;
                while (i + 3 + j < input.Length && char.IsDigit(input[i + 3 + j]))
                    j++;

                //substitutes the value
                int.TryParse(input.Substring(i + 3, j), out int valId);
                if (valId < vals.Count)
                    input = input.Replace(input.Substring(i, j + 3), vals[valId].ToString());
                else
                    input = input.Replace(input.Substring(i, j + 3), "0");

                //next val
                i = input.IndexOf("val");
            }

            return input;
        }
    }
}
