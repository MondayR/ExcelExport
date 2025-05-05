
/// <summary>
/// 代表一次预测结果，包含期号、红球和蓝球。
/// </summary>
public class PredictionResult
{
    /// <summary>
    /// 预测对应的期号 (初始为空，待回填)
    /// </summary>
    public string Issue { get; set; } = string.Empty; // 默认为空字符串

    /// <summary>
    /// 预测的红球列表 (已排序)
    /// </summary>
    public List<int> Reds { get; set; }

    /// <summary>
    /// 预测的蓝球
    /// </summary>
    public int Blue { get; set; }

    /// <summary>
    /// 中奖情况 (待回填)
    /// </summary>
    public string PrizeInfo { get; set; } = string.Empty; // 默认为空

    // 为了方便导出，提供格式化的字符串
    public string RedBallsString => string.Join(" ", Reds.Select(n => n.ToString("D2")));
    public string BlueBallString => Blue.ToString("D2");
}