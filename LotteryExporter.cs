using System.Collections.Generic;
using System.IO;
using System.Linq;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System; 

/// <summary>
/// 提供导出双色球预测结果和遗漏数据到 Excel 文件的功能。
/// </summary>
public static class LotteryExporter
{
    private const string FilePath = "PredictionAndOmission.xlsx"; // 统一定义文件名
    private const string PredictionSheetName = "推荐组合";
    private const string RedOmissionSheetName = "红球遗漏表";
    private const string BlueOmissionSheetName = "蓝球遗漏表";
    /// <summary>
    /// 将预测结果和红球、蓝球的遗漏数据导出到 Excel 文件。
    /// </summary>
    /// <param name="predictions">预测的红球和蓝球组合列表。</param>
    /// <param name="redOmissions">红球的遗漏数据，键为红球号码，值为遗漏期数。</param>
    /// <param name="blueOmissions">蓝球的遗漏数据，键为蓝球号码，值为遗漏期数。</param>
    /// <param name="filePath">导出的 Excel 文件路径，默认为 "PredictionAndOmission.xlsx"。</param>
    public static void ExportPredictionsAndOmissions(
        List<PredictionResult> predictions,
        Dictionary<int, int> redOmissions,
        Dictionary<int, int> blueOmissions)
    {
        // 创建一个新的 Excel 工作簿
        IWorkbook workbook = new XSSFWorkbook();
        ISheet predictionSheet;
        int firstEmptyRowIndex = -1; // 用于记录第一个期号为空的行的索引

        if (File.Exists(FilePath))
        {
            try
            {
                using (var fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read))
                {
                    workbook = new XSSFWorkbook(fs);
                }
                predictionSheet = workbook.GetSheet(PredictionSheetName);
                if (predictionSheet == null)
                {
                    Console.WriteLine($"警告：工作簿 '{FilePath}' 中未找到工作表 '{PredictionSheetName}'，将创建新表。");
                    predictionSheet = workbook.CreateSheet(PredictionSheetName);
                    CreatePredictionHeader(predictionSheet);
                    firstEmptyRowIndex = 1; // 新表，从第一行数据开始
                }
                else
                {
                    // 查找第一个期号为空的行
                    for (int i = 1; i <= predictionSheet.LastRowNum; i++) // 从数据行开始检查
                    {
                        var row = predictionSheet.GetRow(i);
                        var issueCell = row?.GetCell(0); // 期号在第一列 (索引 0)
                        if (row == null || issueCell == null || string.IsNullOrWhiteSpace(issueCell.ToString()))
                        {
                            firstEmptyRowIndex = i;
                            break;
                        }
                    }
                    if (firstEmptyRowIndex == -1) // 如果没找到空行，则追加到末尾
                    {
                        firstEmptyRowIndex = predictionSheet.LastRowNum + 1;
                    }
                    // 确保表头存在且正确
                    EnsurePredictionHeader(predictionSheet);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误：读取 Excel 文件 '{FilePath}' 失败: {ex.Message}。将创建新文件。");
                workbook = new XSSFWorkbook();
                predictionSheet = workbook.CreateSheet(PredictionSheetName);
                CreatePredictionHeader(predictionSheet);
                firstEmptyRowIndex = 1;
            }
        }
        else
        {
            workbook = new XSSFWorkbook();
            predictionSheet = workbook.CreateSheet(PredictionSheetName);
            CreatePredictionHeader(predictionSheet);
            firstEmptyRowIndex = 1;
        }

        // --- 2. 填充或覆盖预测数据 ---
        Console.WriteLine(firstEmptyRowIndex <= predictionSheet.LastRowNum && firstEmptyRowIndex > 0
            ? $"正在覆盖从第 {firstEmptyRowIndex} 行开始的预测数据..."
            : $"正在追加新的预测数据到第 {firstEmptyRowIndex} 行...");

        for (int i = 0; i < predictions.Count; i++)
        {
            var rowIndex = firstEmptyRowIndex + i;
            var row = predictionSheet.GetRow(rowIndex) ?? predictionSheet.CreateRow(rowIndex);

            // 设置单元格值 (期号为空，红球，蓝球，中奖情况为空)
            row.CreateCell(0).SetCellValue(predictions[i].Issue); // 期号 (空)
            row.CreateCell(1).SetCellValue(predictions[i].RedBallsString); // 红球
            row.CreateCell(2).SetCellValue(predictions[i].BlueBallString); // 蓝球
            // 确保第4列存在，即使是空的
            var prizeCell = row.GetCell(3) ?? row.CreateCell(3);
            prizeCell.SetCellValue(predictions[i].PrizeInfo); // 中奖情况 (空)
        }

        // 如果是覆盖，可能需要清除旧的多余行 (如果新预测比旧预测少)
        // (暂时简化，不清除旧行)

        // --- 3. 更新遗漏数据 Sheet (如果需要，可以创建或覆盖) ---
        UpdateOmissionSheet(workbook, RedOmissionSheetName, "红球", redOmissions);
        UpdateOmissionSheet(workbook, BlueOmissionSheetName, "蓝球", blueOmissions);


        // --- 4. 自动调整列宽 ---
        // 只调整预测表的列宽
        for (int i = 0; i < 4; i++) // 现在有4列了
        {
            predictionSheet.AutoSizeColumn(i);
        }
        // 遗漏表的列宽在 UpdateOmissionSheet 中处理

        // --- 5. 保存文件 ---
        try
        {
            using (var fs = new FileStream(FilePath, FileMode.Create, FileAccess.Write))
            {
                workbook.Write(fs);
            }
            Console.WriteLine($" 预测组合+遗漏数据已更新到: {Path.GetFullPath(FilePath)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"错误：保存 Excel 文件 '{FilePath}' 失败: {ex.Message}");
        }
        finally
        {
            // workbook.Close(); // 通常不需要
        }


        #region 后续逻辑优化摒弃
        //// Sheet1: 推荐组合
        //IWorkbook workbook = new XSSFWorkbook();
        //var sheet1 = workbook.CreateSheet("推荐组合");
        //var header1 = sheet1.CreateRow(0);
        //header1.CreateCell(0).SetCellValue("红球"); // 表头：红球
        //header1.CreateCell(1).SetCellValue("蓝球"); // 表头：蓝球

        //// 填充推荐组合数据
        //for (int i = 0; i < predictions.Count; i++)
        //{
        //    var row = sheet1.CreateRow(i + 1);
        //    // 红球组合（格式化为两位数字）
        //    row.CreateCell(0).SetCellValue(string.Join(" ", predictions[i].reds.Select(n => n.ToString("D2"))));
        //    // 蓝球号码（格式化为两位数字）
        //    row.CreateCell(1).SetCellValue(predictions[i].blue.ToString("D2"));
        //}

        //// Sheet2: 红球遗漏
        //var sheet2 = workbook.CreateSheet("红球遗漏表");
        //var header2 = sheet2.CreateRow(0);
        //header2.CreateCell(0).SetCellValue("红球"); // 表头：红球
        //header2.CreateCell(1).SetCellValue("遗漏期数"); // 表头：遗漏期数

        //// 按遗漏期数降序排列红球数据并填充
        //var sortedRed = redOmissions.OrderByDescending(kv => kv.Value).ToList();
        //for (int i = 0; i < sortedRed.Count; i++)
        //{
        //    var row = sheet2.CreateRow(i + 1);
        //    row.CreateCell(0).SetCellValue(sortedRed[i].Key.ToString("D2")); // 红球号码
        //    row.CreateCell(1).SetCellValue(sortedRed[i].Value); // 遗漏期数
        //}

        //// Sheet3: 蓝球遗漏
        //var sheet3 = workbook.CreateSheet("蓝球遗漏表");
        //var header3 = sheet3.CreateRow(0);
        //header3.CreateCell(0).SetCellValue("蓝球"); // 表头：蓝球
        //header3.CreateCell(1).SetCellValue("遗漏期数"); // 表头：遗漏期数

        //// 按遗漏期数降序排列蓝球数据并填充
        //var sortedBlue = blueOmissions.OrderByDescending(kv => kv.Value).ToList();
        //for (int i = 0; i < sortedBlue.Count; i++)
        //{
        //    var row = sheet3.CreateRow(i + 1);
        //    row.CreateCell(0).SetCellValue(sortedBlue[i].Key.ToString("D2")); // 蓝球号码
        //    row.CreateCell(1).SetCellValue(sortedBlue[i].Value); // 遗漏期数
        //}

        //// 自动列宽
        //foreach (var sheet in new[] { sheet1, sheet2, sheet3 })
        //{
        //    sheet.AutoSizeColumn(0); // 调整第一列宽度
        //    sheet.AutoSizeColumn(1); // 调整第二列宽度
        //}

        //// 保存文件
        //using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        //workbook.Write(fs);

        //// 输出导出成功的提示信息
        //Console.WriteLine($" 推荐组合+遗漏数据已导出到: {Path.GetFullPath(filePath)}");
        #endregion
    }

    /// <summary>
    /// 创建推荐组合表的表头
    /// </summary>
    /// <param name="sheet"></param>
    private static void CreatePredictionHeader(ISheet sheet)
    {
        var headerRow = sheet.CreateRow(0);
        headerRow.CreateCell(0).SetCellValue("期号");
        headerRow.CreateCell(1).SetCellValue("红球");
        headerRow.CreateCell(2).SetCellValue("蓝球");
        headerRow.CreateCell(3).SetCellValue("中奖情况");
    }

    /// <summary>
    /// 确保推荐组合表的表头存在且正确
    /// </summary>
    /// <param name="sheet"></param>
    private static void EnsurePredictionHeader(ISheet sheet)
    {
        var headerRow = sheet.GetRow(0);
        if (headerRow == null || headerRow.GetCell(0)?.ToString() != "期号" || headerRow.GetCell(3)?.ToString() != "中奖情况")
        {
            // 如果表头不完整或不正确，重新创建
            // 注意：这可能会覆盖第一行数据，如果第一行不是表头的话！
            if (headerRow == null) headerRow = sheet.CreateRow(0);
            headerRow.CreateCell(0).SetCellValue("期号");
            headerRow.CreateCell(1).SetCellValue("红球");
            headerRow.CreateCell(2).SetCellValue("蓝球");
            headerRow.CreateCell(3).SetCellValue("中奖情况");
        }
    }

    /// <summary>
    /// 更新或创建遗漏数据表
    /// </summary>
    /// <param name="workbook"></param>
    /// <param name="sheetName"></param>
    /// <param name="header1"></param>
    /// <param name="omissions"></param>
    private static void UpdateOmissionSheet(IWorkbook workbook, string sheetName, string header1, Dictionary<int, int> omissions)
    {
        ISheet sheet = workbook.GetSheet(sheetName);
        if (sheet != null) // 如果存在，先删除再创建（简单覆盖）
        {
            int sheetIndex = workbook.GetSheetIndex(sheet);
            workbook.RemoveSheetAt(sheetIndex);
        }
        sheet = workbook.CreateSheet(sheetName);

        // 创建表头
        var headerRow = sheet.CreateRow(0);
        headerRow.CreateCell(0).SetCellValue(header1);
        headerRow.CreateCell(1).SetCellValue("遗漏期数");

        // 填充数据
        var sortedOmissions = omissions.OrderByDescending(kv => kv.Value).ToList();
        for (int i = 0; i < sortedOmissions.Count; i++)
        {
            var row = sheet.CreateRow(i + 1);
            row.CreateCell(0).SetCellValue(sortedOmissions[i].Key.ToString("D2"));
            row.CreateCell(1).SetCellValue(sortedOmissions[i].Value);
        }

        // 调整列宽
        sheet.AutoSizeColumn(0);
        sheet.AutoSizeColumn(1);
    }

    /// <summary>
    /// 读取历史预测结果（包括未开奖的）
    /// </summary>
    public static List<PredictionResult> ReadPredictionHistory()
    {
        var history = new List<PredictionResult>();
        if (!File.Exists(FilePath)) return history;

        IWorkbook workbook;
        try
        {
            using (var fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) // 允许多进程读
            {
                workbook = new XSSFWorkbook(fs);
            }
            var sheet = workbook.GetSheet(PredictionSheetName);
            if (sheet == null) return history;

            for (int i = 1; i <= sheet.LastRowNum; i++) // 从数据行开始
            {
                var row = sheet.GetRow(i);
                if (row == null) continue;

                try
                {
                    var prediction = new PredictionResult
                    {
                        Issue = row.GetCell(0)?.ToString() ?? string.Empty,
                        // 解析红球，需要容错
                        Reds = row.GetCell(1)?.ToString()?
                                  .Split(new[] { ' ', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                                  .Select(s => int.TryParse(s, out int n) ? n : -1)
                                  .Where(n => n != -1)
                                  .OrderBy(n => n)
                                  .ToList() ?? new List<int>(),
                        // 解析蓝球，需要容错
                        Blue = int.TryParse(row.GetCell(2)?.ToString(), out int b) ? b : -1,
                        PrizeInfo = row.GetCell(3)?.ToString() ?? string.Empty
                    };

                    // 进行基本的验证
                    if (prediction.Reds.Count == 6 && prediction.Blue != -1)
                    {
                        history.Add(prediction);
                    }
                    else
                    {
                        Console.WriteLine($"警告：读取预测文件第 {i + 1} 行数据无效，已跳过。红球数: {prediction.Reds.Count}, 蓝球: {prediction.Blue}");
                    }
                }
                catch (Exception rowEx)
                {
                    Console.WriteLine($"错误：解析预测文件第 {i + 1} 行时出错: {rowEx.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"错误：读取预测历史文件 '{FilePath}' 失败: {ex.Message}");
        }
        return history;
    }

    /// <summary>
    /// 将更新后的预测历史写回文件 (包括期号和中奖情况)
    /// </summary>
    public static void WritePredictionHistory(List<PredictionResult> updatedHistory)
    {
        if (!File.Exists(FilePath))
        {
            Console.WriteLine($"错误：预测文件 '{FilePath}' 不存在，无法写入历史记录。");
            return;
        }

        try
        {
            IWorkbook workbook;
            ISheet sheet;

            // --- 步骤 1: 读取现有内容到内存 ---
            // 为了安全地覆盖写入，我们先完整读取文件内容
            // 如果文件很大，这可能会消耗较多内存
            using (var readStream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) // 允许读取时其他进程也在读
            {
                workbook = new XSSFWorkbook(readStream);
                sheet = workbook.GetSheet(PredictionSheetName);
                if (sheet == null)
                {
                    Console.WriteLine($"错误：在文件 '{FilePath}' 中未找到工作表 '{PredictionSheetName}'。");
                    // workbook.Close(); // 关闭 workbook
                    return; // 无法继续
                }
                // 注意：这里 workbook 对象已经加载了 sheet 内容，readStream 可以关闭了
            } // readStream 在此自动关闭

            // --- 步骤 2: 在内存中修改 Workbook/Sheet ---
            // 找到表头，从下一行开始写
            int startRow = 1; // 假设表头在第0行

            // 确保表头正确 (如果需要)
            EnsurePredictionHeader(sheet);

            for (int i = 0; i < updatedHistory.Count; i++)
            {
                var rowIndex = startRow + i;
                var row = sheet.GetRow(rowIndex) ?? sheet.CreateRow(rowIndex); // 获取或创建行

                // 更新或写入单元格
                row.CreateCell(0).SetCellValue(updatedHistory[i].Issue);
                row.CreateCell(1).SetCellValue(updatedHistory[i].RedBallsString);
                row.CreateCell(2).SetCellValue(updatedHistory[i].BlueBallString);
                row.CreateCell(3).SetCellValue(updatedHistory[i].PrizeInfo);
            }

            // 处理可能的旧数据行 (如果需要清除)
            int expectedLastRow = startRow + updatedHistory.Count - 1;
            if (expectedLastRow < sheet.LastRowNum)
            {
                Console.WriteLine($"注意：更新后的预测数量 ({updatedHistory.Count}) 少于文件中原有数量({sheet.LastRowNum - startRow + 1})。正在清除多余的旧预测行...");
                for (int i = expectedLastRow + 1; i <= sheet.LastRowNum; i++)
                {
                    var rowToRemove = sheet.GetRow(i);
                    if (rowToRemove != null)
                    {
                        // 清除内容比移除行更安全
                        rowToRemove.CreateCell(0).SetCellValue(string.Empty);
                        rowToRemove.CreateCell(1).SetCellValue(string.Empty);
                        rowToRemove.CreateCell(2).SetCellValue(string.Empty);
                        rowToRemove.CreateCell(3).SetCellValue(string.Empty);
                        // 或者 sheet.RemoveRow(rowToRemove); 但要小心对后续行的影响
                    }
                }
                // NPOI 可能不会自动更新 LastRowNum，但内容已被清除
            }

            // 重新自动调整列宽
            for (int i = 0; i < 4; i++) sheet.AutoSizeColumn(i);

            // --- 步骤 3: 将修改后的 Workbook 写回文件 ---
            // 使用新的 FileStream 进行写入，模式为 Create (覆盖)
            using (var writeStream = new FileStream(FilePath, FileMode.Create, FileAccess.Write, FileShare.None)) // 独占写入
            {
                workbook.Write(writeStream); // 默认 leaveOpen 是 false，写入后会关闭 workbook 资源，但 writeStream 由 using 管理
            } // writeStream 在此自动关闭

            Console.WriteLine($" 预测历史记录已更新并写回: {Path.GetFullPath(FilePath)}");
        }
        catch (IOException ioEx) // 特别处理文件占用
        {
            Console.WriteLine($"错误：写入预测历史文件 '{FilePath}' 时发生 IO 错误（文件可能被占用或权限问题）: {ioEx.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"错误：写入预测历史文件 '{FilePath}' 失败: {ex.Message}\n{ex.StackTrace}");
        }
    }
}
