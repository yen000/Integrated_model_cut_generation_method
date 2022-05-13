using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
namespace Integrated_model_cut_generation
{
    class GA
    {
        public static List<List<List<int>>> mutation_permutation_result = new List<List<List<int>>>();
        public static int total_job_op_size;
        public static double GA_LB;

        public static List<List<double>> tabu_best_machine_job = new List<List<double>>(); //記錄每個組合的最佳
        //public static double tabu_best_cmax;
        //public static  int[] tabu_best_OS_string;
        //public static int[] tabu_best_MS_string;

        public static List<List<List<List<int>>>> GA_T_result = new List<List<List<List<int>>>>();
        public static List<double> GA_opt_cmax_shift = new List<double>();

        //public static StreamWriter opt_cmax_shift_record = new StreamWriter("opt_cmax_shift.csv");

        static public void model(bool initial_sol)
        {
            int job_amount = read_data.job_order_amount.Count;
            int machine_amount = read_data.machine_information.Count;

            int population_size =2000; //400 800
            double crossover_rate = 0.8; //0.8
            double mutation_rate = 0.02;  //0.1 0.06 (cr:0.8 / mu 0.02 / re:0.005)
            double replication_rate = 0.005; //0.02 
            int tournament_b = 2;
            int max_iter =400;  //200

            List<List<List<double>>> record_best_machine_job = new List<List<List<double>>>(); //記錄每個組合的最佳
            List<double> record_best_cmax = new List<double>();
            List<int[]> record_best_OS_string = new List<int[]>();
            List<int[]> record_best_MS_string = new List<int[]>();

            List<List<double>> processing_time_ratio = new List<List<double>>();
            //List<List<List<int>>> record_job_operation_machine = new List<List<List<int>>>();
            GC.Collect();
            int[] count_total_operation = new int[] { 0, 0, 0, 0, 0 };
            
            List<int> OS_string = new List<int>();

            Console.WriteLine(job_amount);
            #region initialize MS & OS intial
            for (int i = 0; i < job_amount; i++)
            {
                for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
                {
                    total_job_op_size++;
                    OS_string.Add(i); //為了之後產生job字串                       

                    switch (read_data.job_operation_list[i][j])
                    {
                        case "A":
                            count_total_operation[0]++;
                            break;
                        case "B":
                            count_total_operation[1]++;
                            break;
                        case "C":
                            count_total_operation[2]++;
                            break;
                        case "D":
                            count_total_operation[3]++;
                            break;
                        case "E":
                            count_total_operation[4]++;
                            break;
                    }
                }
            }
            OS_string.ToArray();
            for (int i = 0; i < read_data.operation_time.Count; i++)
            {
                processing_time_ratio.Add(new List<double>());
                //record_job_operation_machine.Add(new List<List<int>>());
                double tmp_total_rato = 0;
                for (int j = 0; j < read_data.operation_time[i].Count; j++)
                {
                    //record_job_operation_machine[i].Add(new List<int>());
                    processing_time_ratio[i].Add(1 / (double)read_data.operation_time[i][j]);
                    tmp_total_rato += (double)(1 / (double)read_data.operation_time[i][j]);
                }
                //Console.WriteLine("i " + i + " " + tmp_total_rato);
                processing_time_ratio[i].Add(tmp_total_rato);
            }

            for (int i = 0; i < processing_time_ratio.Count; i++)
            {
                for (int j = 0; j < processing_time_ratio[i].Count - 1; j++)
                {
                    processing_time_ratio[i][j] = (processing_time_ratio[i][j] / processing_time_ratio[i][processing_time_ratio[i].Count - 1]) * count_total_operation[i];

                    int tmp = (int)(processing_time_ratio[i][j]);
                    //Console.Write("operation " + i + "  " + count_total_operation[i] + "  tmp: " + tmp + "  # " + processing_time_ratio[i][j]);

                    if (processing_time_ratio[i][j] - tmp >= 0.5) processing_time_ratio[i][j] = Math.Round(processing_time_ratio[i][j]);
                    else processing_time_ratio[i][j] = Math.Floor(processing_time_ratio[i][j]);

                    //Console.Write("  after change : " + processing_time_ratio[i][j]);

                }
                //Console.WriteLine();
            }
            #endregion
            GC.Collect();
            /////////////////////////////////////////////////////////////////////////////////////////////////////////

            #region initialize MS & OS population
            // INITIALIZE  MS string /  OS string
            int[][][] job_assign_machine = new int[job_amount][][];  // assign machine 問題 xijk value
            int[] MS_string = new int[total_job_op_size];
            //List<List<List<int>>> record_best_job_assign_machine = new List<List<List<int>>>();

            int tmp_total_job_op_size = 0;
            for (int i = 0; i < job_amount; i++)
            {
                job_assign_machine[i] = new int[read_data.job_operation_list[i].Count][];
                //record_best_job_assign_machine.Add(new List<List<int>>());
                for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
                {
                    int m_amount = machine_amount_method(i, j, read_data.job_operation_list, read_data.operation_avilable_machine);
                    job_assign_machine[i][j] = new int[m_amount];
                    //record_best_job_assign_machine[i].Add(new List<int>());


                    for (int k = 0; k < m_amount; k++)
                    {
                        if (processing_time_ratio[processing_time_find_index(i, j, read_data.job_operation_list)][k] > 0)
                        {
                            job_assign_machine[i][j][k] = 1;
                            //Console.WriteLine("i j k " + i + " " + j + " " + k);
                            MS_string[tmp_total_job_op_size] = find_machine(i, j, read_data.job_operation_list, read_data.operation_avilable_machine)[k];
                            //record_job_operation_machine[processing_time_find_index(i, j, read_data.job_operation_list)][k].Add(i);
                            //record_job_operation_machine[processing_time_find_index(i, j, read_data.job_operation_list)][k].Add(j);
                            processing_time_ratio[processing_time_find_index(i, j, read_data.job_operation_list)][k] -= 1;
                            break;
                        }
                        else
                        {
                            job_assign_machine[i][j][k] = 0;
                            //record_best_job_assign_machine[i][j][k] = 0;
                        }
                    }

                    tmp_total_job_op_size++;

                }
            }

            int MS_initial_random_amount = (int)Math.Ceiling((double)total_job_op_size / 3);
            
            //Console.Write("Before\nOS string ");
            //for(int i=0; i< OS_string.Count; i++)
            //{
            //    Console.Write(OS_string[i]);
            //}
            //Console.Write("\n MS string ");
            //for (int i = 0; i < OS_string.Count; i++)
            //{
            //    Console.Write(MS_string[i]);
            //}
            //Console.WriteLine("\naAter");
            GA_initialization.model(MS_string.ToList(), OS_string.ToArray());
            MS_string = GA_initialization.initial_MS_initial.ToArray();
            OS_string = GA_initialization.initial_OS_initial.ToList();

            //Console.Write("\nOS string ");
            //for (int i = 0; i < OS_string.Count; i++)
            //{
            //    Console.Write(OS_string[i]);
            //}
            //Console.Write("\n MS string ");
            //for (int i = 0; i < OS_string.Count; i++)
            //{
            //    Console.Write(MS_string[i]);
            //}
            //Console.WriteLine();

            List<int[]> MS_string_population = new List<int[]>();
            MS_string_population.Add(MS_string);

            for(int i=0; i< population_size - 1; i++)
            {
                MS_string_population.Add(generate_MS_string(MS_string_population[i], MS_initial_random_amount, job_amount));
                //MS_string_population.Add(generate_MS_string2(MS_string_population[i], job_amount));
            }

            List<int[]> OS_string_population = new List<int[]>();
            OS_string_population.Add(OS_string.ToArray());
            for (int i = 0; i < population_size - 1; i++)
            {
                OS_string_population.Add(generate_OS_string(OS_string_population[i]));               
            }

            //for(int j=0; j< OS_string_population.Count; j++)
            //{
            //    Console.WriteLine("/////////////////");
            //    for (int i = 0; i < OS_string_population[j].Length; i++)
            //    {
            //        Console.Write(OS_string_population[j][i]);
            //    }
            //    Console.WriteLine();
            //    for (int i = 0; i < OS_string_population[j].Length; i++)
            //    {
            //        Console.Write(MS_string_population[j][i]);
            //    }
            //    Console.WriteLine();

            //}

            #endregion
            GC.Collect();
            for (int iter = 0; iter < max_iter + 1; iter++) 
            {
                Console.Write("\r/// GA Iter {0} ///", iter);
                #region selection
                //selection & caculate fitness
                #region calculate fitness
                // for calculating FITNESS
                List<List<List<double>>> machine_job = new List<List<List<double>>>(); //記錄每個組合的最後排程解 
                List<double> cmax_each_case = new List<double>();

                #region cal machine job (current slash)
                /*for (int i = 0; i < OS_string_population.Count; i++)
                {
                    machine_job.Add(new List<List<double>>());
                    for (int j = 0; j < machine_amount; j++)
                    {
                        machine_job[i].Add(new List<double>());
                    }
                }

                for (int i = 0; i < MS_string_population.Count; i++)
                {
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

                    for (int j = 0; j < MS_string_population[i].Length; j++)
                    {
                        //Console.WriteLine("job: " + move[i][j] + " count_operation: " + count_operation[move[i][j]] + " machine_index: " + k);
                        machine_job[i][MS_string_population[i][j]].Add(OS_string_population[i][j]);
                        machine_job[i][MS_string_population[i][j]].Add(count_operation[OS_string_population[i][j]]);
                        double processing_time = read_data.operation_time[processing_time_find_index(OS_string_population[i][j], count_operation[OS_string_population[i][j]], read_data.job_operation_list)][find_k_index(OS_string_population[i][j], count_operation[OS_string_population[i][j]], read_data.job_operation_list, MS_string_population[i][j])];
                        machine_job[i][MS_string_population[i][j]].Add(processing_time);

                        double cal = total_processing_time[MS_string_population[i][j]] + processing_time;
                        if (count_operation[OS_string_population[i][j]] != 0)
                        {
                            for (int pi = 0; pi < machine_job[i].Count; pi++)
                            {
                                for (int pj = 0; pj < machine_job[i][pi].Count; pj += 4)
                                {
                                    if (machine_job[i][pi][pj] == OS_string_population[i][j] && machine_job[i][pi][pj + 1] == (count_operation[OS_string_population[i][j]] - 1)) //快睡著了 QQQQQQQQQQQQQQQQQ
                                    {
                                        if (total_processing_time[MS_string_population[i][j]] < machine_job[i][pi][pj + 3]) //跟前一個工序看
                                        {
                                            double tmp_shift = (Math.Ceiling(machine_job[i][pi][pj + 3] / 8)) * 8;
                                            if (machine_job[i][pi][pj + 3] + processing_time > tmp_shift)
                                            {
                                                total_processing_time[MS_string_population[i][j]] = tmp_shift + processing_time;
                                            }
                                            else total_processing_time[MS_string_population[i][j]] = machine_job[i][pi][pj + 3] + processing_time;
                                        }
                                        else // based on machine directly + processing time
                                        {
                                            double tmp_shift = (Math.Ceiling(total_processing_time[MS_string_population[i][j]] / 8)) * 8;
                                            if (total_processing_time[MS_string_population[i][j]] + processing_time > tmp_shift)
                                            {
                                                total_processing_time[MS_string_population[i][j]] = tmp_shift + processing_time;
                                            }
                                            else total_processing_time[MS_string_population[i][j]] += processing_time;
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            total_processing_time[MS_string_population[i][j]] += processing_time;
                        }

                        machine_job[i][MS_string_population[i][j]].Add(total_processing_time[MS_string_population[i][j]]);

                        count_operation[OS_string_population[i][j]]++;
                    }

                    double cmax = 0;

                    for (int m = 0; m < machine_job[i].Count; m++)
                    {
                        if(total_processing_time[m]>cmax)
                        {
                            cmax = total_processing_time[m];
                        }
                    }
                    cmax_each_case.Add(cmax);
                }*/
                #endregion

                var cal_fitness_result = cal_fitness(OS_string_population, MS_string_population, machine_amount, job_amount);
                machine_job = cal_fitness_result.machine_job;
                cmax_each_case = cal_fitness_result.cmax_each_case;

                var result_machine_job = machine_job.ToArray();
                var result_cmax = cmax_each_case.ToArray();
                var result_OS_string_population = OS_string_population.ToArray();
                var result_MS_string_population = MS_string_population.ToArray();

                Array.Sort(result_cmax, result_machine_job);
                Array.Sort(cmax_each_case.ToArray(), result_OS_string_population);
                Array.Sort(cmax_each_case.ToArray(), result_MS_string_population);

                record_best_machine_job.Add(result_machine_job[0]);
                record_best_cmax.Add(result_cmax[0]);
                record_best_MS_string.Add(result_MS_string_population[0]);
                record_best_OS_string.Add(result_OS_string_population[0]);

                GC.Collect();

                if (iter > (max_iter / 2))
                {
                    //Console.WriteLine("\rcollect T iter: " + iter);
                    collect_T_result(result_machine_job.ToList(), result_cmax.ToList(), job_amount); //取後半的結果 //20220302
                }

                #region output 

                // OUTPUT RESULT
                //Console.WriteLine("^^^^^^^^^^^^^^^^^^^^^^^^^^^^^  ITER " + iter);
                //for (int i = 0; i < MS_string_population.Count; i++)
                //{
                //     Console.WriteLine("\n/////////\nMS");
                //     for (int j = 0; j < MS_string_population[i].Length; j++)
                //     {
                //         Console.Write(MS_string_population[i][j]);
                //     }
                //     Console.WriteLine("\nOS");
                //     for (int j = 0; j < OS_string_population[i].Length; j++)
                //     {
                //         Console.Write(OS_string_population[i][j]);
                //     }
                //     Console.WriteLine();
                //     for (int a = 0; a < machine_job[i].Count; a++)
                //     {
                //         Console.WriteLine("M " + a);
                //         for (int b = 0; b < machine_job[i][a].Count; b += 4)
                //         {
                //             Console.WriteLine(machine_job[i][a][b] + " " + machine_job[i][a][b + 1] + " " + machine_job[i][a][b + 2] + " " + machine_job[i][a][b + 3]);
                //         }

                //     }
                //     Console.WriteLine("cmax " + cmax_each_case[i]);
                //}
                #endregion

                #endregion

                // elitist selection
                int elitist_count =(int)(population_size * replication_rate);
                for (int i = 0; i < elitist_count; i++)
                {
                    for (int j = 0; j < MS_string_population[i].Length; j++)
                    {
                        MS_string_population[i][j] = result_MS_string_population[i][j];
                    }
                    for (int j = 0; j < OS_string_population[i].Length; j++)
                    {
                        OS_string_population[i][j] = result_OS_string_population[i][j];
                    }
                }

                // tournament selection
                List<List<int>> tournament_record = new List<List<int>>();
                for(int i= elitist_count; i<MS_string_population.Count; i++)
                {
                    label_tournament:
                    Random rnd = new Random();
                    int[] choose_competition = new int[tournament_b];
                    choose_competition[0] = rnd.Next(MS_string_population.Count);
                    
                    choose_competition[1] = -1;

                    while (choose_competition[1] == -1) 
                    {
                        int choose_index= rnd.Next(MS_string_population.Count);
                        if (choose_index != choose_competition[0]) choose_competition[1] = choose_index;
                    }

                    for(int t_index_i=0; t_index_i < tournament_record.Count; t_index_i++)
                    {
                        if ((choose_competition[0] == tournament_record[t_index_i][0] && choose_competition[1]== tournament_record[t_index_i][1]) || (choose_competition[1] == tournament_record[t_index_i][0] && choose_competition[0]== tournament_record[t_index_i][1]))
                        {
                            goto label_tournament;
                        }
                    }

                    //Console.WriteLine("0: "+choose_competition[0]+ "  1: " + choose_competition[1]);
                    tournament_record.Add(new List<int>() { choose_competition[0], choose_competition[1] });

                    if (cmax_each_case[choose_competition[0]] <= cmax_each_case[choose_competition[1]])
                    {
                        MS_string_population[i] = MS_string_population[choose_competition[0]];
                        OS_string_population[i] = OS_string_population[choose_competition[0]];
                    }
                    else
                    {
                        MS_string_population[i] = MS_string_population[choose_competition[1]];
                        OS_string_population[i] = OS_string_population[choose_competition[1]];
                    }
                }
                /*Console.WriteLine("****************************");
                for (int i = 0; i < MS_string_population.Count; i++)
                {
                    Console.WriteLine("\n/////////\nMS");
                    for (int j = 0; j < MS_string_population[i].Length; j++)
                    {
                        Console.Write(MS_string_population[i][j]);
                    }
                    Console.WriteLine("\nOS");
                    for (int j = 0; j < OS_string_population[i].Length; j++)
                    {
                        Console.Write(OS_string_population[i][j]);
                    }
               
                }*/

            #endregion

                if(iter< max_iter)
                {
                    #region crossover
                    // Crossover
                    // OS string crossover
                    // divide into two jobset
                    List<int> jobset1 = new List<int>();
                    while (jobset1.Count < (job_amount / 2))
                    {
                        Random rnd = new Random();
                        int job_index = rnd.Next(job_amount);

                        if (!(jobset1.Contains(job_index))) jobset1.Add(job_index);
                    }

                    for (int i=0; i<OS_string_population.Count; i+=2)
                    {
                        Random rnd_cross_rate = new Random();

                        if (rnd_cross_rate.NextDouble() < crossover_rate)
                        {
                            Random rnd = new Random();
                            int choose_function = rnd.Next(2);
                            if (choose_function == 0)
                            {
                                List<int[]> OS_crossover_result = OS_crossover_fuction1(OS_string_population[i], OS_string_population[i + 1], jobset1);
                                OS_string_population[i] = OS_crossover_result[0];
                                OS_string_population[i + 1] = OS_crossover_result[1];

                                List<int[]> MS_crossover_result = MS_crossover_fuction(MS_string_population[i], MS_string_population[i + 1]);
                                MS_string_population[i] = MS_crossover_result[0];
                                MS_string_population[i + 1] = MS_crossover_result[1];
                            }
                            else
                            {
                                List<int[]> OS_crossover_result = OS_crossover_fuction2(OS_string_population[i], OS_string_population[i + 1], jobset1, job_amount);
                                OS_string_population[i] = OS_crossover_result[0];
                                OS_string_population[i + 1] = OS_crossover_result[1];

                                List<int[]> MS_crossover_result = MS_crossover_fuction(MS_string_population[i], MS_string_population[i + 1]);
                                MS_string_population[i] = MS_crossover_result[0];
                                MS_string_population[i + 1] = MS_crossover_result[1];
                            }

                        }
                    }
                    #endregion
                    GC.Collect();
                    #region mutation
                    // mutation
                    for (int i = 0; i < OS_string_population.Count; i++)
                    {
                        Random rnd_mutation_rate = new Random();

                        if (rnd_mutation_rate.NextDouble() < mutation_rate)
                        {
                            Random rnd = new Random();
                            int choose_function = rnd.Next(2);
                            if (choose_function == 0)
                            {
                                MS_string_population[i] = generate_MS_string(MS_string_population[i], MS_string_population[i].Length / 2, job_amount);

                                List<int[]> mutation_result = mutation_func1(OS_string_population[i], MS_string_population[i], 1, 2);
                                OS_string_population[i] = mutation_result[0];
                                //MS_string_population[i] = mutation_result[1];
                            }
                            else 
                            {
                                MS_string_population[i] = generate_MS_string(MS_string_population[i], MS_string_population[i].Length / 2, job_amount);

                                List<int[]> mutation_result = mutation_func2(OS_string_population[i], MS_string_population[i], 3);
                                OS_string_population[i] = mutation_result[0];
                                //MS_string_population[i] = mutation_result[1];
                            }
 
                        }
                    }
                    #endregion
                   
                }
            }
            Console.WriteLine("\n^^^^^^^^^^^^ end ^^^^^^^^^^^^^");
            GC.Collect();
            //opt_cmax_shift_record.Close();
            #region final result
            var minValue = record_best_cmax.Min();
            int minIndex = record_best_cmax.IndexOf(minValue);
            GA_LB = minValue;

            StreamWriter cut1 = new StreamWriter("cut1.csv");
            cut1.WriteLine(GA_LB);
            cut1.Close();

            Console.WriteLine("min " + minValue);

            Console.WriteLine("\n/////////\nMS");
            for (int j = 0; j < record_best_MS_string[minIndex].Length; j++)
            {
                Console.Write(record_best_MS_string[minIndex][j]);
            }
            Console.WriteLine("\nOS");
            for (int j = 0; j < record_best_OS_string[minIndex].Length; j++)
            {
                Console.Write(record_best_OS_string[minIndex][j]);
            }
            Console.WriteLine();
            for (int a = 0; a < record_best_machine_job[minIndex].Count; a++)
            {
                Console.WriteLine("M " + a);
                for (int b = 0; b < record_best_machine_job[minIndex][a].Count; b += 4)
                {
                    Console.WriteLine(record_best_machine_job[minIndex][a][b] + " " + record_best_machine_job[minIndex][a][b + 1] + " " + record_best_machine_job[minIndex][a][b + 2] + " " + record_best_machine_job[minIndex][a][b + 3]);
                }

            }
            Console.WriteLine("cmax " + record_best_cmax[minIndex]);

            /*
            Console.WriteLine("////////////  tabu ///////////////");
            tabu_best_machine_job=record_best_machine_job[minIndex];
            tabu_best_cmax = record_best_cmax[minIndex];
            tabu_best_OS_string = record_best_OS_string[minIndex];
            tabu_best_MS_string = record_best_MS_string[minIndex];
            Console.WriteLine("\n/////////\nMS");
            for (int j = 0; j < tabu_best_MS_string.Length; j++)
            {
                Console.Write(tabu_best_MS_string[j]);
            }
            Console.WriteLine("\nOS");
            for (int j = 0; j < tabu_best_OS_string.Length; j++)
            {
                Console.Write(tabu_best_OS_string[j]);
            }
            Console.WriteLine();
            for (int a = 0; a < tabu_best_machine_job.Count; a++)
            {
                Console.WriteLine("M " + a);
                for (int b = 0; b < tabu_best_machine_job[a].Count; b += 4)
                {
                    Console.WriteLine(tabu_best_machine_job[a][b] + " " + tabu_best_machine_job[a][b + 1] + " " + tabu_best_machine_job[a][b + 2] + " " + tabu_best_machine_job[a][b + 3]);
                }

            }
            Console.WriteLine("cmax " + tabu_best_cmax);
            */

            #endregion

            #region t_result_output
            /*for (int iter=0; iter<GA_T_result.Count; iter++)
            {
                Console.WriteLine("-------------");
                for(int i=0; i<GA_T_result[iter].Count; i++)
                {
                    for(int j=0; j<GA_T_result[iter][i].Count; j++)
                    {
                        for(int k=0; k<GA_T_result[iter][i][j].Count; k++)
                        {
                            Console.WriteLine("T i j s " + i + "  " + j + "  " + k + "  :" + GA_T_result[iter][i][j][k]);
                        }
                    }
                }
            }*/
            #endregion

            //Console.Read();
        }

        public static void collect_T_result(List<List<List<double>>> result_machine_job, List<double> result_cmax, int job_amount)
        {
            List<int> cut_index = new List<int>();
            //for (int i = 0; i < result_machine_job.Count * 0.005; i++) 
            //{
            //    cut_index.Add(i);
            //}

            while (cut_index.Count < 15)  ///modified 0315 //10 15 20
            {
                Random rnd = new Random();
                int index = rnd.Next((int)(result_machine_job.Count * 0.01)); /// modified 220308 //0.005
                if (!(cut_index.Contains(index))) cut_index.Add(index);
            }

            for(int iter=0; iter< cut_index.Count; iter++)
            {
                GA_T_result.Add(new List<List<List<int>>>());
                double opt_cmax_shift = Math.Ceiling(result_cmax[cut_index[iter]] / 8);
                GA_opt_cmax_shift.Add(opt_cmax_shift);
                //opt_cmax_shift_record.WriteLine(opt_cmax_shift);

                for (int i = 0; i < job_amount; i++)
                {
                    GA_T_result[GA_T_result.Count - 1].Add(new List<List<int>>());
                    for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
                    {
                        GA_T_result[GA_T_result.Count - 1][i].Add(new List<int>());
                        for (int s = 0; s < opt_cmax_shift; s++)
                        {
                            GA_T_result[GA_T_result.Count - 1][i][j].Add(0);
                        }
                    }
                }

                for (int x = 0; x < result_machine_job[cut_index[iter]].Count; x++)  
                {
                    //Console.WriteLine("T Machine " + (x + 1));

                    for (int y = 0; y < result_machine_job[cut_index[iter]][x].Count; y += 4)
                    {
                        double tmp_shift = Math.Ceiling(result_machine_job[cut_index[iter]][x][y + 3] / 8) - 1; // job排完的best cmax  0-8 | 8-16 | 16-24 | 24-32

                        //Console.Write("## " + result_machine_job[cut_index[iter]][x][y] + " " + result_machine_job[cut_index[iter]][x][y + 1] + " " + result_machine_job[cut_index[iter]][x][y + 2] + " " + result_machine_job[cut_index[iter]][x][y + 3]);

                        //Console.WriteLine("  count tmp shift " + tmp_shift);
                        //Console.WriteLine("@@ " + all_T_result[all_T_result.Count - 1][(int)machine_job[current_cmax_index][x][y]][(int)machine_job[current_cmax_index][x][y + 1]].Count);
                        //Console.WriteLine("## " + all_T_result[all_T_result.Count - 1][(int)machine_job[current_cmax_index][x][y]][(int)machine_job[current_cmax_index][x][y + 1]][(int)tmp_shift]);

                        GA_T_result[GA_T_result.Count - 1][(int)result_machine_job[cut_index[iter]][x][y]][(int)result_machine_job[cut_index[iter]][x][y + 1]][(int)tmp_shift] = 1;

                        int k_index = find_k_index((int)result_machine_job[cut_index[iter]][x][y], (int)result_machine_job[cut_index[iter]][x][y + 1], read_data.job_operation_list, x);
                    }

                }
            }
        }
        public static List<List<int>> record_change_index(int[] current_combination, int combimnation_count, int combination_k)
        {
            List<List<int>> move_record = new List<List<int>>();
            List<string> total_index_list = new List<string>();
            for (int i = 0; i < current_combination.Length; i++)
            {
                total_index_list.Add(i.ToString());
            }

            while (move_record.Count < combimnation_count)
            {
                List<int> tmp_index = new List<int>();
                bool check1 = true;
                while (tmp_index.Count < combination_k)
                {
                    Random rnd = new Random();
                    int select_index = rnd.Next(total_index_list.Count);
                    bool check2 = true;
                    for (int i = 0; i < tmp_index.Count; i++)
                    {
                        if (tmp_index[i] == select_index)
                        {
                            check2 = false;
                            break;
                        }
                    }
                    if (check2) tmp_index.Add(select_index);
                }

                tmp_index.Sort();

                for (int i = 0; i < move_record.Count; i++)
                {
                    int count = 0;
                    for (int j = 0; j < move_record[i].Count; j++)
                    {
                        if (tmp_index[j] == move_record[i][j])
                        {
                            count++;
                        }
                    }

                    if (count == combination_k)
                    {
                        check1 = false;
                        break;
                    }
                }

                if (check1) move_record.Add(tmp_index);

            }

            /*Console.WriteLine("move record");
            for (int i = 0; i < move_record.Count; i++)
            {
                for (int j = 0; j < move_record[i].Count; j++)
                {
                    Console.Write(move_record[i][j] + " ");
                }
                Console.WriteLine();
            }*/
            return move_record;
        }
        public static int find_ML_string_index(int current_job, int count_operation)
        {
            int index = -1;
            for (int i=0; i<read_data.job_operation_list.Count; i++)
            {
                for(int j=0; j<read_data.job_operation_list[i].Count; j++)
                {
                    index++;
                    if ((i == current_job) && (j == count_operation)) goto label;
                }
            }
            label:
            return index;
        }
        public static (List<List<List<double>>> machine_job, List<double> cmax_each_case) cal_fitness(List<int[]> OS_string_population, List<int[]> MS_string_population, int machine_amount, int job_amount)
        {
            List<List<List<double>>> machine_job = new List<List<List<double>>>();
            List<double> cmax_each_case = new List<double>();            

            for (int i = 0; i < OS_string_population.Count; i++)
            {
                machine_job.Add(new List<List<double>>());
                for (int j = 0; j < machine_amount; j++)
                {
                    machine_job[i].Add(new List<double>());
                }
            }

            for (int i = 0; i < MS_string_population.Count; i++)
            {
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

                //List<int> corresponding_machine_index = correspoinding_machine_index_func(OS_string_population[i], job_amount, MS_string_population[i]);
                for (int j = 0; j < MS_string_population[i].Length; j++)
                {
                    //Console.WriteLine("*** OS " + OS_string_population[i][j]+"   MS "+ MS_string_population[i][corresponding_machine_index[j]]);
                    //Console.WriteLine("job: " + move[i][j] + " count_operation: " + count_operation[move[i][j]] + " machine_index: " + k);

                    int MS_string_index = MS_string_population[i][find_ML_string_index(OS_string_population[i][j], count_operation[OS_string_population[i][j]])];

                    machine_job[i][MS_string_index].Add(OS_string_population[i][j]);
                    machine_job[i][MS_string_index].Add(count_operation[OS_string_population[i][j]]);
                    double processing_time = read_data.operation_time[processing_time_find_index(OS_string_population[i][j], count_operation[OS_string_population[i][j]], read_data.job_operation_list)][find_k_index(OS_string_population[i][j], count_operation[OS_string_population[i][j]], read_data.job_operation_list, MS_string_index)];
                    machine_job[i][MS_string_index].Add(processing_time);

                    double cal = total_processing_time[MS_string_index] + processing_time;
                    if (count_operation[OS_string_population[i][j]] != 0)
                    {
                        for (int pi = 0; pi < machine_job[i].Count; pi++)
                        {
                            for (int pj = 0; pj < machine_job[i][pi].Count; pj += 4)
                            {
                                if (machine_job[i][pi][pj] == OS_string_population[i][j] && machine_job[i][pi][pj + 1] == (count_operation[OS_string_population[i][j]] - 1)) //快睡著了 QQQQQQQQQQQQQQQQQ
                                {
                                    if (total_processing_time[MS_string_index] < machine_job[i][pi][pj + 3]) //跟前一個工序看
                                    {
                                        double tmp_shift = (Math.Ceiling(machine_job[i][pi][pj + 3] / 8)) * 8;
                                        if (machine_job[i][pi][pj + 3] + processing_time > tmp_shift)
                                        {
                                            total_processing_time[MS_string_index] = tmp_shift + processing_time;
                                        }
                                        else total_processing_time[MS_string_index] = machine_job[i][pi][pj + 3] + processing_time;
                                    }
                                    else // based on machine directly + processing time
                                    {
                                        double tmp_shift = (Math.Ceiling(total_processing_time[MS_string_index] / 8)) * 8;
                                        if (total_processing_time[MS_string_index] + processing_time > tmp_shift)
                                        {
                                            total_processing_time[MS_string_index] = tmp_shift + processing_time;
                                        }
                                        else total_processing_time[MS_string_index] += processing_time;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        total_processing_time[MS_string_index] += processing_time;
                    }

                    machine_job[i][MS_string_index].Add(total_processing_time[MS_string_index]);

                    count_operation[OS_string_population[i][j]]++;
                }

                double cmax = 0;

                for (int m = 0; m < machine_job[i].Count; m++)
                {
                    if (total_processing_time[m] > cmax)
                    {
                        cmax = total_processing_time[m];
                    }
                }
                cmax_each_case.Add(cmax);
            }

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

            return (machine_job, cmax_each_case);
        }
        public static List<int> correspoinding_machine_index_func(int[] OS_p, int job_amount, int [] MS_p)
        {
            List<int> correspoinding_machine_index_func = new List<int>();
            int[] sequence = new int[OS_p.Length];
            int count = 0;
            for (int j = 0; j < job_amount; j++)
            {
                for (int i = 0; i < OS_p.Length; i++)
                {
                    if (OS_p[i] == j)
                    {
                        sequence[i]=count;
                        count++;
                    }

                }
            }


            /*Console.WriteLine("\nos string");
            for(int i=0; i<OS_p.Length; i++)
            {
                Console.Write(OS_p[i]);
            }

            Console.WriteLine("\nms string");
            for (int i = 0; i < MS_p.Length; i++)
            {
                Console.Write(MS_p[i]);
            }

            Console.WriteLine("\ncorrespoinding_machine_index_func\n");
            for(int i=0; i< sequence.Length; i++)
            {
                Console.Write(sequence[i]);
            }
            Console.WriteLine();*/
            return sequence.ToList();

        }
        public static List<int[]> mutation_func1(int[] OS_p, int[] MS_p, int combimnation_count, int combination_k)
        {
            List<int[]> mutation_func = new List<int[]>();
          
            List<List<int>> move_record = new List<List<int>>();
            List<string> total_index_list = new List<string>();
            for (int i = 0; i < OS_p.Length; i++)
            {
                total_index_list.Add(i.ToString());
            }

            #region change
            while (move_record.Count < combimnation_count)
            {
                List<int> tmp_index = new List<int>();
                bool check1 = true;
                while (tmp_index.Count < combination_k)
                {
                    Random rnd = new Random();
                    int select_index = rnd.Next(total_index_list.Count);
                    bool check2 = true;
                    for (int i = 0; i < tmp_index.Count; i++)
                    {
                        if (tmp_index[i] == select_index)
                        {
                            check2 = false;
                            break;
                        }
                    }
                    if (check2) tmp_index.Add(select_index);
                }

                tmp_index.Sort();

                for (int i = 0; i < move_record.Count; i++)
                {
                    int count = 0;
                    for (int j = 0; j < move_record[i].Count; j++)
                    {
                        if (tmp_index[j] == move_record[i][j])
                        {
                            count++;
                        }
                    }

                    if (count == combination_k)
                    {
                        check1 = false;
                        break;
                    }
                }

                if (check1) move_record.Add(tmp_index);

            }
            #endregion

            /*Console.WriteLine("move record");
            for (int i = 0; i < move_record.Count; i++)
            {
                for (int j = 0; j < move_record[i].Count; j++)
                {
                    Console.Write(move_record[i][j] + " ");
                }
                Console.WriteLine();
            }
            */
          
            for (int i = 0; i < move_record.Count; i++)
            {
                // Console.WriteLine(move_combination[i][0]+": "+current_sol[move_combination[i][0]] + "  " + move_combination[i][1] + ": " + current_sol[move_combination[i][1]]);
                List<int> mutation_OS = OS_p.ToList();
                List<int> mutation_MS = MS_p.ToList();

                for (int j = 0; j < move_record[i].Count; j++)
                {
                    mutation_OS[move_record[i][j]] = OS_p[move_record[i][move_record[i].Count - 1 - j]];
                    mutation_MS[move_record[i][j]] = MS_p[move_record[i][move_record[i].Count - 1 - j]];

                    //Console.WriteLine("************************************" + move_record[i][j] + "      " + move_record[i][move_record[i].Count - 1 - j]);
                }

                mutation_func.Add(mutation_OS.ToArray());
                mutation_func.Add(mutation_MS.ToArray());

            }
            return mutation_func;
        }
        public static List<int[]> mutation_func2(int[] OS_p, int[] MS_p, int combimnation_count)
        {
            List<int[]> mutation_func2 = new List<int[]>();

            List<int> permu_index = new List<int>();
            while (permu_index.Count < combimnation_count)
            {
                Random rnd_ = new Random();
                int index = rnd_.Next(OS_p.Length);

                for (int i = 0; i < permu_index.Count; i++)
                {
                    if (index == permu_index[i]) goto label;
                }

                permu_index.Add(index);
            label:
                int a = 0;
            }

            permu_index.Sort();

            String str = "012";
            int n = str.Length;
            mutation_permutation_result.Add(new List<List<int>>());
            mutation_permutation_result.Add(new List<List<int>>());
            permute(str, 0, n - 1, OS_p, MS_p, permu_index);

            Random rnd = new Random();
            int choose_mutation_index = rnd.Next(mutation_permutation_result[0].Count);

            mutation_func2.Add(mutation_permutation_result[0][choose_mutation_index].ToArray());
            mutation_func2.Add(mutation_permutation_result[1][choose_mutation_index].ToArray());

            return mutation_func2;
        }
        private static void permute(String str, int l, int r, int [] result_OS, int[] result_MS, List<int> change_index)
        {
            if (l == r)
            {
                //Console.WriteLine(str);

                var tmp_list_OS = result_OS.ToList();
                var tmp_list_MS = result_MS.ToList();

                List<int> tmp_change_OS = new List<int>();
                List<int> tmp_change_MS = new List<int>();
                for (int i = 0; i < change_index.Count; i++)
                {
                    //Console.Write(change_index[int.Parse((str[i].ToString()))]);
                    tmp_change_OS.Add(tmp_list_OS[change_index[int.Parse((str[i].ToString()))]]);
                    tmp_change_MS.Add(tmp_list_MS[change_index[int.Parse((str[i].ToString()))]]);

                }

                for (int i = 0; i < tmp_change_OS.Count; i++)
                {
                    tmp_list_OS[change_index[i]] = tmp_change_OS[i];
                    tmp_list_MS[change_index[i]] = tmp_change_MS[i];
                }
                //Console.WriteLine();
                mutation_permutation_result.Add(new List<List<int>>());
                mutation_permutation_result.Add(new List<List<int>>());
                mutation_permutation_result[0].Add(tmp_list_OS);
                mutation_permutation_result[1].Add(tmp_list_MS);

            }
            else
            {
                for (int i = l; i <= r; i++)
                {
                    str = swap(str, l, i);
                    permute(str, l + 1, r, result_OS, result_MS, change_index);
                    str = swap(str, l, i);
                }
            }
        }
        public static String swap(String a, int i, int j)
        {
            char temp;
            char[] charArray = a.ToCharArray();
            temp = charArray[i];
            charArray[i] = charArray[j];
            charArray[j] = temp;
            string s = new string(charArray);
            return s;
        }
        public static List<int[]> MS_crossover_fuction(int[] p1, int[] p2)
        {
            List<int[]> MS_crossover_fuction = new List<int[]>();
            var parent1 = p1.ToArray();
            var parent2 = p2.ToArray();
            MS_crossover_fuction.Add(parent1);
            MS_crossover_fuction.Add(parent2);

            int[] position = new int[2];
            Random rnd = new Random();
            position[0] = rnd.Next(p1.Length);
            while(true)
            {
                position[1] = rnd.Next(p1.Length);
                if (position[1] != position[0]) break;
            }

            Array.Sort(position);

            for (int i = position[0]; i <= position[1]; i++)
            {
                MS_crossover_fuction[0][i] = p2[i];
            }

            for (int i = position[0]; i <= position[1]; i++)
            {
                MS_crossover_fuction[1][i] = p1[i];
            }

            /*Console.WriteLine("pos1 " + position[0] + " pos2 " + position[1]);

            Console.WriteLine("\np1");
            for(int i=0; i<p1.Length; i++)
            {
                Console.Write(p1[i]);
            }
            Console.WriteLine("\np2");
            for (int i = 0; i < p2.Length; i++)
            {
                Console.Write(p2[i]);
            }
            Console.WriteLine("\nos1");
            for (int i = 0; i < MS_crossover_fuction[0].Length; i++)
            {
                Console.Write(MS_crossover_fuction[0][i]);
            }
            Console.WriteLine("\nos2");
            for (int i = 0; i < MS_crossover_fuction[1].Length; i++)
            {
                Console.Write(MS_crossover_fuction[1][i]);
            }
            Console.WriteLine();
            */
            
            return MS_crossover_fuction;
        }
        public static List<int[]> OS_crossover_fuction1(int[] p1, int[] p2, List<int> jobset1)
        {
            List<int[]> OS_crossover_fuction = new List<int[]>();
            List<int> p1_rest = new List<int>();
            List<int> p2_rest = new List<int>();
            var parent1 = p1.ToArray();
            var parent2 = p2.ToArray();
            OS_crossover_fuction.Add(parent1);
            OS_crossover_fuction.Add(parent2);

            for (int i = 0; i < OS_crossover_fuction[0].Length; i++)
            {
                bool check1 = false, check2 = false;
                for (int a = 0; a < jobset1.Count; a++)
                {
                    if (OS_crossover_fuction[0][i] == jobset1[a])
                    {
                        check1 = true;
                    }

                    if (OS_crossover_fuction[1][i] == jobset1[a])
                    {
                        check2 = true;
                    }

                    if (check1 && check2) break;
                }
                if (!check1)
                {
                    p1_rest.Add(p1[i]);
                    OS_crossover_fuction[0][i] = -1;
                }
                if (!check2)
                {
                    p2_rest.Add(p2[i]);
                    OS_crossover_fuction[1][i] = -1;
                }
            }
            int tmp_index1 = 0;
            for (int i = 0; i < OS_crossover_fuction[0].Length; i++)
            {
                if (OS_crossover_fuction[0][i] == -1)
                {
                    OS_crossover_fuction[0][i] = p2_rest[tmp_index1];
                    tmp_index1++;
                }
            }

            int tmp_index2 = 0;
            for (int i = 0; i < OS_crossover_fuction[1].Length; i++)
            {
                if (OS_crossover_fuction[1][i] == -1)
                {
                    OS_crossover_fuction[1][i] = p1_rest[tmp_index2];
                    tmp_index2++;
                }
            }

            /*Console.WriteLine("job set");
            for(int a=0; a<jobset1.Count; a++)
            {
                Console.Write(jobset1[a]);
            }

            Console.WriteLine("\np1");
            for(int i=0; i<p1.Length; i++)
            {
                Console.Write(p1[i]);
            }
            Console.WriteLine("\np2");
            for (int i = 0; i < p2.Length; i++)
            {
                Console.Write(p2[i]);
            }
            Console.WriteLine("\nos1");
            for (int i = 0; i < OS_crossover_fuction[0].Length; i++)
            {
                Console.Write(OS_crossover_fuction[0][i]);
            }
            Console.WriteLine("\nos2");
            for (int i = 0; i < OS_crossover_fuction[1].Length; i++)
            {
                Console.Write(OS_crossover_fuction[1][i]);
            }
            Console.WriteLine();
            */
            return OS_crossover_fuction;
        }
        public static List<int[]> OS_crossover_fuction2(int[] p1, int[] p2, List<int> jobset1,int job_amount)
        {
            List<int[]> OS_crossover_fuction = new List<int[]>();
            List<int> p1_rest = new List<int>();
            List<int> p2_rest = new List<int>();
            var parent1 = p1.ToArray();
            var parent2 = p2.ToArray();
            OS_crossover_fuction.Add(parent1);
            OS_crossover_fuction.Add(parent2);

            List<int> jobset2 = new List<int>();
            for(int i=0; i<job_amount; i++)
            {
                bool check = true;
                for(int a=0; a<jobset1.Count; a++)
                {
                    if(i==jobset1[a])
                    {
                        check = false;
                        break;
                    }
                }
                if (check) jobset2.Add(i);
            }
            /*Console.WriteLine("jobset2");
            for(int i=0; i<jobset2.Count; i++)
            {
                Console.Write(jobset2[i]);
            }
            Console.WriteLine("\njobset1");

            for (int i=0; i<jobset1.Count; i++)
            {
                Console.Write(jobset1[i]);
            }
            */

            for (int i = 0; i < OS_crossover_fuction[0].Length; i++)
            {
                bool check2_1 = false;
                for (int a = 0; a < jobset2.Count; a++)
                {
                    if (OS_crossover_fuction[0][i] == jobset1[a])
                    {
                        check2_1 = true;
                    }
                    if (check2_1) break;

                }

                if (check2_1)
                {
                    p1_rest.Add(p1[i]); //22
                }
                else
                {
                    OS_crossover_fuction[0][i] = -1;

                }


                bool check1_2 = false;
                for (int a = 0; a < jobset1.Count; a++)
                {
                    if (OS_crossover_fuction[1][i] == jobset2[a])
                    {
                        check1_2 = true;
                    }

                    if (check1_2) break;
                }
               
                if (check1_2)
                {
                    p2_rest.Add(p2[i]); //3131
                }
                else
                {
                    OS_crossover_fuction[1][i] = -1;
                }



            }
            int tmp_index1 = 0;
            for (int i = 0; i < OS_crossover_fuction[0].Length; i++)
            {
                if (OS_crossover_fuction[0][i] == -1)
                {
                    OS_crossover_fuction[0][i] = p2_rest[tmp_index1];
                    tmp_index1++;
                }
            }

            int tmp_index2 = 0;
            for (int i = 0; i < OS_crossover_fuction[1].Length; i++)
            {
                if (OS_crossover_fuction[1][i] == -1)
                {
                    OS_crossover_fuction[1][i] = p1_rest[tmp_index2];
                    tmp_index2++;
                }
            }

            /*Console.WriteLine("job set");
            for (int a = 0; a < jobset1.Count; a++)
            {
                Console.Write(jobset1[a]);
            }

            Console.WriteLine("\np1");
            for (int i = 0; i < p1.Length; i++)
            {
                Console.Write(p1[i]);
            }
            Console.WriteLine("\np2");
            for (int i = 0; i < p2.Length; i++)
            {
                Console.Write(p2[i]);
            }
            Console.WriteLine("\nos1");
            for (int i = 0; i < OS_crossover_fuction[0].Length; i++)
            {
                Console.Write(OS_crossover_fuction[0][i]);
            }
            Console.WriteLine("\nos2");
            for (int i = 0; i < OS_crossover_fuction[1].Length; i++)
            {
                Console.Write(OS_crossover_fuction[1][i]);
            }
            Console.WriteLine();
            */
            return OS_crossover_fuction;
        }
        public static int[] generate_OS_string(int[] parent)
        {
            Random rd = new Random();
            int choose_mehod = rd.Next(2);

            if (choose_mehod == 0) 
            {
                var tmp = parent.ToList();
                List<int> initial_parent = new List<int>();

                //Console.WriteLine(tmp.Count);
                while (tmp.Count != 0)
                {
                    Random rnd = new Random();
                    int index = rnd.Next(tmp.Count);
                    initial_parent.Add(tmp[index]);
                    tmp.RemoveAt(index);
                }

                return initial_parent.ToArray();
            }
            else
            {
                var new_parent = parent.ToList();
                var new_parent_tmp = parent.ToList();

                List<int> exchange_index = cyclic_exchange_index(parent.Length);
               
                for (int i = 0; i < exchange_index.Count - 1; i++)
                {
                    new_parent[exchange_index[i + 1]] = new_parent_tmp[exchange_index[i]];
                }

                return new_parent.ToArray();
            }
        }
        public static List<int> cyclic_exchange_index(int parent_num) /// MODIFIED
        {
            List<int> cyclic_exchange_index = new List<int>();
            int set_element_num = 3;//設定Set元素個數為3
            int tmp_parent_num = parent_num;
            int tmp = 0;
            while (parent_num > set_element_num)
            {
                Random rd = new Random();
                cyclic_exchange_index.Add(3 * tmp + rd.Next(set_element_num));
                parent_num -= set_element_num;
                tmp++;
            }
            Random rd_last = new Random();
            cyclic_exchange_index.Add(3 * tmp + rd_last.Next(parent_num));
            cyclic_exchange_index.Add(cyclic_exchange_index[0]);

            return cyclic_exchange_index;
        }

        public static int[] generate_MS_string(int[] MS_string, int random_amount, int job_amount)
        {
            var generate_MS_string = MS_string.ToArray();

            List<int> random_index = new List<int>();
            while(random_index.Count<random_amount)
            {
                Random rnd = new Random();
                int index = rnd.Next(generate_MS_string.Length);
                bool check = true;

                for (int i = 0; i < random_index.Count; i++)
                {
                    if (random_index[i] == index)
                    {
                        check = false;
                        break;
                    }
                }

                if (check) random_index.Add(index);
            }
            random_index.Sort();
           
            int tmp_job_op = 0, tmp_random_index=0;

            for (int i = 0; i < job_amount; i++)
            {
                for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
                {
                    if (tmp_job_op == random_index[tmp_random_index]) 
                    {
                        tmp_random_index++;
                        int m_amount = machine_amount_method(i, j, read_data.job_operation_list, read_data.operation_avilable_machine);
                        int mach_k;

                        while (true)
                        {
                            Random mach_rnd = new Random();
                            mach_k = mach_rnd.Next(m_amount);
                            if(find_machine(i, j, read_data.job_operation_list, read_data.operation_avilable_machine)[mach_k]!= generate_MS_string[tmp_job_op])
                            {
                                generate_MS_string[tmp_job_op] = find_machine(i, j, read_data.job_operation_list, read_data.operation_avilable_machine)[mach_k];
                                break;
                            }
                        }
                    }
                    tmp_job_op++;
                    if (tmp_random_index == random_index.Count) goto label;
                }
            }

        label:            
            return generate_MS_string;
        }
        public static int[] generate_MS_string2(int[] MS_string, int job_amount)
        {
            var generate_MS_string = MS_string.ToArray();

            /* List<int> random_index = new List<int>();
             while (random_index.Count < job_amount)
             {
                 Random rnd = new Random();
                 int index = rnd.Next(generate_MS_string.Length);
                 bool check = true;

                 for (int i = 0; i < random_index.Count; i++)
                 {
                     if (random_index[i] == index)
                     {
                         check = false;
                         break;
                     }
                 }

                 if (check) random_index.Add(index);
             }
             random_index.Sort();
            */
            int tmp_job_op = 0;
            for (int i = 0; i < job_amount; i++)
            {
                for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
                {
                    int m_amount = machine_amount_method(i, j, read_data.job_operation_list, read_data.operation_avilable_machine);
                    int mach_k;

                    while (true)
                    {
                        Random mach_rnd = new Random();
                        mach_k = mach_rnd.Next(m_amount);
                        if (find_machine(i, j, read_data.job_operation_list, read_data.operation_avilable_machine)[mach_k] != generate_MS_string[tmp_job_op])
                        {
                            generate_MS_string[tmp_job_op] = find_machine(i, j, read_data.job_operation_list, read_data.operation_avilable_machine)[mach_k];
                            break;
                        }
                    }
                    tmp_job_op++;
                }
            }

            return generate_MS_string;
        }
        public static int find_k_index(int job, int operation, List<List<string>> job_operation_list, int machine)
        {
            int k_index = 0;
            switch (job_operation_list[job][operation])
            {
                case "A":
                    for (int i = 0; i < read_data.operation_avilable_machine[0].Count; i++)
                    {
                        if (int.Parse(read_data.operation_avilable_machine[0][i]) == (machine + 1))
                        {
                            k_index = i;
                            break;
                        }
                    }
                    break;
                case "B":
                    for (int i = 0; i < read_data.operation_avilable_machine[1].Count; i++)
                    {
                        if (int.Parse(read_data.operation_avilable_machine[1][i]) == (machine + 1))
                        {
                            k_index = i;
                            break;
                        }
                    }
                    break;
                case "C":
                    for (int i = 0; i < read_data.operation_avilable_machine[2].Count; i++)
                    {
                        if (int.Parse(read_data.operation_avilable_machine[2][i]) == (machine + 1))
                        {
                            k_index = i;
                            break;
                        }
                    }
                    break;
                case "D":
                    for (int i = 0; i < read_data.operation_avilable_machine[3].Count; i++)
                    {
                        if (int.Parse(read_data.operation_avilable_machine[3][i]) == (machine + 1))
                        {
                            k_index = i;
                            break;
                        }
                    }
                    break;
                case "E":
                    for (int i = 0; i < read_data.operation_avilable_machine[4].Count; i++)
                    {
                        if (int.Parse(read_data.operation_avilable_machine[4][i]) == (machine + 1))
                        {
                            k_index = i;
                            break;
                        }
                    }
                    break;
            }
            return k_index;
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
        public static List<int> find_machine(int job, int operation, List<List<string>> job_operation_list, List<List<string>> operation_avilable_machine)
        {
            List<int> find_machine = new List<int>();
            int tmp1 = 0;
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

            for (int i = 0; i < operation_avilable_machine[tmp1].Count; i++)
            {
                find_machine.Add((int.Parse(operation_avilable_machine[tmp1][i])) - 1);
            }

            return (find_machine);
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
       

        /*public static List<int[]> mutation_fuction(int[] OS_p, int[] MS_p, int change_num)
        {
            List<int[]> mutation_fuction = new List<int[]>();
            mutation_fuction.Add(OS_p.ToArray());
            mutation_fuction.Add(MS_p.ToArray());


            List<string> total_index_list = new List<string>();
            for (int i = 0; i < OS_p.Length; i++)
            {
                total_index_list.Add(i.ToString());
            }

            List<int> choose_index = new List<int>();
            while (choose_index.Count < change_num)
            {
                Random rnd = new Random();
                int select_index = rnd.Next(total_index_list.Count);
                bool check2 = true;
                for (int i = 0; i < choose_index.Count; i++)
                {
                    if (choose_index[i] == select_index)
                    {
                        check2 = false;
                        break;
                    }
                }
                if (check2) choose_index.Add(select_index);
            }

            choose_index.Sort();
            Console.Write("\nparent os");
            for (int i = 0; i < OS_p.Length; i++)
            {
                Console.Write(OS_p[i]);
            }


            Console.Write("\nparent ms");
            for (int i = 0; i < MS_p.Length; i++)
            {
                Console.Write(MS_p[i]);
            }

            Console.Write("\nafter mutation os");
            for (int i = 0; i < OS_p.Length; i++)
            {
                Console.Write(mutation_fuction[0][i]);
            }

            Console.Write("\nafter mutation ms");
            for (int i = 0; i < OS_p.Length; i++)
            {
                Console.Write(mutation_fuction[1][i]);
            }
            Console.WriteLine();

            for (int j = 0; j < choose_index.Count; j++)
            {
                Console.WriteLine(choose_index[j] + "    " + (choose_index.Count - 1 - j));
                mutation_fuction[0][choose_index[j]] = OS_p[choose_index.Count - 1 - j];
                //tmp_current_sol[move_combination[i][j]] = current_sol[  move_combination[i][move_combination[i].Count - 1 - j]   ];
                // tmp_current_sol[move_combination[i][j]] = current_sol[  move_combination[i][move_combination[i].Count - 1 - j]   ];

                mutation_fuction[1][choose_index[j]] = MS_p[choose_index.Count - 1 - j];
            }

            Console.Write("\nparent os");
            for (int i = 0; i < OS_p.Length; i++)
            {
                Console.Write(OS_p[i]);
            }


            Console.Write("\nparent ms");
            for (int i = 0; i < MS_p.Length; i++)
            {
                Console.Write(MS_p[i]);
            }

            Console.Write("\nafter mutation os");
            for (int i = 0; i < OS_p.Length; i++)
            {
                Console.Write(mutation_fuction[0][i]);
            }

            Console.Write("\nafter mutation ms");
            for (int i = 0; i < OS_p.Length; i++)
            {
                Console.Write(mutation_fuction[1][i]);
            }
            return mutation_fuction;
        }*/
    }
}
