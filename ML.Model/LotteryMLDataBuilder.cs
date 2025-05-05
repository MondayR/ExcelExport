/// <summary>
/// 提供用于构建双色球彩票机器学习训练数据的功能。
/// </summary>
public static class LotteryMLDataBuilder
{
    /// <summary>
    /// 构建用于机器学习模型训练的输入数据。
    /// </summary>
    /// <param name="history">包含彩票历史记录的列表。</param>
    /// <param name="lookback">回溯期数，用于生成特征数据，默认为 5。</param>
    /// <returns>返回构建好的训练数据列表，每条数据包含特征、红球标签和蓝球标签。</returns>
    public static List<ModelInput> BuildTrainingData(List<LotteryData> history, int lookback = 5)
    {
        var result = new List<ModelInput>();

        for (int i = lookback; i < history.Count; i++)
        {
            var features = new List<float>();
            for (int j = i - lookback; j < i; j++)
            {
                var reds = history[j].RedBalls.Split(' ').Select(int.Parse).ToArray();
                features.AddRange(reds.Select(r => (float)r));
            }

            var nextReds = history[i].RedBalls.Split(' ').Select(uint.Parse).ToArray();
            Array.Sort(nextReds);
            var blue = uint.Parse(history[i].BlueBall);

            result.Add(new ModelInput
            {
                Features = features.ToArray(),
                RedBall1 = nextReds[0],
                RedBall2 = nextReds[1],
                RedBall3 = nextReds[2],
                RedBall4 = nextReds[3],
                RedBall5 = nextReds[4],
                RedBall6 = nextReds[5],
                BlueLabel = blue
            });
        }

        return result;
    }
}