using System;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

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
        {"Attach", new(){"vessel", "support", "method","position", "force", "angle", "release_time", "note"}},
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
                if (comp.Name != "Component")
                {
                    errors.Add(new VerifyError
                    {
                        step = "Hardware definition",
                        errors = new List<string> { "The Hardware section should only contain Component tags." }
                    });
                    continue;
                }

                if (comp.Attributes?["id"] == null) continue;
                string id = comp.Attributes["id"].Value;
                hardwareList.Add(id);

                if (available != null && !available.Contains(id))
                {
                    errors.Add(new VerifyError
                    {
                        step = "Hardware definition",
                        errors = new List<string> { $"{id} is not defined in available hardware list." }
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
                    errs.Add($"{name} is not defined in available reagents list.");
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

        foreach (XmlNode proc in procedures)
        {
            foreach (XmlNode step in proc.ChildNodes)
            {
                var errs = new List<string>();
                string action = step.Name;

                if (!mandatory_properties.ContainsKey(action))
                {
                    errs.Add($"There is no {action} action in XDL.");
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
                            string allowedStr = allowed.Count > 0 ? string.Join(", ", allowed) : "none";
                            errs.Add($"The {attr.Name} property in {action} is not allowed. Allowed: {allowedStr}");
                        }
                    }

                    // vessel 检查
                    foreach (string v in new[] { "vessel", "from_vessel", "to_vessel","tool", "support" })
                    {
                        if (step.Attributes?[v] != null && !hardware.Contains(step.Attributes[v].Value))
                            errs.Add($"{step.Attributes[v].Value} is not defined in Hardware.");
                    }

                    // reagent 检查
                    if (step.Attributes?["reagent"] != null && !reagents.Contains(step.Attributes["reagent"].Value))
                        errs.Add($"{step.Attributes["reagent"].Value} is not defined in Reagents.");
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

        return errors;
    }
}
