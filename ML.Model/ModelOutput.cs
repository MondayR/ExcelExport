using Microsoft.ML.Data;

public class ModelOutput
{
    // 这个 PredictedBlue 是由 MapKeyToValue 从 PredictedLabel (Key) 转换回来的原始值
    [ColumnName("PredictedBlue")]
    public uint PredictedBlue { get; set; }

    // Score 数组的索引对应于 MapValueToKey 生成的 Key (通常从 0 开始)
    [ColumnName("Score")]
    public float[] Score { get; set; }

    // 这些属性需要由对应的 MapKeyToValue 转换器填充
    // 命名需要与 LotteryTrainer 中 MapKeyToValue 的 outputColumnName 参数一致
    public uint PredictedBall1 { get; set; }
    public uint PredictedBall2 { get; set; }
    public uint PredictedBall3 { get; set; }
    public uint PredictedBall4 { get; set; }
    public uint PredictedBall5 { get; set; }
    public uint PredictedBall6 { get; set; }
}


// 用于红球预测 (包含 MapKeyToValue 的结果)
public class RedBallModelOutput
{
    // PredictedLabel 是 Key 类型, MapKeyToValue 将其转换为原始值存入 PredictedBallX
    public uint PredictedBall1 { get; set; }
    public uint PredictedBall2 { get; set; }
    public uint PredictedBall3 { get; set; }
    public uint PredictedBall4 { get; set; }
    public uint PredictedBall5 { get; set; }
    public uint PredictedBall6 { get; set; }
    // 红球通常不需要看 Score
}


// 用于蓝球预测 (只包含原始 Key 和 Score)
public class BlueBallModelOutput
{
    // Trainer 直接输出预测的 Key
    [ColumnName("PredictedLabel")] // Trainer 输出的默认列名是 PredictedLabel
    public uint PredictedLabel { get; set; } // 这是 Key 类型 (0, 1, ...)

    // Trainer 输出的 Score 数组
    [ColumnName("Score")]
    public float[] Score { get; set; } // 索引对应 Key (0, 1, ...)
}