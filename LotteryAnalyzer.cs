/// <summary>
/// 双色球彩票分析
/// </summary>
public static class LotteryAnalyzer
{
    /// <summary>
    /// 分析双色球彩票的历史记录数据，输出高频号码、冷门号码、和值范围、跨度范围等统计信息。
    /// </summary>
    /// <param name="dataList">包含彩票历史记录的列表。</param>
    public static void Analyze(List<LotteryData> dataList)
    {
        Console.WriteLine("\n ===== 双色球开奖记录分析 =====\n");

        // 红球号码出现次数统计
        var redCount = new Dictionary<int, int>();
        // 蓝球号码出现次数统计
        var blueCount = new Dictionary<int, int>();
        // 红球和值集合
        var redSums = new List<int>();
        // 红球跨度集合
        var redSpans = new List<int>();
        // 连号出现的期数统计
        var LineCount = 0;

        // 遍历每一期彩票数据
        foreach (var data in dataList)
        {
            // 处理红球
            var reds = data.RedBalls.Split(' ').Select(int.Parse).OrderBy(n => n).ToList();
            foreach (var red in reds)
                redCount[red] = redCount.GetValueOrDefault(red) + 1;

            // 连号判断
            for (int i = 1; i < reds.Count; i++)
                if (reds[i] - reds[i - 1] == 1) LineCount++;

            // 红球和值与跨度
            redSums.Add(reds.Sum());
            redSpans.Add(reds.Max() - reds.Min());

            // 蓝球
            var blue = int.Parse(data.BlueBall);
            blueCount[blue] = blueCount.GetValueOrDefault(blue) + 1;
        }

        // 输出高频红球
        PrintTop(" 高频红球", redCount, 10);
        // 输出冷门红球
        PrintTop(" 冷门红球", redCount.OrderBy(kv => kv.Value).ToDictionary(kv => kv.Key, kv => kv.Value), 10);
        // 输出高频蓝球
        PrintTop(" 高频蓝球", blueCount, 5);

        // 输出连号统计信息
        Console.WriteLine($" 出现连号的期数（至少1组）：{LineCount} / {dataList.Count} ≈ {LineCount * 100.0 / dataList.Count:F2}%");
        // 输出红球和值范围和平均值
        Console.WriteLine($" 红球和值范围：{redSums.Min()} ~ {redSums.Max()}，平均值：{redSums.Average():F2}");
        // 输出红球跨度范围和平均跨度
        Console.WriteLine($" 红球最大跨度范围：{redSpans.Min()} ~ {redSpans.Max()}，平均跨度：{redSpans.Average():F2}");
    }

    /// <summary>
    /// 打印出现次数最多或最少的号码及其出现次数。
    /// </summary>
    /// <param name="title">输出的标题。</param>
    /// <param name="dict">号码及其出现次数的字典。</param>
    /// <param name="topN">需要输出的前 N 个号码。</param>
    private static void PrintTop(string title, Dictionary<int, int> dict, int topN)
    {
        Console.WriteLine($"\n{title}：");
        // 按出现次数降序排序并输出前 N 个号码
        foreach (var kv in dict.OrderByDescending(kv => kv.Value).Take(topN))
        {
            Console.Write($"[{kv.Key:D2}:{kv.Value}] ");
        }
        Console.WriteLine();
    }
}