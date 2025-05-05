using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Org.BouncyCastle.Utilities.Collections;

class Program
{
    // 定义一个静态的 HttpClient 实例，用于发送 HTTP 请求
    private static readonly HttpClient _httpClient = new HttpClient();
    private const string HistoryDataFile = "LotteryData.xlsx";
    private const string PredictionFile = "PredictionAndOmission.xlsx";

    static async Task Main()
    {
        try
        {
            // 1. 读取本地历史数据
            var history = LoadHistoricalData();

            // 2. 拉取增量数据
            var newData = await FetchIncrementalData();
            // 合并历史数据和增量数据，并按期号排序
            var allData = history.Concat(newData).OrderBy(d => d.Issue).ToList();

            if (allData.Count == 0)
            {
                Console.WriteLine("没有任何数据，程序终止。");
                return;
            }

            // 3. 保存合并后的数据
            if (newData.Count > 0)
            {
                ExportToExcel(newData);
                Console.WriteLine("数据已合并并保存完毕");
            }

            // 4. 分析 + 推荐 + 导出 + 训练
            // 调用分析器进行数据分析
            LotteryAnalyzer.Analyze(allData);
            LotteryAdvancedAnalyzer.Analyze(allData);

            // 生成预测结果
            var predictions = LotteryPredictor.GeneratePredictions(allData, 10);
            // 打印预测结果
            LotteryPredictor.PrintPrediction(predictions);

            // 构建训练数据并进行模型训练和预测
            var trainData = LotteryMLDataBuilder.BuildTrainingData(allData);
            var mlPrediction = LotteryTrainer.TrainAndPredict(trainData);

            // 合并所有预测结果
            var allPredictions = new List<PredictionResult>();
            allPredictions.AddRange(predictions);
            if (mlPrediction != null)
            {
                allPredictions.Add(mlPrediction); // 添加 ML 预测结果
            }
            else
            {
                Console.WriteLine("\n ML.NET 未能生成有效预测，将不包含在导出中。");
            }

            // 计算红球和蓝球的遗漏值
            var redOmissions = CalculateRedOmissions(allData);
            var blueOmissions = CalculateBlueOmissions(allData);
            // 导出预测和遗漏值
            LotteryExporter.ExportPredictionsAndOmissions(allPredictions, redOmissions, blueOmissions);

        }
        catch (Exception ex)
        {
            Console.WriteLine($"程序运行时出现异常: {ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>
    /// 拉取增量数据（从上次记录的期号开始）
    /// </summary>
    /// <returns></returns>
    private static async Task<List<LotteryData>> FetchIncrementalData()
    {
        try
        {
            var historyPath = "LotteryData.xlsx";
            string lastIssue = "20000";// 默认起始期号(初次运行时，没有找到Excel文件时有效)

            // 如果历史数据文件存在，读取最后一期的期号
            if (File.Exists(historyPath))
            {
                using var fs = new FileStream(historyPath, FileMode.Open, FileAccess.Read);
                IWorkbook workbookHis = new XSSFWorkbook(fs);
                var sheet = workbookHis.GetSheetAt(0);
                var lastRow = sheet.GetRow(sheet.LastRowNum);
                lastIssue = lastRow?.GetCell(1)?.ToString();
                workbookHis?.Close();
            }

            Console.WriteLine($"正在获取自 {lastIssue} 之后的增量数据...");

            // 构造增量数据的请求 URL
            var url = $"https://datachart.500.com/ssq/history/newinc/history.php?start={lastIssue}&end=99999";
            var html = await _httpClient.GetStringAsync(url);

            // 使用 HtmlAgilityPack 解析 HTML
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // 定位数据表格
            var table = doc.DocumentNode.SelectSingleNode("//tbody[@id='tdata']");
            if (table == null)
            {
                Console.WriteLine("未找到数据表结构，返回空数据。");
                return new List<LotteryData>();
            }

            // 获取表格中的所有行
            var rows = table.SelectNodes("./tr");
            if (rows == null)
            {
                Console.WriteLine("表中没有数据行，返回空数据。");
                return new List<LotteryData>();
            }

            var newData = new List<LotteryData>();

            // 遍历每一行，提取数据
            foreach (var row in rows)
            {
                var tds = row.SelectNodes("td");
                if (tds == null || tds.Count < 16) continue;

                var issue = tds[0].InnerText.Trim();
                if (issue == lastIssue) continue;// 跳过已存在的期号

                // 添加新的数据记录
                newData.Add(new LotteryData
                {
                    Issue = issue,
                    RedBalls = string.Join(" ", tds.Skip(1).Take(6).Select(td => td.InnerText.Trim())),
                    BlueBall = tds[7].InnerText.Trim(),
                    Date = tds[15].InnerText.Trim()
                });
            }

            if (newData.Count > 0)
            {
                Console.WriteLine($"成功拉取 {newData.Count} 条新增开奖数据！");

                // --- ★ 处理历史预测的中奖情况回填 ★ ---
                ProcessPrizeChecking(newData.Last());
                // ----------------------------------------
            }

            return newData;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"拉取增量数据时发生异常: {ex.Message}");
            return new List<LotteryData>();
        }
    }

    /// <summary>
    /// 处理历史预测的中奖情况回填
    /// </summary>
    /// <param name="newDrawings"></param>
    private static void ProcessPrizeChecking(LotteryData drawing)
    {
        Console.WriteLine("\n--- 开始检查历史预测的中奖情况 ---");
        // 1. 读取 PredictionAndOmission.xlsx 中的预测记录
        var predictionHistory = LotteryExporter.ReadPredictionHistory();
        if (predictionHistory.Count == 0)
        {
            Console.WriteLine(" 未找到历史预测记录文件或内容为空，跳过中奖检查。");
            return;
        }

        // 2. 筛选出期号为空的预测 (这些是等待开奖的)
        var pendingPredictions = predictionHistory.Where(p => string.IsNullOrWhiteSpace(p.Issue)).ToList();
        if (pendingPredictions.Count == 0)
        {
            Console.WriteLine(" 未找到期号为空的历史预测，无需进行中奖检查。");
            return;
        }
        Console.WriteLine($" 发现 {pendingPredictions.Count} 条待开奖的预测记录。");

        bool updated = false; // 标记是否有记录被更新

        Console.WriteLine($"  正在处理开奖期号: {drawing.Issue}");
        var actualReds = drawing.RedBalls.Split(' ').Select(int.Parse).ToList();
        var actualBlue = int.Parse(drawing.BlueBall);

        // 3. 查找并更新对应的待开奖预测
        // 简单处理：将当前开奖期号赋给所有待开奖预测
        foreach (var prediction in pendingPredictions)
        {
            // 检查这条预测是否已经被之前的开奖更新过（不太可能，但做个检查）
            if (string.IsNullOrWhiteSpace(prediction.Issue))
            {
                prediction.Issue = drawing.Issue; // 回填期号
                prediction.PrizeInfo = LotteryPrizeChecker.CheckPrize(prediction.Reds, prediction.Blue, actualReds, actualBlue); // 计算中奖情况
                Console.WriteLine($"   > 预测 红球:[{prediction.RedBallsString}] 蓝球:[{prediction.BlueBallString}] => {prediction.PrizeInfo}");
                updated = true;
            }
        }
        // 处理完一期开奖后，就不应该再有空的预测了 (在本次拉取的数据范围内)
        // 更新 pendingPredictions 列表 (移除已处理的)
        pendingPredictions.RemoveAll(p => !string.IsNullOrWhiteSpace(p.Issue));

        // 4. 如果有更新，写回 Excel 文件
        if (updated)
        {
            Console.WriteLine("正在将更新后的中奖情况写回 Excel 文件...");
            LotteryExporter.WritePredictionHistory(predictionHistory); // 写入包含所有记录（更新过的和未更新的）的列表
        }
        else
        {
            Console.WriteLine("没有预测记录被更新（可能没有待开奖预测或拉取的数据无法匹配）。");
        }
        Console.WriteLine("--- 中奖情况检查完毕 ---");
    }


    /// <summary>
    /// 读取本地历史数据
    /// </summary>
    /// <returns></returns>
    private static List<LotteryData> LoadHistoricalData()
    {
        try
        {
            var dataList = new List<LotteryData>();
            var path = "LotteryData.xlsx";
            if (!File.Exists(path)) return dataList;// 如果文件不存在，返回空列表

            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            IWorkbook workbook = new XSSFWorkbook(fs);
            var sheet = workbook.GetSheetAt(0);

            // 遍历 Excel 表格的每一行，读取数据
            for (int i = 1; i <= sheet.LastRowNum; i++)
            {
                var row = sheet.GetRow(i);
                if (row == null) continue;

                dataList.Add(new LotteryData
                {
                    Date = row.GetCell(0).ToString(),
                    Issue = row.GetCell(1).ToString(),
                    RedBalls = row.GetCell(2).ToString(),
                    BlueBall = row.GetCell(3).ToString()
                });
            }

            return dataList;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"加载历史数据时发生异常: {ex.Message}");
            return new List<LotteryData>();
        }
    }

    /// <summary>
    /// 将数据导出到 Excel 文件
    /// </summary>
    /// <param name="dataList"></param>
    private static void ExportToExcel(List<LotteryData> dataList)
    {
        try
        {
            var filePath = "LotteryData.xlsx";
            IWorkbook workbook;
            ISheet sheet;

            // 如果文件存在，加载现有文件
            if (File.Exists(filePath))
            {
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    workbook = new XSSFWorkbook(fs);
                    sheet = workbook.GetSheetAt(0);
                }
            }
            else
            {
                // 如果文件不存在，创建新的工作簿和工作表
                workbook = new XSSFWorkbook();
                sheet = workbook.CreateSheet("双色球开奖记录");

                // 创建表头
                var headerRow = sheet.CreateRow(0);
                headerRow.CreateCell(0).SetCellValue("日期");
                headerRow.CreateCell(1).SetCellValue("期号");
                headerRow.CreateCell(2).SetCellValue("红球");
                headerRow.CreateCell(3).SetCellValue("蓝球");
            }

            // 找到最后一行的索引
            var lastRowNum = sheet.LastRowNum;

            // 填充数据
            for (var i = 0; i < dataList.Count; i++)
            {
                var row = sheet.CreateRow(lastRowNum + i + 1);
                row.CreateCell(0).SetCellValue(dataList[i].Date);
                row.CreateCell(1).SetCellValue(dataList[i].Issue);
                row.CreateCell(2).SetCellValue(dataList[i].RedBalls);
                row.CreateCell(3).SetCellValue(dataList[i].BlueBall);
            }

            // 自动调整列宽
            for (int i = 0; i < 4; i++)
            {
                sheet.AutoSizeColumn(i);
            }

            // 保存到文件
            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                workbook.Write(fs);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"导出Excel时发生异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 计算红球的遗漏值
    /// </summary>
    /// <param name="dataList"></param>
    /// <returns></returns>
    private static Dictionary<int, int> CalculateRedOmissions(List<LotteryData> dataList)
    {
        // 初始化红球号码的遗漏值字典
        var omission = Enumerable.Range(1, 33).ToDictionary(n => n, _ => 0);
        foreach (var data in dataList)
        {
            var reds = data.RedBalls.Split(' ').Select(int.Parse).ToList();
            foreach (var key in omission.Keys.ToList())
            {
                omission[key]++;
                if (reds.Contains(key)) omission[key] = 0;
            }
        }
        return omission;
    }

    /// <summary>
    /// 计算蓝球的遗漏值
    /// </summary>
    /// <param name="dataList"></param>
    /// <returns></returns>
    private static Dictionary<int, int> CalculateBlueOmissions(List<LotteryData> dataList)
    {
        // 初始化蓝球号码的遗漏值字典
        var omission = Enumerable.Range(1, 16).ToDictionary(n => n, _ => 0);
        foreach (var data in dataList)
        {
            int blue = int.Parse(data.BlueBall);
            foreach (var key in omission.Keys.ToList())
            {
                omission[key]++;
                if (key == blue) omission[key] = 0;// 如果出现该号码，重置遗漏值
            }
        }
        return omission;
    }
}

/// <summary>
/// 定义彩票数据的模型类
/// </summary>
public class LotteryData
{
    /// <summary>
    /// 日期
    /// </summary>
    [Description("日期")]
    public string Date { get; set; }

    /// <summary>
    /// 期号
    /// </summary>
    [Description("期号")]
    public string Issue { get; set; }

    /// <summary>
    /// 红球号码
    /// </summary>
    [Description("红球号码")]
    public string RedBalls { get; set; }

    /// <summary>
    /// 蓝球号码
    /// </summary>
    [Description("蓝球号码")]
    public string BlueBall { get; set; }
}
