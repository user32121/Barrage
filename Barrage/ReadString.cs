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
        static Random rng = new Random();

        public static object Interpret(string input, Type Treturn, int t)
        {
            input = input.Replace("t", t.ToString());
            input = input.Replace("plyrx", MainWindow.plyrX.ToString());
            input = input.Replace("plyry", MainWindow.plyrY.ToString());

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
                //if this character is a number and prev char is also number, connect them
                if ((char.IsDigit(input[i]) || input[i] == '.') && (char.IsDigit(equation[equation.Count - 1][0]) || input[i] == '.'))
                {
                    equation[equation.Count - 1] += input[i];
                }
                //if(a series of letters)
                else if (char.IsLetter(input[i]) && char.IsLetter(equation[equation.Count - 1][0]))
                {
                    equation[equation.Count - 1] += input[i];
                }
                //if cannot combine into a number
                else
                {
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

                c1 = equation.IndexOf(":", p1, p2 - p1);
                if (c1 != -1)
                    c2 = equation.IndexOf(":", c1 + 1, p2 - c1);
                while (c1 != -1)
                {
                    if (equation[c1 - 1] == "RNG")
                    {
                        //RNG              random           * (b                              - a                            )  + a
                        equation[c1 - 1] = (rng.NextDouble() * (double.Parse(equation[c2 + 1]) - double.Parse(equation[c1 + 1])) + double.Parse(equation[c1 + 1])).ToString();

                        equation.RemoveRange(c1, 4);
                        p2 -= 4;
                    }
                    //math functions
                    //ABS (absolute value)
                    else if (equation[c1 - 1] == "ABS")
                    {
                        equation[c1 - 1] = Math.Abs(double.Parse(equation[c1 + 1])).ToString();

                        equation.RemoveRange(c1, 2);
                        p2 -= 2;
                    }
                    //SIN (sine)
                    else if (equation[c1 - 1] == "SIN")
                    {
                        equation[c1 - 1] = Math.Sin(double.Parse(equation[c1 + 1])).ToString();

                        equation.RemoveRange(c1, 2);
                        p2 -= 2;
                    }
                    //COS (cosine)
                    else if (equation[c1 - 1] == "COS")
                    {
                        equation[c1 - 1] = Math.Cos(double.Parse(equation[c1 + 1])).ToString();

                        equation.RemoveRange(c1, 2);
                        p2 -= 2;
                    }
                    //TAN (tangent)
                    else if (equation[c1 - 1] == "TAN")
                    {
                        equation[c1 - 1] = Math.Tan(double.Parse(equation[c1 + 1])).ToString();

                        equation.RemoveRange(c1, 2);
                        p2 -= 2;
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
