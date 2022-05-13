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
    class Program
    {
        static void Main(string[] args)
        {
            string[] database = new string[] { "data30.xlsx" };

            for (int i = 0; i < database.Length; i++)
            {
                var file = new FileInfo(database[i]);

                read_data.input_data(file);
               
                mathematical_model_multi_obj.model();
                mathematical_ouput_multi_obj.output_method(30);

                //建立GA + AFTER PREPROCESSING
                System.Diagnostics.Stopwatch time = new System.Diagnostics.Stopwatch();

                GA.model(false); /// Cut1 
                Console.WriteLine("GA END");

                collect_T_preprocessing.model(GA.GA_T_result, GA.GA_opt_cmax_shift);              
                GC.Collect();

                // For those exceed limit time -> unsolved
                GA_emp_feasibility_for_unsolved.model(GA.GA_T_result, GA.GA_opt_cmax_shift, false); /// Cut2 & Cut5
                GC.Collect();
                GA_emp_feasibility_op_group_for_unsolved.model(); /// Cut3
                GC.Collect();
                GA_emp_feasibility_emp_group_for_unsolved.model(); /// Cut4
                GC.Collect();

                //Solve integrated model
                cut_mathematical_model_multi_obj.model();
                cut_mathematical_ouput_multi_obj.output_method(20);
                Console.WriteLine("finally end");

            }

            Console.Read();
        }
    }
}
