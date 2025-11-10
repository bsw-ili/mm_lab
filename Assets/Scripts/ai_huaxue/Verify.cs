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
        {"Cool", new(){"vessel"}},

        // ======= Special =======
        {"Wait", new(){"time"}},

        // ======= Measurement =======
        {"MeasureTemperature", new(){"vessel", "tool"}},
        {"MeasureMass", new(){"reagent", "tool"}},

        // ======= Separation =======
        {"Filter", new(){"from_vessel", "to_vessel", "tool"}},
        {"CollectGas", new(){"source_vessel", "collector", "method"}},

        // ======= Observation =======
        {"Observe", new(){"vessel"}},
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

        // ======= Observation =======
        {"Observe", new(){"vessel", "phenomenon", "time", "tool", "note"}},
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
    private static List<VerifyError> VerifyProcedure(XmlElement root, List<string> hardware, List<string> reagents, Dictionary<string, string> reagentStates)
    {
        var errors = new List<VerifyError>();
        var procedures = root.GetElementsByTagName("Procedure");

        HashSet<string> hardwareActions = new() { "Attach", "Insert" };
        HashSet<string> operationActions = new() { "Add", "Transfer", "Stir", "Heat", "Wait", "Cool", "MeasureTemperature", "MeasureMass", "Filter", "CollectGas", "Observe" };

        bool enteredOperationStage = false;

        foreach (XmlNode proc in procedures)
        {
            foreach (XmlNode stage in proc.ChildNodes)
            {
                if (stage.Name != "Stage")
                {
                    errors.Add(new VerifyError
                    {
                        step = stage.OuterXml,
                        errors = new List<string> { "Only <Stage> elements are allowed inside <Procedure>." }
                    });
                    continue;
                }

                string stageType = stage.Attributes?["type"]?.Value ?? "";
                if (stageType != "hardware" && stageType != "operation")
                {
                    errors.Add(new VerifyError
                    {
                        step = stage.OuterXml,
                        errors = new List<string> { "Stage must have type='hardware' or type='operation'." }
                    });
                    continue;
                }

                if (stageType == "operation") enteredOperationStage = true;
                else if (stageType == "hardware" && enteredOperationStage)
                {
                    errors.Add(new VerifyError
                    {
                        step = stage.OuterXml,
                        errors = new List<string> { "Hardware Stage cannot appear after Operation Stage." }
                    });
                }

                foreach (XmlNode step in stage.ChildNodes)
                {
                    string action = step.Name;
                    var errs = new List<string>();

                    if (stageType == "hardware" && !hardwareActions.Contains(action))
                        errs.Add($"Action '{action}' is not allowed in hardware stage.");
                    if (stageType == "operation" && !operationActions.Contains(action))
                        errs.Add($"Action '{action}' is not allowed in operation stage.");

                    if (!mandatory_properties.ContainsKey(action))
                    {
                        errs.Add($"Unknown action '{action}' in procedure.");
                    }
                    else
                    {
                        foreach (string prop in mandatory_properties[action])
                            if (step.Attributes?[prop] == null)
                                errs.Add($"You must have '{prop}' property when doing '{action}'.");

                        foreach (XmlAttribute attr in step.Attributes)
                        {
                            var allowed = new HashSet<string>(optional_properties[action]);
                            foreach (var p in mandatory_properties[action]) allowed.Add(p);
                            if (!allowed.Contains(attr.Name))
                                errs.Add($"The {attr.Name} property in {action} is not allowed. Allowed: {string.Join(", ", allowed)}");
                        }

                        foreach (string v in new[] { "vessel", "from_vessel", "to_vessel", "tool", "support", "source_vessel", "collector" })
                        {
                            if (step.Attributes?[v] != null && !hardware.Contains(step.Attributes[v].Value))
                                errs.Add($"{step.Attributes[v].Value} is not defined in Hardware.");
                        }

                        // reagent 检查
                        if (step.Attributes?["reagent"] != null)
                        {
                            string reagent = step.Attributes["reagent"].Value;
                            if (!reagentStates.ContainsKey(reagent))
                            {
                                errs.Add($"{reagent} is not defined in Reagents or missing 'state' property.");
                            }
                            else
                            {
                                // === Add 操作根据 state 判断工具合法性 ===
                                if (action == "Add" && step.Attributes?["tool"] != null)
                                {
                                    string tool_type = NormalizeHardwareId(step.Attributes["tool"].Value);
                                    string state = reagentStates[reagent];

                                    if (state == "solid")
                                    {
                                        if (tool_type != "spatula")
                                            errs.Add($"When adding solid reagent '{reagent}', you must use 'spatula' as the tool.");
                                    }
                                    else if (state == "liquid")
                                    {
                                        if (tool_type != "dropper" && tool_type != "graduated_cylinder")
                                            errs.Add($"When adding liquid reagent '{reagent}', you must use 'dropper' or 'graduated_cylinder' as the tool.");
                                    }
                                    else
                                    {
                                        errs.Add($"Reagent '{reagent}' has invalid state '{state}'; must be 'solid' or 'liquid'.");
                                    }
                                }
                            }
                        }
                    }

                    if (errs.Count > 0)
                    {
                        errors.Add(new VerifyError
                        {
                            step = step.OuterXml.Replace("\n", " "),
                            errors = errs
                        });
                    }
                }
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
