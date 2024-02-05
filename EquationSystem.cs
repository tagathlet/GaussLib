using System.Data.SqlClient;
using System.Reflection.Emit;

namespace GaussLib
{
    public class EquationSystem
    {
        private Double[,] A;
        private int n;
        private String result;
        private Double time;

        public EquationSystem(Double[,] A, int n) {
            this.A = A;
            this.n = n;
        }

        public String getResult() {
            return result;
        }

        public Double getTime()
        {
            return time;
        }

        private String SystemToText(String delim) {
            String textToWrite = "";

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n + 1; j++)
                {
                    textToWrite += A[i, j];
                    if (j == n) break;

                    if (j == n - 1)
                    {
                        textToWrite += "*x" + (j + 1) + " = ";
                    }
                    else
                    {
                        textToWrite += "*x" + (j + 1) + " + ";
                    }
                }
                textToWrite += delim;
            }
            return textToWrite;
        }

        private String SystemToText()
        {
            return SystemToText("\n");
        }

        public void writeToFile(String name) {
            String textToWrite = SystemToText();
            textToWrite += "\nРешение:\n" + result + "\n";
            textToWrite += "Время: " + time;

            System.IO.File.WriteAllText(name, textToWrite);
        }

        public void writeToDB() {
            DB dataBase = new DB();
            string querystring = $"insert into Table_2(SLAU,Solution,time) values ('{SystemToText("; ")}','{result}','{time}') ";
            SqlCommand command = new SqlCommand(querystring, dataBase.GetConnection());
            dataBase.openConnection();
            command.ExecuteNonQuery();

        }
        public static void Swap_Lines(int k1, int k2, int n, Double[,] A, Boolean[] mark)
        {
            for (int j = 0; j < n; j++)
            {
                Double tmp1;
                tmp1 = A[k1, j];
                A[k1, j] = A[k2, j];
                A[k2, j] = tmp1;
            }
            Boolean tmp;
            tmp = mark[k1];
            mark[k1] = mark[k2];
            mark[k2] = tmp;
        }

        public static double EPS = 1E-5;

        private void stop(DateTime t)
        {
            DateTime end = DateTime.Now;
            TimeSpan ts = (end - t);

            time = ts.TotalNanoseconds;
        }
        public String Solve()
        {
            int m = n;
            Double[] answer = new Double[n];
            Double[,] A = this.A.Clone() as Double[,];

            int min_size = n;
            DateTime start = DateTime.Now;
            for (int k = 0; k < min_size; k++)
            {
                double maxv = 0; int position_of_line_with_maxv = k;
                for (int i = k; i < m; i++)
                {
                    if (Math.Abs(A[i, k]) > maxv)
                    {
                        maxv = Math.Abs(A[i, k]);
                        position_of_line_with_maxv = i;
                    }
                }
                for (int j = 0; j < n + 1; j++)
                {
                    double tmp = A[k, j];
                    A[k, j] = A[position_of_line_with_maxv, j];
                    A[position_of_line_with_maxv, j] = tmp;
                }

                if (Math.Abs(maxv) < EPS)
                {
                    continue;
                }

                for (int i = 0; i < m; i++)
                {
                    if (i == k) continue;

                    double multiplier = A[i, k] / A[k, k];
                    for (int j = k; j < n + 1; j++)
                    {
                        A[i, j] -= multiplier * A[k, j];
                    }
                }
            }

            for (int k = 0; k < min_size; k++)
            {
                if (Math.Abs(A[k, k]) > EPS)
                {
                    double multiplier = A[k, k];
                    if (Math.Abs(multiplier) < EPS) continue;
                    for (int j = k; j < n + 1; j++)
                    {
                        A[k, j] /= multiplier;
                    }
                }
            }

            Boolean[] mark = new Boolean[m];
            for (int i = 0; i < m; i++)
            {
                mark[i] = false;
            }

            for (int k1 = 0; k1 < m; k1++)
            {
                if (mark[k1]) continue;
                for (int k2 = k1 + 1; k2 < m; k2++)
                {
                    Boolean is_equal = true;
                    for (int j = 0; j < n + 1; j++)
                    {
                        if (Math.Abs(A[k1, j] - A[k2, j]) > EPS)
                        {
                            is_equal = false;
                            break;
                        }
                    }
                    if (is_equal)
                    {
                        mark[k2] = true;
                    }
                }
            }
            for (int i = 0; i < m; i++)
            {
                int cnt_of_zeroes = 0;
                for (int j = 0; j < n + 1; j++)
                {
                    if (Math.Abs(A[i, j]) < EPS)
                    {
                        cnt_of_zeroes++;
                        A[i, j] = 0.0;
                    }
                }
                if (cnt_of_zeroes == n + 1)
                {
                    mark[i] = true;
                }
                if (cnt_of_zeroes == n && Math.Abs(A[i, n]) > EPS)
                {
                    result = "The system of equations is inconsistent";
                    stop(start);
                    return result;
                }
            }

            for (int i = 0; i < m; i++)
            {
                for (int j = i + 1; j < m; j++)
                {
                    if (mark[i] && !mark[j])
                    {
                        Swap_Lines(i, j, n, A, mark);
                    }
                }
            }

            int cnt_of_marks = 0;
            for (int i = 0; i < m; i++)
            {
                if (mark[i]) cnt_of_marks++;
            }
            int bottom_border = m - 1 - cnt_of_marks;

            if (bottom_border == n - 1)
            {
                for (int k = n - 1; k >= 0; k--)
                {
                    answer[k] = A[k, n] / A[k, k];
                    result += "x" + (k + 1) + " = " + answer[k] + "\n";
                }
                stop(start);
                return result;
            }

            int cnt_of_free_variables = n - (bottom_border + 1);

            Boolean[] marked_variables = new Boolean[n];
            for (int i = 0; i < n; i++)
            {
                marked_variables[i] = false;
            }

            for (int j = 0; j < n; j++)
            {
                int cnt_of_zeroes = 0;
                for (int i = 0; i < bottom_border; i++)
                {
                    if (Math.Abs(A[i, j]) < EPS)
                    {
                        cnt_of_zeroes++;
                    }
                }
                if (cnt_of_zeroes == bottom_border + 1)
                {
                    if (cnt_of_free_variables > 0)
                    {
                        marked_variables[j] = true;
                        cnt_of_free_variables--;
                    }
                }
            }
            for (int i = n - 1; i >= 0; i--)
            {
                if (cnt_of_free_variables == 0) break;
                marked_variables[i] = true;
                cnt_of_free_variables--;
            }
            result += ("Initialization of free variables:\n");
            for (int i = 0; i < n; i++)
            {
                if (marked_variables[i] == true)
                {
                    answer[i] = 1.0;
                    result += ("Let: " + (i + 1) + "-th variable assigned: 1.0\n");
                }
            }
            result += ("Answer:\n");
            for (int i = 0; i < n; i++)
            {
                if (marked_variables[i] == true)
                {
                    result += ((i + 1) + "-th variable is free\n");
                }
            }

            for (int i = bottom_border; i >= 0; i--)
            {
                double cur_sum = 0;

                int cur_variable = 0;
                for (int j = 0; j < n; j++)
                {
                    if (marked_variables[j] == false && Math.Abs(A[i, j]) > EPS)
                    {
                        cur_variable = j;
                        break;
                    }
                }

                result += ("X[" + (cur_variable + 1) + "] = ");
                for (int j = 0; j < n; j++)
                {
                    if (marked_variables[j] == true)
                    {
                        cur_sum += answer[j] * A[i, j];
                        if (A[i, j] != 0)
                            result += ("(" + -A[i, j] + "/" + A[i, cur_variable] + ")" + "*X[" + (j + 1) + "] + ");
                    }
                }
                result += "(" + A[i, n] + ")\n";

                cur_sum *= -1;
                cur_sum += A[i, n];


                for (int j = 0; j < n; j++)
                {
                    if (marked_variables[j] == false && Math.Abs(A[i, j]) > EPS)
                    {
                        answer[j] = cur_sum / A[i, j];
                        marked_variables[j] = true;
                        break;
                    }
                }
            }
            stop(start);
            return result;
        }

        internal DB DB
        {
            get => default;
            set
            {
            }
        }
    }
}
