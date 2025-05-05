using Microsoft.ML.Data;

public class ModelInput
{
    // 红球历史特征（前几期的每个号码）
    [VectorType(30)] // 假设 5 期 * 6 个球 = 30
    public float[] Features { get; set; }

    // 红球标签（下期的6个红球）
    public uint RedBall1 { get; set; }
    public uint RedBall2 { get; set; }
    public uint RedBall3 { get; set; }
    public uint RedBall4 { get; set; }
    public uint RedBall5 { get; set; }
    public uint RedBall6 { get; set; }

    // 蓝球标签（单分类）
    [ColumnName("BlueLabel")]
    public uint BlueLabel { get; set; }
}