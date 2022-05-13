using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace Integrated_model_cut_generation
{
    class cut_check_all_feasibility_multi_obj
    {
        public static void model()
        {
            //mathematical model
            Console.WriteLine("/// feasibility ///");

            int M = 168;
            
            int day_amount = 7;  //a week shift=7x3=21
            int shift_amount = day_amount * 3;
            int shift_unit_hour = 8;

            #region production scheduling          
            //Constraint1-3
            Console.WriteLine("Constraint1-3");
            for (int i = 0; i < read_data.job_order_amount.Count; i++)
            {
                for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
                {
                    int m_amount = machine_amount_method(i, j, read_data.job_operation_list, read_data.operation_avilable_machine);
                    for (int k = 0; k < m_amount; k++)
                    {
                        double left = Math.Round(cut_mathematical_model_multi_obj.s_result[i][j][k]) + Math.Round(cut_mathematical_model_multi_obj.c_result[i][j][k]);
                        double right = 2 * M * Math.Round(cut_mathematical_model_multi_obj.x_result[i][j][k]);
                        if (left > right) Console.WriteLine("gggggggggg");

                    }

                }
            }

            //Constraint1-4
            Console.WriteLine("Constraint1-4");
            for (int i = 0; i < read_data.job_order_amount.Count; i++)
            {
                for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
                {
                    int m_amount = machine_amount_method(i, j, read_data.job_operation_list, read_data.operation_avilable_machine);
                    for (int k = 0; k < m_amount; k++)
                    {
                        double processing_time = read_data.operation_time[cut_mathematical_model_multi_obj.processing_time_find_index(i, j, read_data.job_operation_list)][k];
                        double left = processing_time + Math.Round(cut_mathematical_model_multi_obj.s_result[i][j][k]);
                        double right = Math.Round(cut_mathematical_model_multi_obj.c_result[i][j][k]) + M * (1 - Math.Round(cut_mathematical_model_multi_obj.x_result[i][j][k]));
                        if (left > right) Console.WriteLine("gggggggggg");


                    }
                }
            }

            //Constraint1-5
            Console.WriteLine("Constraint1-5");
            for (int i = 0; i < read_data.job_order_amount.Count; i++)
            {
                for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
                {
                    for (int i_p = 0; i_p < read_data.job_order_amount.Count; i_p++)
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
                                    for (int k = 0; k < k_index.Count; k++)
                                    {
                                        double left = Math.Round(cut_mathematical_model_multi_obj.s_result[i][j][k_index[k][0]]);
                                        double right = Math.Round(cut_mathematical_model_multi_obj.c_result[i_p][j_p][k_index[k][1]]) - M * (Math.Round(cut_mathematical_model_multi_obj.y_result[i][j][i_p][j_p][y_k_index[k]]));

                                        if (left < right)
                                        {
                                            Console.WriteLine(i + " " + j + " " + k_index[k][0] + "     " + cut_mathematical_model_multi_obj.s_result[i][j][k_index[k][0]]);
                                            Console.WriteLine(i_p + " " + j_p + " " + k_index[k][1] + "     " + cut_mathematical_model_multi_obj.c_result[i_p][j_p][k_index[k][1]]);
                                            Console.WriteLine(cut_mathematical_model_multi_obj.y_result[i][j][i_p][j_p][y_k_index[k]]);
                                            Console.WriteLine("gggggggggg");
                                        }

                                    }
                                }
                            }
                        }
                    }
                }
            }

            Console.WriteLine("Constraint1-6");
            for (int i = 0; i < read_data.job_order_amount.Count; i++)
            {
                for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
                {
                    for (int i_p = 0; i_p < read_data.job_order_amount.Count; i_p++)
                    {
                        if (i < i_p)
                        {
                            for (int j_p = 0; j_p < read_data.job_operation_list[i_p].Count; j_p++)
                            {
                                if (!(i == i_p)) //不跟自己比
                                {
                                    var result = cut_mathematical_model_multi_obj.find_same_machine(i, j, i_p, j_p, read_data.job_operation_list, read_data.operation_avilable_machine);
                                    List<List<int>> k_index = result.find_same_machine;
                                    List<int> y_k_index = result.y_k_index;
                                    //Console.WriteLine(k_index.Count);
                                    for (int k = 0; k < k_index.Count; k++)
                                    {
                                        double left = Math.Round(cut_mathematical_model_multi_obj.s_result[i_p][j_p][k_index[k][1]]);
                                        double right = Math.Round(cut_mathematical_model_multi_obj.c_result[i][j][k_index[k][0]]) + M * (Math.Round(cut_mathematical_model_multi_obj.y_result[i][j][i_p][j_p][y_k_index[k]]) - 1);

                                        if (left < right)
                                        {
                                            //Console.WriteLine("s ")
                                            Console.WriteLine("gggggggggg");
                                        }

                                    }
                                }
                            }
                        }
                    }
                }
            }

            //Constraint1-9
            Console.WriteLine("Constraint1-9");
            for (int i = 0; i < read_data.job_order_amount.Count; i++)
            {
                for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
                {
                    for (int s = 0; s < shift_amount; s++)
                    {
                        double left = 0;
                        int m_amount = machine_amount_method(i, j, read_data.job_operation_list, read_data.operation_avilable_machine);
                        for (int k = 0; k < m_amount; k++)
                        {
                            left += Math.Round(cut_mathematical_model_multi_obj.s_result[i][j][k]);
                        }
                        double right = s * shift_unit_hour + M * (Math.Round(cut_mathematical_model_multi_obj.t_result[i][j][s]) - 1);
                        if (left < right) Console.WriteLine("gggggggggg");


                    }
                }
            }

            //Constraint1-10
            Console.WriteLine("Constraint1-10");
            for (int i = 0; i < read_data.job_order_amount.Count; i++)
            {
                for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
                {
                    for (int s = 0; s < shift_amount - 1; s++)
                    {
                        double left = 0;
                        int m_amount = machine_amount_method(i, j, read_data.job_operation_list, read_data.operation_avilable_machine);
                        for (int k = 0; k < m_amount; k++)
                        {
                            left += Math.Round(cut_mathematical_model_multi_obj.c_result[i][j][k]);
                        }
                        double right = (s + 1) * shift_unit_hour + M * (1 - Math.Round(cut_mathematical_model_multi_obj.t_result[i][j][s]));
                        //Console.WriteLine("i j s " + i + " " + j + " " + s + " " + cut_mathematical_model_multi_obj.t_result[i][j][s]);
                        //Console.WriteLine("left " + left);
                        //Console.WriteLine("right " + right);
                        if (left > right) Console.WriteLine("gggggggggg");
                        //Console.WriteLine("------------\n");

                    }
                }
            }

            #endregion

            #region employee scheduling       
            //Constraint2-3
            for (int e = 0; e < cut_mathematical_model_multi_obj.employee_amount; e++)
            {
                for (int s = 0; s < shift_amount; s++)
                {
                    double left = 0;
                    for (int i = 0; i < read_data.job_order_amount.Count; i++)
                    {
                        for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
                        {
                            if (cut_mathematical_model_multi_obj.emp_amount[i][j].Contains(e))
                            {
                                left += Math.Round(cut_mathematical_model_multi_obj.e_result[i][j][cut_mathematical_model_multi_obj.emp_amount[i][j].IndexOf(e)][s]);
                            }
                        }
                    }
                    double right = M * Math.Round(cut_mathematical_model_multi_obj.z_result[e][s]);
                    if (left > right) Console.WriteLine("gggggggggg");
                }

            }

            //List<int> cal_each_emp_amount = new List<int>();
            //for (int i = 0; i < mathematical_model_revised.job_amount; i++)
            //{
            //    for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
            //    {
            //        cal_each_emp_amount.Add(mathematical_model_revised.emp_amount[i][j].Count);
            //    }
            //}

            //int count_e = 0;
            //while (count_e != cal_each_emp_amount.Count)
            //{
            //    for (int e = 0; e < cal_each_emp_amount[count_e]; e++)
            //    {
            //        for (int s = 0; s < shift_amount; s++)
            //        {
            //            double left = 0;
            //            for (int i = 0; i < read_data.job_order_amount.Count; i++)
            //            {
            //                for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
            //                {
            //                    left += Math.Round(mathematical_model_revised.e_result[i][j][e][s]);
            //                }
            //            }
            //            double right = M * Math.Round(mathematical_model_revised.z_result[e][s]);
            //            if (left > right) Console.WriteLine("gggggggggg");
            //        }
            //    }
            //    count_e++;
            //}

            //Console.WriteLine("Constraint2-3");
            //for (int e = 0; e < read_data.employee_competence_operation.Count; e++)
            //{
            //    for (int s = 0; s < shift_amount; s++)
            //    {
            //        double left = 0;
            //        for (int i = 0; i < read_data.job_order_amount.Count; i++)
            //        {
            //            for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
            //            {
            //                left += Math.Round(mathematical_model_revised.e_result[e][i][j][s]);
            //            }
            //        }
            //        double right = M * Math.Round(mathematical_model_revised.z_result[e][s]);
            //        if (left > right) Console.WriteLine("gggggggggg");
            //    }
            //}
            #endregion

        }

        public static int machine_amount_method(int job, int operation_step, List<List<string>> job_operation_list, List<List<string>> operation_avilable_machine)
        {
            int machine_amount = 0;
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

        public static int find_u_k_index(int job, int operation_step, List<List<string>> job_operation_list, List<List<string>> operation_avilable_machine, int y_k_machine)
        {
            int ans = 0;
            switch (job_operation_list[job][operation_step])
            {
                case "A":
                    for (int i = 0; i < operation_avilable_machine[0].Count; i++)
                    {
                        if (int.Parse(operation_avilable_machine[0][i]) == y_k_machine)
                        {
                            ans = i;
                        }
                    }
                    break;
                case "B":
                    for (int i = 0; i < operation_avilable_machine[1].Count; i++)
                    {
                        if (int.Parse(operation_avilable_machine[1][i]) == y_k_machine)
                        {
                            ans = i;
                        }
                    }
                    break;
                case "C":
                    for (int i = 0; i < operation_avilable_machine[2].Count; i++)
                    {
                        if (int.Parse(operation_avilable_machine[2][i]) == y_k_machine)
                        {
                            ans = i;
                        }
                    }
                    break;
                case "D":
                    for (int i = 0; i < operation_avilable_machine[3].Count; i++)
                    {
                        if (int.Parse(operation_avilable_machine[3][i]) == y_k_machine)
                        {
                            ans = i;
                        }
                    }
                    break;
                case "E":
                    for (int i = 0; i < operation_avilable_machine[4].Count; i++)
                    {
                        if (int.Parse(operation_avilable_machine[4][i]) == y_k_machine)
                        {
                            ans = i;
                        }
                    }
                    break;
            }

            return ans;
        }
    }
}
