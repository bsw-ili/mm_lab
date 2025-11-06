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
        {"Add", new(){"vessel", "reagent","tool"}},
        {"Transfer", new(){"from_vessel", "to_vessel"}},
        {"Attach", new(){"vessel","support"}},
        {"Stir", new(){"vessel", "tool"}},
        {"Insert", new(){ "tool", "vessel"}},
        {"Heat", new(){"vessel", "tool"}},
        {"Wait", new(){"time"}},
    };

    private static readonly Dictionary<string, List<string>> optional_properties = new()
    {
        // ======= 物质操作类 =======
        {"Add", new(){"vessel", "reagent", "tool", "mass","volume", "temperature", "rate", "order", "stirring", "note"}},
        {"Insert", new(){ "tool", "vessel", "purpose", "depth", "angle", "alignment", "note"}},
        {"Attach", new(){"vessel", "support", "tool", "method","position", "force", "angle", "release_time", "note"}},
        {"Transfer", new(){"from_vessel", "to_vessel", "volume", "tool", "speed", "temperature", "cover", "note"}},

        // ======= 过程控制类 =======
        {"Stir", new(){"vessel", "time", "tool", "speed", "direction", "interval", "auto_stop", "note"}},
        {"Heat", new(){"vessel", "temp", "time", "device", "mode", "ramp_rate", "cooling", "target_sensor", "note"}},
        {"Wait", new(){"time", "reason", "tool", "condition", "until", "note"}},
    };

    private static readonly List<string> reagent_properties = new()
    {"name", "inchi", "cas", "role", "preserve", "use_for_cleaning", "clean_with", "stir", "temp", "atmosphere", "purity"};

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

        // 验证 Hardware, Reagents, Procedure
        var (hardwareList, hwErrors) = ParseHardware(root, availableHardware);
        errorList.AddRange(hwErrors);

        var (reagentList, rErrors) = ParseReagents(root, availableReagents);
        errorList.AddRange(rErrors);

        var pErrors = VerifyProcedure(root, hardwareList, reagentList);
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
                    hardwareList.Add(baseId);

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
    private static (List<string> reagents, List<VerifyError> errors) ParseReagents(XmlElement root, List<string> available)
    {
        var reagentList = new List<string>();
        var errors = new List<VerifyError>();

        var reagentsNodes = root.GetElementsByTagName("Reagent");
        foreach (XmlNode reagent in reagentsNodes)
        {
            var errs = new List<string>();

            if (reagent.Attributes?["name"] == null)
                errs.Add("Missing 'name' property in Reagent.");
            else
            {
                string name = reagent.Attributes["name"].Value;
                reagentList.Add(name);

                if (available != null && !available.Contains(name))
                    errs.Add($"{name} is not defined in available reagents list:{string.Join(",", available)}.");
            }

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

        return (reagentList, errors);
    }

    // ========== Procedure ==========
    private static List<VerifyError> VerifyProcedure(XmlElement root, List<string> hardware, List<string> reagents)
    {
        var errors = new List<VerifyError>();
        var procedures = root.GetElementsByTagName("Procedure");

        // 固体与液体试剂分类，可根据实际情况修改
        var liquidReagents = ChemistryDefinitions.allowedLiquids_dict.Keys.ToList();
        var solidReagents = ChemistryDefinitions.allowedSolids_dict.Keys.ToList();

        HashSet<string> hardwareActions = new() { "Attach", "Insert" };
        HashSet<string> operationActions = new() { "Add", "Transfer", "Stir", "Heat", "Wait" };
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

                if (stageType == "operation")
                    enteredOperationStage = true;
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

                    // 检查动作是否在当前阶段允许
                    if (stageType == "hardware" && !hardwareActions.Contains(action))
                        errs.Add($"Action '{action}' is not allowed in hardware stage.");
                    if (stageType == "operation" && !operationActions.Contains(action))
                        errs.Add($"Action '{action}' is not allowed in operation stage.");

                    // 动作存在性
                    if (!mandatory_properties.ContainsKey(action))
                    {
                        errs.Add($"Unknown action '{action}' in procedure.");
                    }
                    else
                    {
                        // 必要属性
                        foreach (string prop in mandatory_properties[action])
                        {
                            if (step.Attributes?[prop] == null)
                                errs.Add($"You must have '{prop}' property when doing '{action}'.");
                        }

                        // 非法属性
                        foreach (XmlAttribute attr in step.Attributes)
                        {
                            if (!optional_properties[action].Contains(attr.Name))
                            {
                                var allowed = new HashSet<string>(optional_properties[action]);
                                foreach (var p in mandatory_properties[action]) allowed.Add(p);
                                string allowedStr = string.Join(", ", allowed);
                                errs.Add($"The {attr.Name} property in {action} is not allowed. Allowed: {allowedStr}");
                            }
                        }

                        // vessel/tool/support 检查
                        foreach (string v in new[] { "vessel", "from_vessel", "to_vessel", "tool", "support" })
                        {
                            if (step.Attributes?[v] != null)
                            {
                                string baseId = NormalizeHardwareId(step.Attributes[v].Value);
                                if (!hardware.Contains(baseId))
                                    errs.Add($"{step.Attributes[v].Value} (base: {baseId}) is not defined in Hardware.");
                            }
                        }

                        // reagent 检查
                        if (step.Attributes?["reagent"] != null && !reagents.Contains(step.Attributes["reagent"].Value))
                            errs.Add($"{step.Attributes["reagent"].Value} is not defined in Reagents.");

                        // === Add 操作时的 tool 合法性检查 ===
                        if (action == "Add" && step.Attributes?["reagent"] != null && step.Attributes?["tool"] != null)
                        {
                            string reagent = step.Attributes["reagent"].Value;
                            string tool = step.Attributes["tool"].Value;
                            tool = NormalizeHardwareId(tool);
                            if (solidReagents.Contains(reagent))
                            {
                                if (tool != "spatula")
                                    errs.Add($"When adding solid reagent '{reagent}', you must use 'spatula' as the tool.");
                            }
                            else if (liquidReagents.Contains(reagent))
                            {
                                if (tool != "dropper" && tool != "graduated_cylinder")
                                    errs.Add($"When adding liquid reagent '{reagent}', you must use 'dropper' or 'graduated_cylinder' as the tool.");
                            }
                            else
                            {
                                errs.Add($"Reagent '{reagent}' is not categorized as solid or liquid; please update the reagent list.");
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
