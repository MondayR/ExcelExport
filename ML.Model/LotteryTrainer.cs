
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms;
// using System.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text; // 需要

/// <summary>
/// 提供双色球彩票的机器学习训练和预测功能
/// </summary>
public static class LotteryTrainer
{
    /// <summary>
    /// 使用 ML.NET 对双色球彩票数据进行训练，并基于最后一组数据进行预测。
    /// </summary>
    /// <param name="data">包含彩票历史记录的输入数据列表。</param>
    public static PredictionResult? TrainAndPredict(List<ModelInput> data)
    {
        if (data == null || data.Count == 0) { Console.WriteLine("错误：训练数据为空。"); return null; }

        var mlContext = new MLContext(seed: 0);

        // --- ★ 手动创建 Key-Value 映射 ---
        Console.WriteLine("步骤 ★: 手动创建蓝球 Key-Value 映射...");
        var uniqueBlueValues = data.Select(d => d.BlueLabel).Distinct().OrderBy(b => b).ToList();
        var valueToKeyMap = new Dictionary<uint, uint>();
        var keyToValueMap = new Dictionary<uint, uint>(); // Key: 0..N-1, Value: 原始蓝球号 (1-16)
        for (int i = 0; i < uniqueBlueValues.Count; i++)
        {
            uint originalValue = uniqueBlueValues[i]; uint key = (uint)i; // Key 从 0 开始
            valueToKeyMap[originalValue] = key; keyToValueMap[key] = originalValue;
            Console.WriteLine($"  手动映射: Key: {key} <-> 原始蓝球: {originalValue:D2}");
        }
        if (keyToValueMap.Count == 0) { Console.WriteLine("错误：未能从输入数据中找到任何蓝球值来创建手动映射。"); return null; }
        uint maxValidKey = (uint)(keyToValueMap.Count - 1); // 记录最大的有效 Key
        Console.WriteLine($" 手动映射创建完成，共 {keyToValueMap.Count} 个蓝球值 (有效 Keys: 0-{maxValidKey})。");


        // --- 1. 加载数据 ---
        Console.WriteLine("步骤 1: 加载数据...");
        IDataView trainData = mlContext.Data.LoadFromEnumerable(data);
        Console.WriteLine($" 数据加载完成。");

        // --- 2. 构建数据处理管道 ---
        Console.WriteLine("步骤 2: 构建数据处理管道...");
        var pipeline = mlContext.Transforms.Concatenate("Features", nameof(ModelInput.Features))
            .Append(mlContext.Transforms.Conversion.MapValueToKey("Ball1_Label", nameof(ModelInput.RedBall1))) // Etc. for Red Balls
            .Append(mlContext.Transforms.Conversion.MapValueToKey("Ball2_Label", nameof(ModelInput.RedBall2)))
            .Append(mlContext.Transforms.Conversion.MapValueToKey("Ball3_Label", nameof(ModelInput.RedBall3)))
            .Append(mlContext.Transforms.Conversion.MapValueToKey("Ball4_Label", nameof(ModelInput.RedBall4)))
            .Append(mlContext.Transforms.Conversion.MapValueToKey("Ball5_Label", nameof(ModelInput.RedBall5)))
            .Append(mlContext.Transforms.Conversion.MapValueToKey("Ball6_Label", nameof(ModelInput.RedBall6)))
            .Append(mlContext.Transforms.Conversion.MapValueToKey( // 仍然需要，用于训练器输入
                outputColumnName: "BlueLabelKey", inputColumnName: nameof(ModelInput.BlueLabel),
                keyOrdinality: ValueToKeyMappingEstimator.KeyOrdinality.ByOccurrence))
            .Append(mlContext.Transforms.NormalizeMinMax("Features"));
        Console.WriteLine(" 数据处理管道构建完成。");

        // --- 3. 拟合管道并转换数据 ---
        Console.WriteLine("步骤 3: 拟合管道并转换数据...");
        ITransformer preprocessedPipeline; IDataView preprocessedData;
        try
        {
            preprocessedPipeline = pipeline.Fit(trainData);
            preprocessedData = preprocessedPipeline.Transform(trainData);
            Console.WriteLine(" 数据预处理完成。");
        }
        catch (Exception ex) { Console.WriteLine($" 数据预处理阶段出错: {ex.Message}"); return null; }

        // --- 4. 训练红球模型 ---
        Console.WriteLine("步骤 4: 训练红球模型...");
        // (省略红球训练代码以保持简洁)
        var ballModels = new List<(ITransformer model, string label)>();
        try { 
            for (int i = 1; i <= 6; i++) 
            { 
                /* ... 红球训练 ... */ 
                var labelColumnName = $"Ball{i}_Label"; 
                var predictionColumnName = $"PredictedBall{i}"; 
                var trainer = mlContext.MulticlassClassification.Trainers.LightGbm(labelColumnName, "Features")
                    .Append(mlContext.Transforms.Conversion.MapKeyToValue(predictionColumnName, "PredictedLabel")); 
                Console.WriteLine($"  正在训练红球模型: {labelColumnName}..."); 
                var model = trainer.Fit(preprocessedData); 
                ballModels.Add((model, labelColumnName)); 
            } 
            Console.WriteLine(" 红球模型训练完成。"); 
        } 
        catch (Exception ex) 
        { 
            Console.WriteLine($" 训练红球模型时出错: {ex.Message}"); 
            return null ; 
        }


        // --- 5. 训练蓝球模型 ---
        Console.WriteLine("步骤 5: 训练蓝球模型...");
        ITransformer blueModel;
        try
        {
            var blueTrainer = mlContext.MulticlassClassification.Trainers.LightGbm("BlueLabelKey", "Features"); // 输出 PredictedLabel (Key) 和 Score
            blueModel = blueTrainer.Fit(preprocessedData);
            Console.WriteLine(" 蓝球模型训练完成。");
        }
        catch (Exception ex) { Console.WriteLine($" 训练蓝球模型时出错: {ex.Message}"); return null; }

        // --- 6. 评估蓝球模型 ---
        Console.WriteLine("\n步骤 6: 评估蓝球模型性能...");
        try
        { /* ... 评估代码 ... */
            var bluePredictionsForEval = blueModel.Transform(preprocessedData); var blueMetrics = mlContext.MulticlassClassification.Evaluate(bluePredictionsForEval, "BlueLabelKey", scoreColumnName: "Score");
            Console.WriteLine("*   MicroAccuracy (微观准确率):    {0:P2}", blueMetrics.MicroAccuracy); Console.WriteLine("*   MacroAccuracy (宏观准确率):    {0:P2}", blueMetrics.MacroAccuracy);
            Console.WriteLine("*   LogLoss (对数损失):          {0:#.###}", blueMetrics.LogLoss); Console.WriteLine("*   LogLossReduction (对数损失减少): {0:#.###}", blueMetrics.LogLossReduction);
            Console.WriteLine("*-----------------------------------------------------------");
        }
        catch (Exception evalEx) { Console.WriteLine($" 评估蓝球模型时出错: {evalEx.Message}"); }

        // --- 7. 进行预测 (红球) ---
        Console.WriteLine("\n步骤 7: 生成红球预测...");
        // (省略红球预测代码以保持简洁)
        var lastInputForRed = data.LastOrDefault(); if (lastInputForRed == null) { Console.WriteLine("错误：无法获取最后一个输入数据点进行红球预测。"); return null; }
        var rnd = new Random(); 
        List<uint> finalRedBalls = null; 
        int redAttempts = 0; 
        const int MAX_RED_ATTEMPTS = 20; 
        while (finalRedBalls == null && redAttempts < MAX_RED_ATTEMPTS) 
        { 
            /* ... 红球预测循环 ... */ 
            redAttempts++; 
            var featuresToPredict = lastInputForRed.Features; 
            var predictionInput = new ModelInput { Features = featuresToPredict }; 
            var predictedRedBalls = new List<uint>(); 
            for (int i = 0; i < ballModels.Count; i++) 
            { 
                var (model, _) = ballModels[i]; 
                var engine = mlContext.Model.CreatePredictionEngine<ModelInput, RedBallModelOutput>(model); 
                var prediction = engine.Predict(predictionInput); 
                try { 
                    var propInfo = typeof(RedBallModelOutput).GetProperty($"PredictedBall{i + 1}"); 
                    if (propInfo != null) 
                        predictedRedBalls.Add(Convert.ToUInt32(propInfo.GetValue(prediction))); 
                    else 
                        Console.WriteLine($"警告：RedBallModelOutput缺少属性 PredictedBall{i + 1}"); 
                } 
                catch (Exception ex) 
                { 
                    Console.WriteLine($"错误：获取 PredictedBall{i + 1} 时出错: {ex.Message}"); 
                } 
            } 
            var uniqueSortedReds = predictedRedBalls.Distinct().OrderBy(n => n).ToList(); 
            if (uniqueSortedReds.Count == 6) 
            { 
                finalRedBalls = uniqueSortedReds; 
                Console.WriteLine($" 红球组合生成成功 (尝试 {redAttempts} 次)."); 
                break; 
            } 
        }
        if (finalRedBalls == null) 
        { 
            Console.WriteLine($"\n警告：在 {MAX_RED_ATTEMPTS} 次尝试后未能生成有效的 6 个唯一红球组合。"); 
            return null; 
        }

        // --- 8. 进行预测 (蓝球) 并使用手动映射解释 ---
        Console.WriteLine("\n步骤 8: 生成蓝球预测详情 (使用手动映射)...");
        var lastInputForBlue = data.LastOrDefault();
        if (lastInputForBlue == null) { Console.WriteLine("错误：无法获取最后一个输入数据点进行蓝球预测。"); return null; }

        PredictionEngine<ModelInput, BlueBallModelOutput> blueEngine;
        try { blueEngine = mlContext.Model.CreatePredictionEngine<ModelInput, BlueBallModelOutput>(blueModel); }
        catch (Exception engineEx) { Console.WriteLine($" 创建蓝球预测引擎时出错: {engineEx.Message}"); return null; }

        var bluePredictionInput = new ModelInput { Features = lastInputForBlue.Features };
        BlueBallModelOutput blueResult;
        try { blueResult = blueEngine.Predict(bluePredictionInput); }
        catch (Exception predictEx) { Console.WriteLine($" 执行蓝球预测时出错: {predictEx.Message}"); return null; }

        uint predictedKey = blueResult.PredictedLabel; // 模型直接输出的 Key
        Console.WriteLine($"  模型预测的内部 Key (PredictedLabel): {predictedKey}");
        Console.WriteLine($"  预测结果中的 Score 数组长度: {(blueResult.Score?.Length ?? 0)}");

        string finalPredictedBlueValueStr = "N/A"; // 最终预测结果
        List<(string BlueValue, float Score, uint Key)> scoreList = new List<(string BlueValue, float Score, uint Key)>();

        // 检查手动映射和 Score 数组是否有效
        if (keyToValueMap.Count > 0 && blueResult.Score != null && blueResult.Score.Length > 0)
        {
            Console.WriteLine("\n 蓝球预测概率 (Scores - 使用手动映射解释):");
            // 检查 Score 数组长度是否合理 (应该等于 Key 的数量)
            if (blueResult.Score.Length != keyToValueMap.Count)
            {
                Console.WriteLine($" 警告: Score 数组长度 ({blueResult.Score.Length}) 与手动映射 Key 数量 ({keyToValueMap.Count}) 不匹配！解释可能不准确。");
                // 如果长度不匹配，我们只能解释 Score 数组中存在的索引对应的 Key
            }

            // 使用手动映射构建 ScoreList，只处理有效的 Key
            foreach (var kvp in keyToValueMap.OrderBy(kv => kv.Key))
            {
                uint currentKey = kvp.Key;
                uint originalValue = kvp.Value;
                string blueValueStr = originalValue.ToString("D2");
                // 确保 Score 数组包含这个 Key 的索引
                if (currentKey < blueResult.Score.Length)
                {
                    float score = blueResult.Score[currentKey];
                    scoreList.Add((blueValueStr, score, currentKey));
                }
                else
                {
                    Console.WriteLine($"  警告: 手动映射 Key {currentKey} 超出了 Score 数组的长度 ({blueResult.Score.Length})，无法获取其分数。");
                }
            }

            // 基于 scoreList (已排序) 确定最终预测
            if (scoreList.Count > 0)
            {
                Console.WriteLine("  (Top 5 预测概率)");
                foreach (var item in scoreList.OrderByDescending(s => s.Score).Take(5))
                {
                    Console.WriteLine($"  蓝球: {item.BlueValue} (Key: {item.Key}) - Score: {item.Score:F6}");
                }

                // *** 核心修正：不再首先尝试用 predictedKey 查找 ***
                // *** 直接使用得分最高的 Key 作为最可靠的预测依据 ***
                var topScoreItem = scoreList.OrderByDescending(s => s.Score).First(); // 获取分数最高的项

                if (keyToValueMap.TryGetValue(topScoreItem.Key, out uint topScoreOriginalValue))
                {
                    finalPredictedBlueValueStr = topScoreOriginalValue.ToString("D2");
                    Console.WriteLine($"\n 模型最可能的预测 (基于最高分): {finalPredictedBlueValueStr} (来自 Key: {topScoreItem.Key}, Score: {topScoreItem.Score:F6})");

                    // 检查模型直接预测的 Key 是否有效，并与最高分比较
                    if (keyToValueMap.ContainsKey(predictedKey))
                    {
                        if (predictedKey != topScoreItem.Key)
                        {
                            Console.WriteLine($"  注意: 模型直接预测的 Key ({predictedKey}) 与得分最高的 Key ({topScoreItem.Key}) 不同。已采用最高分结果。");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"  警告: 模型直接预测的 Key ({predictedKey}) 无效或不在手动映射范围内。已采用最高分结果。");
                    }
                }
                else
                {
                    // 这种情况几乎不可能发生，因为 topScoreItem.Key 来自 keyToValueMap
                    Console.WriteLine($"  严重错误：无法在手动映射中找到得分最高的 Key ({topScoreItem.Key})！");
                    finalPredictedBlueValueStr = "错误"; // 标记错误
                }
            }
            else { Console.WriteLine(" 错误: 无法根据 Score 和手动映射关联预测结果 (scoreList 为空)。"); }
        }
        else { Console.WriteLine("\n 错误: 手动 Key-Value 映射为空或 Score 数组无效，无法解释预测。"); }


        // --- 9. 输出最终的组合预测 ---
        Console.WriteLine("\n ===== ML.NET 最终预测结果 =====");
        PredictionResult mlPrediction = null; // 初始化为 null

        if (finalRedBalls != null && finalPredictedBlueValueStr != "N/A" && finalPredictedBlueValueStr != "错误")
        {
            Console.WriteLine($" 红球推荐: {string.Join(" ", finalRedBalls.Select(n => n.ToString("D2")))}");
            Console.WriteLine($" 蓝球推荐: {finalPredictedBlueValueStr}");
            // 创建 PredictionResult 对象
            mlPrediction = new PredictionResult
            {
                // 将 uint 转换为 int
                Reds = finalRedBalls.Select(r => (int)r).ToList(),
                // 解析蓝球字符串为 int
                Blue = int.TryParse(finalPredictedBlueValueStr, out int blueVal) ? blueVal : -1 // 如果解析失败用-1标记
            };
            if (mlPrediction.Blue == -1)
            {
                Console.WriteLine(" 警告: 无法将预测的蓝球值解析为整数。");
                mlPrediction = null; // 标记预测无效
            }
        }
        else { 
            Console.WriteLine(" 红球推荐: 未能生成有效的组合。");
            Console.WriteLine($" 蓝球推荐: {finalPredictedBlueValueStr}"); // 仍然打印出来，即使可能是 N/A 或错误
        }
        Console.WriteLine(" =================================");
        return mlPrediction; // 返回 PredictionResult 或 null

    } // TrainAndPredict 方法结束
} // LotteryTrainer 类结束