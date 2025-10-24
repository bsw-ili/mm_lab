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

    private void BuildSchema(Dictionary<string, string> op1, Dictionary<string, string> op2)
    {

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
                    new JProperty("equipment_name", new JObject(
                        new JProperty("type", "string"),
                        new JProperty("enum", new JArray(op["object_name"]))
                    )),
                    new JProperty("anchor", new JObject(
                        new JProperty("type", "string"),
                        new JProperty("enum", SafeParseArray(op.ContainsKey("key_points") ? op["key_points"] : ""))
                    ))
                )),
                new JProperty("required", new JArray("equipment_name", "anchor")),
                new JProperty("additionalProperties", false)
            );
        }

        JObject BuildEquipmentSchemaOnly(Dictionary<string, string> op) => new JObject(
            new JProperty("type", "object"),
            new JProperty("properties", new JObject(
                new JProperty("equipment_name", new JObject(
                    new JProperty("type", "string"),
                    new JProperty("enum", new JArray(op["object_name"]))
                ))
            )),
            new JProperty("required", new JArray("equipment_name")),
            new JProperty("additionalProperties", false)
        );

        // 获取化学物质
        JArray allowedLiquids = new JArray(ChemistryDefinitions.allowedLiquids_dict.Keys);
        JArray allowedSolids = new JArray(ChemistryDefinitions.allowedSolids_dict.Keys);

        // 定义所有原子动作
        var atomicActions = new JArray
    {
        

        // ✅ Ignite
        new JObject(
            new JProperty("type","object"),
            new JProperty("properties", new JObject(
                new JProperty("op", new JObject(new JProperty("type","string"), new JProperty("enum", new JArray("Ignite")))),
                new JProperty("equipment", BuildEquipmentSchemaOnly(op1))
            )),
            new JProperty("required", new JArray("op","equipment")),
            new JProperty("additionalProperties", false)
        ),

        // ✅ AddLiquid
        new JObject(
            new JProperty("type","object"),
            new JProperty("properties", new JObject(
                new JProperty("op", new JObject(new JProperty("type","string"), new JProperty("enum", new JArray("AddLiquid")))),
                new JProperty("equipment", BuildEquipmentSchemaOnly(op2)),
                new JProperty("material", new JObject(
                    new JProperty("type","string"),
                    new JProperty("enum", allowedLiquids)
                ))
            )),
            new JProperty("required", new JArray("op","equipment","material")),
            new JProperty("additionalProperties", false)

        ),

        // ✅ AddSolid
        new JObject(
            new JProperty("type","object"),
            new JProperty("properties", new JObject(
                new JProperty("op", new JObject(new JProperty("type","string"), new JProperty("enum", new JArray("AddSolid")))),
                new JProperty("equipment", BuildEquipmentSchemaOnly(op2)),
                new JProperty("material", new JObject(
                    new JProperty("type","string"),
                    new JProperty("enum", allowedSolids)
                ))
            )),
            new JProperty("required", new JArray("op","equipment","material")),
            new JProperty("additionalProperties", false)

        ),

        // ✅ ReverseObject
        new JObject(
            new JProperty("type","object"),
            new JProperty("properties", new JObject(
                new JProperty("op", new JObject(new JProperty("type","string"), new JProperty("enum", new JArray("ReverseObject")))),
                new JProperty("equipment", BuildEquipmentSchemaOnly(op1))
            )),
            new JProperty("required", new JArray("op","equipment")),
            new JProperty("additionalProperties", false)

        ),

        // ✅ FillWithLiquid
        new JObject(
            new JProperty("type","object"),
            new JProperty("properties", new JObject(
                new JProperty("op", new JObject(new JProperty("type","string"), new JProperty("enum", new JArray("FillWithLiquid")))),
                new JProperty("equipment", BuildEquipmentSchemaOnly(op1)),
                new JProperty("material", new JObject(
                    new JProperty("type","string"),
                    new JProperty("enum", allowedLiquids)
                ))
            )),
            new JProperty("required", new JArray("op","equipment","material")),
            new JProperty("additionalProperties", false)

        ),

        // ✅ LayFlat
        new JObject(
            new JProperty("type","object"),
            new JProperty("properties", new JObject(
                new JProperty("op", new JObject(new JProperty("type","string"), new JProperty("enum", new JArray("LayFlat")))),
                new JProperty("equipment", BuildEquipmentSchemaOnly(op1))
            )),
            new JProperty("required", new JArray("op","equipment")),
            new JProperty("additionalProperties", false)

        ),

        // ✅ TiltUp
        new JObject(
            new JProperty("type","object"),
            new JProperty("properties", new JObject(
                new JProperty("op", new JObject(new JProperty("type","string"), new JProperty("enum", new JArray("TiltUp")))),
                new JProperty("equipment", BuildEquipmentSchemaOnly(op1))
            )),
            new JProperty("required", new JArray("op","equipment")),
            new JProperty("additionalProperties", false)

        ),

        // ✅ TiltDown
        new JObject(
            new JProperty("type","object"),
            new JProperty("properties", new JObject(
                new JProperty("op", new JObject(new JProperty("type","string"), new JProperty("enum", new JArray("TiltDown")))),
                new JProperty("equipment", BuildEquipmentSchemaOnly(op1))
            )),
            new JProperty("required", new JArray("op","equipment")),
            new JProperty("additionalProperties", false)


        ),
        // ✅ AlignByAnchor
        new JObject(
            new JProperty("type","object"),
            new JProperty("properties", new JObject(
                new JProperty("op", new JObject(new JProperty("type","string"), new JProperty("enum", new JArray("AlignByAnchor")))),
                new JProperty("source", BuildEquipmentSchema(op1)),
                new JProperty("target", BuildEquipmentSchema(op2)),
                new JProperty("implementAction", new JObject(new JProperty("type","string"))),
                new JProperty("alignMode", new JObject(new JProperty("type","string"), new JProperty("enum", new JArray("AlignPosition","AlignPositionRotation"))))
            )),
            new JProperty("required", new JArray("op","source","target","implementAction","alignMode")),
            new JProperty("additionalProperties", false)

            )
    };

        // ✅ 组合顶层 schema
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
        sb.AppendLine("你是一个虚拟化学实验物体摆放专家。 任务：请你根据输入的操作文本生成合适的动作序列，目标是执行动作序列后实验场景中物体的摆放及状态符合文本中的“操作完成画面”。 请严格按照schema输出 JSON，不要包含任何多余文本或解释。 ");
        sb.AppendLine("已知信息说明 每个实验物体信息包含以下字段: 1. object_name: 物体名称 2. space_radius: 物体近似半径 (包围盒中心到对角线端点的距离) 3. key_points: 可用锚点列表 4. key_points_info: 锚点的详细描述 5. key_points_position: 锚点在世界坐标系中的位置 ");
        sb.AppendLine("操作物体信息:");
        sb.AppendLine(JsonConvert.SerializeObject(op1, Formatting.Indented));
        sb.AppendLine("被操作物体信息:");
        sb.AppendLine(JsonConvert.SerializeObject(op2, Formatting.Indented));
        sb.AppendLine("我们规定z轴朝向为物体的正前方（即物体的朝向，所以我们一般绕z轴旋转物体），y轴为物体正上方，x轴为物体正右方，所有容器初始时开口朝上。");
        sb.AppendLine(@" 
        原子动作定义：
        
        - ReverseObject: 将指定物体翻转180度,开口向下变为开口向上
        字段: { op:ReverseObject,equipment: {equipment_name} }

        - FillWithLiquid: 将指定物体装满液体，
        字段: { op:FillWithLiquid,equipment: {equipment_name}, material: ""liquid_name"" }

        - LayFlat：将指定物体水平放置，开口朝向x轴反方向
        字段: { op:LayFlat,equipment: {equipment_name} }

        - TiltUp：使物体向上倾斜，开口朝向y轴向x轴反方向45度角
        字段: { op:TiltUp,equipment: {equipment_name} }
        
        - TiltDown：使物体向下倾斜，开口朝向y轴反方向与x轴反方向45度角
        字段: { op:TiltDown,equipment: {equipment_name} }

        - Ignite：点燃指定物体 
        字段: { op:Ignite,equipment: {equipment_name} } 
        
        - AddLiquid：为指定物体添加液体（若未指定，默认 material = ""default_liquid""） 
        字段: { op:AddLiquid,equipment: {equipment_name}, material: ""liquid_name"" } 
        
        - AddSolid：为指定物体添加固体（若未指定，默认 material = ""default_solid""） 
        字段: { op:AddSolid,equipment: {equipment_name}, material: ""solid_name"" }

        - AlignByAnchor：根据锚点对齐源物体和目标物体，我们定义了两种对齐模式：
            1. AlignPosition，这种模式下，我们只对齐锚点的位置，而不改变源物体的方向。适用于需要保持物体原有朝向的操作，如加热、倾倒,加入固体，滴加液体等。
            2. AlignPositionRotation，这种模式下，我们不仅对齐锚点的位置，还会调整源物体的方向，使其法向量与目标物体的法向量一致。适用于需要改变物体朝向的操作，如夹持、塞住、插入等。
        请你首先判断AlignByAnchor用来实现什么操作（ImplementAction），如加热、倾倒、夹持、塞住、插入等。然后给出其对应的对齐模式（AlignMode），以适应不同的操作需求。
        字段: { op:AlignByAnchor,source: {equipment_name, anchor}, target: {equipment_name, anchor},implementAction:{action}, alignMode:{mode}}
        ");
        sb.AppendLine("⚠ 每个原子动作必须包含字段 'op'，其值等于动作名（如 'AlignByAnchor'、'Ignite' 等），否则视为无效。");
        sb.AppendLine("我们规定必须且只能执行一次AlignByAnchor，并且执行该操作后，不再执行其他原子操作（除非是Ignite,AddLiquid,AddSolid,FillWithLiquid操作），所以请你合理的安排原子操作顺序。");
        sb.AppendLine("提醒：倾倒液体时源物体应该倾斜放置");
        sb.AppendLine("示例:");
        sb.AppendLine("输入:");
        sb.AppendLine("{\"操作步骤\":\"将烧杯中的稀盐酸缓慢倒入量筒中，控制倾斜角度，防止液体溅出，读数应在视线平行刻度线处读取。\",\"操作完成画面\":\"实验台上，烧杯略倾斜，稀盐酸正缓缓倒入量筒中，液面逐渐上升并与刻度线齐平，整个操作过程稳定且无液体溅出。\",\"操作物体\":\"beaker\",\"被操作物体\":\"measuring_cylinder\"}");
        sb.AppendLine("输出:");
        sb.AppendLine(@"{
          ""steps"": [
            {
              ""op"": ""AddLiquid"",
              ""equipment"": { ""equipment_name"": ""beaker"" },
              ""material"": ""hydrochloric_acid""
            },
            {
              ""op"": ""TiltUp"",
              ""equipment"": { ""equipment_name"": ""beaker"" },
            },
            
            {
              ""op"": ""AlignByAnchor"",
              ""source"": { ""equipment_name"": ""measuring_cylinder"", ""anchor"": ""bottom_center"" },
              ""target"": { ""equipment_name"": ""beaker"", ""anchor"": ""top_rim"" },
              ""implementAction"": ""暂存与准备倒入（量筒靠近烧杯，为后续倒液作准备）"",
              ""alignMode"": ""AlignPosition""
            },
            {
              ""op"": ""AddLiquid"",
              ""equipment"": { ""equipment_name"": ""measuring_cylinder"" },
              ""material"": ""hydrochloric_acid""
            }
          ]
        }");

        sb.AppendLine("用户输入:");
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
                    schema = schema,  // ⚠ 直接传 JObject
                    //strict = true  // ✅ 强制严格模式
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
            string content = response?.choices?[0]?.message?.content?.Trim();
            if (string.IsNullOrEmpty(content)) return null;

            JObject result = JObject.Parse(content);
            JArray steps = (JArray)result["steps"];
            foreach (JObject step in steps)
            {
                if (step["op"] == null)
                {
                    if (step["source"] != null && step["target"] != null)
                        step["op"] = "AlignByAnchor";
                }
            }
            return result.ToString(Formatting.Indented);
        }
        catch (Exception ex)
        {
            Debug.LogError($"解析 JSON 异常: {ex.Message}");
            return null;
        }
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
    public class JsonSchemaFormat
    {
        public string name;
        public object schema;
        //public bool strict = true;  // ✅ 强制严格输出
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
