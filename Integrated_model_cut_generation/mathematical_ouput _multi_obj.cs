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
    public class mathematical_ouput_multi_obj
    {
        public static void output_method(int index)
        {
            //string[] database_result = new string[10] { "database1_result.xlsx", "database2_result.xlsx", "database3_result.xlsx", "database4_result.xlsx", "database5_result.xlsx", "database6_result.xlsx", "database7_result.xlsx", "database8_result.xlsx", "database9_result.xlsx", "database10_result.xlsx" };
            //string[] database_result = new string[] { "M_result_new_database6.xlsx", "M_result_new_database7.xlsx", "M_result_new_database8.xlsx", "M_result_new_database9.xlsx", "M_result_new_database10.xlsx" };
            //string[] database_result = new string[] { "1127_mathe_data8.xlsx" };

            var output_file = new FileInfo("new_database"+index+ "_result.xlsx");

            using (var excel = new ExcelPackage())
            {
                List<List<double>> Machine_result = new List<List<double>>();
                List<List<double>> sorted_machine_result = new List<List<double>>();

                for (int i = 0; i < mathematical_model_multi_obj.machine_amount; i++)
                {
                    List<double> tmp = new List<double>();
                    Machine_result.Add(tmp);
                    sorted_machine_result.Add(tmp);
                }

                //Console.WriteLine();
                for (int i = 0; i < mathematical_model_multi_obj.job_amount; i++)
                {
                    for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
                    {
                        int m_amount = mathematical_model_multi_obj.machine_amount_method(i, j, read_data.job_operation_list, read_data.operation_avilable_machine);
                        for (int k = 0; k < m_amount; k++)
                        {
                            //Console.WriteLine("C" + i + j + k + " :" + c_result[i][j][k] + "     " + "\nX" + i + j + k + " :" + x_result[i][j][k] + "      " + "\nP" + i + j + k + " :" + operation_time[processing_time_find_index(i, j, job_operation_list)][k]);
                            if (Math.Round(mathematical_model_multi_obj.x_result[i][j][k]) == 1)
                            {
                                int machine_index = int.Parse(read_data.operation_avilable_machine[mathematical_model_multi_obj.processing_time_find_index(i, j, read_data.job_operation_list)][k]) - 1;
                                Machine_result[machine_index].Add(i);
                                Machine_result[machine_index].Add(j);
                                Machine_result[machine_index].Add(k);
                                Machine_result[machine_index].Add(mathematical_model_multi_obj.s_result[i][j][k]);
                                Machine_result[machine_index].Add(mathematical_model_multi_obj.c_result[i][j][k]);
                            }
                            //Console.WriteLine("X" + i + j + k + "   " + mathematical_model_multi_obj.x_result[i][j][k] + "       C" + i + j + k + "  " + "   " + mathematical_model_multi_obj.c_result[i][j][k]);

                        }
                        //Console.WriteLine();
                    }
                }

                // WORKSHEET 1
                var worksheet1_objective_value = excel.Workbook.Worksheets.Add("Objective Value");
                worksheet1_objective_value.Cells[1, 1].Value = "Makespan";
                worksheet1_objective_value.Cells[1, 2].Value = mathematical_model_multi_obj.makespan_obj_value;

                worksheet1_objective_value.Cells[2, 1].Value = "Labour Cost";
                worksheet1_objective_value.Cells[2, 2].Value = mathematical_model_multi_obj.labour_cost_obj_value;

                worksheet1_objective_value.Cells[3, 1].Value = "Running Time";
                worksheet1_objective_value.Cells[3, 2].Value = (mathematical_model_multi_obj.time.ElapsedMilliseconds);
                Console.WriteLine("Time: " + (mathematical_model_multi_obj.time.ElapsedMilliseconds)  + " seconds");


                // WORKSHEET 2
                var worksheet2_machine_result = excel.Workbook.Worksheets.Add("Machine Result");
                int row_index = 0;

                for (int i = 0; i < Machine_result.Count; i++)
                {
                    List<List<double>> tmp_machine_result = new List<List<double>>();
                    //Console.WriteLine("Machine " + (i + 1));
                    for (int j = 0; j < Machine_result[i].Count; j += 5)
                    {
                        List<double> tmp = new List<double>();
                        tmp.Add(Machine_result[i][j]);
                        tmp.Add(Machine_result[i][j + 1]);
                        tmp.Add(Machine_result[i][j + 2]);
                        tmp.Add(Machine_result[i][j + 3]);
                        tmp.Add(Machine_result[i][j + 4]);
                        //Console.WriteLine(Machine_result[i][j] + "  " + Machine_result[i][j + 1] + "  " + Machine_result[i][j + 2] + "  " + Machine_result[i][j + 3] + "  " + Machine_result[i][j + 4] + "  ");
                        tmp_machine_result.Add(tmp);
                    }
                    var sortedList = tmp_machine_result.OrderBy(x => x[3]);
                    tmp_machine_result = sortedList.ToList();
                    //Console.WriteLine("sorted");
                    row_index++;
                    worksheet2_machine_result.Cells[row_index, 1].Value = "Machine " + read_data.machine_information[i];
                    row_index++;
                    worksheet2_machine_result.Cells[row_index, 1].Value = "i";
                    worksheet2_machine_result.Cells[row_index, 2].Value = "j";
                    worksheet2_machine_result.Cells[row_index, 3].Value = "k";
                    worksheet2_machine_result.Cells[row_index, 4].Value = "Operation";
                    worksheet2_machine_result.Cells[row_index, 5].Value = "Starting time";
                    worksheet2_machine_result.Cells[row_index, 6].Value = "Completion time";
                    worksheet2_machine_result.Cells[row_index, 7].Value = "Shift";
                    worksheet2_machine_result.Cells[row_index, 8].Value = "Employee";
                    for (int a = 0; a < tmp_machine_result.Count; a++)
                    {
                        row_index++;
                        //Console.WriteLine(tmp_machine_result[a][0] + "  " + tmp_machine_result[a][1] + "  " + tmp_machine_result[a][2] + "  " + tmp_machine_result[a][3] + "  " + tmp_machine_result[a][4] + "  ");
                        worksheet2_machine_result.Cells[row_index, 1].Value = tmp_machine_result[a][0];
                        worksheet2_machine_result.Cells[row_index, 2].Value = tmp_machine_result[a][1];
                        worksheet2_machine_result.Cells[row_index, 3].Value = tmp_machine_result[a][2];
                        worksheet2_machine_result.Cells[row_index, 4].Value = read_data.job_operation_list[(int)tmp_machine_result[a][0]][(int)tmp_machine_result[a][1]];
                        worksheet2_machine_result.Cells[row_index, 5].Value = tmp_machine_result[a][3];
                        worksheet2_machine_result.Cells[row_index, 6].Value = tmp_machine_result[a][4];
                        for (int s = 0; s < mathematical_model_multi_obj.shift_amount; s++)
                        {
                            if (Math.Round(mathematical_model_multi_obj.t_result[(int)tmp_machine_result[a][0]][(int)tmp_machine_result[a][1]][s]) == 1)
                            {
                                worksheet2_machine_result.Cells[row_index, 7].Value = s;
                                //Console.WriteLine("T" + (int)tmp_machine_result[a][0] + " " + (int)tmp_machine_result[a][1] + " " + s);
                                int emp_count = mathematical_model_multi_obj.emp_amount[(int)tmp_machine_result[a][0]][(int)tmp_machine_result[a][1]].Count;
                                for (int e = 0; e < emp_count; e++)
                                {
                                    if (Math.Round(mathematical_model_multi_obj.e_result[(int)tmp_machine_result[a][0]][(int)tmp_machine_result[a][1]][e][s]) == 1)
                                    {
                                        worksheet2_machine_result.Cells[row_index, 8].Value = mathematical_model_multi_obj.emp_amount[(int)tmp_machine_result[a][0]][(int)tmp_machine_result[a][1]][e];
                                        //Console.WriteLine("E" + " " + e + " " + (int)tmp_machine_result[a][0] + " " + (int)tmp_machine_result[a][1] + " " + s);
                                    }
                                }
                            }
                        }
                    }
                    //Console.WriteLine();
                }


               // Console.WriteLine("T");
                //for (int i = 0; i < mathematical_model_multi_obj.job_amount; i++)
                //{
                //    for (int j = 0; j < read_data.job_operation_list[i].Count; j++)
                //    {
                //        for (int s = 0; s < mathematical_model_multi_obj.shift_amount; s++)
                //        {
                //            if (Math.Round(mathematical_model_multi_obj.t_result[i][j][s]) == 1)
                //            {
                //                Console.Write("T" + i + j + s + "  ");
                //                int emp_count = mathematical_model_multi_obj.emp_amount[i][j].Count;
                //                for (int e = 0; e < emp_count; e++)
                //                {
                //                    if (Math.Round(mathematical_model_multi_obj.e_result[i][j][e][s]) == 1)
                //                    {
                //                        Console.Write("E" + e + i + j + s + "   ");
                //                    }
                //                }
                //                Console.WriteLine();
                //            }
                //        }
                //    }
                //}
                var worksheet3_employee_result = excel.Workbook.Worksheets.Add("Employee Result");
                int row_index3 = 1;
                worksheet3_employee_result.Cells[row_index3, 1].Value = "Employee";
                worksheet3_employee_result.Cells[row_index3, 2].Value = "Shift";
                //Console.WriteLine("Z");
                for (int e = 0; e < mathematical_model_multi_obj.employee_amount; e++)
                {
                    row_index3++;
                    worksheet3_employee_result.Cells[row_index3, 1].Value = "Employee " + e;
                    int column_index3 = 1;
                    for (int s = 0; s < mathematical_model_multi_obj.shift_amount; s++)
                    {
                        if (Math.Round(mathematical_model_multi_obj.z_result[e][s]) == 1)
                        {
                            column_index3++;
                            //Console.WriteLine("Z" + e + s);
                            worksheet3_employee_result.Cells[row_index3, column_index3].Value = s;
                        }
                    }
                }

                //Console.WriteLine("Cmax" + mathematical_model_multi_obj.objective_value);

                excel.SaveAs(output_file);

            }


            //StreamWriter time_record = new StreamWriter("result_record.csv", true);
            //time_record.WriteLine(mathematical_model_multi_obj.objective_value + "," + (mathematical_model_multi_obj.time.ElapsedMilliseconds) / 1000 + " seconds");
            //time_record.Close();




        }



    }
}
