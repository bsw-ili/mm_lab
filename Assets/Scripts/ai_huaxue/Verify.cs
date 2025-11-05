using System;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

public static class Verify
{
    // ======= 核心属性定义 =======
    private static readonly Dictionary<string, List<string>> mandatory_properties = new()
    {
        {"Add", new(){"vessel", "reagent"}},
        {"Separate", new(){"purpose", "product_phase", "from_vessel", "separation_vessel", "to_vessel"}},
        {"Transfer", new(){"from_vessel", "to_vessel"}},
        {"StartStir", new(){"vessel"}},
        {"Stir", new(){"vessel", "time"}},
        {"StopStir", new(){"vessel"}},
        {"HeatChill", new(){"vessel", "temp", "time"}},
        {"HeatChillToTemp", new(){"vessel", "temp"}},
        {"StartHeatChill", new(){"vessel", "temp"}},
        {"StopHeatChill", new(){"vessel"}},
        {"EvacuateAndRefill", new(){"vessel"}},
        {"Purge", new(){"vessel"}},
        {"StartPurge", new(){"vessel"}},
        {"StopPurge", new(){"vessel"}},
        {"Filter", new(){"vessel"}},
        {"FilterThrough", new(){"from_vessel", "to_vessel", "through"}},
        {"WashSolid", new(){"vessel", "solvent", "volume"}},
        {"Wait", new(){"time"}},
        {"Repeat", new(){}},
        {"CleanVessel", new(){"vessel"}},
        {"Crystallize", new(){"vessel"}},
        {"Dissolve", new(){"vessel", "solvent"}},
        {"Dry", new(){"vessel"}},
        {"Evaporate", new(){"vessel"}},
        {"Irradiate", new(){"vessel", "time"}},
        {"Precipitate", new(){"vessel"}},
        {"ResetHandling", new(){}},
        {"RunColumn", new(){"from_vessel", "to_vessel"}},
        {"RunCV", new(){}},
        {"Monitor", new(){"vessel", "quantity"}}
    };

    private static readonly Dictionary<string, List<string>> optional_properties = new()
    {
        {"Add", new(){"vessel", "reagent", "volume", "mass", "amount", "dropwise", "time", "stir", "stir_speed", "viscous", "purpose"}},
        {"Separate", new(){"purpose", "product_phase", "from_vessel", "separation_vessel", "to_vessel", "waste_phase_to_vessel", "solvent", "solvent_volume", "through", "repeats", "stir_time", "stir_speed", "settling_time"}},
        {"Transfer", new(){"from_vessel", "to_vessel", "volume", "amount", "time", "viscous", "rinsing_solvent", "rinsing_volume", "rinsing_repeats", "solid"}},
        {"StartStir", new(){"vessel", "stir_speed", "purpose"}},
        {"Stir", new(){"vessel", "time", "stir_speed", "continue_stirring", "purpose"}},
        {"StopStir", new(){"vessel"}},
        {"HeatChill", new(){"vessel", "temp", "time", "stir", "stir_speed", "purpose"}},
        {"HeatChillToTemp", new(){"vessel", "temp", "active", "continue_heatchill", "stir", "stir_speed", "purpose"}},
        {"StartHeatChill", new(){"vessel", "temp", "purpose"}},
        {"StopHeatChill", new(){"vessel"}},
        {"EvacuateAndRefill", new(){"vessel", "gas", "repeats"}},
        {"Purge", new(){"vessel", "gas", "time", "pressure", "flow_rate"}},
        {"StartPurge", new(){"vessel", "gas", "pressure", "flow_rate"}},
        {"StopPurge", new(){"vessel"}},
        {"Filter", new(){"vessel", "filtrate_vessel", "stir", "stir_speed", "temp", "continue_heatchill", "volume"}},
        {"FilterThrough", new(){"from_vessel", "to_vessel", "through", "eluting_solvent", "eluting_volume", "eluting_repeats", "residence_time"}},
        {"WashSolid", new(){"vessel", "solvent", "volume", "filtrate_vessel", "temp", "stir", "stir_speed", "time", "repeats"}},
        {"Wait", new(){"time"}},
        {"Repeat", new(){"repeats", "children", "loop_variables", "iterative"}},
        {"CleanVessel", new(){"vessel", "solvent", "volume", "temp", "repeats"}},
        {"Crystallize", new(){"vessel", "ramp_time", "ramp_temp"}},
        {"Dissolve", new(){"vessel", "solvent", "volume", "amount", "temp", "time", "stir_speed"}},
        {"Dry", new(){"vessel", "time", "pressure", "temp", "continue_heatchill"}},
        {"Evaporate", new(){"vessel", "time", "pressure", "temp", "stir_speed"}},
        {"Irradiate", new(){"vessel", "time", "wavelegth", "color", "temp", "stir", "stir_speed", "cooling_power"}},
        {"Precipitate", new(){"vessel", "time", "temp", "stir_speed", "reagent", "volume", "amount", "add_time"}},
        {"ResetHandling", new(){"solvent", "volume", "repeats"}},
        {"RunColumn", new(){"from_vessel", "to_vessel", "column"}},
        {"RunCV", new(){}},
        {"Monitor", new(){"vessel", "quantity"}}
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
                    foreach (string v in new[] { "vessel", "from_vessel", "to_vessel" })
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
