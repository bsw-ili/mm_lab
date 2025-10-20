using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

public class OpenAIExtractor_json : MonoBehaviour
{
    private string apiKey;
    private string apiUrl;
    private readonly int maxRetries = 3;
    private readonly float retryDelay = 1.5f;

    private JObject schema; // 完整动作 schema
    private Dictionary<string, float> sizeCache = new Dictionary<string, float>();

    void Awake()
    {
        apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("❌ 未找到环境变量 OPENAI_API_KEY，请先设置 OPENAI_API_KEY！");
            return;
        }

        apiUrl = "https://api.vveai.com/v1/chat/completions";

    }

    // 获取物体大致大小
    public float GetApproximateSize(GameObject obj)
    {
        if (obj == null) return 0.5f;

        string key = obj.name;
        if (sizeCache.TryGetValue(key, out float cached)) return cached;

        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers != null && renderers.Length > 0)
        {
            Bounds combined = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                combined.Encapsulate(renderers[i].bounds);

            float size = combined.extents.magnitude;
            sizeCache[key] = size;
            return size;
        }
        else
        {
            sizeCache[key] = 0.5f;
            return 0.5f;
        }
    }

    // 提取物体信息
    private Dictionary<string, string> ExtractObjectInfo(GameObject obj)
    {
        var info = new Dictionary<string, string>();
        if (obj == null) return info;

        string objectName = obj.name;
        info["object_name"] = objectName;
        info["space_radius"] = GetApproximateSize(obj).ToString();

        if (ChemistryDefinitions.AnchorDict.ContainsKey(objectName))
        {
            info["key_points"] = JsonConvert.SerializeObject(ChemistryDefinitions.AnchorDict[objectName].Keys);
            info["key_points_info"] = JsonConvert.SerializeObject(ChemistryDefinitions.AnchorDict[objectName], Formatting.Indented);

            Transform anchors = obj.transform.Find("Anchors");
            if (anchors != null)
            {
                var anchorPositions = new Dictionary<string, string>();
                foreach (Transform child in anchors)
                    anchorPositions[child.name] = child.position.ToString();
                info["key_points_position"] = JsonConvert.SerializeObject(anchorPositions, Formatting.Indented);
            }
        }

        return info;
    }

    // 主提取接口
    public async Task<string> Extract(string userInput, (GameObject opObj, GameObject targetObj) op_objects)
    {
        if (string.IsNullOrEmpty(userInput))
        {
            Debug.LogError("❌ 输入文本为空！");
            return null;
        }

        try
        {
            var op1 = ExtractObjectInfo(op_objects.opObj);
            var op2 = ExtractObjectInfo(op_objects.targetObj);
            BuildSchema(op1, op2); // 生成完整 schema
            string prompt = BuildPrompt(userInput, op1, op2);
            return await SendApiRequest(prompt);
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ 提取 JSON 时出错: {ex.Message}");
            return null;
        }
    }

    // 构造完整 schema
    private void BuildSchema(Dictionary<string, string> op1, Dictionary<string, string> op2)
    {
        JObject BuildVector3Schema() => new JObject(
            new JProperty("type", "array"),
            new JProperty("items", new JObject(new JProperty("type", "number"))),
            new JProperty("minItems", 3),
            new JProperty("maxItems", 3)
        );

        JObject BuildEquipmentSchema(Dictionary<string, string> op)
        {
            JArray SafeParseArray(string raw)
            {
                try { return JArray.Parse(raw); }
                catch { return new JArray(raw.Split(',')); }
            }

            return new JObject(
                new JProperty("type", "object"),
                new JProperty("properties", new JObject(
                    new JProperty("equipment", new JObject(
                        new JProperty("type", "string"),
                        new JProperty("enum", new JArray(op["object_name"]))
                    )),
                    new JProperty("anchor", new JObject(
                        new JProperty("type", "string"),
                        new JProperty("enum", SafeParseArray(op.ContainsKey("key_points") ? op["key_points"] : ""))
                    ))
                )),
                new JProperty("required", new JArray("equipment", "anchor"))
            );
        }

        JObject BuildEquipmentSchemaOnly(Dictionary<string, string> op) => new JObject(
            new JProperty("type", "object"),
            new JProperty("properties", new JObject(
                new JProperty("equipment", new JObject(
                    new JProperty("type", "string"),
                    new JProperty("enum", new JArray(op["object_name"]))
                ))
            )),
            new JProperty("required", new JArray("equipment")),
            new JProperty("additionalProperties", false)
        );

        JArray allowedLiquids = new JArray(ChemistryDefinitions.allowedLiquids_dict.Keys);
        JArray allowedSolids = new JArray(ChemistryDefinitions.allowedSolids_dict.Keys);

        var atomicActions = new JArray
        {
            // MoveToAnchor
            new JObject(
                new JProperty("type","object"),
                new JProperty("properties", new JObject(
                    new JProperty("op", new JObject(new JProperty("type","string"), new JProperty("enum", new JArray("MoveToAnchor")))),
                    new JProperty("source", BuildEquipmentSchema(op1)),
                    new JProperty("target", BuildEquipmentSchema(op2)),
                    new JProperty("offset", BuildVector3Schema())
                )),
                new JProperty("required", new JArray("op","source","target","offset"))
            ),
            // RotateAroundAnchor
            new JObject(
                new JProperty("type","object"),
                new JProperty("properties", new JObject(
                    new JProperty("op", new JObject(new JProperty("type","string"), new JProperty("enum", new JArray("RotateAroundAnchor")))),
                    new JProperty("source", BuildEquipmentSchema(op1)),
                    new JProperty("rotation", BuildVector3Schema())
                )),
                new JProperty("required", new JArray("op","source","rotation"))
            ),
            // ScaleObject
            new JObject(
                new JProperty("type","object"),
                new JProperty("properties", new JObject(
                    new JProperty("op", new JObject(new JProperty("type","string"), new JProperty("enum", new JArray("ScaleObject")))),
                    new JProperty("source", BuildEquipmentSchemaOnly(op1)),
                    new JProperty("scale", new JObject(new JProperty("type","number"), new JProperty("minimum",0.0)))
                )),
                new JProperty("required", new JArray("op","source","scale"))
            ),
            // Ignite
            new JObject(
                new JProperty("type","object"),
                new JProperty("properties", new JObject(
                    new JProperty("op", new JObject(new JProperty("type","string"), new JProperty("enum", new JArray("Ignite")))),
                    new JProperty("source", BuildEquipmentSchemaOnly(op1))
                )),
                new JProperty("required", new JArray("op","source"))
            ),
            // AddLiquid
            new JObject(
                new JProperty("type","object"),
                new JProperty("properties", new JObject(
                    new JProperty("op", new JObject(new JProperty("type","string"), new JProperty("enum", new JArray("AddLiquid")))),
                    new JProperty("source", BuildEquipmentSchemaOnly(op2)),
                    new JProperty("material", new JObject(new JProperty("type","string"), new JProperty("enum", allowedLiquids)))
                )),
                new JProperty("required", new JArray("op","source","material"))
            ),
            // AddSolid
            new JObject(
                new JProperty("type","object"),
                new JProperty("properties", new JObject(
                    new JProperty("op", new JObject(new JProperty("type","string"), new JProperty("enum", new JArray("AddSolid")))),
                    new JProperty("source", BuildEquipmentSchemaOnly(op2)),
                    new JProperty("material", new JObject(new JProperty("type","string"), new JProperty("enum", allowedSolids)))
                )),
                new JProperty("required", new JArray("op","source","material"))
            )
        };

        schema = new JObject(
            new JProperty("type", "object"),
            new JProperty("properties", new JObject(
                new JProperty("steps", new JObject(
                    new JProperty("type", "array"),
                    new JProperty("items", new JObject(new JProperty("oneOf", atomicActions))),
                    new JProperty("minItems", 1)
                ))
            )),
            new JProperty("required", new JArray("steps")),
            new JProperty("additionalProperties", false)
        );
    }

    // 构造 prompt
    private string BuildPrompt(string userInput, Dictionary<string, string> op1, Dictionary<string, string> op2)
    {
        var sb = new StringBuilder();
        sb.AppendLine("你是一个虚拟化学实验物体摆放专家。 任务：根据输入的操作文本，生成符合 JSON Schema 的动作序列，目标是执行动作序列后实验场景中物体的摆放及状态符合操作后的画面。 请严格输出 JSON，不要包含任何多余文本或解释。 "); 
        sb.AppendLine("## 已知信息说明 每个实验物体信息包含以下字段: 1. object_name: 物体名称 2. space_radius: 物体近似半径 (包围盒中心到对角线端点的距离) 3. key_points: 可用锚点列表 4. key_points_info: 锚点的详细描述 5. key_points_position: 锚点在世界坐标系中的位置 ");
        sb.AppendLine("## 操作物体信息:");
        sb.AppendLine(JsonConvert.SerializeObject(op1, Formatting.Indented));
        sb.AppendLine("## 被操作物体信息:");
        sb.AppendLine(JsonConvert.SerializeObject(op2, Formatting.Indented));
        sb.AppendLine("我们规定z轴朝向为物体的正前方（即物体的朝向，所以我们一般绕z轴旋转物体），y轴为物体正上方，x轴为物体正右方。");
        sb.AppendLine(@" ## 动作定义 - AlignByAnchor：移动源物体，将其从当前锚点位置移动到目标物体锚点位置，并旋转源物体对齐法向量（即面对齐），字段: { source: {equipment, anchor}, target: {equipment, anchor}} - Ignite：点燃指定物体 字段: { source: {equipment} } - AddLiquid：为指定物体添加液体（若未指定，默认 material = ""default_liquid""） 字段: { source: {equipment}, material: ""liquid_name"" } - AddSolid：为指定物体添加固体（若未指定，默认 material = ""default_solid""） 字段: { source: {equipment}, material: ""solid_name"" }");
        //sb.AppendLine(@" ## 输入与输出示例 ### 示例 1 **输入**：""把试管口对准烧杯口，然后倾斜倒入水"" **输出**： { ""steps"": [ { ""op"": ""MoveToAnchor"", ""source"": {""equipment"": ""test_tube"", ""anchor"": ""mouth""}, ""target"": {""equipment"": ""beaker"", ""anchor"": ""top_rim""}, ""offset"": [0, 0.05, 0] }, { ""op"": ""RotateAroundAnchor"", ""source"": {""equipment"": ""test_tube"", ""anchor"": ""mouth""}, ""rotation"": [45, 0, 0] }, { ""op"": ""AddLiquid"", ""source"": {""equipment"": ""beaker""}, ""material"": ""water"" } ] } ### 示例 2 **输入**：""将蜡烛点燃"" **输出**： { ""steps"": [ { ""op"": ""Ignite"", ""source"": {""equipment"": ""candle""} } ] } ### 示例 3 **输入**：""在烧杯中加入氯化钠固体"" **输出**： { ""steps"": [ { ""op"": ""AddSolid"", ""source"": {""equipment"": ""beaker""}, ""material"": ""sodium_chloride"" } ] } ### 示例 4 **输入**：""将试管缩小一半大小"" **输出**： { ""steps"": [ { ""op"": ""ScaleObject"", ""source"": {""equipment"": ""test_tube""}, ""scale"": 0.5 } ] } ");
        sb.AppendLine("## 用户输入:");
        sb.AppendLine(userInput);
        return sb.ToString();
    }

    private async Task<string> SendApiRequest(string prompt)
    {
        if (string.IsNullOrEmpty(apiKey) || schema == null) return null;

        var requestData = new ChatRequest
        {
            model = "gpt-4.1",
            messages = new ChatMessage[]
            {
            new ChatMessage { role="system", content="你是一个3D虚拟实验助手，严格输出符合 JSON Schema 的 JSON。" },
            new ChatMessage { role="user", content=prompt }
            },
            response_format = new ResponseFormat
            {
                type = "json_schema",
                json_schema = new JsonSchemaFormat
                {
                    name = "ExperimentPlacement",
                    schema = schema  // ⚠ 直接传 JObject
                }
            }
        };

        string jsonBody = JsonConvert.SerializeObject(requestData);
        Debug.Log("发送JSON大小: " + jsonBody.Length);

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            byte[] bodyData = Encoding.UTF8.GetBytes(jsonBody);
            using (UnityWebRequest www = new UnityWebRequest(apiUrl, "POST"))
            {
                www.uploadHandler = new UploadHandlerRaw(bodyData) { contentType = "application/json" };
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Authorization", "Bearer " + apiKey);
                www.timeout = 60;

                await SendWebRequestAsync(www);

                if (www.result == UnityWebRequest.Result.Success)
                    return ParseResponse(www.downloadHandler.text);

                Debug.LogWarning($"API请求失败 ({attempt}/{maxRetries}): {www.error}");
            }

            if (attempt < maxRetries)
                await Task.Delay(TimeSpan.FromSeconds(retryDelay * attempt));
        }

        Debug.LogError($"在 {maxRetries} 次尝试后仍无法获取有效响应");
        return null;
    }


    private string ParseResponse(string json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try
        {
            var response = JsonConvert.DeserializeObject<ChatResponse>(json);
            if (response?.choices != null && response.choices.Length > 0)
                return response.choices[0].message?.content?.Trim();
        }
        catch (Exception ex)
        {
            Debug.LogError($"解析 JSON 异常: {ex.Message}");
        }
        return null;
    }

    private async Task SendWebRequestAsync(UnityWebRequest www)
    {
#if UNITY_2020_1_OR_NEWER
        await www.SendWebRequest();
#else
        var op = www.SendWebRequest();
        while (!op.isDone) await Task.Yield();
#endif
    }

    // ===== JSON Schema 请求体结构 =====
    [Serializable]
    private class ChatRequest
    {
        public string model;
        public ChatMessage[] messages;
        public ResponseFormat response_format;
    }

    [Serializable]
    private class ChatMessage
    {
        public string role;
        public string content;
    }

    [Serializable]
    private class ResponseFormat
    {
        public string type;
        public JsonSchemaFormat json_schema;
    }

    [Serializable]
    private class JsonSchemaFormat
    {
        public string name;
        public object schema;
    }

    [Serializable]
    private class ChatResponse
    {
        public Choice[] choices;
    }

    [Serializable]
    private class Choice
    {
        public ChatMessage message;
    }
}
