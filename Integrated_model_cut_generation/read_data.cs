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
    class read_data
    {
        public static List<int> job_order_amount = new List<int>();
        public static List<List<string>> job_operation_list = new List<List<string>>();
        public static List<List<string>> operation_avilable_machine = new List<List<string>>();
        public static List<List<int>> operation_time = new List<List<int>>();
        public static List<string> machine_information = new List<string>();
        public static List<List<string>> employee_competence_operation = new List<List<string>>();
        public static List<List<int>> day_off_list = new List<List<int>>();
        public static List<double> labour_cost = new List<double>();
        public static void input_data(FileInfo file)
        {
            #region read data

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            //var file = new FileInfo("test.xlsx");

            using (var excel = new ExcelPackage(file))
            {
                #region job information
                // job information
                ExcelWorksheet sheet = excel.Workbook.Worksheets[0];
                int startRow = sheet.Dimension.Start.Row;//起始列編號，從1算起
                int endRow = sheet.Dimension.End.Row;

                int startColumn = sheet.Dimension.Start.Column;
                int endColumn = sheet.Dimension.End.Column;
                //Console.WriteLine("start " + startRow);
                //Console.WriteLine("End " + endRow);


                for (int currentRow = startRow + 1; currentRow <= endRow; currentRow++)
                {
                    string cellValue = sheet.Cells[currentRow, 2].Text;

                    if (string.IsNullOrEmpty(cellValue))//這是一個完全空白列(使用者用Delete鍵刪除動作)
                    {
                        continue;
                    }
                    //Console.WriteLine(cellValue);
                    job_order_amount.Add(int.Parse(cellValue));
                }

                for (int currentRow = startRow + 1; currentRow <= endRow; currentRow++)
                {
                    List<string> tmp = new List<string>();

                    for (int currentcoloumn = startColumn + 2; currentcoloumn <= endColumn; currentcoloumn++)
                    {
                        string cellValue = sheet.Cells[currentRow, currentcoloumn].Text;

                        if (string.IsNullOrEmpty(cellValue))
                        {
                            continue;
                        }

                        //Console.Write(cellValue);
                        tmp.Add(cellValue);

                    }
                    //Console.WriteLine("a"+tmp.Count);

                    if (tmp.Count == 0) continue;
                    else job_operation_list.Add(tmp);

                }
                //Console.WriteLine("count0" + job_operation_list.Count);
                #endregion

                #region operation available machine
                sheet = excel.Workbook.Worksheets[1];
                startRow = sheet.Dimension.Start.Row;
                endRow = sheet.Dimension.End.Row;
                //Console.WriteLine("End " + endRow);

                startColumn = sheet.Dimension.Start.Column;
                endColumn = sheet.Dimension.End.Column;
                for (int currentRow = startRow + 1; currentRow <= endRow; currentRow++)
                {
                    List<string> tmp = new List<string>();
                    for (int currentcoloumn = startColumn + 1; currentcoloumn <= endColumn; currentcoloumn++)
                    {
                        string cellValue = sheet.Cells[currentRow, currentcoloumn].Text;

                        if (string.IsNullOrEmpty(cellValue))
                        {
                            continue;
                        }
                        //Console.Write(cellValue);
                        tmp.Add(cellValue);
                    }
                    //Console.WriteLine();

                    if (tmp.Count == 0) continue;
                    else operation_avilable_machine.Add(tmp);
                }
                //Console.WriteLine("count1 " + operation_avilable_machine.Count);

                #endregion

                #region processing time
                // operation information
                sheet = excel.Workbook.Worksheets[2];
                startRow = sheet.Dimension.Start.Row;
                endRow = sheet.Dimension.End.Row;
                //Console.WriteLine("End " + endRow);

                startColumn = sheet.Dimension.Start.Column;
                endColumn = sheet.Dimension.End.Column;

                for (int currentRow = startRow + 1; currentRow <= endRow; currentRow++)
                {
                    List<int> tmp = new List<int>();
                    for (int currentcoloumn = startColumn + 1; currentcoloumn <= endColumn; currentcoloumn++)
                    {
                        string cellValue = sheet.Cells[currentRow, currentcoloumn].Text;

                        if (string.IsNullOrEmpty(cellValue))
                        {
                            continue;
                        }
                        //Console.Write(cellValue);
                        tmp.Add(int.Parse(cellValue));
                    }
                    if (tmp.Count == 0) continue;
                    else operation_time.Add(tmp);
                }
                //Console.WriteLine("count2 " + operation_time.Count);

                #endregion

                #region machine information
                sheet = excel.Workbook.Worksheets[3];
                startRow = sheet.Dimension.Start.Row;
                endRow = sheet.Dimension.End.Row;
                //Console.WriteLine("End " + endRow);


                for (int currentRow = startRow + 1; currentRow <= endRow; currentRow++)
                {
                    string cellValue = sheet.Cells[currentRow, 1].Text;
                    machine_information.Add(cellValue);
                    //Console.WriteLine(cellValue);
                }
                //Console.WriteLine("count3 " + machine_information.Count);

                #endregion

                #region employee available machine information
                // employee available machine information
                sheet = excel.Workbook.Worksheets[4];
                startRow = sheet.Dimension.Start.Row;
                endRow = sheet.Dimension.End.Row;

                //Console.WriteLine("@@End " + endRow);
                startColumn = sheet.Dimension.Start.Column;
                endColumn = sheet.Dimension.End.Column;

                for (int currentRow = startRow + 1; currentRow <= endRow; currentRow++)
                {
                    List<string> tmp = new List<string>();

                    for (int currentcoloumn = startColumn + 1; currentcoloumn <= endColumn; currentcoloumn++)
                    {
                        string cellValue = sheet.Cells[currentRow, currentcoloumn].Text;

                        if (string.IsNullOrEmpty(cellValue))
                        {
                            continue;
                        }

                        //Console.Write(cellValue+" ");
                        tmp.Add(cellValue);

                    }
                    //Console.WriteLine();
                    if (tmp.Count == 0) continue;
                    else employee_competence_operation.Add(tmp);

                }
                //Console.WriteLine("count3 " + employee_competence_operation.Count);
                #endregion

                #region employee labour cost
                // employee available machine information
                sheet = excel.Workbook.Worksheets[5];
                startRow = sheet.Dimension.Start.Row;
                endRow = sheet.Dimension.End.Row;

                //Console.WriteLine("@@End " + endRow);
                startColumn = sheet.Dimension.Start.Column;
                endColumn = sheet.Dimension.End.Column;

                for (int currentRow = startRow + 1; currentRow <= endRow; currentRow++)
                {
                    labour_cost.Add(int.Parse(sheet.Cells[currentRow, endColumn].Text));
                    Console.WriteLine(labour_cost[labour_cost.Count - 1]);
                }
                //Console.WriteLine("count3 " + employee_competence_operation.Count);
                #endregion

            }
            #endregion

        }
    }
}
