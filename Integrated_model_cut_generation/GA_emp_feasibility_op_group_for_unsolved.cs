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
    class GA_emp_feasibility_op_group_for_unsolved
    {
        public static int[][] count_t_op_VI = new int[1][];
        public static int[] each_operation_count = new int[1];

        public static void model()
        {
            //tabu_upgrade_job.job_shop_result;

            //int shift_amount = (int)(Math.Ceiling(tabu_upgrade_job.best_cmax / 8));// job排完的best cmax

            int job_amount = read_data.job_order_amount.Count;
            int machine_amount = read_data.machine_information.Count;
            int employee_amount = read_data.employee_competence_operation.Count;
            int day_amount = 3;  //a week shift=7x3=21           
            int shift_unit_hour = 8;

            int M = 168;

            each_operation_count = new int[read_data.operation_time.Count];

            for (int i = 0; i < job_amount; i++)
            {
                for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
                {
                    each_operation_count[processing_time_find_index(i, j, read_data.job_operation_list)]++;
                }
            }

            //for(int i=0; i<each_operation_count.Length; i++)
            //{
            //    Console.WriteLine(each_operation_count[i]);
            //}

            //Console.WriteLine("enter");

            GC.Collect();
            //Console.Read();

            StreamReader total_iter = new StreamReader("total_iter.csv");
            int total_iteration = int.Parse(total_iter.ReadLine());
            total_iter.Close();

            count_t_op_VI = new int[total_iteration][]; /// CHANGE
            //count_t_op_VI = new int[GA_upgrade2.GA2_T_result.Count][]; // ***

            for (int i = 0; i < count_t_op_VI.Length; i++) 
            {
                count_t_op_VI[i] = new int[read_data.operation_time.Count];
            }
            GC.Collect();

            Console.WriteLine("Second stage # iter: " + count_t_op_VI.Length);

            StreamWriter cut3 = new StreamWriter("cut3.csv");
            StreamReader after_preprocessing_tijs = new StreamReader("after_preprocessing_tijs.csv"); ///modified 0329

            for (int iter = 0; iter < total_iteration; iter++) /// modified0330 /// CHANGE
            {
                string[] input = after_preprocessing_tijs.ReadLine().Split(',');
                int shift_amount = shift_cal(input);
                //Console.WriteLine("shift " + shift_amount);

                if (iter >= 0) 
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

                    for (int op = 0; op < read_data.operation_time.Count; op++)
                    {

                        Cplex model = new Cplex();

                        //int shift_amount = (int)GA.GA_opt_cmax_shift[iter];
                        //int shift_amount = (int)GA_upgrade2.GA2_opt_cmax_shift[iter]; // ***
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
                                e_variable[e][i] = new INumVar[read_data.job_operation_list[i].Count][];
                                for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
                                {
                                    e_variable[e][i][j] = model.NumVarArray(shift_amount, 0, int.MaxValue, NumVarType.Bool);
                                }
                            }
                        }

                        for (int e = 0; e < employee_amount; e++)
                        {
                            z_variable[e] = model.NumVarArray(shift_amount, 0, int.MaxValue, NumVarType.Bool);
                        }

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
                                        if (processing_time_find_index(i, j, read_data.job_operation_list) == op)
                                        {
                                            obj.AddTerm(1, e_variable[e][i][j][s]);
                                        }
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
                                    if (processing_time_find_index(i, j, read_data.job_operation_list) == op)
                                    {
                                        ILinearNumExpr constraint2_1 = model.LinearNumExpr();
                                        for (int e = 0; e < employee_amount; e++)
                                        {
                                            constraint2_1.AddTerm(1, e_variable[e][i][j][s]);
                                        }
                                        model.AddLe(constraint2_1, transfer[i][j][s]); // ***
                                    }

                                }
                            }
                        }

                        //Constraint2-2
                        // Console.WriteLine("Constraint2-2");
                        List<List<int>> remove_employee_competence = employee_operation_method(read_data.employee_competence_operation); //已轉換為index形式
                        for (int e = 0; e < employee_amount; e++)
                        {
                            for (int i = 0; i < job_amount; i++)
                            {
                                for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
                                {
                                    if (processing_time_find_index(i, j, read_data.job_operation_list) == op)
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
                        //Console.WriteLine("\rSecond stage iter: " + iter);
                        TextWriter tw = System.IO.File.CreateText("cplex_tmp.log");
                        model.SetOut(tw);
                        model.SetWarning(tw);
                        model.Solve();
                        tw.Close();
                        GC.Collect();

                        count_t_op_VI[iter][op] = (int)model.GetObjValue();
                        cut3.Write((int)model.GetObjValue() + ",");
                        GC.Collect();
                    }
                    GC.Collect();
                    cut3.WriteLine();

                }
               

            }

            cut3.Close();
            GC.Collect();

            //for(int iter = 0; iter < GA.GA_T_result.Count; iter++)
            //{
            //    Console.WriteLine("-------- iter -------- " + iter);

            //    for (int op = 0; op < read_data.operation_time.Count; op++)
            //    {
            //        Console.WriteLine("op: " + op + " :" + count_t_op_VI[iter][op]);

            //    }
            //}

            // Console.Read();
            GC.Collect();
            after_preprocessing_tijs.Close();
            Console.WriteLine("Stage 2 end");

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
        public static int processing_time_find_index(int job, int operation, List<List<string>> job_operation_list)
        {
            int processitng_time_index = 0;
            switch (job_operation_list[job][operation])
            {
                case "A":
                    processitng_time_index = 0;
                    break;
                case "B":
                    processitng_time_index = 1;
                    break;
                case "C":
                    processitng_time_index = 2;
                    break;
                case "D":
                    processitng_time_index = 3;
                    break;
                case "E":
                    processitng_time_index = 4;
                    break;
            }
            return processitng_time_index;
        }
    }
}
