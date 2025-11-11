using System;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Linq;

public static class Verify
{
    // ======= 核心属性定义 =======
    private static readonly Dictionary<string, List<string>> mandatory_properties = new()
    {
        // ======= Hardware Manipulate =======
        {"Attach", new(){"vessel", "support"}},
        {"Insert", new(){"tool", "vessel"}},

        // ======= Reagents Manipulate =======
        {"Add", new(){"vessel", "reagent", "tool"}},
        {"Transfer", new(){"from_vessel", "to_vessel", "tool"}},

        // ======= Stirring =======
        {"Stir", new(){"vessel", "tool"}},

        // ======= Temperature Control =======
        {"Heat", new(){"vessel", "tool"}},
        {"Cool", new(){"vessel","tool"}},

        // ======= Special =======
        {"Wait", new(){"time"}},

        // ======= Measurement =======
        {"MeasureTemperature", new(){"vessel", "tool"}},
        {"MeasureMass", new(){"reagent", "tool"}},

        // ======= Separation =======
        {"Filter", new(){"from_vessel", "to_vessel", "tool"}},
        {"CollectGas", new(){"source_vessel", "collector", "method"}},
    };

    private static readonly Dictionary<string, List<string>> optional_properties = new()
    {
        // ======= Hardware Manipulate =======
        {"Attach", new(){"vessel", "support", "tool", "method", "position", "force", "angle", "release_time", "note"}},
        {"Insert", new(){"tool", "vessel", "purpose", "depth", "angle", "alignment", "note"}},

        // ======= Reagents Manipulate =======
        {"Add", new(){"vessel", "reagent", "tool", "mass", "volume", "temperature", "rate", "order", "stirring", "note"}},
        {"Transfer", new(){"from_vessel", "to_vessel", "volume", "tool", "speed", "temperature", "cover", "note"}},

        // ======= Stirring =======
        {"Stir", new(){"vessel", "tool", "time", "speed", "direction", "interval", "auto_stop", "note"}},

        // ======= Temperature Control =======
        {"Heat", new(){"vessel", "tool", "temp", "time", "device", "mode", "ramp_rate", "cooling", "target_sensor", "note"}},
        {"Cool", new(){"vessel", "tool", "temp", "time", "method", "note"}},

        // ======= Special =======
        {"Wait", new(){"time", "reason", "tool", "condition", "until", "note"}},

        // ======= Measurement =======
        {"MeasureTemperature", new(){"vessel", "tool", "value", "note"}},
        {"MeasureMass", new(){"reagent", "tool", "value", "note"}},

        // ======= Separation =======
        {"Filter", new(){"from_vessel", "to_vessel", "tool", "method", "filter_type", "pressure", "note"}},
        {"CollectGas", new(){"source_vessel", "collector", "method", "tool", "duration", "note"}},

    };


    private static readonly List<string> reagent_properties = new()
    {"name", "state","inchi", "cas", "role", "preserve", "use_for_cleaning", "clean_with", "stir", "temp", "atmosphere", "purity"};

    // 错误结构体
    public class VerifyError
    {
        public string step;
        public List<string> errors;
    }

    // ========== 主入口 ==========
    public static List<VerifyError> VerifyXDL(string xdl, List<string> availableHardware = null, List<string> availableReagents = null)
    {
        var errorList = new List<VerifyError>();
        XmlDocument doc = new XmlDocument();

        try
        {
            doc.LoadXml(xdl);
        }
        catch (Exception e)
        {
            return new List<VerifyError> {
            new VerifyError {
                errors = new List<string> { $"Input XDL cannot be parsed as XML: {e.Message}" }
            }
        };
        }

        var root = doc.DocumentElement;
        if (root == null)
        {
            errorList.Add(new VerifyError { errors = new List<string> { "Empty XML document." } });
            return errorList;
        }

        // 1️⃣ 验证 Hardware
        var (hardwareList, hwErrors) = ParseHardware(root, availableHardware);
        errorList.AddRange(hwErrors);

        // 2️⃣ 验证 Reagents（返回 state 字典）
        var (reagentList, rErrors, reagentStates) = ParseReagents(root, availableReagents);
        errorList.AddRange(rErrors);

        // 3️⃣ 验证 Procedure，并传入 reagentStates
        var pErrors = VerifyProcedure(root, hardwareList, reagentList, reagentStates);
        errorList.AddRange(pErrors);

        return errorList;
    }

    // ========== Hardware ==========
    private static (List<string> hardwareList, List<VerifyError> errors) ParseHardware(XmlElement root, List<string> available)
    {
        var hardwareList = new List<string>();
        var errors = new List<VerifyError>();

        var hardwareNodes = root.GetElementsByTagName("Hardware");
        foreach (XmlNode hw in hardwareNodes)
        {
            foreach (XmlNode comp in hw.ChildNodes)
            {
                var errs = new List<string>();

                if (comp.Name != "Component")
                {
                    errs.Add("The Hardware section should only contain Component tags.");
                    errors.Add(new VerifyError
                    {
                        step = "Hardware definition",
                        errors = errs
                    });
                    continue;
                }

                // ===== 检查 id =====
                if (comp.Attributes?["id"] == null)
                {
                    errs.Add("Missing 'id' property in Component.");
                }
                else
                {
                    string id = comp.Attributes["id"].Value;
                    string baseId = NormalizeHardwareId(id);
                    hardwareList.Add(id);

                    if (available != null && !available.Contains(baseId))
                        errs.Add($"{id} (base: {baseId}) is not defined in available hardware list:{string.Join(",", available)}.");
                }

                // ===== 检查 contains =====
                if (comp.Attributes?["contains"] == null)
                {
                    errs.Add($"Missing 'contains' property in Component (required). Use 'empty' if no reagents are present.");
                }

                if (errs.Count > 0)
                {
                    errors.Add(new VerifyError
                    {
                        step = comp.OuterXml,
                        errors = errs
                    });
                }
            }
        }

        return (hardwareList, errors);
    }

    // ========== Reagents ==========
    private static (List<string> reagents, List<VerifyError> errors, Dictionary<string, string> reagentStates) ParseReagents(XmlElement root, List<string> available)
    {
        var reagentList = new List<string>();
        var reagentStates = new Dictionary<string, string>();
        var errors = new List<VerifyError>();

        var reagentsNodes = root.GetElementsByTagName("Reagent");
        foreach (XmlNode reagent in reagentsNodes)
        {
            var errs = new List<string>();

            if (reagent.Attributes?["name"] == null)
            {
                errs.Add("Missing 'name' property in Reagent.");
            }
            else
            {
                string name = reagent.Attributes["name"].Value;
                reagentList.Add(name);

                if (available != null && !available.Contains(name))
                    errs.Add($"{name} is not defined in available reagents list:{string.Join(",", available)}.");

                // === 检查 state 属性 ===
                if (reagent.Attributes?["state"] == null)
                    errs.Add($"Reagent '{name}' must have 'state' property (solid or liquid).");
                else
                    reagentStates[name] = reagent.Attributes["state"].Value.ToLower(); // 保存 state
            }

            // 检查非法属性
            foreach (XmlAttribute attr in reagent.Attributes)
            {
                if (!reagent_properties.Contains(attr.Name))
                    errs.Add($"The {attr.Name} property in Reagent is not allowed.");
            }

            if (errs.Count > 0)
            {
                errors.Add(new VerifyError
                {
                    step = reagent.OuterXml,
                    errors = errs
                });
            }
        }

        return (reagentList, errors, reagentStates);
    }

    // ========== Procedure ==========
    // Procedure 部分验证
    // ========== Procedure ==========
    // Procedure 部分验证
    private static List<VerifyError> VerifyProcedure(XmlElement root, List<string> hardware, List<string> reagents, Dictionary<string, string> reagentStates)
    {
        var errors = new List<VerifyError>();
        var procedures = root.GetElementsByTagName("Procedure");

        foreach (XmlNode proc in procedures)
        {
            foreach (XmlNode step in proc.ChildNodes)
            {
                if (step.NodeType != XmlNodeType.Element) continue;

                string action = step.Name;
                var errs = new List<string>();

                if (!mandatory_properties.ContainsKey(action))
                {
                    errs.Add($"Unknown action '{action}'.");
                }
                else
                {
                    // ===== 检查必需属性 =====
                    foreach (string prop in mandatory_properties[action])
                        if (step.Attributes?[prop] == null)
                            errs.Add($"Missing mandatory attribute '{prop}' in '{action}'.");

                    // ===== 检查非法属性 =====
                    var allowed = new HashSet<string>(optional_properties[action]);
                    foreach (var p in mandatory_properties[action]) allowed.Add(p);
                    foreach (XmlAttribute attr in step.Attributes)
                        if (!allowed.Contains(attr.Name))
                            errs.Add($"Illegal attribute '{attr.Name}' in '{action}'. Allowed: {string.Join(", ", allowed)}");

                    // ===== 硬件存在性 =====
                    foreach (string v in new[] { "vessel", "from_vessel", "to_vessel", "tool", "support", "source_vessel", "collector" })
                        if (step.Attributes?[v] != null && !hardware.Contains(step.Attributes[v].Value))
                            errs.Add($"'{step.Attributes[v].Value}' not found in Hardware list.");

                    // ===== 试剂存在性与工具逻辑 =====
                    if (step.Attributes?["reagent"] != null)
                    {
                        string reagent = step.Attributes["reagent"].Value;
                        if (!reagentStates.ContainsKey(reagent))
                            errs.Add($"Reagent '{reagent}' not defined or missing 'state'.");
                        else if (action == "Add")
                        {
                            string tool_type = NormalizeHardwareId(step.Attributes["tool"].Value);
                            string state = reagentStates[reagent];
                            if (state == "solid" && tool_type != "spatula")
                                errs.Add($"Solid reagent '{reagent}' must be added using 'spatula'.");
                            else if (state == "liquid" && tool_type != "dropper" && tool_type != "graduated_cylinder")
                                errs.Add($"Liquid reagent '{reagent}' must use 'dropper' or 'graduated_cylinder'.");
                        }
                    }

                    // ===== 新增：防止同物体操作 =====
                    var involvedObjects = new Dictionary<string, string>();
                    foreach (var key in new[] { "vessel", "from_vessel", "to_vessel", "tool", "support", "source_vessel", "collector" })
                    {
                        if (step.Attributes?[key] != null)
                            involvedObjects[key] = step.Attributes[key].Value;
                    }

                    // 检查不同属性引用同一个物体
                    var duplicates = involvedObjects
                        .GroupBy(kv => kv.Value)
                        .Where(g => g.Count() > 1)
                        .Select(g => $"{g.Key} ({g.First().Value})");
                    if (duplicates.Any())
                    {
                        var sameObj = string.Join(", ", duplicates);
                        errs.Add($"Invalid operation: different roles reference the same object → {sameObj}.");
                    }
                }

                if (errs.Count > 0)
                    errors.Add(new VerifyError { step = step.OuterXml, errors = errs });
            }
        }

        return errors;
    }


    // ========== 工具函数：去除编号 ==========
    private static string NormalizeHardwareId(string id)
    {
        return Regex.Replace(id, @"[_\-]?\d+$", "");
    }
}
