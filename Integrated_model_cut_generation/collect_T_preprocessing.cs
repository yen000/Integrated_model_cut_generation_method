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
    class collect_T_preprocessing
    {
        public static void model(List<List<List<List<int>>>> T_result, List<double> opt_cmax_shift)
        {
            int job_amount= read_data.job_order_amount.Count;

            StreamWriter collect_tijs = new StreamWriter("collect_tijs.csv");

            for (int iter = 0; iter < T_result.Count; iter++)
            {
                int shift_amount = (int)opt_cmax_shift[iter];
                for (int i = 0; i < job_amount; i++)
                {
                    for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
                    {
                        for (int s = 0; s < shift_amount; s++)
                        {
                            if (T_result[iter][i][j][s] == 1) collect_tijs.Write(i + "," + j + "," + s + ",");
                        }
                    }
                }

                collect_tijs.WriteLine();
                //Console.WriteLine("\rFirst stage iter: " + iter);
                GC.Collect();
            }
            Console.WriteLine("Before: " + T_result.Count);
            collect_tijs.Close();

            StreamReader Tijs = new StreamReader("collect_tijs.csv");
            List<string> Tijs_read_data= new List<string>();
            string data="";

            while ((data = Tijs.ReadLine()) != null) 
            {
                Tijs_read_data.Add(data);
            }
            Tijs.Close();

            for (int i = 0; i < Tijs_read_data.Count; i++) 
            {
                for (int j = Tijs_read_data.Count - 1; j > i; j--) 
                {
                    if (Tijs_read_data[i] == Tijs_read_data[j])
                    {
                        Tijs_read_data.RemoveAt(j);
                        GA.GA_T_result.RemoveAt(j);
                        GA.GA_opt_cmax_shift.RemoveAt(j);
                    }
                }
            }

            StreamWriter Tijs_update = new StreamWriter("after_preprocessing_tijs.csv");

            int iter_begin = 0;

            //if (Tijs_read_data.Count > 1100) /// modified 0327
            //{
            //    iter_begin = Tijs_read_data.Count - 1100;
            //}

            int count_iter = 0;
            for(int iter= iter_begin; iter<Tijs_read_data.Count; iter++)
            {
                count_iter++;
                Tijs_update.WriteLine(Tijs_read_data[iter]);

                //Console.WriteLine(string.Join("", Tijs_read_data[iter].Split(',')));
                

                //int tmp_shift = (int)GA.GA_opt_cmax_shift[iter];
                //for (int i = 0; i < GA.GA_T_result[iter].Count; i++)
                //{
                //    for(int j=0; j<GA.GA_T_result[iter][i].Count; j++)
                //    {
                //        for(int s=0; s<tmp_shift; s++)
                //        {
                //            if(GA.GA_T_result[iter][i][j][s]==1)
                //            {
                //                Console.Write(i + "" + j + "" + s);
                //            }
                //        }
                //    }
                //}
                //Console.WriteLine();
                //Console.WriteLine("////");
            }
            
            Tijs_update.Close();
            Console.WriteLine("After "+Tijs_read_data.Count);

            StreamWriter total_iter = new StreamWriter("total_iter.csv");
            total_iter.WriteLine(count_iter);
            total_iter.Close();
        }
    }
}
