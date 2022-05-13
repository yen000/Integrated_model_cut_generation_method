using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ILOG.Concert;
using ILOG.CPLEX;

namespace Integrated_model_cut_generation
{
    class GA_emp_feasibility_for_unsolved
    {
        public static double[][][][] GA_e_result = new double[1][][][];
        public static double[][] GA_z_result = new double[1][];
        public static List<int> check_feasible = new List<int>(); //0:feasible //1:infeasible
        public static List<double[][][][]> record_e_result = new List<double[][][][]>(); //
        public static bool[][] t_i_j = new bool[1][]; //
        public static List<int> Z_bound = new List<int>();
        public static int total_operation_count;


        public static int tijs_LB;

        public static int[] count_t_VI = new int[1];
        public static void model(List<List<List<List<int>>>> T_result, List<double> opt_cmax_shift, bool check_local)
        {
            //int shift_amount = (int)(Math.Ceiling(tabu_upgrade_job.best_cmax / 8));// job排完的best cmax

            int job_amount = read_data.job_order_amount.Count;
            int machine_amount = read_data.machine_information.Count;

            int employee_amount = read_data.employee_competence_operation.Count;

            int day_amount = 7;  //a week shift=7x3=21
            //shift_amount = day_amount * 1;
            //int shift_amount = 16;
            int shift_unit_hour = 8;

            int M = 168;
            GC.Collect();
            total_operation_count = 0;
            for (int i = 0; i < job_amount; i++)
            {
                for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
                {
                    total_operation_count++;
                }
            }
            GC.Collect();
            //t_i_j = new bool[job_amount][];

            StreamReader total_iter = new StreamReader("total_iter.csv");
            int total_iteration = int.Parse(total_iter.ReadLine());
            total_iter.Close();

            count_t_VI = new int[total_iteration];
            Console.WriteLine("First stage # iter" + total_iteration);

            StreamWriter cut2 = new StreamWriter("cut2.csv");
            StreamWriter cut5 = new StreamWriter("cut5.csv");
            StreamReader after_preprocessing_tijs = new StreamReader("after_preprocessing_tijs.csv"); ///modified 0329

            for (int iter = 0; iter < total_iteration; iter++) /// modified0330
            {
                GC.Collect();
                string[] input = after_preprocessing_tijs.ReadLine().Split(',');
                //Console.WriteLine(iter);
                int shift_amount = shift_cal(input);

                if (iter >= 2100)
                {
                    int[][][] transfer = new int[job_amount][][];
                    for (int i = 0; i < job_amount; i++)
                    {
                        transfer[i] = new int[read_data.job_operation_list[i].Count][];
                        for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
                        {
                            transfer[i][j] = new int[shift_amount];
                            for (int s = 0; s < shift_amount; s++)
                            {
                                transfer[i][j][s] = 0;
                                for (int r = 0; r < input.Length - 1; r += 3)
                                {
                                    if ((int.Parse(input[r]) == i) && (int.Parse(input[r + 1]) == j) && (int.Parse(input[r + 2]) == s))
                                    {
                                        transfer[i][j][s] = 1;
                                        break;
                                    }
                                }
                            }
                        }
                    }


                    Cplex model = new Cplex();

                    //int shift_amount = (int)opt_cmax_shift[iter];

                    GC.Collect();

                    #region declare_variable
                    // Declare variable

                    INumVar[][][][] e_variable = new INumVar[employee_amount][][][];
                    INumVar[][] z_variable = new INumVar[employee_amount][];

                    for (int e = 0; e < employee_amount; e++)
                    {
                        e_variable[e] = new INumVar[job_amount][][];
                        for (int i = 0; i < job_amount; i++)
                        {
                            //t_i_j[i] = new bool[read_data.job_operation_list[i].Count];
                            e_variable[e][i] = new INumVar[read_data.job_operation_list[i].Count][];
                            for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
                            {
                                //t_i_j[i][j] = true;
                                e_variable[e][i][j] = model.NumVarArray(shift_amount, 0, int.MaxValue, NumVarType.Bool);

                            }
                        }
                    }

                    for (int e = 0; e < employee_amount; e++)
                    {
                        z_variable[e] = model.NumVarArray(shift_amount, 0, int.MaxValue, NumVarType.Bool);
                    }

                    //Cmax = model.NumVar(0, float.MaxValue);
                    #endregion

                    #region obj
                    ILinearNumExpr obj = model.LinearNumExpr();
                    for (int e = 0; e < employee_amount; e++)
                    {
                        for (int i = 0; i < job_amount; i++)
                        {
                            for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
                            {
                                for (int s = 0; s < shift_amount; s++)
                                {
                                    obj.AddTerm(1, e_variable[e][i][j][s]);
                                }
                            }
                        }
                    }

                    model.AddMaximize(obj);

                    #endregion

                    #region employee scheduling
                    //Constraint2-1
                    //Console.WriteLine("Constraint2-1");
                    for (int i = 0; i < job_amount; i++)
                    {
                        for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
                        {
                            for (int s = 0; s < shift_amount; s++)
                            {
                                ILinearNumExpr constraint2_1 = model.LinearNumExpr();
                                //constraint2_1.AddTerm(-1, t_variable[i][j][s]);
                                for (int e = 0; e < employee_amount; e++)
                                {
                                    constraint2_1.AddTerm(1, e_variable[e][i][j][s]);
                                    //Console.Write("E" + e + i + j + s + "  ");
                                }
                                // Console.WriteLine("T" + i + j + s);
                                model.AddLe(constraint2_1, transfer[i][j][s]);
                                //if (T_result[iter][i][j][s] == 1)
                                //{
                                //    Console.Write(i + "" + j + "" + s);
                                //}
                            }
                        }
                    }


                    //Constraint2-2
                    //Console.WriteLine("Constraint2-2");
                    List<List<int>> remove_employee_competence = employee_operation_method(read_data.employee_competence_operation); //已轉換為index形式
                    for (int e = 0; e < employee_amount; e++)
                    {
                        for (int i = 0; i < job_amount; i++)
                        {
                            for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
                            {
                                bool check = false;
                                if (read_data.employee_competence_operation[e].Contains(read_data.job_operation_list[i][j]))
                                {
                                    check = true;
                                }
                                if (check == false)
                                {
                                    //Console.WriteLine("E e " + e + " " + i + " " + j);
                                    for (int s = 0; s < shift_amount; s++)
                                    {
                                        ILinearNumExpr constraint2_2 = model.LinearNumExpr();
                                        constraint2_2.AddTerm(1, e_variable[e][i][j][s]);
                                        model.AddEq(constraint2_2, 0);
                                    }
                                }
                            }
                        }
                    }

                    //Constraint2-3
                    //Console.WriteLine("Constraint2-3");
                    for (int e = 0; e < employee_amount; e++)
                    {
                        for (int s = 0; s < shift_amount; s++)
                        {
                            ILinearNumExpr constraint2_3 = model.LinearNumExpr();
                            constraint2_3.AddTerm(-M, z_variable[e][s]);
                            for (int i = 0; i < job_amount; i++)
                            {
                                for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
                                {
                                    constraint2_3.AddTerm(1, e_variable[e][i][j][s]);
                                }
                            }
                            model.AddLe(constraint2_3, 0);

                        }
                    }

                    //Constraint2-4
                    //Console.WriteLine("Constraint2-4");
                    for (int e = 0; e < employee_amount; e++)
                    {
                        for (int s = 0; s < shift_amount - 1; s++)
                        {
                            ILinearNumExpr constraint2_4 = model.LinearNumExpr();
                            constraint2_4.AddTerm(1, z_variable[e][s]);
                            constraint2_4.AddTerm(1, z_variable[e][s + 1]);
                            model.AddLe(constraint2_4, 1);
                        }
                    }

                    //Constraint2-5
                    //Console.WriteLine("Constraint2-7");
                    for (int e = 0; e < employee_amount; e++)
                    {
                        for (int s = 0; s < shift_amount; s++)
                        {
                            ILinearNumExpr constraint2_5 = model.LinearNumExpr();
                            constraint2_5.AddTerm(-1, z_variable[e][s]);
                            for (int i = 0; i < job_amount; i++)
                            {
                                for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
                                {
                                    constraint2_5.AddTerm(1, e_variable[e][i][j][s]);
                                }
                            }
                            model.AddGe(constraint2_5, 0);
                        }
                    }

                    //Constraint2-6
                    //Console.WriteLine("Constraint2-6");
                    for (int e = 0; e < employee_amount; e++)
                    {
                        ILinearNumExpr constraint2_6 = model.LinearNumExpr();
                        for (int s = 0; s < shift_amount; s++)
                        {
                            constraint2_6.AddTerm(shift_unit_hour, z_variable[e][s]);
                        }
                        model.AddLe(constraint2_6, 5 * shift_unit_hour);

                    }


                    //Constraint2-7
                    //Console.WriteLine("Constraint2-7");
                    for (int e = 0; e < employee_amount; e++)
                    {
                        for (int s = 0; s < shift_amount - 2; s += 3)
                        {
                            ILinearNumExpr constraint2_7 = model.LinearNumExpr();
                            constraint2_7.AddTerm(1, z_variable[e][s]);
                            constraint2_7.AddTerm(1, z_variable[e][s + 1]);
                            constraint2_7.AddTerm(1, z_variable[e][s + 2]);
                            model.AddLe(constraint2_7, 1);
                        }
                    }

                    #endregion
                    GC.Collect();
                    TextWriter tw = System.IO.File.CreateText("cplex_tmp.log");
                    model.SetOut(tw);
                    model.SetWarning(tw);
                    model.Solve();
                    tw.Close();

                    #region variable_result
                    //E result
                    GA_e_result = new double[employee_amount][][][];
                    for (int e = 0; e < employee_amount; e++)
                    {
                        GA_e_result[e] = new double[job_amount][][];
                        for (int i = 0; i < job_amount; i++)
                        {
                            GA_e_result[e][i] = new double[read_data.job_operation_list[i].Count][];
                            for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
                            {
                                GA_e_result[e][i][j] = model.GetValues(e_variable[e][i][j]);
                            }
                        }
                    }

                    record_e_result.Add(GA_e_result);

                    //Z result
                    GA_z_result = new double[employee_amount][];
                    for (int e = 0; e < employee_amount; e++)
                    {
                        GA_z_result[e] = model.GetValues(z_variable[e]);
                    }

                    List<List<int>> verify = new List<List<int>>();
                    for(int s=0; s<shift_amount; s++)
                    {
                        verify.Add(new List<int>());
                    }
                    int cal_Zes = 0;
                    for (int e = 0; e < employee_amount; e++)
                    {
                        for (int s = 0; s < shift_amount; s++)
                        {
                            if (Math.Round(GA_z_result[e][s]) == 1)
                            {
                                verify[s].Add(e);
                                //Console.WriteLine("e s " + e + " " + s);
                                cal_Zes++;
                            }
                        }
                    }
                    Z_bound.Add(cal_Zes);


                    for (int e = 0; e < employee_amount; e++)
                    {
                        for (int s = 0; s < shift_amount; s++)
                        {
                            double left = 0;
                            for (int i = 0; i < read_data.job_order_amount.Count; i++)
                            {
                                for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
                                {
                                    left += Math.Round(GA_e_result[e][i][j][s]);
                                }
                            }
                            double right = M * Math.Round(GA_z_result[e][s]);
                            if (left > right) Console.WriteLine("gggggggggg");
                        }

                    }

                    //for(int i=0; i< verify.Count; i++)
                    //{
                    //    Console.WriteLine("shift " + i);
                    //    for(int j=0; j<verify[i].Count; j++)
                    //    {
                    //        Console.Write(verify[i][j]);
                    //    }
                    //    Console.WriteLine();
                    //}

                    //Console.Read();

                    GC.Collect();
                    /*for (int e = 0; e < employee_amount; e++)
                    {
                        for (int i = 0; i < job_amount; i++)
                        {
                            for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
                            {
                                for (int s = 0; s < shift_amount; s++)
                                {
                                    Console.WriteLine("e i j s " + e + " " + i + " " + j + " " + s + " : " + tabu_e_result[e][i][j][s]);
                                }
                            }
                        }
                    }

                    for (int e = 0; e < employee_amount; e++)
                    {
                        for (int s = 0; s < shift_amount; s++)
                        {
                            if (tabu_z_result[e][s] == 1) Console.WriteLine("e s " + e + " " + s + " : " + tabu_z_result[e][s]);
                        }
                    }*/

                    #endregion

                    GC.Collect();
                    count_t_VI[iter] = (int)model.GetObjValue();
                    cut2.WriteLine((int)model.GetObjValue());
                }

                

            }
            cut2.Close();
            //collect_tijs.Close();
            GC.Collect();
           
            after_preprocessing_tijs.Close();

            GC.Collect();
            Console.WriteLine("Stage 1 end");

            int upperbound = employee_amount * day_amount * 3;
            int lowerbound = 1;
            Console.WriteLine("!! total_op " + total_operation_count);

            StreamWriter z_result = new StreamWriter("z_result.csv");
            for (int iter = 0; iter < Z_bound.Count; iter++)
            {
                
                if ((count_t_VI[iter] == total_operation_count)) //feasible sol.
                {
                    //Console.WriteLine("ENTER");
                    z_result.WriteLine(count_t_VI[iter] + "," + Z_bound[iter] + "," + "true");
;                   if (Z_bound[iter] < upperbound) upperbound = Z_bound[iter];
                }
                else //infeasible sol.
                {
                    //Console.WriteLine("ENTER2");
                    if (Z_bound[iter] > lowerbound) lowerbound = Z_bound[iter];
                    z_result.WriteLine(count_t_VI[iter] + "," + Z_bound[iter] + "," + "false");
                }
            }
            z_result.Close();
            //var minValue = Z_upper_bound.Min();
            cut5.WriteLine(upperbound);
            cut5.WriteLine(lowerbound);
            cut5.Close();

            //Console.WriteLine("Zes");
            //for(int i=0; i<Z_upper_bound.Count; i++)
            //{
            //    Console.WriteLine(Z_upper_bound[i]);
            //}


            //Console.Read();

        }
        public static int shift_cal(string[] data)
        {
            int shift_amount = 0;
            for (int i = 2; i < data.Length - 1; i += 3)
            {
                if (int.Parse(data[i]) > shift_amount) shift_amount = int.Parse(data[i]);
            }

            return (shift_amount + 1);
        }
        public static List<int> find_competence_emp(int job, int operation_step, List<List<string>> job_operation_list, List<List<string>> employee_competence_operation)
        {
            string operation = "";

            List<int> find_competence_emp = new List<int>();
            switch (job_operation_list[job][operation_step])
            {
                case "A":
                    operation = "A";
                    break;
                case "B":
                    operation = "B";
                    break;
                case "C":
                    operation = "C";
                    break;
                case "D":
                    operation = "D";
                    break;
                case "E":
                    operation = "E";
                    break;
            }

            for (int i = 0; i < employee_competence_operation.Count; i++)
            {
                for (int j = 0; j < employee_competence_operation[i].Count; j++)
                {
                    if (employee_competence_operation[i][j] == operation)
                    {
                        find_competence_emp.Add(i);
                        break;
                    }
                }
            }


            return find_competence_emp;

        }
        public static List<List<int>> employee_operation_method(List<List<string>> employee_competence_operation)
        {
            List<List<int>> remove_employee_competence = new List<List<int>>();
            List<string> operation_list = new List<string>() { "A", "B", "C", "D", "E" };

            for (int e = 0; e < employee_competence_operation.Count; e++)
            {
                List<int> tmp = new List<int>();
                for (int i = 0; i < operation_list.Count; i++)
                {
                    if (!(employee_competence_operation[e].Contains(operation_list[i])))
                    {
                        tmp.Add(i);
                    }
                }
                remove_employee_competence.Add(tmp);
            }

            return remove_employee_competence;
        }
    }
}
