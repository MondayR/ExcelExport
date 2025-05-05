/// <summary>
/// 提供双色球彩票的高级统计分析功能，包括遗漏统计、奇偶比分析、和值区间分布和分区统计等。
/// </summary>
public static class LotteryAdvancedAnalyzer
{
    /// <summary>
    /// 对双色球彩票的历史记录数据进行高级统计分析，并输出分析结果。
    /// </summary>
    /// <param name="dataList">包含彩票历史记录的列表。</param>
    public static void Analyze(List<LotteryData> dataList)
    {
        Console.WriteLine("\n ===== 高级统计分析 =====");

        // 初始化红球和蓝球的遗漏统计字典
        var redOmission = Enumerable.Range(1, 33).ToDictionary(n => n, _ => 0);
        var blueOmission = Enumerable.Range(1, 16).ToDictionary(n => n, _ => 0);
        // 记录红球和蓝球上次出现的期数
        var redLastSeen = new Dictionary<int, int>();
        var blueLastSeen = new Dictionary<int, int>();

        // 初始化奇偶比、和值区间和分区统计的字典
        var oddEvenCount = new Dictionary<string, int>();
        var sumDistribution = new Dictionary<string, int>();
        var zoneDistribution = new Dictionary<string, int>();

        // 遍历每一期彩票数据
        for (int i = 0; i < dataList.Count; i++)
        {
            var reds = dataList[i].RedBalls.Split(' ').Select(int.Parse).ToList(); // 当前期的红球号码
            var blue = int.Parse(dataList[i].BlueBall); // 当前期的蓝球号码

            // --- 1. 遗漏计算 ---
            foreach (var r in redOmission.Keys.ToList())
            {
                redOmission[r]++; // 红球遗漏期数递增
                if (reds.Contains(r))
                {
                    redLastSeen[r] = i; // 记录红球上次出现的期数
                    redOmission[r] = 0; // 重置遗漏期数
                }
            }

            foreach (var b in blueOmission.Keys.ToList())
            {
                blueOmission[b]++; // 蓝球遗漏期数递增
                if (b == blue)
                {
                    blueLastSeen[b] = i; // 记录蓝球上次出现的期数
                    blueOmission[b] = 0; // 重置遗漏期数
                }
            }

            // --- 2. 奇偶比 ---
            int odd = reds.Count(r => r % 2 != 0); // 统计奇数个数
            int even = 6 - odd; // 统计偶数个数
            var key = $"{odd}:{even}"; // 奇偶比的键
            oddEvenCount[key] = oddEvenCount.GetValueOrDefault(key) + 1; // 更新奇偶比统计

            // --- 3. 和值区间 ---
            int sum = reds.Sum(); // 计算红球和值
            string sumZone = sum switch
            {
                <= 80 => "小于等于80",
                <= 120 => "81~120",
                _ => "大于120"
            };
            // 更新和值区间统计
            sumDistribution[sumZone] = sumDistribution.GetValueOrDefault(sumZone) + 1;

            // --- 4. 分区统计 ---
            int z1 = reds.Count(n => n >= 1 && n <= 11); // 第一区间（1~11）的红球个数
            int z2 = reds.Count(n => n >= 12 && n <= 22); // 第二区间（12~22）的红球个数
            int z3 = reds.Count(n => n >= 23 && n <= 33); // 第三区间（23~33）的红球个数
            string zoneKey = $"[{z1}-{z2}-{z3}]"; // 分区统计的键
            // 更新分区统计
            zoneDistribution[zoneKey] = zoneDistribution.GetValueOrDefault(zoneKey) + 1;
        }

        // 输出红球遗漏
        Console.WriteLine("\n 红球遗漏情况（前15）:");
        foreach (var kv in redOmission.OrderByDescending(kv => kv.Value).Take(15))
        {
            Console.Write($"[{kv.Key:D2}:{kv.Value}] ");
        }

        // 输出蓝球遗漏统计（全部）
        Console.WriteLine("\n\n 蓝球遗漏情况（全部）:");
        foreach (var kv in blueOmission.OrderByDescending(kv => kv.Value))
        {
            Console.Write($"[{kv.Key:D2}:{kv.Value}] ");
        }

        // 奇偶统计
        Console.WriteLine("\n\n 奇偶比分布:");
        foreach (var kv in oddEvenCount.OrderByDescending(kv => kv.Value))
        {
            Console.WriteLine($"奇偶比 {kv.Key} 出现 {kv.Value} 次");
        }

        // 和值统计
        Console.WriteLine("\n 红球和值区间分布:");
        foreach (var kv in sumDistribution.OrderByDescending(kv => kv.Value))
        {
            Console.WriteLine($"{kv.Key}：{kv.Value} 次");
        }

        // 分区统计
        Console.WriteLine("\n 红球三区间分布（[01-11]-[12-22]-[23-33]）:");
        foreach (var kv in zoneDistribution.OrderByDescending(kv => kv.Value).Take(10))
        {
            Console.WriteLine($"{kv.Key}：{kv.Value} 次");
        }

        Console.WriteLine("\n 高级分析完毕！");
    }
}
