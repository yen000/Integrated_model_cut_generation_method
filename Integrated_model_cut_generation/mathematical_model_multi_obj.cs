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
    class mathematical_model_multi_obj
    {
        //public static int M;
        public static int job_amount;
        public static int machine_amount;
        public static int employee_amount;
        public static int day_amount;  //a week shift=7x3=21
        public static int shift_amount;
        public static int shift_unit_hour;
        public static int setup_time;

        public static double[][][] x_result = new double[1][][];
        public static double[][][] s_result = new double[1][][];
        public static double[][][] c_result = new double[1][][];
        public static double[][][] t_result = new double[1][][];
        public static double[][][][][] y_result = new double[1][][][][];

        public static double[][][][] e_result = new double[1][][][];
        public static double[][] z_result = new double[1][];
        
        public static double makespan_obj_value;
        public static double labour_cost_obj_value;

        public static System.Diagnostics.Stopwatch time = new System.Diagnostics.Stopwatch();
        public static List<int> mathe_MS_string = new List<int>();
        public static List<int> mathe_ES_string = new List<int>();
        public static int[] mathe_OS_string = new int[1];

        public static List<List<List<int>>> emp_amount = new List<List<List<int>>>();

        public static void model()
        {

            Console.WriteLine("/// Cplex start ///");

            job_amount = read_data.job_order_amount.Count;
            machine_amount = read_data.machine_information.Count;
            employee_amount = read_data.employee_competence_operation.Count;
            Console.WriteLine(employee_amount);

            day_amount = 7;  //a week shift=7x3=21
            shift_amount = day_amount * 3;
            shift_unit_hour = 8;
            int M = shift_amount * shift_unit_hour;
            double alpha = 10000; //makespan 
            double beta = 1; //labour cost

            int total_op_count = 0;

            Cplex model = new Cplex();

            #region declare variable
            // Declare variable
            INumVar[][][] s_variable = new INumVar[job_amount][][];
            INumVar[][][] c_variable = new INumVar[job_amount][][];
            INumVar[][][] x_variable = new INumVar[job_amount][][];
            INumVar[][][][][] y_variable = new INumVar[job_amount][][][][];
            INumVar[][][] t_variable = new INumVar[job_amount][][];
            INumVar[][][] u_variable = new INumVar[job_amount][][];
            INumVar Cmax;

            INumVar[][][][] e_variable = new INumVar[job_amount][][][];
            INumVar[][] z_variable = new INumVar[employee_amount][];

            // S variable
            for (int i = 0; i < job_amount; i++)
            {
                s_variable[i] = new INumVar[read_data.job_operation_list[i].Count][];
                for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
                {
                    int m_amount = machine_amount_method(i, j, read_data.job_operation_list, read_data.operation_avilable_machine);
                    s_variable[i][j] = model.NumVarArray(m_amount, 0, float.MaxValue, NumVarType.Float);
                    total_op_count++;
                }
            }
            // C variable
            for (int i = 0; i < job_amount; i++)
            {
                c_variable[i] = new INumVar[read_data.job_operation_list[i].Count][];
                for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
                {
                    int m_amount = machine_amount_method(i, j, read_data.job_operation_list, read_data.operation_avilable_machine);
                    c_variable[i][j] = model.NumVarArray(m_amount, 0, float.MaxValue, NumVarType.Float);
                }
            }
            // X variable
            for (int i = 0; i < job_amount; i++)
            {
                x_variable[i] = new INumVar[read_data.job_operation_list[i].Count][];
                for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
                {
                    //x_variable[i][j] = new INumVar[job_amount];

                    int m_amount = machine_amount_method(i, j, read_data.job_operation_list, read_data.operation_avilable_machine);
                    x_variable[i][j] = model.NumVarArray(m_amount, 0, int.MaxValue, NumVarType.Bool);

                }
            }
            // Y variable
            for (int i = 0; i < job_amount; i++)
            {
                y_variable[i] = new INumVar[read_data.job_operation_list[i].Count][][][];
                for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
                {
                    y_variable[i][j] = new INumVar[job_amount][][];

                    for (int i_p = 0; i_p < job_amount; i_p++)
                    {
                        if (i < i_p)
                        {
                            y_variable[i][j][i_p] = new INumVar[read_data.job_operation_list[i_p].Count][];

                            for (int j_p = 0; j_p < read_data.job_operation_list[i_p].Count; j_p++)
                            {
                                y_variable[i][j][i_p][j_p] = model.NumVarArray(machine_amount, 0, int.MaxValue, NumVarType.Bool);
                            }

                        }

                    }
                }
            }
            // T variable
            for (int i = 0; i < job_amount; i++)
            {
                t_variable[i] = new INumVar[read_data.job_operation_list[i].Count][];
                for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
                {
                    t_variable[i][j] = model.NumVarArray(shift_amount, 0, int.MaxValue, NumVarType.Bool);
                }
            }
          
            //for (int e = 0; e < employee_amount; e++)
            //{
            //    e_variable[e] = new INumVar[job_amount][][];
            //    for (int i = 0; i < job_amount; i++)
            //    {
            //        e_variable[e][i] = new INumVar[read_data.job_operation_list[i].Count][];
            //        for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
            //        {
            //            e_variable[e][i][j] = model.NumVarArray(shift_amount, 0, int.MaxValue, NumVarType.Bool);
            //        }
            //    }
            //} 
            
            // E variable
            emp_amount = emp_amount_method(read_data.job_operation_list, read_data.employee_competence_operation);
           
            for (int i = 0; i < job_amount; i++) 
            {
                e_variable[i] = new INumVar[read_data.job_operation_list[i].Count][][];
                for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
                {
                    int emp_count = emp_amount[i][j].Count;
                    e_variable[i][j] = new INumVar[emp_count][];
                    for(int e=0; e< emp_count; e++)
                    {
                        e_variable[i][j][e] = model.NumVarArray(shift_amount, 0, int.MaxValue, NumVarType.Bool);
                    }

                }
            }

            // Z variable
            for (int e = 0; e < employee_amount; e++)
            {
                z_variable[e] = model.NumVarArray(shift_amount, 0, int.MaxValue, NumVarType.Bool);
            }

            Cmax = model.NumVar(0, float.MaxValue);
            #endregion

            //Objective function
            ILinearNumExpr labour_cost_obj = model.LinearNumExpr();
            for (int c = 0; c < read_data.labour_cost.Count; c++) 
            {
                for (int s = 0; s < shift_amount; s++) 
                {
                    labour_cost_obj.AddTerm(beta * read_data.labour_cost[c], z_variable[c][s]);
                }
            }

            ILinearNumExpr makespan_obj = model.LinearNumExpr();
            makespan_obj.AddTerm(alpha, Cmax);

            model.AddMinimize(model.Sum(makespan_obj, labour_cost_obj));

            #region production scheduling
            //Constraint1-1
            Console.WriteLine("Constraint1-1");
            for (int i = 0; i < job_amount; i++)
            {
                for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
                {
                    ILinearNumExpr constraint1_1 = model.LinearNumExpr();
                    int m_amount = machine_amount_method(i, j, read_data.job_operation_list, read_data.operation_avilable_machine);
                    for (int k = 0; k < m_amount; k++)
                    {
                        constraint1_1.AddTerm(1, x_variable[i][j][k]);
                    }

                    model.AddEq(constraint1_1, 1);
                }
            }

            //Constraint1-2
            Console.WriteLine("Constraint1-2");
            for (int i = 0; i < job_amount; i++)
            {
                for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
                {
                    ILinearNumExpr constraint1_2 = model.LinearNumExpr();
                    for (int s = 0; s < shift_amount; s++)
                    {
                        constraint1_2.AddTerm(1, t_variable[i][j][s]);
                    }

                    model.AddEq(constraint1_2, 1);
                }
            }

            //Constraint1-3
            Console.WriteLine("Constraint1-3");
            for (int i = 0; i < job_amount; i++)
            {
                for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
                {
                    int m_amount = machine_amount_method(i, j, read_data.job_operation_list, read_data.operation_avilable_machine);
                    for (int k = 0; k < m_amount; k++)
                    {
                        ILinearNumExpr constraint1_3 = model.LinearNumExpr();
                        constraint1_3.AddTerm(1, s_variable[i][j][k]);
                        constraint1_3.AddTerm(1, c_variable[i][j][k]);
                        constraint1_3.AddTerm(-2 * M, x_variable[i][j][k]);
                        model.AddLe(constraint1_3, 0);

                        /*ILinearNumExpr constraint1_3_main = model.LinearNumExpr();
                        constraint1_3_main.AddTerm(1, s_variable[i][j][k]);
                        constraint1_3_main.AddTerm(1, c_variable[i][j][k]);

                        ILinearNumExpr constraint1_3_sub = model.LinearNumExpr();
                        constraint1_3_sub.AddTerm(1, x_variable[i][j][k]);
                         model.Add(model.IfThen(model.Eq(constraint1_3_sub, 0), model.Le(constraint1_3_main, 0)));*/

                        /*ILinearNumExpr constraint1_3 = model.LinearNumExpr();
                        constraint1_3.AddTerm(1, s_variable[i][j][k]);
                        constraint1_3.AddTerm(1, c_variable[i][j][k]);

                        IConstraint constraint1_3_main = model.Le(constraint1_3, 0);
                        IConstraint constraint1_3_indicator = model.Eq(x_variable[i][j][k], 0);
                        Console.WriteLine("X" + i + j + k + "  C" + i + j + k);
                        model.Add(model.IfThen(constraint1_3_indicator, constraint1_3_main));*/
                    }

                }
            }

            //Constraint1-4
            Console.WriteLine("Constraint1-4");
            for (int i = 0; i < job_amount; i++)
            {
                for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
                {
                    int m_amount = machine_amount_method(i, j, read_data.job_operation_list, read_data.operation_avilable_machine);
                    for (int k = 0; k < m_amount; k++)
                    {
                        ILinearNumExpr constraint1_4 = model.LinearNumExpr();
                        double processing_time = read_data.operation_time[processing_time_find_index(i, j, read_data.job_operation_list)][k];
                        constraint1_4.AddTerm(-1, s_variable[i][j][k]);
                        constraint1_4.AddTerm(1, c_variable[i][j][k]);
                        constraint1_4.AddTerm(-(processing_time + 0.1), x_variable[i][j][k]); //<
                        // constraint1_4.AddTerm(-1*setup_time, u_variable[i][j][k]);
                        model.AddGe(constraint1_4, -(processing_time + 0.1) + processing_time);  //<

                        /*ILinearNumExpr constraint1_4_main = model.LinearNumExpr();
                        double processing_time = operation_time[processing_time_find_index(i, j, job_operation_list)][k];
                        constraint1_4_main.AddTerm(-1, s_variable[i][j][k]);
                        constraint1_4_main.AddTerm(1, c_variable[i][j][k]);

                        ILinearNumExpr constraint1_4_sub = model.LinearNumExpr();
                        constraint1_4_sub.AddTerm(1, x_variable[i][j][k]);
                        model.Add(model.IfThen(model.Eq(constraint1_4_sub, 1), model.Ge(constraint1_4_main, processing_time)));*/

                        /* ILinearNumExpr constraint1_4 = model.LinearNumExpr();
                         double processing_time = read_data.operation_time[processing_time_find_index(i, j, read_data.job_operation_list)][k];
                         constraint1_4.AddTerm(-1, s_variable[i][j][k]);
                         constraint1_4.AddTerm(1, c_variable[i][j][k]);
                         // constraint1_4.AddTerm(-1*setup_time, u_variable[i][j][k]);

                         IConstraint constraint1_4_main = model.Ge(constraint1_4, processing_time);
                         IConstraint constraint1_4_indicator = model.Eq(x_variable[i][j][k], 1);
                         model.Add(model.IfThen(constraint1_4_indicator, constraint1_4_main));*/
                    }
                }
            }


            //Constraint1-5
            Console.WriteLine("Constraint1-5");
            for (int i = 0; i < job_amount; i++)
            {
                for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
                {
                    for (int i_p = 0; i_p < job_amount; i_p++)
                    {
                        if (i < i_p)
                        {
                            for (int j_p = 0; j_p < read_data.job_operation_list[i_p].Count; j_p++)
                            {
                                if (!(i == i_p)) //不跟自己比
                                {
                                    var result = find_same_machine(i, j, i_p, j_p, read_data.job_operation_list, read_data.operation_avilable_machine);
                                    List<List<int>> k_index = result.find_same_machine;
                                    List<int> y_k_index = result.y_k_index;
                                    //Console.WriteLine(k_index.Count);

                                    for (int k = 0; k < k_index.Count; k++)
                                    {
                                        //Console.WriteLine("i j i_p j_p k " + i + " " + j + " " + i_p + " " + j_p + " " + y_k_index[k]);
                                        //Console.WriteLine("k1: " + k_index[k][0] + " k2: " + k_index[k][1]);

                                        for (int a = 0; a < k_index[k].Count; a++) 
                                        {
                                            for (int b = a + 1; b < k_index[k].Count; b++) 
                                            {
                                                ILinearNumExpr constraint1_5 = model.LinearNumExpr();
                                                constraint1_5.AddTerm(1, s_variable[i][j][k_index[k][a]]);
                                                constraint1_5.AddTerm(-1, c_variable[i_p][j_p][k_index[k][b]]);
                                                constraint1_5.AddTerm(M, y_variable[i][j][i_p][j_p][y_k_index[k]]);
                                                //constraint1_5.AddTerm(-M, x_variable[i][j][k_index[k][0]]);
                                                //constraint1_5.AddTerm(-M, x_variable[i_p][j_p][k_index[k][1]]);
                                                model.AddGe(constraint1_5, 0);
                                            }
                                        }
                                        
                                        //Console.WriteLine("Y" + i + j + "  " + "Y" + i_p + j_p + "   M: " + y_k_index[k]);
                                        /*ILinearNumExpr constraint1_5_main = model.LinearNumExpr();
                                        constraint1_5_main.AddTerm(1, s_variable[i][j][k_index[k][0]]);
                                        constraint1_5_main.AddTerm(-1, c_variable[i_p][j_p][k_index[k][1]]);

                                        ILinearNumExpr constraint1_5_sub = model.LinearNumExpr();
                                        constraint1_5_sub.AddTerm(1, y_variable[i][j][i_p][j_p][y_k_index[k]]);
                                        model.Add(model.IfThen(model.Eq(constraint1_5_sub, 0), model.Ge(constraint1_5_main, 0)));*/
                                    }
                                }
                            }
                        }
                    }
                }
            }

            //Constraint1-6
            /*Console.WriteLine("Constraint1-6");
            for (int i = 0; i < job_amount; i++)
            {
                for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
                {
                    for (int i_p = 0; i_p < job_amount; i_p++)
                    {
                        if (i < i_p)
                        {
                            for (int j_p = 0; j_p < read_data.job_operation_list[i_p].Count; j_p++)
                            {
                                if (!(i == i_p))
                                {
                                    var result = find_same_machine(i, j, i_p, j_p, read_data.job_operation_list, read_data.operation_avilable_machine);
                                    //List<List<int>> k_index = result.find_same_machine;
                                    List<int> y_k_index = result.y_k_index;
                                    for (int k = 0; k < y_k_index.Count; k++)
                                    {
                                        Console.WriteLine("i j i_p j_p k " + i + " " + j + " " + i_p + " " + j_p + " " + y_k_index[k]);
                                        ILinearNumExpr constraint1_6 = model.LinearNumExpr();
                                        constraint1_6.AddTerm(1, y_variable[i][j][i_p][j_p][y_k_index[k]]);
                                        constraint1_6.AddTerm(1, y_variable[i_p][j_p][i][j][y_k_index[k]]);
                                        model.AddEq(constraint1_6, 1);
                                    }
                                }
                            }
                        }

                    }
                }
            }*/
            Console.WriteLine("Constraint1-6");
            for (int i = 0; i < job_amount; i++)
            {
                for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
                {
                    for (int i_p = 0; i_p < job_amount; i_p++)
                    {
                        if (i < i_p)
                        {
                            for (int j_p = 0; j_p < read_data.job_operation_list[i_p].Count; j_p++)
                            {
                                if (!(i == i_p)) //不跟自己比
                                {
                                    var result = find_same_machine(i, j, i_p, j_p, read_data.job_operation_list, read_data.operation_avilable_machine);
                                    List<List<int>> k_index = result.find_same_machine;
                                    List<int> y_k_index = result.y_k_index;
                                    //Console.WriteLine(k_index.Count);
                                    for (int k = 0; k < k_index.Count; k++)
                                    {
                                        //Console.WriteLine("i j i_p j_p k " + i + " " + j + " " + i_p + " " + j_p + " " + y_k_index[k]);
                                        //Console.WriteLine("k1: " + k_index[k][0] + " k2: " + k_index[k][1]);

                                        for (int a = 0; a < k_index[k].Count; a++)
                                        {
                                            for (int b = a + 1; b < k_index[k].Count; b++)
                                            {
                                                ILinearNumExpr constraint1_6 = model.LinearNumExpr();
                                                constraint1_6.AddTerm(-1, c_variable[i][j][k_index[k][a]]);
                                                constraint1_6.AddTerm(1, s_variable[i_p][j_p][k_index[k][b]]);
                                                constraint1_6.AddTerm(-M, y_variable[i][j][i_p][j_p][y_k_index[k]]); //<
                                                //constraint1_5.AddTerm(-M, x_variable[i][j][k_index[k][0]]);
                                                //constraint1_5.AddTerm(-M, x_variable[i_p][j_p][k_index[k][1]]);
                                                model.AddGe(constraint1_6, -M); //<
                                            }
                                        }
                                       
                                        //Console.WriteLine("Y" + i + j + "  " + "Y" + i_p + j_p + "   M: " + y_k_index[k]);
                                        /*ILinearNumExpr constraint1_5_main = model.LinearNumExpr();
                                        constraint1_5_main.AddTerm(1, s_variable[i][j][k_index[k][0]]);
                                        constraint1_5_main.AddTerm(-1, c_variable[i_p][j_p][k_index[k][1]]);

                                        ILinearNumExpr constraint1_5_sub = model.LinearNumExpr();
                                        constraint1_5_sub.AddTerm(1, y_variable[i][j][i_p][j_p][y_k_index[k]]);
                                        model.Add(model.IfThen(model.Eq(constraint1_5_sub, 0), model.Ge(constraint1_5_main, 0)));*/
                                    }
                                }
                            }
                        }
                    }
                }
            }

            //Constraint 1-7 version2
            Console.WriteLine("Constraint1-7");
            for (int i = 0; i < job_amount; i++)
            {
                for (int j = 1; j < read_data.job_operation_list[i].Count; j++)
                {
                    int m_amount = machine_amount_method(i, j, read_data.job_operation_list, read_data.operation_avilable_machine);
                    ILinearNumExpr constraint1_7 = model.LinearNumExpr();
                    for (int k = 0; k < m_amount; k++)
                    {
                        constraint1_7.AddTerm(1, s_variable[i][j][k]);
                    }
                    m_amount = machine_amount_method(i, j - 1, read_data.job_operation_list, read_data.operation_avilable_machine);
                    for (int k = 0; k < m_amount; k++)
                    {
                        constraint1_7.AddTerm(-1, c_variable[i][j - 1][k]);
                    }
                    model.AddGe(constraint1_7, 0);
                }
            }


            //Constraint1-8 version2
            Console.WriteLine("constraint1-8");
            for (int i = 0; i < job_amount; i++)
            {
                int j = read_data.job_operation_list[i].Count - 1;
                int m_amount = machine_amount_method(i, j, read_data.job_operation_list, read_data.operation_avilable_machine);
                ILinearNumExpr constraint1_8 = model.LinearNumExpr();
                constraint1_8.AddTerm(1, Cmax);
                for (int k = 0; k < m_amount; k++)
                {
                    constraint1_8.AddTerm(-1, c_variable[i][j][k]);
                }
                model.AddGe(constraint1_8, 0);

            }


            //Constraint1-9
            Console.WriteLine("Constraint1-9");
            for (int i = 0; i < job_amount; i++)
            {
                for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
                {
                    for (int s = 0; s < shift_amount; s++)
                    {
                        ILinearNumExpr constraint1_9 = model.LinearNumExpr();
                        constraint1_9.AddTerm(-M, t_variable[i][j][s]);
                        int m_amount = machine_amount_method(i, j, read_data.job_operation_list, read_data.operation_avilable_machine);
                        for (int k = 0; k < m_amount; k++)
                        {
                            constraint1_9.AddTerm(1, s_variable[i][j][k]);
                        }
                        model.AddGe(constraint1_9, s * shift_unit_hour - M);

                        /*ILinearNumExpr constraint1_9 = model.LinearNumExpr();
                        double tmp = (-1) * s * shift_unit_hour;


                        constraint1_9.AddTerm(tmp, t_variable[i][j][s]);
                        int m_amount = machine_amount_method(i, j, read_data.job_operation_list, read_data.operation_avilable_machine);
                        for (int k = 0; k < m_amount; k++)
                        {
                            constraint1_9.AddTerm(1, s_variable[i][j][k]);
                        }
                        model.AddGe(constraint1_9, 0);*/

                    }
                }
            }

            //Constraint1-10
            Console.WriteLine("Constraint1-10");
            for (int i = 0; i < job_amount; i++)
            {
                for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
                {
                    for (int s = 0; s < shift_amount - 1; s++)
                    {
                        ILinearNumExpr constraint1_10 = model.LinearNumExpr();
                        constraint1_10.AddTerm(M, t_variable[i][j][s]);
                        int m_amount = machine_amount_method(i, j, read_data.job_operation_list, read_data.operation_avilable_machine);
                        for (int k = 0; k < m_amount; k++)
                        {
                            constraint1_10.AddTerm(1, c_variable[i][j][k]);
                        }
                        model.AddLe(constraint1_10, (s + 1) * shift_unit_hour + M);

                        /*ILinearNumExpr constraint1_10 = model.LinearNumExpr();
                        int m_amount = machine_amount_method(i, j, read_data.job_operation_list, read_data.operation_avilable_machine);
                        for (int k = 0; k < m_amount; k++)
                        {
                            constraint1_10.AddTerm(1, c_variable[i][j][k]);
                        }

                        IConstraint constraint1_10_main = model.Le(constraint1_10, (s + 1) * shift_unit_hour);
                        IConstraint constraint1_10_indicator = model.Eq(t_variable[i][j][s], 1);

                        model.Add(model.IfThen(constraint1_10_indicator, constraint1_10_main));*/

                    }
                }
            }
            #endregion

            #region employee scheduling
            //Constraint2-1
            Console.WriteLine("Constraint2-1");
            for (int i = 0; i < job_amount; i++)
            {
                for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
                {
                    for (int s = 0; s < shift_amount; s++)
                    {
                        ILinearNumExpr constraint2_1 = model.LinearNumExpr();
                        constraint2_1.AddTerm(-1, t_variable[i][j][s]);
                        int emp_count = emp_amount[i][j].Count;
                        for (int e = 0; e < emp_count; e++)
                        {
                            constraint2_1.AddTerm(1, e_variable[i][j][e][s]);
                            //Console.Write("E" + e + i + j + s + "  ");
                        }
                        //Console.WriteLine("T" + i + j + s);
                        model.AddEq(constraint2_1, 0);
                    }
                }
            }

            //Constraint2-2
            //Console.WriteLine("Constraint2-2");
            //List<List<int>> remove_employee_competence = employee_operation_method(read_data.employee_competence_operation); //已轉換為index形式
            //for (int e = 0; e < employee_amount; e++)
            //{
            //    for (int i = 0; i < job_amount; i++)
            //    {
            //        for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
            //        {
            //            bool check = false;
            //            if (read_data.employee_competence_operation[e].Contains(read_data.job_operation_list[i][j]))
            //            {
            //                check = true;
            //            }
            //            if (check == false)
            //            {
            //                for (int s = 0; s < shift_amount; s++)
            //                {
            //                    ILinearNumExpr constraint2_2 = model.LinearNumExpr();
            //                    constraint2_2.AddTerm(1, e_variable[e][i][j][s]);
            //                    model.AddEq(constraint2_2, 0);
            //                }
            //            }
            //        }
            //    }
            //}

            List<int> cal_each_emp_amount = new List<int>();
            List<List<int>> each_emp = new List<List<int>>();
            for(int i=0; i<job_amount; i++)
            {
                for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
                {
                    cal_each_emp_amount.Add(emp_amount[i][j].Count);
                    each_emp.Add(emp_amount[i][j]);
                }
            }

            //Constraint2-3
            Console.WriteLine("Constraint2-3");
            //int count_e = 0;
            //while (count_e != cal_each_emp_amount.Count) 
            //{
            //    for (int e = 0; e < cal_each_emp_amount[count_e]; e++)
            //    {
            //        for (int s = 0; s < shift_amount; s++)
            //        {
            //            ILinearNumExpr constraint2_3 = model.LinearNumExpr();
            //            constraint2_3.AddTerm(-M, z_variable[each_emp[count_e][e]][s]);
            //            Console.Write("Z "+each_emp[count_e][e]+" ");
            //            for (int i = 0; i < job_amount; i++)
            //            {
            //                for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
            //                {
            //                    constraint2_3.AddTerm(1, e_variable[i][j][e][s]);
            //                }
            //            }
            //            model.AddLe(constraint2_3, 0);

            //            /*ILinearNumExpr constraint2_3 = model.LinearNumExpr();
            //            for (int i = 0; i < job_amount; i++)
            //            {
            //                for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
            //                {
            //                    constraint2_3.AddTerm(1, e_variable[e][i][j][s]);
            //                }
            //            }

            //            IConstraint constraint2_3_main = model.Le(constraint2_3, 0);
            //            IConstraint constraint2_3_indicator = model.Eq(z_variable[e][s], 0);
            //            model.Add(model.IfThen(constraint2_3_indicator, constraint2_3_main));*/

            //        }
                    
            //    }
            //    count_e++;
            //    //Console.WriteLine();
            //}

            for (int e = 0; e < employee_amount; e++) 
            {
                for (int s = 0; s < shift_amount; s++)
                {
                    ILinearNumExpr constraint2_3_revised = model.LinearNumExpr();
                    constraint2_3_revised.AddTerm(-M, z_variable[e][s]);

                    for (int i = 0; i < job_amount; i++)
                    {
                        for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
                        {
                            if(emp_amount[i][j].Contains(e))
                            {
                                constraint2_3_revised.AddTerm(1, e_variable[i][j][emp_amount[i][j].IndexOf(e)][s]);
                            }
                        }
                    }
                    model.AddLe(constraint2_3_revised, 0);
                }

            }
            //Constraint2-4
            Console.WriteLine("Constraint2-4");
            //count_e = 0;
            //while (count_e != cal_each_emp_amount.Count)
            //{
            //    for (int e = 0; e < cal_each_emp_amount[count_e]; e++)
            //    {
            //        for (int s = 0; s < shift_amount; s++)
            //        {
            //            ILinearNumExpr constraint2_4 = model.LinearNumExpr();
            //            constraint2_4.AddTerm(-1, z_variable[each_emp[count_e][e]][s]);
            //            for (int i = 0; i < job_amount; i++)
            //            {
            //                for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
            //                {
            //                    constraint2_4.AddTerm(1, e_variable[i][j][e][s]);
            //                }
            //            }
            //            model.AddGe(constraint2_4, 0);
            //        }
            //    }
            //    count_e++;
            //}

            for (int e = 0; e < employee_amount; e++)
            {
                for (int s = 0; s < shift_amount; s++)
                {
                    ILinearNumExpr constraint2_4_revised = model.LinearNumExpr();
                    constraint2_4_revised.AddTerm(-1, z_variable[e][s]);
                    for (int i = 0; i < job_amount; i++)
                    {
                        for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
                        {
                            if (emp_amount[i][j].Contains(e))
                            {
                                constraint2_4_revised.AddTerm(1, e_variable[i][j][emp_amount[i][j].IndexOf(e)][s]);

                            }
                        }
                    }
                    model.AddGe(constraint2_4_revised, 0);
                }
            }

            //Constraint2-5
            Console.WriteLine("Constraint2-5");
            for (int e = 0; e < employee_amount; e++)
            {
                for (int s = 0; s < shift_amount - 1; s++)
                {
                    ILinearNumExpr constraint2_5 = model.LinearNumExpr();
                    constraint2_5.AddTerm(1, z_variable[e][s]);
                    constraint2_5.AddTerm(1, z_variable[e][s + 1]);
                    model.AddLe(constraint2_5, 1);
                }
            }

            //Constraint2-6
            Console.WriteLine("Constraint2-6");
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
            Console.WriteLine("Constraint2-7");
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
        

            model.SetParam(Cplex.Param.Emphasis.Memory, true);
            model.SetParam(Cplex.Param.Threads, 4);
            model.SetParam(Cplex.Param.MIP.Tolerances.Integrality, 1e-11);
            model.SetParam(Cplex.Param.TimeLimit, 60 * 60 * 4);//單位:秒    
            //model.SetParam(Cplex.Param.TimeLimit, 60 * 0.5);
            //model.SetParam(Cplex.Param.MIP.Display, 4);

            //using (System.IO.TextWriter tw = System.IO.File.CreateText("cplex.log"))
            //{
            //    time.Reset(); time.Start();

            //    model.SetOut(tw);
            //    model.SetWarning(tw);
            //    model.Solve();

            //    time.Stop();
            //}

            time.Start();
            model.Solve();
            time.Stop();

            #region variable result
            // X result
            x_result = new double[job_amount][][];
            for (int i = 0; i < job_amount; i++)
            {
                x_result[i] = new double[read_data.job_operation_list[i].Count][];
                for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
                {
                    x_result[i][j] = model.GetValues(x_variable[i][j]);
                }
            }

            // S result
            s_result = new double[job_amount][][];
            for (int i = 0; i < job_amount; i++)
            {
                s_result[i] = new double[read_data.job_operation_list[i].Count][];
                for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
                {
                    s_result[i][j] = model.GetValues(s_variable[i][j]);
                }
            }

            // C result
            c_result = new double[job_amount][][];
            for (int i = 0; i < job_amount; i++)
            {
                c_result[i] = new double[read_data.job_operation_list[i].Count][];
                for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
                {
                    c_result[i][j] = model.GetValues(c_variable[i][j]);
                }
            }

            // Y result
            y_result = new double[job_amount][][][][];
            for (int i = 0; i < job_amount; i++)
            {
                y_result[i] = new double[read_data.job_operation_list[i].Count][][][];
                for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
                {
                    y_result[i][j] = new double[job_amount][][];
                    for (int i_p = 0; i_p < job_amount; i_p++)
                    {
                        if (i < i_p)
                        {
                            y_result[i][j][i_p] = new double[read_data.job_operation_list[i_p].Count][];
                            for (int j_p = 0; j_p < read_data.job_operation_list[i_p].Count; j_p++)
                            {
                                if (!(i == i_p))
                                {
                                    var result = find_same_machine(i, j, i_p, j_p, read_data.job_operation_list, read_data.operation_avilable_machine);
                                    //List<List<int>> k_index = result.find_same_machine;
                                    List<int> y_k_index = result.y_k_index;
                                    y_result[i][j][i_p][j_p] = new double[machine_amount];

                                    //Console.WriteLine(k_index.Count);
                                    for (int k = 0; k < y_k_index.Count; k++)
                                    {
                                        y_result[i][j][i_p][j_p][y_k_index[k]] = model.GetValue(y_variable[i][j][i_p][j_p][y_k_index[k]]);                                        
                                    }
                                }
                            }
                        }

                    }
                }
            }

            // T result
            t_result = new double[job_amount][][];
            for (int i = 0; i < job_amount; i++)
            {
                t_result[i] = new double[read_data.job_operation_list[i].Count][];
                for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
                {
                    t_result[i][j] = model.GetValues(t_variable[i][j]);
                }
            }

            // E result
            e_result = new double[job_amount][][][];
            for (int i = 0; i < job_amount; i++)
            {
                e_result[i] = new double[read_data.job_operation_list[i].Count][][];

                for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
                {
                    e_result[i][j] = new double[employee_amount][];

                    int emp_count = emp_amount[i][j].Count;
                    for (int e = 0; e < emp_count; e++)
                    {
                        e_result[i][j][e] = model.GetValues(e_variable[i][j][e]);
                    }
                }
            }

             //Z result
             z_result = new double[employee_amount][];
             for (int e = 0; e < employee_amount; e++)
             {
                 z_result[e] = model.GetValues(z_variable[e]);

             }

            makespan_obj_value = model.GetValue(Cmax);
            for (int e = 0; e < z_result.Length; e++) 
            {
                for (int s = 0; s < z_result[e].Length; s++) 
                {
                    if (Math.Round(z_result[e][s]) == 1) 
                    {
                        labour_cost_obj_value += (read_data.labour_cost[e] * z_result[e][s]);
                        Console.WriteLine("emp: " + e + "  labour cost: " + read_data.labour_cost[e] + "  z_result" + z_result[e][s]);
                    }
                    //labour_cost_obj_value += (read_data.labour_cost[e] * z_result[e][s]);
                }
            }

            #endregion


            Console.WriteLine("Objective value: " + model.GetObjValue());
            Console.WriteLine("makespan " + makespan_obj_value);
            Console.WriteLine("labour cost " + labour_cost_obj_value);
            Console.WriteLine("time : " + time.ElapsedMilliseconds);


            check_all_feasibility_multi_obj.model();

            
           
        }
        public static bool check_emp_op(int job, int operation_step, List<List<string>> job_operation_list, List<List<string>> employee_competence_operation, int emp)
        {
            bool check_emp_op = false;

            for (int emp_op = 0; emp_op < employee_competence_operation[emp].Count; emp_op++)
            {
                if (job_operation_list[job][operation_step] == employee_competence_operation[emp][emp_op])
                {
                    check_emp_op = true;
                    break;
                }
            }
            return check_emp_op;
        }
        public static List<List<double>> sorted_OS_string()
        {
            List<List<double>> tmp_OS_string = new List<List<double>>();
            int count = 0;
            for (int i = 0; i < job_amount; i++)
            {
                for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
                {
                    tmp_OS_string.Add(new List<double>());
                    int m_amount = machine_amount_method(i, j, read_data.job_operation_list, read_data.operation_avilable_machine);
                    for (int k = 0; k < m_amount; k++)
                    {
                        //Console.WriteLine("i j k " + i + " " + j + " " + k + " " + s_result[i][j][k]+"   "+x_result[i][j][k]);
                        if (x_result[i][j][k] != 0)
                        {
                            tmp_OS_string[count].Add(s_result[i][j][k]);
                            tmp_OS_string[count].Add(i);
                            tmp_OS_string[count].Add(j);
                        }
                    }
                    count++;
                }
            }
            var sortedList = tmp_OS_string.OrderBy(x => x[0]);
            tmp_OS_string = sortedList.ToList();


            //for (int i = 0; i < tmp_OS_string.Count; i++)
            //{
            //    for (int j = 0; j < tmp_OS_string[i].Count; j++)
            //    {
            //        Console.Write(tmp_OS_string[i][j] + " ");

            //    }
            //    Console.WriteLine();
            //}
            return tmp_OS_string;

            //Console.WriteLine("After change");

            //for (int i = 0; i < tmp_OS_string.Count; i++)
            //{
            //    for (int j = 0; j < tmp_OS_string[i].Count; j++)
            //    {
            //        Console.Write(tmp_OS_string[i][j] + " ");

            //    }
            //    Console.WriteLine();
            //}

        }
        public static int machine_amount_method(int job, int operation_step, List<List<string>> job_operation_list, List<List<string>> operation_avilable_machine)
        {
            int machine_amount = 0;

            //Console.WriteLine("job " + job + "  op " + operation_step);
            switch (job_operation_list[job][operation_step])
            {
                case "A":
                    machine_amount = operation_avilable_machine[0].Count;
                    break;
                case "B":
                    machine_amount = operation_avilable_machine[1].Count;
                    break;
                case "C":
                    machine_amount = operation_avilable_machine[2].Count;
                    break;
                case "D":
                    machine_amount = operation_avilable_machine[3].Count;
                    break;
                case "E":
                    machine_amount = operation_avilable_machine[4].Count;
                    break;
            }
            return machine_amount;
        }

        public static List<List<List<int>>> emp_amount_method(List<List<string>> job_operation_list, List<List<string>> employee_competence_operation)
        {
            List<List<List<int>>> cal_emp_amount = new List<List<List<int>>>();
            for(int i=0; i<job_amount; i++)
            {
                cal_emp_amount.Add(new List<List<int>>());

                for (int j=0; j<job_operation_list[i].Count; j++)
                {
                    cal_emp_amount[i].Add(new List<int>());
                    for (int e = 0; e < employee_amount; e++)
                    {
                        if (employee_competence_operation[e].Contains(job_operation_list[i][j]))
                        {
                            cal_emp_amount[i][j].Add(e);
                        }
                    }
                }
            }
           
           // Console.WriteLine();

            return cal_emp_amount;
        }

        public static (List<List<int>> find_same_machine, List<int> y_k_index) find_same_machine(int job, int operation, int job_p, int operation_p, List<List<string>> job_operation_list, List<List<string>> operation_avilable_machine)
        {
            List<List<int>> find_same_machine = new List<List<int>>();
            List<int> y_k_index = new List<int>();
            int tmp1 = 0, tmp2 = 0;

            switch (job_operation_list[job][operation])
            {
                case "A":
                    tmp1 = 0;
                    break;
                case "B":
                    tmp1 = 1;
                    break;
                case "C":
                    tmp1 = 2;
                    break;
                case "D":
                    tmp1 = 3;
                    break;
                case "E":
                    tmp1 = 4;
                    break;
            }

            switch (job_operation_list[job_p][operation_p])
            {
                case "A":
                    tmp2 = 0;
                    break;
                case "B":
                    tmp2 = 1;
                    break;
                case "C":
                    tmp2 = 2;
                    break;
                case "D":
                    tmp2 = 3;
                    break;
                case "E":
                    tmp2 = 4;
                    break;
            }

            /*Console.WriteLine(job + " " + operation);
            Console.WriteLine(tmp1);
            Console.WriteLine(job_p + " " + operation_p);
            Console.WriteLine(tmp2);*/
            for (int i = 0; i < operation_avilable_machine[tmp1].Count; i++)
            {
                for (int j = 0; j < operation_avilable_machine[tmp2].Count; j++)
                {
                    List<int> tmp = new List<int>();
                    if (operation_avilable_machine[tmp1][i] == operation_avilable_machine[tmp2][j])
                    {
                        tmp.Add(i);
                        tmp.Add(j);
                        //Console.WriteLine(i + " " + j);
                    }
                    if (tmp.Count > 0)
                    {
                        y_k_index.Add(int.Parse(operation_avilable_machine[tmp1][i]) - 1);
                        find_same_machine.Add(tmp);
                    }

                }
            }

            for (int i = 0; i < find_same_machine.Count; i++)
            {
                //Console.WriteLine(find_same_machine[i][0] + "  " + find_same_machine[i][1]);
            }

            return (find_same_machine, y_k_index);
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

        public static int find_k_index_machine(int job, int operation_step, List<List<string>> job_operation_list, List<List<string>> operation_avilable_machine, int y_k_machine)
        {
            int ans = 0;
            switch (job_operation_list[job][operation_step])
            {
                case "A":
                    ans = (int.Parse(operation_avilable_machine[0][y_k_machine])) - 1;
                    break;
                case "B":
                    ans = (int.Parse(operation_avilable_machine[1][y_k_machine])) - 1;
                    break;
                case "C":
                    ans = (int.Parse(operation_avilable_machine[2][y_k_machine])) - 1;
                    break;
                case "D":
                    ans = (int.Parse(operation_avilable_machine[3][y_k_machine])) - 1;
                    break;
                case "E":
                    ans = (int.Parse(operation_avilable_machine[4][y_k_machine])) - 1;
                    break;
            }

            return ans;
        }
        public static int find_e_index_emp(int job, int operation_step, List<List<string>> job_operation_list, List<List<string>> employee_competence_operation, int emp)
        {
            int ans = 0;
            switch (job_operation_list[job][operation_step])
            {
                case "A":
                    ans = int.Parse(employee_competence_operation[0][emp]);
                    break;
                case "B":
                    ans = int.Parse(employee_competence_operation[1][emp]);
                    break;
                case "C":
                    ans = int.Parse(employee_competence_operation[2][emp]);
                    break;
                case "D":
                    ans = int.Parse(employee_competence_operation[3][emp]);
                    break;
                case "E":
                    ans = int.Parse(employee_competence_operation[4][emp]);
                    break;
            }

            return ans;
        }
    }
}

/*
            //Constraint 1-7 
            Console.WriteLine("Constraint1-7");
            for (int i = 0; i < job_amount; i++)
            {
                for (int j = 1; j < job_operation_list[i].Count; j++)
                {
                    int m_amount = machine_amount_method(i, j, job_operation_list, operation_avilable_machine);

                    for (int k = 0; k < m_amount; k++)
                    {
                        m_amount = machine_amount_method(i, j - 1, job_operation_list, operation_avilable_machine);
                        for (int k_p = 0; k_p < m_amount; k_p++)
                        {
                            ILinearNumExpr constraint1_7 = model.LinearNumExpr();
                            constraint1_7.AddTerm(-1, c_variable[i][j - 1][k_p]);
                            constraint1_7.AddTerm(1, s_variable[i][j][k]);
                            model.AddGe(constraint1_7, 0);
                        }

                    } 
                }
            }

            // Constraint1-8
            Console.WriteLine("constraint1-8");
            for (int i = 0; i < job_amount; i++)
            {
                int j = job_operation_list[i].Count - 1;
                int m_amount = machine_amount_method(i, j, job_operation_list, operation_avilable_machine);
                for (int k = 0; k < m_amount; k++)
                {
                    ILinearNumExpr constraint1_8 = model.LinearNumExpr();
                    constraint1_8.AddTerm(-1, c_variable[i][j][k]);
                    constraint1_8.AddTerm(1, Cmax);
                    model.AddGe(constraint1_8, 0);
                }
            }
           */
