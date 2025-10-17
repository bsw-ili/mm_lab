using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;   // 需要有 Newtonsoft.Json.dll

//[Serializable]
//public class ChatMessage
//{
//    public string role;
//    public string content;
//}

//[Serializable]
//public class ChatRequest
//{
//    public string model;
//    public ChatMessage[] messages;
//    public int max_tokens;
//}

//[Serializable]
//public class ChatResponse
//{
//    public Choice[] choices;
//}

//[Serializable]
//public class Choice
//{
//    public Message message;
//}

//[Serializable]
//public class Message
//{
//    public string role;
//    public string content;
//}

public class TestExtract : MonoBehaviour
{
    private string apiKey;
    private string apiUrl = "https://api.chatanywhere.tech/v1/chat/completions";

    async void Start()
    {
        // 读取 API Key
        apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        Debug.Log("API Key 状态: " + (string.IsNullOrEmpty(apiKey) ? "❌ 空" : "✅ 存在"));

        if (string.IsNullOrEmpty(apiKey))
            return;

        // 测试调用
        string result = await Extract("在集气瓶中注满水");
        Debug.Log("最终结果:\n" + result);
    }

    public async Task<string> Extract(string input)
    {
        ChatRequest requestData = new ChatRequest
        {
            model = "gpt-4.1-mini",
            messages = new ChatMessage[]
            {
                new ChatMessage { role = "system", content = "你是一个测试机器人。" },
                new ChatMessage { role = "user", content = input }
            },
            max_tokens = 64
        };

        // ✅ 用 Newtonsoft.Json 序列化，避免 Unity 自带 JsonUtility 的限制
        string jsonBody = JsonConvert.SerializeObject(requestData);
        Debug.Log("请求 JSON:\n" + jsonBody);

        using (UnityWebRequest www = new UnityWebRequest(apiUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("Authorization", "Bearer " + apiKey);

            var operation = www.SendWebRequest();
            while (!operation.isDone) await Task.Yield();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("✅ Response 原始文本:\n" + www.downloadHandler.text);
                return www.downloadHandler.text;
            }
            else
            {
                Debug.LogError("❌ OpenAI Error: " + www.error + "\nResponse: " + www.downloadHandler.text);
                return null;
            }
        }
    }
}
