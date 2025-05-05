/// <summary>
/// 提供双色球彩票的预测功能，包括生成推荐号码组合和打印预测结果。
/// </summary>
public static class LotteryPredictor
{
    /// <summary>
    /// 根据历史记录生成推荐的双色球号码组合。
    /// </summary>
    /// <param name="history">包含彩票历史记录的列表。</param>
    /// <param name="groupCount">需要生成的推荐组合数量，默认为 10。</param>
    /// <returns>返回推荐的号码组合列表，每个组合包含 6 个红球和 1 个蓝球。</returns>
    public static List<PredictionResult> GeneratePredictions(List<LotteryData> history, int groupCount = 10)
    {
        var rand = new Random();

        // 红球和蓝球的频率统计字典
        var redFreq = new Dictionary<int, int>();
        var blueFreq = new Dictionary<int, int>();

        // 遍历历史记录，统计红球和蓝球的出现频率
        foreach (var data in history)
        {
            var reds = data.RedBalls.Split(' ').Select(int.Parse);
            foreach (var r in reds)
                redFreq[r] = redFreq.GetValueOrDefault(r) + 1;

            int blue = int.Parse(data.BlueBall);
            blueFreq[blue] = blueFreq.GetValueOrDefault(blue) + 1;
        }

        // 获取红球的热号（出现频率最高的前 20 个）和冷号（出现频率最低的前 13 个）
        var hotReds = redFreq.OrderByDescending(kv => kv.Value).Take(20).Select(kv => kv.Key).ToList();
        var coldReds = redFreq.OrderBy(kv => kv.Value).Take(13).Select(kv => kv.Key).ToList();
        // 获取蓝球的热号（出现频率最高的前 6 个）
        var hotBlues = blueFreq.OrderByDescending(kv => kv.Value).Take(6).Select(kv => kv.Key).ToList();


        var result = new List<PredictionResult>(); 

        // 获取最近一期的红球号码，用于避免生成与最近一期相同的组合
        var lastIssue = history.OrderByDescending(d => d.Issue).First();
        var lastReds = lastIssue.RedBalls.Split(' ').Select(int.Parse).ToList();

        // 生成推荐组合，直到达到指定数量
        while (result.Count < groupCount)
        {
            var reds = new HashSet<int>();

            // 热号挑 4~5 个
            int hotCount = rand.Next(4, 6);
            while (reds.Count < hotCount)
                reds.Add(hotReds[rand.Next(hotReds.Count)]);

            // 冷号补足到 6
            while (reds.Count < 6)
                reds.Add(coldReds[rand.Next(coldReds.Count)]);

            // 将红球排序，确保输出的组合有序
            var redList = reds.OrderBy(n => n).ToList();
            // 如果生成的红球组合与最近一期相同，则跳过
            if (IsSameList(redList, lastReds)) continue;

            // 从热号中随机挑选 1 个蓝球
            int blue = hotBlues[rand.Next(hotBlues.Count)];

            result.Add(new PredictionResult { Reds = redList, Blue = blue });
        }

        return result;
    }

    /// <summary>
    /// 判断两个红球号码列表是否完全相同。
    /// </summary>
    /// <param name="a">第一个红球号码列表。</param>
    /// <param name="b">第二个红球号码列表。</param>
    /// <returns>如果两个列表完全相同，则返回 true；否则返回 false。</returns>
    private static bool IsSameList(List<int> a, List<int> b)
    {
        return a.Count == b.Count && a.Zip(b).All(pair => pair.First == pair.Second);
    }

    /// <summary>
    /// 打印推荐的双色球号码组合。
    /// </summary>
    /// <param name="predictions">包含推荐号码组合的列表。</param>
    public static void PrintPrediction(List<PredictionResult> predictions)
    {
        Console.WriteLine("\n ===== 推荐号码组合 =====");
        int idx = 1;
        // 遍历每个推荐组合并打印
        foreach (var prediction in predictions)
        {
            Console.WriteLine($"[{idx++:00}] 红球：{prediction.RedBallsString}  | 蓝球：{prediction.BlueBallString}");
        }
    }
}
