using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integrated_model_cut_generation
{
    class GA_initialization
    {
        public static List<int> initial_MS_initial = new List<int>();
        public static int[] initial_OS_initial = new int[0];
        static public void model(List<int> input_MS_string, int[] input_OS_string)
        {
            int job_amount = read_data.job_order_amount.Count;
            int machine_amount = read_data.machine_information.Count;

            List<int> OS_string = new List<int>();
            int[] MS_string = new int[input_MS_string.Count];

            MS_string = input_MS_string.ToArray();
            OS_string = input_OS_string.ToList();
            initial_MS_initial = MS_string.ToList();
            initial_OS_initial = OS_string.ToArray();

            Random rd = new Random();
            int start_index = rd.Next(OS_string.Count / 3, OS_string.Count - 1);
            int end_index = rd.Next(start_index + 1, OS_string.Count);

            //start_index = 3;
            //end_index = 5;

            //Console.WriteLine("start index " + start_index);
            //Console.WriteLine("end index " + end_index);

            //Console.WriteLine("start: " + start_index + " end: " + end_index);
            /// 數規模型前半段不變
            int[] OS_string_initial = new int[start_index];
            OS_string_initial = OS_string.ToList().GetRange(0, start_index).ToArray();

            int[] MS_string_initial = new int[start_index];
            MS_string_initial = MS_string.ToList().GetRange(0, start_index).ToArray();

            var initial_preproces = initial_preprocessing(OS_string_initial, MS_string_initial, machine_amount, job_amount);
            int [] JL_initial_operation_count = initial_preproces.count_operation;
            List<int>current_operation_count = initial_preproces.count_operation.ToList();
            GC.Collect();
            /// 用GRASP的段落
            //int[] sub_MS_string = new int[end_index - start_index + 1];
            int[] sub_OS_string = new int[end_index - start_index + 1];
            sub_OS_string = OS_string.ToList().GetRange(start_index, end_index - start_index + 1).ToArray();

            bool[] os_ms_match_machine_index = new bool[sub_OS_string.Length]; ///modified 0328
            for(int i=0; i<sub_OS_string.Length; i++)
            {
                os_ms_match_machine_index[i] = false;

            }

            //Console.WriteLine("SUB OS");
            //for (int i = 0; i < sub_OS_string.Length; i++)
            //{
            //    Console.Write(sub_OS_string[i]);
            //}
            //Console.WriteLine();

            double[] current_machine_time = initial_preproces.machine_initial_time;
            List<List<int>> JL_CL = new List<List<int>>();
            int already_assign_op_count =0;
            List<int> final_sub_OS = new List<int>();
            //List<int> final_sub_MS = new List<int>(); ///modified0328
            int[] final_sub_MS = new int[end_index - start_index + 1];

            for (int i = 0; i < job_amount; i++) 
            {
                JL_CL.Add(new List<int>());
                for(int j=0; j<sub_OS_string.Length; j++)
                {
                    if (sub_OS_string[j] == i) 
                    {
                        JL_CL[i].Add(JL_initial_operation_count[i]);
                        JL_initial_operation_count[i]++;
                    }
                }
            }
            GC.Collect();
            //Console.WriteLine("JL_CL");
            //for (int i = 0; i < JL_CL.Count; i++)
            //{
            //    Console.WriteLine("JOB " + i);
            //    for (int j = 0; j < JL_CL[i].Count; j++)
            //    {
            //        Console.Write(JL_CL[i][j] + " ");
            //    }
            //    Console.WriteLine();
            //}



            while (already_assign_op_count < sub_OS_string.Length)
            {
                //Console.WriteLine("//// iter");
                //Console.WriteLine("not enter method count op");
                //for (int i = 0; i < current_operation_count.Count; i++)
                //{
                //    Console.WriteLine(current_operation_count[i]);
                //}
                //Console.WriteLine();
                var GRASP_result= GRASP(machine_amount, job_amount, JL_CL, current_machine_time, current_operation_count);
                JL_CL[GRASP_result.best_J].RemoveAt(0);
                GC.Collect();
                //Console.WriteLine("JL_CL");
                //for (int i = 0; i < JL_CL.Count; i++)
                //{
                //    Console.WriteLine("JOB " + i);
                //    for (int j = 0; j < JL_CL[i].Count; j++)
                //    {
                //        Console.Write(JL_CL[i][j] + " ");
                //    }
                //    Console.WriteLine();
                //}

                //Console.WriteLine("best J" + GRASP_result.best_J + "  M " + GRASP_result.best_J_machine + " time " + GRASP_result.best_J_op_time);

                current_machine_time[GRASP_result.best_J_machine] = GRASP_result.best_J_op_time;
                //Console.WriteLine("machine time");
                //for (int i = 0; i < current_machine_time.Length; i++)
                //{
                //    Console.WriteLine(current_machine_time[i]);
                //}
                final_sub_OS.Add(GRASP_result.best_J);
                //final_sub_MS.Add(GRASP_result.best_J_machine); ///modified0328
                int match_index = 0; ///modified0328
                //Console.WriteLine("SUB OS");
                //for(int i=0; i< sub_OS_string.Length; i++)
                //{
                //    Console.WriteLine("i"+ i+" "+sub_OS_string[i]);
                //}
                for (int i=0; i<sub_OS_string.Length; i++) ///modified0328
                {
                    if ((sub_OS_string[i] == GRASP_result.best_J) && (os_ms_match_machine_index[i] == false)) 
                    {
                        match_index = i;
                        os_ms_match_machine_index[i] = true;
                        break;
                    }
                }
                final_sub_MS[match_index]= GRASP_result.best_J_machine; ///modified0328
                //Console.WriteLine("@@ " + match_index + "  " + GRASP_result.best_J_machine);

                current_operation_count[GRASP_result.best_J]++;
                already_assign_op_count++;
               // Console.WriteLine();
            }

            for (int i = start_index; i <= end_index; i++) 
            {
                initial_MS_initial[i] = final_sub_MS[i - start_index];
                initial_OS_initial[i] = final_sub_OS[i - start_index];
                GC.Collect();
                //Console.WriteLine("MS " + final_sub_MS[i - start_index]);
                //Console.WriteLine("OS " + final_sub_OS[i - start_index]);

            }
            //Console.WriteLine();
            //for (int i = 0; i < initial_MS_initial.Count; i++)
            //{
            //    Console.Write(initial_MS_initial[i]);
            //}
            //Console.WriteLine();
            //for (int i = 0; i < initial_OS_initial.Length; i++)
            //{
            //    Console.Write(initial_OS_initial[i]);
            //}

        }

       
        public static (int best_J, int best_J_machine, double best_J_op_time) GRASP(int machine_amount, int job_amount, List<List<int>> JL_CL, double[] machine_current_time, List<int>current_operation_count)
        {
            List<List<double>> tmp_best = new List<List<double>>();

            //Console.WriteLine("enter method count op");
            //for(int i=0; i<current_operation_count.Count; i++)
            //{
            //    Console.WriteLine(current_operation_count[i]);
            //}
            //Console.WriteLine();
            for (int i = 0; i < JL_CL.Count; i++) 
            {
                if (JL_CL[i].Count != 0) 
                {
                    tmp_best.Add(new List<double>());
                    tmp_best[tmp_best.Count - 1].Add(i);
                    int min_index = 0;
                    double current_min = 9999; ;
                    for (int m = 0; m < GA.machine_amount_method(i, current_operation_count[i], read_data.job_operation_list, read_data.operation_avilable_machine); m++)
                    {
                        if ((machine_current_time[int.Parse(read_data.operation_avilable_machine[processing_time_find_index(i, current_operation_count[i], read_data.job_operation_list)][m]) - 1] + read_data.operation_time[processing_time_find_index(i, current_operation_count[i], read_data.job_operation_list)][m]) < current_min)
                        {
                            min_index = m;
                            current_min = (machine_current_time[int.Parse(read_data.operation_avilable_machine[processing_time_find_index(i, current_operation_count[i], read_data.job_operation_list)][m]) - 1] + read_data.operation_time[processing_time_find_index(i, current_operation_count[i], read_data.job_operation_list)][m]);
                        }
                    }
                    tmp_best[tmp_best.Count-1].Add(int.Parse(read_data.operation_avilable_machine[processing_time_find_index(i, current_operation_count[i], read_data.job_operation_list)][min_index]) - 1); //min machine
                    tmp_best[tmp_best.Count - 1].Add(current_min); //min current machine completion time
                }
                
            }
            GC.Collect();
            int min_op_time_index = 0;
            for (int i = 0; i < tmp_best.Count; i++) 
            {
                if (tmp_best[i][2] < tmp_best[min_op_time_index][2]) min_op_time_index = i;
            }
            //for(int i=0; i<tmp_best.Count; i++)
            //{
            //    Console.WriteLine("%% " + tmp_best[i][0] + " " + tmp_best[i][1] + " " + tmp_best[i][2]);
            //}
            int best_J = (int)tmp_best[min_op_time_index][0];

            //Console.WriteLine("best J" + best_J);

            return ((int)tmp_best[min_op_time_index][0], (int)tmp_best[min_op_time_index][1], tmp_best[min_op_time_index][2]);

        }

        public static (double[] machine_initial_time, int[] count_operation) initial_preprocessing (int[] OS_string_initial, int[] MS_string_initial, int machine_amount, int job_amount)
        {
            List<List<double>> machine_job = new List<List<double>>();

            for (int i = 0; i < machine_amount; i++)
            {
                machine_job.Add(new List<double>());
            }

            int[] count_operation = new int[job_amount]; // 紀錄job到哪個op
            for (int job = 0; job < job_amount; job++)
            {
                count_operation[job] = 0;
            }

            double[] total_processing_time = new double[machine_amount]; //紀錄每個machine目前的最大時間
            for (int machine = 0; machine < machine_amount; machine++)
            {
                total_processing_time[machine] = 0;
            }
            GC.Collect();
            //List<int> corresponding_machine_index = correspoinding_machine_index_func(OS_string_population[i], job_amount, MS_string_population[i]);
            for (int j = 0; j < MS_string_initial.Length; j++)
            {
                //Console.WriteLine("*** OS " + OS_string_population[i][j]+"   MS "+ MS_string_population[i][corresponding_machine_index[j]]);
                //Console.WriteLine("job: " + move[i][j] + " count_operation: " + count_operation[move[i][j]] + " machine_index: " + k);
                machine_job[MS_string_initial[j]].Add(OS_string_initial[j]);
                machine_job[MS_string_initial[j]].Add(count_operation[OS_string_initial[j]]);
                double processing_time = read_data.operation_time[processing_time_find_index(OS_string_initial[j], count_operation[OS_string_initial[j]], read_data.job_operation_list)] [GA.find_k_index(OS_string_initial[j], count_operation[OS_string_initial[j]], read_data.job_operation_list, MS_string_initial[j])];
                //Console.WriteLine(j + " " + MS_string_initial[j] + " " + processing_time_find_index(OS_string_initial[j], count_operation[OS_string_initial[j]], read_data.job_operation_list) + " " + GA.find_k_index(OS_string_initial[j], count_operation[OS_string_initial[j]], read_data.job_operation_list, MS_string_initial[j]) + "  " + processing_time);
                machine_job[MS_string_initial[j]].Add(processing_time);

                double cal = total_processing_time[MS_string_initial[j]] + processing_time;
                if (count_operation[OS_string_initial[j]] != 0) 
                {
                    for (int pi = 0; pi < machine_job.Count; pi++)
                    {
                        for (int pj = 0; pj < machine_job[pi].Count; pj += 4)
                        {
                            if (machine_job[pi][pj] == OS_string_initial[j] && machine_job[pi][pj + 1] == (count_operation[OS_string_initial[j]] - 1)) //快睡著了 QQQQQQQQQQQQQQQQQ
                            {
                                if (total_processing_time[MS_string_initial[j]] < machine_job[pi][pj + 3]) //跟前一個工序看
                                {
                                    double tmp_shift = (Math.Ceiling(machine_job[pi][pj + 3] / 8)) * 8;
                                    if (machine_job[pi][pj + 3] + processing_time > tmp_shift)
                                    {
                                        total_processing_time[MS_string_initial[j]] = tmp_shift + processing_time;
                                    }
                                    else total_processing_time[MS_string_initial[j]] = machine_job[pi][pj + 3] + processing_time;
                                }
                                else // based on machine directly + processing time
                                {
                                    double tmp_shift = (Math.Ceiling(total_processing_time[MS_string_initial[j]] / 8)) * 8;
                                    if (total_processing_time[MS_string_initial[j]] + processing_time > tmp_shift)
                                    {
                                        total_processing_time[MS_string_initial[j]] = tmp_shift + processing_time;
                                    }
                                    else total_processing_time[MS_string_initial[j]] += processing_time;
                                }
                            }
                        }
                    }
                }
                else
                {
                    total_processing_time[MS_string_initial[j]] += processing_time;
                }

                machine_job[MS_string_initial[j]].Add(total_processing_time[MS_string_initial[j]]);

                count_operation[OS_string_initial[j]]++;
                //Console.WriteLine("@@"+total_processing_time[0]);
            }
            GC.Collect();
            //for (int cas = 0; cas < machine_job.Count; cas++)
            //{
            //    Console.WriteLine("Case " + cas);
            //    for (int machine1 = 0; machine1 < machine_job[cas].Count; machine1++)
            //    {
            //        Console.WriteLine("Machine " + machine1);

            //        for (int machine2 = 0; machine2 < machine_job[cas][machine1].Count; machine2 += 4)
            //        {
            //            Console.WriteLine(machine_job[cas][machine1][machine2 + 0] + " " + machine_job[cas][machine1][machine2 + 1] + " " + machine_job[cas][machine1][machine2 + 2] + " " + machine_job[cas][machine1][machine2 + 3] + " ");
            //        }
            //        Console.WriteLine();
            //    }
            //}

            //for (int i = 0; i < machine_job.Count; i++)
            //{
            //    Console.WriteLine("machine " + i);
            //    for (int j = 0; j < machine_job[i].Count; j += 4)
            //    {
            //        Console.WriteLine(machine_job[i][j] + "  " + machine_job[i][j + 1] + "  " + machine_job[i][j + 2] + "  " + machine_job[i][j + 3]);
            //    }
            //}

            //Console.WriteLine("method total processng time");
            //for (int i = 0; i < total_processing_time.Length; i++)
            //{
            //    Console.WriteLine(total_processing_time[i]);
            //}

            //Console.WriteLine("method count operation");
            //for (int i = 0; i < count_operation.Length; i++)
            //{
            //    Console.WriteLine(count_operation[i]);
            //}
            //Console.Read();
            return (total_processing_time, count_operation);
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
