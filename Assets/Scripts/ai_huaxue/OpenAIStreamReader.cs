using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.Collections.Generic;
using TMPro;
using System;
using System.Collections;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

public class OpenAIStreamReader : MonoBehaviour
{
    public TMP_Text outputText;
    public TMP_FontAsset chineseFont;
    public LLMProvider provider = LLMProvider.OpenAI;
    public float typingSpeed = 30f; // 每秒字符数

    private LLMConfig openAI = new LLMConfig(
        "https://api.chatanywhere.tech/v1/chat/completions",
        "gpt-4o",
        "sk-qXb5DTWhorrVY2JnMPgRmow3ClQdJVf7FSPO7hhXZWmj7WDA" // 请替换成你的Key
    );

    private StringBuilder currentResponse = new StringBuilder();
    private Queue<char> typingQueue = new Queue<char>();
    private float typingTimer = 0f;
    public List<string> steps;
    public List<string> imagePaths; // 图片路径列表
    private Message message;
    private string steps_str;
    public List<String> imagePaths_base64;
    public string dialogue;


    void Start()
    {
        outputText.font = chineseFont;
        steps_str = string.Join(" ", steps);
        dialogue = "[Teacher]:"+steps[0];
        imagePaths_base64 = new List<string>();
        foreach(string img in imagePaths)
        {
            imagePaths_base64.Add(ImageConverter.LoadImageToBase64(img));
        }
    }


    void Update()
    {
        if (typingQueue.Count > 0)
        {
            typingTimer += Time.deltaTime * typingSpeed;
            while (typingTimer >= 1f && typingQueue.Count > 0)
            {
                outputText.text += typingQueue.Dequeue();
                typingTimer -= 1f;
            }
        }
    }


    public List<Content> setPrompt(string steps_str, string dialogue, List<string> imagePaths)
    {
        string prompt = $@"
        我是一个中学生[Student]。你是一位老师[Teacher]，总是用苏格拉底式教学法来回应我。
            你不能直接告诉我化学实验的操作步骤，而是努力提出恰当的问题，引导我找到正确的化学实验操作步骤，直到完成化学实验。
            请从“[Step 1]”开始提问，提问应包含所有的[Step N]！！！（N为当前步骤的编号，提问含义相同即可，并非要完全一致）
            如果你认为你已经化学实验已经教完了（即用户做完了所有的步骤），请告诉学生该实验已经完成。
            你的回复需要满足以下符合以下教学标准：
            -  如果[Student]在提问的是常识性问题或在理解某个概念时感到困难，那么[Teacher]是可以直接进行解释的。
            -  在其他情况下，不允许以陈述句或修辞方式直接给出化学实验的操作步骤。
            -  [Teacher]需要首先对[Student]当前做的化学实验操作步骤的正确性做出判断，并给出原因，然后使用提问的方式引导[Student]思考下一个步骤，
            -  引导[Student]的问题不宜过细，保证对话不会过于冗长。
            -  [Teacher]需要以一名老师的口吻来讲话，不得使用诸如“让我问你”这类表达方式。
            -  你可以使用鼓励的语气对[Student]正确的化学实验操作步骤进行肯定，而不是僵硬的使用“你提到”。
            -  你的回复应当引导[Student]完成某一个具体的化学实验操作。
            -  仅以[Teacher]回复一次，等待[Student]的回复。

            以下是一些示例:
            #化学实验:[[image1]]
            #步骤:[step 1]我们如何开始准备加热水的试管呢？\n[step 2]如何确保水在试管中开始沸腾？\n[step 3]我们如何观察水蒸气的冷凝现象？
            #对话: [Teacher]: 我们如何开始准备加热水的试管呢？\n[Student]: [[image2]]\n
            #回复:[Teacher]: 非常正确！试管斜夹在铁架台上, 可以很方便的对试管进行加热，接下来，我们需要确保水在试管中开始沸腾。你觉得我们该怎么做呢？\n

            这是目标化学实验:
            #化学实验：[[image3]]
            #步骤:{steps_str}
            #对话: {dialogue}
            #回复:
        ";
        return BuildMultimodalContentWithBase64(prompt, imagePaths);
    }

    // 修正：返回 List<Content>
    public List<Content> BuildMultimodalContentWithBase64(string text, List<string> imagePaths)
    {
        var contentList = new List<Content>();
        var pattern = @"\[\[image(\d+)]]";
        int lastIndex = 0;

        foreach (Match match in Regex.Matches(text, pattern))
        {
            int start = match.Index;
            int end = match.Index + match.Length;
            int imgIndex = int.Parse(match.Groups[1].Value) - 1;

            if (start > lastIndex)
            {
                contentList.Add(new Content
                {
                    type = "text",
                    text = text.Substring(lastIndex, start - lastIndex)
                });
            }

            if (imgIndex >= 0 && imgIndex < imagePaths.Count)
            {
                string dataUri = "data:image/png;base64," + imagePaths_base64[imgIndex];
                contentList.Add(new Content
                {
                    type = "image_url",
                    image_url = new ImageUrl { url = dataUri }
                });
            }
            else
            {
                throw new ArgumentException($"未找到匹配的图片 [[image{imgIndex + 1}]]");
            }

            lastIndex = end;
        }

        if (lastIndex < text.Length)
        {
            contentList.Add(new Content { type = "text", text = text.Substring(lastIndex) });
        }

        return contentList;
    }

    public IEnumerator StreamChat()
    {
        outputText.text = "";
        currentResponse.Clear();
        typingQueue.Clear();
        message = new Message
        {
            role = "user",
            content = setPrompt(steps_str, dialogue, imagePaths)
        };

        var cfg = GetConfig(provider);
        var requestJson = JsonConvert.SerializeObject(new ChatRequest
        {
            model = cfg.model,
            stream = true,
            messages = new List<Message> { message }
        }, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

        byte[] bodyRaw = Encoding.UTF8.GetBytes(requestJson);

        using (var request = new UnityWebRequest(cfg.apiUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new StreamingDownloadHandler(OnChunkReceived);
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + cfg.apiKey);

            yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            if (request.result != UnityWebRequest.Result.Success)
#else
            if (request.isHttpError || request.isNetworkError)
#endif
            {
                Debug.LogError("请求失败: " + request.error);
            }
            else
            {
                dialogue += "[Teacher]"+currentResponse.ToString();
            }
        }
    }

    private void OnChunkReceived(string chunk)
    {
        if (string.IsNullOrWhiteSpace(chunk)) return;
        if (!chunk.TrimStart().StartsWith("data:")) return;

        string json = chunk.Substring(6).Trim();
        if (json == "[DONE]") return;

        try
        {
            var part = JsonConvert.DeserializeObject<StreamingChunk>(json);
            if (part?.choices != null &&
                part.choices.Length > 0 &&
                part.choices[0].delta != null &&
                !string.IsNullOrEmpty(part.choices[0].delta.content))
            {
                string delta = part.choices[0].delta.content;
                currentResponse.Append(delta);
                foreach (char c in delta) typingQueue.Enqueue(c);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("解析失败: " + e.Message);
        }
    }

    LLMConfig GetConfig(LLMProvider p) => openAI;

    // -------- 数据结构 --------
    [Serializable] public class Message { public string role; public List<Content> content; }
    [Serializable] public class Content { public string type; public string text; public ImageUrl image_url; }
    [Serializable] public class ImageUrl { public string url; }
    public class ChatRequest { public string model; public bool stream; public List<Message> messages; }
    public class StreamingChunk { public Choice[] choices; }
    public class Choice { public Delta delta; }
    public class Delta { public string content; }
    public class LLMConfig { public string apiUrl; public string model; public string apiKey; public LLMConfig(string u, string m, string k) { apiUrl = u; model = m; apiKey = k; } }
    public enum LLMProvider { OpenAI, DeepSeek, Tongyi }
}

// 自定义流式下载处理器
public class StreamingDownloadHandler : DownloadHandlerScript
{
    private StringBuilder buffer = new StringBuilder();
    private Action<string> onLine;

    public StreamingDownloadHandler(Action<string> onLine) : base(new byte[1024])
    {
        this.onLine = onLine;
    }

    protected override bool ReceiveData(byte[] data, int dataLength)
    {
        if (data == null || dataLength == 0) return false;

        string text = Encoding.UTF8.GetString(data, 0, dataLength);
        buffer.Append(text);

        int newline;
        while ((newline = buffer.ToString().IndexOf("\n")) != -1)
        {
            string line = buffer.ToString(0, newline).Trim();
            buffer.Remove(0, newline + 1);
            onLine?.Invoke(line);
        }
        return true;
    }

    protected override void CompleteContent()
    {
        if (buffer.Length > 0)
        {
            onLine?.Invoke(buffer.ToString().Trim());
            buffer.Clear();
        }
    }
}
