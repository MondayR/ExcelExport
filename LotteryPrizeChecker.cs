// 新建文件 LotteryPrizeChecker.cs
using System.Collections.Generic;
using System.Linq;

public static class LotteryPrizeChecker
{
    /// <summary>
    /// 计算给定预测与实际开奖结果的中奖等级。
    /// </summary>
    /// <param name="predictionReds">预测的红球列表 (6个, 已排序)。</param>
    /// <param name="predictionBlue">预测的蓝球。</param>
    /// <param name="actualReds">实际开奖的红球列表 (6个)。</param>
    /// <param name="actualBlue">实际开奖的蓝球。</param>
    /// <returns>中奖等级描述字符串。</returns>
    public static string CheckPrize(List<int> predictionReds, int predictionBlue, List<int> actualReds, int actualBlue)
    {
        if (predictionReds == null || predictionReds.Count != 6 || actualReds == null || actualReds.Count != 6)
        {
            return "无效输入"; // 或抛出异常
        }

        int redMatchCount = predictionReds.Count(pr => actualReds.Contains(pr));
        bool blueMatch = predictionBlue == actualBlue;

        return (redMatchCount, blueMatch) switch
        {
            (6, true) => "一等奖",
            (6, false) => "二等奖",
            (5, true) => "三等奖",
            (5, false) => "四等奖",
            (4, true) => "四等奖", // 4+1 也是四等奖
            (4, false) => "五等奖",
            (3, true) => "五等奖", // 3+1 也是五等奖
            (2, true) => "六等奖",
            (1, true) => "六等奖",
            (0, true) => "六等奖",
            _ => "未中奖"
        };
    }
}