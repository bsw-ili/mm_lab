using Newtonsoft.Json;
using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class Choice
{
    public ChatMessage message;
}

public static class UnityWebRequestAwaiterExtension
{
    public static TaskAwaiter GetAwaiter(this UnityWebRequestAsyncOperation asyncOp)
    {
        var tcs = new System.Threading.Tasks.TaskCompletionSource<object>();
        asyncOp.completed += _ => tcs.SetResult(null);
        return ((System.Threading.Tasks.Task)tcs.Task).GetAwaiter();
    }
}

public class OpenAIExtractor : MonoBehaviour
{
    private string apiKey;
    private string apiUrl;

    private const string MODEL_NAME = "gpt-4.1"; // ✅ 提取常量

    void Awake()
    {
        apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("❌ 未找到环境变量 OPENAI_API_KEY，请先在系统中设置！");
            return;
        }

        apiUrl = "https://api.vveai.com/v1/chat/completions";
    }

    /// <summary>
    /// 异步提取实验操作动作
    /// </summary>
    public async Task<string> Extract(string reply)
    {
        if (string.IsNullOrWhiteSpace(reply))
        {
            Debug.LogWarning("⚠️ 输入文本为空，返回空结果。");
            return "无操作文本";
        }

        string prompt = BuildPrompt(reply);
        ChatRequest requestData = CreateRequest(prompt);

        string jsonBody = JsonConvert.SerializeObject(requestData);
        try
        {
            using (UnityWebRequest www = new UnityWebRequest(apiUrl, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");
                www.SetRequestHeader("Authorization", $"Bearer {apiKey}");

                // ✅ 使用真正异步等待，不阻塞主线程
                await www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    return ParseResponse(www.downloadHandler.text);
                }
                else
                {
                    Debug.LogError($"❌ 网络错误: {www.result}\n{www.error}\n{www.downloadHandler.text}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ OpenAIExtractor 异常: {ex.Message}");
        }

        return "无操作文本"; // 默认兜底
    }

    // ✅ 单独封装：构造请求体
    private static ChatRequest CreateRequest(string prompt)
    {
        return new ChatRequest
        {
            model = MODEL_NAME,
            messages = new ChatMessage[]
            {
                new ChatMessage { role = "system", content = "你是一个化学实验助手。" },
                new ChatMessage { role = "user", content = prompt }
            },
            max_tokens = 512
        };
    }

    // ✅ 单独封装：生成提示词
    private static string BuildPrompt(string reply)
    {
        return $@"
        你是一名化学实验助手。
        任务：从学生的回答文本中提取其中的具体“操作动作”，并按列表输出。

        【提取规则】
        1. 操作动作必须涉及实验器材（如beaker、test_tube、spray_bottle等）。
        2. 操作动作必须是学生在实验中“实际进行”的行为，如加热、连接、倒扣、倾倒、插入、装满、点燃等。
        3. 操作文本必须保留学生原文，不得改写。
        4. 排除以下情况：
           - 疑问或不确定语气（如“能不能”、“如何”、“是不是”）。
           - 涉及人或身体的动作（如“用手拿着”、“我拿起”，“移开”，“移动”）。
           - 仅描述现象或结果（如“产生气泡”、“水变浑浊”）。
           - 警示性或禁止性内容（如“不能…”、“不要…”）。
           - 仅为准备或取用器材的行为（如“取一个烧杯”、“准备一支试管”、“拿一个漏斗”），这些不属于实验操作。
           - 提醒或要求（如“注意不要让水溅出”、“确保连接紧密”，“要用外焰加热”）。
        5. 每个操作动作语句中最多包含包含两个实验器材。
        6. 对于存在多个操作动作的语句（存在多于两个实验器材），拆分成单个操作动作语句。
           如：“用试管夹把试管固定在铁架台上。”，存在三个实验器材，拆分成单个操作动作语句：“试管夹把试管固定”，“试管固定在铁架台上”。 
        7. 若无有效操作动作，输出“无操作文本”。

        【输出格式】
        - 操作文本：
          - [操作动作1]
          - [操作动作2]
          - …
        - 若无操作 → 输出：无操作文本

        【示例】  
        学生回答：  
        “取一个试管，加入稀盐酸，用酒精灯加热，最后移开酒精灯让其冷却。”  

        输出：  
        - 操作文本：  
          - 加入稀盐酸  
          - 用酒精灯加热  

        学生回答：  
        “用试管夹把试管固定在铁架台上。”  

        输出：  
        - 操作文本：  
          - 试管夹把试管固定  
          - 试管固定在铁架台上 

        学生回答：
        {reply}
        ";
    }

    // ✅ 单独封装：解析响应
    private static string ParseResponse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            Debug.LogWarning("⚠️ OpenAI 返回内容为空。");
            return "无操作文本";
        }

        try
        {
            var response = JsonConvert.DeserializeObject<ChatResponse>(json);
            if (response?.choices != null && response.choices.Length > 0)
            {
                string result = response.choices[0].message.content?.Trim();
                return string.IsNullOrEmpty(result) ? "无操作文本" : result;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ 解析返回 JSON 时出错: {ex.Message}\n内容: {json}");
        }

        return "无操作文本";
    }

    /// <summary>
    /// 异步翻译化学实验分析文本为英文
    public async Task<string> TranslateToEnglish(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            Debug.LogWarning("⚠️ 翻译输入为空。");
            return "";
        }

        string prompt = BuildTranslationPrompt(input);
        ChatRequest requestData = new ChatRequest
        {
            model = "gpt-4.1-mini",
            messages = new ChatMessage[]
            {
            new ChatMessage
            {
                role = "system",
                content = "You are a professional English translator specialized in chemistry experiments."
            },
            new ChatMessage
            {
                role = "user",
                content = prompt
            }
            },
            max_tokens = 1024
        };

        string jsonBody = JsonConvert.SerializeObject(requestData);

        try
        {
            using (UnityWebRequest www = new UnityWebRequest(apiUrl, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");
                www.SetRequestHeader("Authorization", $"Bearer {apiKey}");

                await www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    string responseText = www.downloadHandler.text;
                    string translation = ParseResponse(responseText);
                    Debug.Log("🌍 翻译成功: " + translation);
                    return translation;
                }
                else
                {
                    Debug.LogError($"❌ 翻译请求失败: {www.result}\n{www.error}\n{www.downloadHandler.text}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ TranslateToEnglish 异常: {ex.Message}");
        }

        return "";
    }

    /// <summary>
    /// 构建翻译提示词
    /// </summary>
    private static string BuildTranslationPrompt(string input)
    {
        return $@"
    Translate the following chemistry experiment analysis text into fluent and precise English.
    The translation should preserve all chemical terms, apparatus names, and logical relations clearly.

    【Input】:
    {input}

    【Output】:
    (Only provide the translated English text without any explanation)
    ";
        }

}
