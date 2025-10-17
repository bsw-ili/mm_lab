using System.Collections.Generic;

public static class ChemistryDefinitions
{
    // 状态字典：器材 -> (状态 -> 描述)
    public static readonly Dictionary<string, Dictionary<string, string>> StateDict =
        new Dictionary<string, Dictionary<string, string>>
    {
        { "beaker", new Dictionary<string, string>{
            { "empty", "The beaker is empty" },
            { "contains_solution", "The beaker contains solution" },
            { "contains_solids", "The beaker contains solids" },
        }},
        { "large_beaker", new Dictionary<string, string>{
            { "empty", "The beaker is empty" },
            { "contains_solution", "The beaker contains solution" },
            { "contains_solids", "The beaker contains solids" },
        }},
        { "test_tube", new Dictionary<string, string>{
            { "empty", "The test tube is empty" },
            { "contains_solution", "The test tube contains solution" },
            { "contains_potassium_permanganate", "The test tube contains potassium permanganate" }
        }},
        { "small_test_tube", new Dictionary<string, string>{
            { "empty", "The test tube is empty" },
            { "contains_solution", "The test tube contains solution" },
            { "contains_potassium_permanganate", "The test tube contains potassium permanganate" }
        }},
        {
            "hard_paper", new Dictionary<string, string>{
                {"clean", "The hard paper surface is clean and flat.（硬纸表面干净平整。）"},
                {"wet", "The hard paper is damp or soaked with liquid.（硬纸受潮或被液体浸湿。）"},
                {"folded", "The hard paper has been folded or creased.（硬纸被折叠或出现折痕。）"},
                {"burned", "The hard paper has burn marks or is partially charred.（硬纸有烧焦痕迹或部分被炭化。）"}
            }
        },
        {
            "spray_bottle", new Dictionary<string, string>{
                {"empty", "The spray bottle is empty and contains no liquid.（喷壶为空，未装液体。）"},
                {"filled", "The spray bottle is filled with liquid and ready for use.（喷壶已装液体，可使用。）"},
                {"spraying", "The spray bottle is being used and releasing mist.（喷壶正在喷洒液体。）"},
                {"leaking", "The spray bottle has a loose cap or damaged seal causing leakage.（喷壶因盖松或密封损坏而漏液。）"},
                {"stored", "The spray bottle is closed and stored safely.（喷壶已关闭并安全存放。）"}
            }
        },

        {
            "graphite_electrode", new Dictionary<string, string>{
                {"clean_dry", "The electrode surface is clean and dry, ready for use or connection.（电极表面干净、干燥，处于可使用或待连接状态。）"},
                {"immersed_in_solution", "The lower tip of the electrode is immersed in an electrolyte or solution for conducting current.（电极下端浸入电解液或溶液中，用于导电反应。）"},
                {"connected_to_power_supply", "The upper end of the electrode is connected to the power source or clamp for current input.（电极上端已与电源或夹具连接，用于输入电流。）"},
                {"heating_or_glowing", "The electrode tip emits heat or glows due to current passage during electrolysis or arc discharge.（电流通过时电极尖端发热或发光，用于电解或电弧放电过程。）"},
                {"worn_or_consumed", "The electrode tip becomes shorter or thinner after long use due to carbon consumption.（长时间使用后电极尖端变短或变细，因碳被消耗。）"}
            }
        },
        {
            "ph_indicator_paper", new Dictionary<string, string>{
                {"dry", "The pH paper is dry and ready for use.（试纸干燥，可正常使用。）"},
                {"wet_after_test", "The pH paper has been dipped in solution and changed color.（试纸已浸入溶液并发生颜色变化。）"},
                {"disposed", "The pH paper has been used and discarded.（试纸已使用并丢弃。）"}
            }
        },
        {
            "toothpick", new Dictionary<string, string>{
                {"clean", "The toothpick is clean and ready for use.（牙签干净，可用于实验操作。）"},
                {"in_use", "The toothpick is being used to transfer or stir a sample.（牙签正在用于转移或搅拌样品。）"},
                {"wet", "The toothpick has been dipped in a liquid and is wet.（牙签已浸入液体，表面潮湿。）"},
                {"discarded", "The toothpick has been used and discarded.（牙签已使用并丢弃。）"}
            }
        },
        {
            "protractor", new Dictionary<string, string>{
                {"clean", "The protractor is clean and the scale markings are clear.（量角器干净，刻度清晰。）"},
                {"in_use", "The protractor is being used to measure or draw an angle.（量角器正在用于测量或绘制角度。）"},
                {"misaligned", "The protractor is not properly aligned with the baseline, leading to inaccurate readings.（量角器未与基线对齐，可能导致读数不准。）"},
                {"stored", "The protractor is stored properly and not in use.（量角器已妥善保存，未使用。）"}
            }
        },


        { "test_tube_rack", new Dictionary<string, string>{
            { "empty", "The rack has no test tubes" },
            { "holding_tube", "The rack is holding test tube" }
        }},
        { "conical_flask", new Dictionary<string, string>{
            { "empty", "The conical flask is empty" },
            { "contains_liquid", "The conical flask contains liquid" },
            { "contains_solids", "The conical flask contains solids" },

        }},
        { "volumetric_flask", new Dictionary<string, string>{
            { "empty", "The volumetric flask is empty" },
            { "contains_solution", "The volumetric flask contains solution" },
        }},
        { "round-bottom_flask", new Dictionary<string, string>{
            { "empty", "The flask is empty" },
            { "contains_liquid", "The flask contains liquid" },
        }},
        { "measuring_cylinder", new Dictionary<string, string>{
            { "empty", "The cylinder is empty" },
            { "contains_liquid", "The cylinder contains liquid" }
        }},
        { "burette", new Dictionary<string, string>{
            { "empty", "The burette is empty" },
            { "filled", "The burette is filled with liquid" },
            { "in_use", "The burette is being used for titration" }
        }},
        { "pipette", new Dictionary<string, string>{
            { "empty", "The pipette is empty" },
            { "filled", "The pipette is filled with liquid" },
            { "in_use", "The pipette is being used" }
        }},
        { "watch_glass", new Dictionary<string, string>{
            { "empty", "The watch glass is empty" },
            { "holding_substance", "The watch glass holds a sample" }
        }},
        { "wide-mouth_bottle", new Dictionary<string, string>{
            { "empty", "The bottle is empty" },
            { "contains_liquid", "The bottle contains liquid" },
            { "contains_solids", "The conical flask contains solids" },

        }},
        { "glass_rod", new Dictionary<string, string>{
            { "idle", "The glass rod is idle" },
            { "in_use", "The glass rod is being used to stir" }
        }},
        { "evaporating_dish", new Dictionary<string, string>{
            { "empty", "The dish is empty" },
            { "contains_solution", "The dish contains solution" },
        }},
        { "crucible", new Dictionary<string, string>{
            { "empty", "The crucible is empty" },
            { "contains_substance", "The crucible contains a substance" },
            { "heating", "The crucible is being heated" }
        }},
        { "gas_jar", new Dictionary<string, string>{
            { "empty", "The gas jar is empty" },
            { "contains_gas", "The gas jar contains gas" },
            { "contains_water","The gas_jar contains water" }
        }},
        { "petri_dish", new Dictionary<string, string>{
            { "empty", "The petri dish is empty" },
            { "contains_sample", "The dish contains a sample" }
        }},
        { "flat-bottom_flask", new Dictionary<string, string>{
            { "empty", "The flask is empty" },
            { "contains_liquid", "The flask contains liquid" },
            { "heating", "The flask is being heated" }
        }},
        { "narrow-mouth_bottle", new Dictionary<string, string>{
            { "empty", "The bottle is empty" },
            { "contains_liquid", "The bottle contains liquid" },
        }},
        { "alcohol_lamp", new Dictionary<string, string>{
            { "off", "The lamp is off" },
            { "lit", "The lamp is lit" },
            { "refilling", "The lamp is being refilled" }
        }},
        { "bunsen_burner", new Dictionary<string, string>{
            { "off", "The burner is off" },
            { "lit", "The burner is lit" },
            { "adjusting_flame", "The flame is being adjusted" }
        }},
        { "tripod_stand", new Dictionary<string, string>{
            { "idle", "The tripod is idle" },
            { "supporting_object", "The tripod is supporting an object" }
        }},
        { "wire_gauze", new Dictionary<string, string>{
            { "idle", "The gauze is idle" },
            { "supporting_object", "The gauze is supporting a container" }
        }},
        { "crucible_tongs", new Dictionary<string, string>{
            { "holding_object", "The tongs are holding a crucible" },
            { "idle", "The tongs are idle" }
        }},
        { "alcohol_lamp_cap", new Dictionary<string, string>{
            { "on_lamp", "The cap is covering the lamp" },
            { "off_lamp", "The cap is removed" }
        }},
        { "test_tube_holder", new Dictionary<string, string>{
            { "holding_tube", "The holder is holding a test tube" },
            { "empty", "The holder is empty" }
        }},
        { "thermometer", new Dictionary<string, string>{
            { "measuring", "The thermometer is measuring temperature" },
            { "being_held", "The thermometer is being held" }
        }},
        { "balance", new Dictionary<string, string>{
            { "idle", "The balance is idle" },
            { "weighing", "The balance is weighing an object" }
        }},
        { "stopwatch", new Dictionary<string, string>{
            { "stopped", "The stopwatch is stopped" },
            { "running", "The stopwatch is running" }
        }},
        { "retort_stand", new Dictionary<string, string>{
            { "idle", "The stand is idle" },
            { "holding_object", "The stand is holding a clamp or flask" }
        }},
        { "ground_glass_plate", new Dictionary<string, string>{
            { "empty", "The plate is empty" },
            { "holding_substance", "The plate is holding a sample" }
        }},
        { "separatory_funnel", new Dictionary<string, string>{
            { "empty", "The funnel is empty" },
            { "contains_liquid", "The funnel contains liquid" },
            { "in_use", "The funnel is being used for separation" }
        }},
        { "iron_clamp", new Dictionary<string, string>{
            { "holding_object", "The clamp is holding an object" },
            { "empty", "The clamp is idle" }
        }},
        { "ring_clamp", new Dictionary<string, string>{
            { "holding_object", "The ring is holding a container" },
            { "empty", "The ring is idle" }
        }},
        { "filter_paper", new Dictionary<string, string>{
            { "dry", "The paper is dry" },
            { "wet", "The paper is wet with filtrate" }
        }},
        { "dropper", new Dictionary<string, string>{
            { "empty", "The dropper is empty" },
            { "filled", "The dropper contains liquid" },
            { "in_use", "The dropper is being used" }
        }},
        { "rubber_stopper", new Dictionary<string, string>{
            { "on_container", "The stopper is sealing a container" },
            { "off_container", "The stopper is removed" }
        }},
        { "combustion_spoon", new Dictionary<string, string>{
            { "empty", "The spoon is empty" },
            { "holding_substance", "The spoon holds a substance" }
        }},
        { "long-neck_funnel", new Dictionary<string, string>{
            { "empty", "The funnel is empty" },
            { "contains_liquid", "The funnel contains liquid" }
        }},
        { "dropper_bottle", new Dictionary<string, string>{
            { "empty", "The bottle is empty" },
            { "contains_liquid", "The bottle contains liquid" }
        }},
        { "wash_bottle", new Dictionary<string, string>{
            { "empty", "The bottle is empty" },
            { "filled", "The bottle contains liquid" }
        }},
        { "spatula", new Dictionary<string, string>{
            { "idle", "The spatula is idle" },
            { "holding_substance", "The spatula holds a sample" },
        }},
        { "scoopula", new Dictionary<string, string>{
            { "idle", "The scoopula is idle" },
            { "holding_substance", "The scoopula holds a sample" }
        }},
        { "filter_funnel", new Dictionary<string, string>{
            { "empty", "The funnel is empty" },
            { "contains_liquid", "The funnel contains liquid" }
        }},
        { "distilling_flask", new Dictionary<string, string>{
            { "empty", "The flask is empty" },
            { "contains_liquid", "The flask contains liquid" },
            { "heating", "The flask is being heated" }
        }},
        { "mortar", new Dictionary<string, string>{
            { "empty", "The mortar is empty" },
            { "contains_substance", "The mortar contains a substance" }
        }},
        { "water_trough", new Dictionary<string, string>{
            { "empty", "The trough is empty" },
            { "contains_water", "The trough contains water" }
        }},
        { "matchstick", new Dictionary<string, string>{
            { "unused", "The matchstick is unused" },
            { "used", "The matchstick has been struck" }
        }},
        { "cotton_wool", new Dictionary<string, string>{
            { "dry", "The cotton wool is dry" },
            { "wet", "The cotton wool is wet" }
        }},
        { "wooden_splint", new Dictionary<string, string>{
            { "unused", "The splint is unused" },
            { "lit", "The splint is lit" }
        }},
        { "tweezers", new Dictionary<string, string>{
            { "holding_object", "The tweezers are holding an object" },
            { "idle", "The tweezers are idle" }
        }},
        { "delivery_tube", new Dictionary<string, string>{
            { "empty", "The tube is empty" },
            { "carrying_gas", "The tube carries gas" }
        }},
        { "clay_triangle", new Dictionary<string, string>{
            { "supporting_crucible", "The triangle is supporting a crucible" },
            { "idle", "The triangle is idle" }
        }},


        { "U-tube", new Dictionary<string, string>{
            { "empty", "The U-tube is empty" },
            { "contains_liquid", "The U-tube contains liquid" }
        }},
        { "bulb_desiccator", new Dictionary<string, string>{
            { "empty", "The desiccator is empty" },
            { "contains_desiccant", "The desiccator contains desiccant" }
        }},
        { "condenser", new Dictionary<string, string>{
            { "empty", "The condenser is empty" },
            { "carrying_liquid", "The condenser contains liquid" }
        }},
        { "asbestos_mesh", new Dictionary<string, string>{
            { "idle", "The mesh is idle" },
            { "supporting_object", "The mesh supports a container" }
        }},
        { "test_tube_brush", new Dictionary<string, string>{
            { "cleaning", "The brush is cleaning a test tube" },
            { "idle", "The brush is idle" }
        }},
        { "Kipp’s_apparatus", new Dictionary<string, string>{
            { "empty", "The apparatus is empty" },
            { "producing_gas", "The apparatus is producing gas" }
        }},
        { "gas_washing_bottle", new Dictionary<string, string>{
            { "empty", "The bottle is empty" },
            { "containing_solution", "The bottle contains solution for gas washing" }
        }},
        {
            "power_supply", new Dictionary<string, string>{
                {"powered_off","The power supply is switched off and not providing any output voltage or current.（电源处于关闭状态，未输出电压或电流。）"},
                {"powered_on_idle","The power supply is turned on but no load is connected; the circuit is open.（电源已开启但未连接负载，电路处于开路状态。）"},
                {"supplying_power","The power supply is actively delivering current and voltage to the connected circuit or electrodes.（电源正在向连接的电路或电极提供电流和电压。）"},
                {"adjusting_output","The voltage or current is being adjusted via the control knobs or interface.（正在通过控制旋钮或界面调节输出电压或电流。）"},
                {"overloaded_or_fault","The power supply experiences overload, short circuit, or internal fault, indicated by warning lights.（电源出现过载、短路或内部故障，通常伴随警示灯提示。）"}
            }
        },

        {
            "light_bulb", new Dictionary<string, string>{
                {"off_state","The bulb is not lit; no current flows through the filament or LED.（灯泡未点亮，灯丝或LED中无电流通过。）"},
                {"on_state","The bulb is emitting light as current flows through the filament or LED.（灯泡点亮，电流通过灯丝或LED产生光。）"},
                {"flickering","The bulb flashes intermittently due to unstable power or loose connection.（由于电源不稳或接触不良，灯泡闪烁。）"},
                {"burned_out","The filament is broken or LED circuit failed; the bulb no longer lights.（灯丝烧断或LED电路损坏，灯泡不再发光。）"},
                {"heating_up","The bulb surface is warming due to prolonged operation.（灯泡因长时间工作而发热。）"}
            }
        },
        {
            "rubber_stop_with_tube", new Dictionary<string, string>{
                {"unattached", "The rubber stopper with tube is not connected to any apparatus.（橡皮塞未与任何器材连接。）"},
                {"inserted_in_test_tube", "The stopper is inserted into the test tube mouth, sealing it.（橡皮塞插入试管口并密封。）"},
                {"connected_to_gas_tube", "The glass tube through the stopper is connected to a gas delivery tube.（穿过橡皮塞的玻璃管与导气管相连。）"},
                {"gas_passing", "Gas is currently passing through the inserted tube.（气体正在通过玻璃管流动。）"},
                {"sealed_tightly", "The stopper is tightly fitted to prevent gas leakage.（橡皮塞密封良好，防止气体泄漏。）"}
            }
        },
        {
            "porcelain_plate", new Dictionary<string, string>{
                {"clean", "The porcelain plate is clean and ready for use.（瓷板干净，可用于实验。）"},
                {"contains_sample", "The porcelain plate contains solid or liquid samples.（瓷板上放有固体或液体样品。）"},
                {"heated", "The porcelain plate is being heated or has just been heated.（瓷板正在加热或刚被加热。）"},
                {"stained", "The porcelain plate has stains or residue and needs cleaning.（瓷板上有污迹或残留物，需要清洗。）"}
            }
        },
        {
            "glass_slide", new Dictionary<string, string>{
                {"clean", "The glass slide is clean and ready for mounting samples.（载玻片干净，可用于装载样品。）"},
                {"has_sample", "The glass slide has a sample placed on its surface.（载玻片上放有样品。）"},
                {"with_cover_slip", "The slide is covered with a cover slip for observation.（载玻片上覆盖了盖玻片，可进行显微观察。）"},
                {"used", "The glass slide has been used and may contain residue.（载玻片已使用，可能有残留物。）"}
            }
        },
        {
            "color_chart", new Dictionary<string, string>{
                {"clean", "The color chart is clean and all color regions are clearly visible.（比色卡干净，颜色区域清晰可见。）"},
                {"in_use", "The color chart is being used for comparing the pH or concentration color.（比色卡正在用于比对pH值或浓度颜色。）"},
                {"faded", "The color chart is faded or worn, and colors are less distinguishable.（比色卡颜色褪色或磨损，不易区分。）"},
                {"stored", "The color chart is stored properly and not in use.（比色卡已妥善保存，未使用。）"}
            }
        },
        {
            "syringe", new Dictionary<string, string>{
                {"empty", "The syringe contains no liquid.（注射器为空。）"},
                {"filled", "The syringe is filled with a liquid ready for use.（注射器已装液体，可使用。）"},
                {"in_use", "The syringe is currently being used for injection or extraction.（注射器正在使用中，用于注射或抽取。）"},
                {"disposed", "The syringe has been used and properly discarded.（注射器已使用并妥善处理。）"}
            }
        },
        {
            "iron_rod", new Dictionary<string, string>{
                {"clean", "The iron rod is clean and free from rust or residue.（铁棒干净，无锈蚀或残留物。）"},
                {"rusty", "The iron rod shows signs of rust and may require cleaning before use.（铁棒有锈迹，使用前可能需要清理。）"},
                {"in_use", "The iron rod is currently being used in an experiment.（铁棒正在实验中使用。）"},
                {"heated", "The iron rod has been heated or is hot due to prior use.（铁棒已被加热或因使用而发热。）"}
            }
        },
        {
            "ammeter", new Dictionary<string, string>{
                {"off", "The ammeter is switched off and not measuring current.（电流表关闭，未测量电流。）"},
                {"measuring", "The ammeter is actively measuring current in a circuit.（电流表正在测量电路中的电流。）"},
                {"overload", "The ammeter is overloaded — the current exceeds its maximum range.（电流表过载，电流超过最大量程。）"},
                {"disconnected", "The ammeter is not connected to any circuit.（电流表未连接到电路。）"}
            }
        },
        {
            "candle", new Dictionary<string, string>{
                {"unlit", "The candle is intact and not yet ignited.（蜡烛未点燃，完好。）"},
                {"burning", "The candle is currently burning with a flame.（蜡烛正在燃烧。）"},
                {"extinguished", "The candle was lit but the flame has been put out.（蜡烛曾点燃，但火焰已熄灭。）"},
                {"melted", "The candle has partially melted due to burning.（蜡烛因燃烧部分熔化。）"}
            }
        },
        {
            "plastic_bottle", new Dictionary<string, string>{
                {"empty", "The plastic bottle contains no liquid.（塑料瓶为空。）"},
                {"filled", "The plastic bottle is filled with liquid.（塑料瓶装有液体。）"},
                {"open", "The bottle cap is removed or loose.（瓶盖已打开或未拧紧。）"},
                {"closed", "The bottle cap is securely closed.（瓶盖已紧闭。）"},
                {"used", "The bottle has been used and may contain residue.（塑料瓶已使用，可能有残留物。）"}
            }
        },
        {
            "knife", new Dictionary<string, string>{
                {"clean", "The knife is clean and ready for use.（刀具干净，可使用。）"},
                {"in_use", "The knife is currently being used for cutting or preparing materials.（刀具正在使用中。）"},
                {"dull", "The knife blade is dull and may need sharpening.（刀刃钝，需要磨利。）"},
                {"stored", "The knife is safely stored and not in use.（刀具已安全存放，未使用。）"}
            }
        },
        {
            "waste_liquid_container", new Dictionary<string, string>{
                {"empty", "The container is empty and ready to collect waste liquids.（容器为空，可用于收集废液。）"},
                {"partially_filled", "The container contains some waste liquid.（容器内有部分废液。）"},
                {"full", "The container is full and should be emptied.（容器已满，应清空废液。）"},
                {"sealed", "The container is sealed to prevent spillage or contamination.（容器已密封，防止溢出或污染。）"},
                {"in_use", "The container is actively being used for collecting waste liquids.（容器正在使用中，用于收集废液。）"}
            }
        },
        {
            "porcelain_bowl", new Dictionary<string, string>{
                {"empty", "The porcelain bowl is empty and ready for use.（瓷碗为空，可使用。）"},
                {"contains_sample", "The bowl contains solid or liquid samples.（瓷碗内放有固体或液体样品。）"},
                {"heated", "The bowl is being heated or has been heated.（瓷碗正在加热或已加热。）"},
                {"dirty", "The bowl has residue or stains and needs cleaning.（瓷碗有残留物或污渍，需要清洗。）"}
            }
        },
        {
            "zinc_strip", new Dictionary<string, string>{
                {"clean", "The zinc strip surface is clean and shiny, ready for reaction.（锌条表面干净光亮，可用于反应。）"},
                {"oxidized", "The zinc strip surface is dull or coated with oxide.（锌条表面变暗或覆盖氧化层。）"},
                {"in_reaction", "The zinc strip is immersed in solution and reacting.（锌条正浸入溶液中反应。）"},
                {"used", "The zinc strip has been used and partially consumed.（锌条已使用，部分被消耗。）"}
            }
        },
        {
            "iron_strip", new Dictionary<string, string>{
                {"clean", "The zinc strip surface is clean and shiny, ready for reaction.（铁条表面干净光亮，可用于反应。）"},
                {"oxidized", "The zinc strip surface is dull or coated with oxide.（铁条表面变暗或覆盖氧化层。）"},
                {"in_reaction", "The zinc strip is immersed in solution and reacting.（铁条正浸入溶液中反应。）"},
                {"used", "The zinc strip has been used and partially consumed.（铁条已使用，部分被消耗。）"}
            }
        },
        {
            "measuring_cup", new Dictionary<string, string>{
                {"empty", "The measuring cup is empty and ready for use.（量杯为空，可使用。）"},
                {"filled", "The measuring cup contains a measured amount of liquid.（量杯中已加入一定体积的液体。）"},
                {"overflowing", "The liquid level exceeds the maximum scale mark.（液体超过最大刻度线，可能溢出。）"},
                {"dirty", "The measuring cup has residue and needs cleaning.（量杯内有残留物，需要清洗。）"}
            }
        },

        {
            "magnesium_ribbon", new Dictionary<string, string>{
                {"clean", "The zinc strip surface is clean and shiny, ready for reaction.（镁带表面干净光亮，可用于反应。）"},
                {"oxidized", "The zinc strip surface is dull or coated with oxide.（镁带表面变暗或覆盖氧化层。）"},
                {"in_reaction", "The zinc strip is immersed in solution and reacting.（镁带正浸入溶液中反应。）"},
                {"used", "The zinc strip has been used and partially consumed.（镁带已使用，部分被消耗。）"}
            }
        },
        {
            "bottle_cap", new Dictionary<string, string>{
                {"removed", "The bottle cap is taken off and placed aside.（瓶盖已被取下，放置在一旁）"},
                {"loosely_attached", "The bottle cap is loosely screwed on, not fully sealed.（瓶盖松散旋合，未完全密封）"},
                {"tightly_sealed", "The bottle cap is tightly screwed onto the bottle neck, ensuring a firm seal.（瓶盖紧密旋合在瓶口上，保证良好密封）"}
            }
        },
        {
            "paper_flower", new Dictionary<string, string>{
                {"dry", "The paper flower is dry and not yet exposed to liquid.（纸花干燥，未接触液体。）"},
                {"wet", "The paper flower has absorbed some liquid and appears damp.（纸花吸收了液体，呈湿润状态。）"},
                {"color_changed", "The paper flower has changed color due to chemical reaction or indicator effect.（纸花因化学反应或指示剂作用而变色。）"},
                {"damaged", "The paper flower is torn or deformed after use.（纸花使用后破损或变形。）"}
            }
        },

        {
            "copper_strip", new Dictionary<string, string>{
                {"clean", "The copper strip surface is clean and shiny, ready for reaction.（铜条表面干净光亮，可用于反应。）"},
                {"oxidized", "The copper strip surface is dull or coated with oxide.（铜条表面变暗或覆盖氧化层。）"},
                {"in_reaction", "The copper strip is immersed in solution and reacting.（铜条正浸入溶液中反应。）"},
                {"used", "The copper strip has been used and partially consumed.（铜条已使用，部分被消耗。）"}
            }
        },
        {
            "iron_nail", new Dictionary<string, string>{
                {"clean", "The iron nail is clean and metallic in appearance.（铁钉表面干净，有金属光泽。）"},
                {"rusted", "The iron nail has rusted due to oxidation.（铁钉表面生锈，发生氧化反应。）"},
                {"in_reaction", "The iron nail is immersed in solution and reacting chemically.（铁钉正浸入溶液中参与化学反应。）"},
                {"used", "The iron nail has been used and may show corrosion or discoloration.（铁钉已使用，表面可能有腐蚀或变色。）"}
            }
        },
        {
            "hard_aluminum_sheet", new Dictionary<string, string>{
                {"clean", "The zinc strip surface is clean and shiny, ready for reaction.（硬铝板表面干净光亮，可用于反应。）"},
                {"oxidized", "The zinc strip surface is dull or coated with oxide.（硬铝板表面变暗或覆盖氧化层。）"},
                {"in_reaction", "The zinc strip is immersed in solution and reacting.（硬铝板正浸入溶液中反应。）"},
                {"used", "The zinc strip has been used and partially consumed.（硬铝板已使用，部分被消耗。）"}
            }
        },




    };


    public static readonly Dictionary<string, Dictionary<string, string>> AnchorDict = new Dictionary<string, Dictionary<string, string>>()
    {
        { "beaker", new Dictionary<string, string> {
            { "bottom_center", "The geometric center of the bottom of the beaker (the center point of the plane that touches the table or iron stand)" },
            { "inside_bottom", "The center point of the bottom of the beaker (where liquid settles or dissolved matter accumulates)" },
            { "top_rim", "The center point of the beaker's upper rim (the highest point on the rim's circumference)" },
            { "side_surface", "The middle of the outer side of the beaker (usually facing the experimenter)" }
        }},
        { "large_beaker", new Dictionary<string, string> {
            { "bottom_center", "The geometric center of the bottom of the beaker (the center point of the plane that touches the table or iron stand)" },
            { "inside_bottom", "The center point of the bottom of the beaker (where liquid settles or dissolved matter accumulates)" },
            { "top_rim", "The center point of the beaker's upper rim (the highest point on the rim's circumference)" },
            { "side_surface", "The middle of the outer side of the beaker (usually facing the experimenter)" }
        }},
        {
            "rubber_stop_with_tube", new Dictionary<string, string>{
                {"top_surface", "The flat top part of the rubber stopper, used for sealing the test tube opening.（橡皮塞的上平面，用于密封试管口。）"},
                {"inserted_tube_end", "The end of the glass tube passing through the stopper, allowing gas to enter or exit.（穿过橡皮塞的玻璃管端，用于气体进出。）"},
                {"side_surface", "The cylindrical side area that fits tightly inside the test tube mouth.（与试管口紧密配合的圆柱形侧面。）"}
            }
        },
        {
            "porcelain_bowl", new Dictionary<string, string>{
                {"inner_center", "Located at the central bottom of the bowl interior — used for placing or mixing samples.（位于瓷碗内底部中心，用于放置或混合样品。）"},
                {"rim_edge", "Located at the top edge of the bowl — used for handling or pouring liquids.（位于瓷碗上缘，用于拿取或倒出液体。）"},
                {"outer_surface", "Located along the outer wall — provides grip and support when handling.（位于瓷碗外壁，用于握持和支撑。）"}
            }
        },
        {
            "zinc_strip", new Dictionary<string, string>{
                {"immersed_end", "Located at the lower end of the strip — the part immersed in solution during reactions.（位于锌条下端，是浸入溶液中参与反应的部分。）"},
                {"top_end", "Located at the upper end of the strip — used for clamping or connecting to electrodes.（位于锌条上端，用于夹持或连接电极。）"},
                {"surface_area", "Located along the flat surface — the main reaction area in contact with the solution.（位于锌条平面区域，是与溶液接触进行反应的主要区域。）"}
            }
        },
        {
            "iron_strip", new Dictionary<string, string>{
                {"immersed_end", "Located at the lower end of the strip — the part immersed in solution during reactions.（位于铁条下端，是浸入溶液中参与反应的部分。）"},
                {"top_end", "Located at the upper end of the strip — used for clamping or connecting to electrodes.（位于铁条上端，用于夹持或连接电极。）"},
                {"surface_area", "Located along the flat surface — the main reaction area in contact with the solution.（位于铁条平面区域，是与溶液接触进行反应的主要区域。）"}
            }
        },
        {
            "measuring_cup", new Dictionary<string, string>{
                {"top_rim", "Located at the upper edge of the cup — used for pouring or viewing the liquid level.（位于量杯上缘，用于倒液或观察液面高度。）"},
                {"body_center", "Located along the transparent side surface — marked with scale lines for volume measurement.（位于量杯侧面的透明区域，带有刻度线用于读数。）"},
                {"base_bottom", "Located at the flat bottom — ensures stable placement on a table or lab bench.（位于量杯底部，用于稳定放置在桌面或实验台上。）"}
            }
        },
        {
            "bottle_cap", new Dictionary<string, string>{
                {"top_surface", "The flat or slightly curved upper surface of the cap — used for gripping and sealing the bottle.（瓶盖上表面，用于抓握和密封瓶口）"},
                {"inner_thread", "The inner threaded section that screws onto the bottle neck to secure it tightly.（瓶盖内侧螺纹部分，与瓶口旋合以确保密封）"}
            }
        },
        {
            "paper_flower", new Dictionary<string, string>{
                {"petal_tip", "Located at the outer edge of the paper petals — shows the color change most clearly during experiments.（位于纸花花瓣的外缘，是观察颜色变化最明显的部分。）"},
                {"center_part", "Located at the middle of the flower — serves as the structural core holding the petals together.（位于纸花中心部分，用于固定花瓣结构。）"},
                {"stem_end", "Located at the bottom or attached end — used for holding or dipping into liquid.（位于纸花底部或连接处，用于手持或浸入液体。）"}
            }
        },
        {
            "spray_bottle", new Dictionary<string, string>{
                {"nozzle_tip", "Located at the front of the spray head — used to release fine mist or liquid droplets.（位于喷头前端，用于喷出细雾或液滴。）"},
                {"trigger_handle", "Located below or behind the nozzle — pressed to operate the spraying mechanism.（位于喷头下方或后方，通过按压触发喷雾。）"},
                {"bottle_body", "Located along the main container section — holds the liquid to be sprayed.（位于瓶身主体部分，用于储存待喷洒的液体。）"},
                {"bottom_base", "Located at the bottom — ensures stable placement when set down.（位于瓶底，用于稳固放置。）"}
            }
        },
        
        {
            "hard_aluminum_sheet", new Dictionary<string, string>{
                {"immersed_end", "Located at the lower end of the sheet — the part immersed in solution during reactions.（位于硬铝板下端，是浸入溶液中参与反应的部分。）"},
                {"top_end", "Located at the upper end of the sheet — used for clamping or connecting to electrodes.（位于硬铝板上端，用于夹持或连接电极。）"},
                {"surface_area", "Located along the flat surface — the main reaction area in contact with the solution.（位于硬铝板平面区域，是与溶液接触进行反应的主要区域。）"}
            }
        },

        {
            "magnesium_ribbon", new Dictionary<string, string>{
                {"immersed_end", "Located at the lower end of the ribbon — the part immersed in solution during reactions.（位于镁带下端，是浸入溶液中参与反应的部分。）"},
                {"top_end", "Located at the upper end of the ribbon — used for clamping or connecting to electrodes.（位于镁带上端，用于夹持或连接电极。）"},
                {"surface_area", "Located along the flat surface — the main reaction area in contact with the solution.（位于镁带平面区域，是与溶液接触进行反应的主要区域。）"}
            }
        },
        {
            "copper_strip", new Dictionary<string, string>{
                {"immersed_end", "Located at the lower end of the strip — the part immersed in solution during reactions.（位于铜条下端，是浸入溶液中参与反应的部分。）"},
                {"top_end", "Located at the upper end of the strip — used for clamping or connecting to electrodes.（位于铜条上端，用于夹持或连接电极。）"},
                {"surface_area", "Located along the flat surface — the main reaction area in contact with the solution.（位于铜条平面区域，是与溶液接触进行反应的主要区域。）"}
            }
        },
        {
            "iron_nail", new Dictionary<string, string>{
                {"tip_end", "Located at the pointed tip — used for insertion or contact in experiments.（位于铁钉尖端，用于插入或与其他物质接触。）"},
                {"head_top", "Located at the flat head of the nail — used for hammering or holding with tools.（位于铁钉平头部分，用于敲击或夹持固定。）"},
                {"shaft_body", "Located along the cylindrical shaft — provides structural connection or reaction surface.（位于铁钉的杆身部分，用于连接或与溶液接触反应。）"}
            }
        },

        {
            "graphite_electrode",new Dictionary<string, string>{
                {"tip_end","Located at the lower pointed or exposed end of the electrode — this is the part immersed in the electrolyte or solution for conducting electricity.（位于电极下端尖部或裸露部分，是浸入电解液或溶液中导电的部分。）" },
                {"upper_end","Located at the opposite end of the electrode — usually connected to the power supply or clamping device.（位于电极的上端，用于连接电源或夹具固定。）" },
                {"side_surface","Located along the cylindrical side of the electrode — represents the conductive carbon surface and contact region.（位于电极圆柱侧壁处，表示电极的导电碳表面及接触区域。）" }
            }
        },
        {
          "light_bulb", new Dictionary<string, string>{
                {"glass_bulb","Located at the outer transparent or frosted shell — encloses the filament or LED components.（位于外部透明或磨砂玻壳处，用于包裹灯丝或LED组件。）"},
                {"filament_or_LED","Located inside the bulb — the light-emitting component that glows when current passes through.（位于灯泡内部，是通电后发光的灯丝或LED元件。）"},
                {"metal_base","Located at the lower metallic screw or contact section — connects the bulb to the power socket.（位于下端金属螺纹或接触片处，用于连接电源插座。）"},
                {"contact_tip","Located at the very bottom center of the bulb base — serves as the main electrical contact point.（位于灯泡底部中心，用作主要电接触点。）"}
           }
        },
        { "test_tube", new Dictionary<string, string> {
            { "bottom_center", "The center of the bottom of the test tube (closest to the flame or hot plate)" },
            { "mouth", "Center point of test tube opening" },
            { "side_surface", "Middle of the outer wall of the test tube" },
            { "inside_bottom", "Bottom center of the test tube" }
        }},
        { "small_test_tube", new Dictionary<string, string> {
            { "bottom_center", "The center of the bottom of the test tube (closest to the flame or hot plate)" },
            { "mouth", "Center point of test tube opening" },
            { "side_surface", "Middle of the outer wall of the test tube" },
            { "inside_bottom", "Bottom center of the test tube" }
        }},
        { "test_tube_rack", new Dictionary<string, string> {
            { "top_surface", "test tube holding surface" },
            { "bottom_center", "support bottom" }
        }},
       { "conical_flask", new Dictionary<string, string> {
            { "bottom_center", "Located at the center of the flask base — used as the placement or heating point on the lab table or tripod." },
            { "neck_center", "Located at the middle of the neck — serves as a reference for attaching stoppers, thermometers, or connecting tubes." },
            { "mouth", "Located at the top opening of the neck — used for pouring liquids, inserting stoppers, or connecting other apparatus." },
            { "side_surface", "Located on the slanted conical wall — represents the main body surface where the flask expands outward from the neck to the base." }
        }},
        { "volumetric_flask", new Dictionary<string, string> {
            { "bottom_center", "placement point" },
            { "neck_center", "narrow neck middle" },
            { "mouth", "opening" },
            { "side_surface", "flask side wall" }
        }},
        { "round-bottom_flask", new Dictionary<string, string> {
            { "bottom_center", "round bottom center" },
            { "neck_center", "neck middle" },
            { "mouth", "opening" }
        }},
        { "measuring_cylinder", new Dictionary<string, string> {
            { "bottom_center", "placement point" },
            { "inside_bottom", "liquid start point" },
            { "top_rim", "top rim" },
            { "side_surface", "graduation surface" }
        }},
        { "burette", new Dictionary<string, string> {
            { "top_rim", "Located at the upper rim of the burette — used for adding liquid or connecting a funnel during filling." },
            { "tip", "Located at the lower narrow outlet — dispensing point where liquid drops out during titration." },
            { "side_surface", "Located along the main cylindrical body — represents the visible measuring tube surface with scale markings." },
            { "attachment_point", "Located near the upper or middle section — used for clamping the burette to a stand with a burette holder." }
        }},
        {
            "power_supply", new Dictionary<string, string>{
                {"positive_terminal","Located at the red-marked or '+' output port — connects to the positive electrode or circuit lead.（位于带红色标记或“+”符号的输出端，用于连接正极或电路导线。）"},
                {"negative_terminal","Located at the black-marked or '−' output port — connects to the negative electrode or circuit lead.（位于带黑色标记或“−”符号的输出端，用于连接负极或电路导线。）"},
                {"control_panel","Located on the front surface — contains switches, voltage/current knobs, and display screen.（位于正面面板处，包含电源开关、电压/电流调节旋钮及显示屏。）"},
                {"power_cord_port","Located at the back of the device — used for connecting the external AC power cord.（位于装置背面，用于连接外部交流电源线。）"}
            }
        },

        { "pipette", new Dictionary<string, string> {
            { "connection_point", "Located at the upper joint where the pipette connects to the pipet pump — ensures proper alignment and airtight sealing." },
            { "tip", "Located at the lower narrow outlet — dispensing end used for releasing liquid into another container." },
            { "side_surface", "Located along the main cylindrical body — includes the graduated scale for measuring liquid volume." },
        }},

        { "watch_glass", new Dictionary<string, string> {
            { "top_center", "sample center" },
            { "bottom_center", "placement center" }
        }},
        { "wide-mouth_bottle", new Dictionary<string, string> {
            { "bottom_center", "placement point" },
            { "mouth", "wide opening" },
            { "side_surface", "bottle body" }
        }},
        { "glass_rod", new Dictionary<string, string> {
            { "end_a", "Located at one end of the glass rod — can be used for stirring or guiding liquid flow." },
            { "end_b", "Located at the opposite end of the glass rod — serves as an alternate contact or holding end." },
            { "center", "Located at the midpoint of the rod — often used as the balance or support point when placed across a container." }
        }},
        { "evaporating_dish", new Dictionary<string, string> {
            { "bottom_center", "Located at the geometric center of the dish bottom — serves as the stable placement point on the wire gauze or tripod." },
            { "inside_bottom", "Located at the inner bottom surface — represents the area where the solution is heated and evaporation occurs." },
            { "rim", "Located along the upper edge of the dish — used for handling with tongs or for observing crystallization near the edge." }
        }},

        { "crucible", new Dictionary<string, string> {
            { "bottom_center", "Located at the geometric center of the crucible base — used as the heating and placement point above the flame or clay triangle." },
            { "mouth_center", "Located at the center of the crucible mouth opening — represents the central point for adding or removing substances, and for placing or aligning the lid." },
            { "side_surface", "Located on the outer curved wall — used as a contact surface when clamping with crucible tongs." }
        }},

        { "gas_jar", new Dictionary<string, string> {
            { "bottom_center", "The geometric center point of the bottom of the gas cylinder." },
            { "mouth", "The center point of the gas bottle mouth" },
            { "side_surface", "The general position of the outer surface of the middle part of the gas bottle can be taken as a point in the normal direction of the model shell" }
        }},
        { "petri_dish", new Dictionary<string, string> {
            { "bottom_center", "culture base" },
            { "top_rim", "lid rim" }
        }},
        { "flat-bottom_flask", new Dictionary<string, string> {
            { "bottom_center", "placement point" },
            { "mouth", "opening" },
            { "neck_center", "neck middle" },
            { "side_surface", "flask side wall" }
        }},
        { "narrow-mouth_bottle", new Dictionary<string, string> {
            { "bottom_center", "placement point" },
            { "mouth", "narrow opening" },
            { "side_surface", "bottle body" }
        }},
        { "alcohol_lamp", new Dictionary<string, string> {
            { "bottom_center", "Indicates the placement of the lamp body in contact with the table or stand." },
            { "flame_center", "It indicates the center of the flame burning and is the \"heating anchor point\" during heating experiments." },
            { "mouth", "It represents the outlet where fuel evaporates and flame is generated, and is the physical starting point of the flame." },
            { "side_surface", "Represents the alcohol lamp housing area, used for interaction (such as gripping, picking) or collision detection." }
        }},
        { "bunsen_burner", new Dictionary<string, string> {
            { "base_center", "Located at the geometric center of the burner base — serves as the support and placement point on the lab bench." },
            { "flame_center", "Located above the nozzle where the flame appears — represents the ignition and heating position." },
            { "gas_inlet", "Located at the side or bottom connection — where the gas hose attaches to supply fuel." }
        }},

        {"tripod_stand", new Dictionary<string, string> {
            { "bottom_center", "The center of the tripod bottom - serves as the overall placement and force reference point" },
            { "top_center", "Support platform center - used to place equipment or support experimental devices" }
        }},
        { "wire_gauze", new Dictionary<string, string> {
            { "center", "Located at the geometric center of the mesh — represents the main heating area above the flame." },
        }},
        {
            "crucible_tongs", new Dictionary<string, string> {
                { "tip", "Located at the clamping end — represents the gripping jaws used to hold or transfer hot crucibles and other heated apparatus." },
                { "handle", "Located at the opposite end — represents the handle area used for holding and controlling the tongs safely." }
            }
        },

        { "alcohol_lamp_cap", new Dictionary<string, string> {
            { "top_center", "Located at the top center of the cap — used as a reference for alignment or grasping when removing or placing the cap." },
            { "inner_rim", "Located along the inner edge of the cap — serves as the contact and sealing surface that fits snugly onto the alcohol lamp mouth." }
        }},

        {
            "test_tube_holder", new Dictionary<string, string>{
                { "clamp_center", "Located at the gripping jaws — this is the part that holds the test tube securely during heating or handling.（位于夹持钳口处，是固定试管、防止滑落的部位，用于加热或搬运时夹持试管。）" },
                { "handle", "Located at the elongated handle — this is the part held by the user to operate the clamp safely and maintain distance from heat sources.（位于长柄处，是操作者手持的位置，用于安全控制夹持器并远离热源。）" }
            }
        },
        {
            "ph_indicator_paper", new Dictionary<string, string>{
                {"strip_end", "Located at the tip of the indicator strip — this end is used for dipping into the solution to test pH.（位于试纸条的末端，用于浸入溶液测试酸碱性。）"},
                {"top_edge", "Located at the top edge of the strip — held by tweezers or fingers to avoid contamination.（位于试纸上端，用于镊子或手指夹持，防止污染。）"}
            }
        },
        {
            "porcelain_plate", new Dictionary<string, string>{
                {"center_surface", "Located at the flat center of the plate — used for holding or mixing small amounts of solid or liquid samples.（位于瓷板的中心平面，用于放置或混合少量固体或液体样品。）"},
                {"bottom_center", "Located at the bottom center of the plate — contact point with the lab bench for stable placement.（位于瓷板底部中心，是与实验台接触以保持稳定的位置。）"}
            }
        },
        {
            "glass_slide", new Dictionary<string, string>{
                {"center_surface", "Located at the flat central area of the slide — used to place specimens or droplets for microscopic observation.（位于载玻片的中央平面，用于放置样品或液滴进行显微观察。）"},
                {"edge_end", "Located at one end of the slide edge — typically used for handling or labeling to avoid contaminating the sample area.（位于载玻片边缘一端，用于拿取或贴标签，避免污染样品区域。）"}
            }
        },
        {
            "color_chart", new Dictionary<string, string>{
                {"front_surface", "Located on the front face of the chart — displays the color scale for comparison.（位于比色卡的正面，用于显示颜色刻度供比对使用。）"},
                {"bottom_edge", "Located at the bottom edge — used for holding or resting on a flat surface.（位于比色卡底部边缘，用于手持或放置在平面上保持稳定。）"}
            }
        },
        {
            "toothpick", new Dictionary<string, string>{
                {"tip_end", "Located at the pointed tip of the toothpick — used for picking, stirring, or transferring small samples.（位于牙签的尖端，用于挑取、搅拌或转移少量样品。）"},
                {"opposite_end", "Located at the blunt or held end — used for gripping or handling during operation.（位于牙签的钝端或手持端，用于夹持或操作时拿取。）"}
            }
        },

        {
            "protractor", new Dictionary<string, string>{
                {"center_point", "Located at the midpoint of the straight edge — the rotation or alignment center for measuring angles.（位于量角器直边的中点，是测量角度时的旋转或对准中心。）"},
                {"arc_edge", "Located along the curved edge marked with degrees — used to read the measurement of an angle.（位于刻度弧形边上，用于读取角度数值。）"}
            }
        },
        {
            "syringe", new Dictionary<string, string>{
                {"plunger_end", "Located at the top of the plunger — used to push or pull the liquid inside the barrel.（位于活塞顶部，用于推动或拉动注射器内的液体。）"},
                {"needle_tip", "Located at the pointed tip of the needle — used to inject or draw liquid.（位于针头尖端，用于注射或抽取液体。）"},
                {"barrel_center", "Located along the cylindrical barrel — holds the liquid and usually has volume markings.（位于注射器筒体中部，用于容纳液体，通常带有刻度标记。）"}
            }
        },
        {
            "iron_rod", new Dictionary<string, string>{
                {"tip_end", "Located at one end of the rod — often used for striking, stirring, or contact with other materials.（位于铁棒的一端，用于敲击、搅拌或与其他材料接触。）"},
                {"middle_section", "Located along the central length of the rod — used for gripping or supporting during experiments.（位于铁棒中部，用于握持或支撑实验操作。）"},
                {"bottom_end", "Located at the opposite end of the tip — can also serve as a support or handling point.（位于铁棒的另一端，可用作支撑或手持端。）"}
            }
        },

        {
            "ammeter", new Dictionary<string, string>{
                {"dial_center", "Located at the center of the display dial — shows the current reading.（位于表盘中心，用于显示电流读数。）"},
                {"input_terminal", "Located at the positive input terminal — where the current enters the ammeter.（位于正输入端子，电流由此进入电流表。）"},
                {"output_terminal", "Located at the negative output terminal — where the current exits the ammeter.（位于负输出端子，电流由此流出电流表。）"}
            }
        },
        {
            "candle", new Dictionary<string, string>{
                {"wick_tip", "Located at the top of the wick — the part that is ignited to produce flame.（位于烛芯顶端，用于点燃产生火焰。）"},
                {"candle_body", "Located along the main cylindrical or tapered body — provides fuel for the flame.（位于蜡烛主体部分，为火焰提供燃料。）"},
                {"base_end", "Located at the bottom of the candle — used for stable placement on a holder or surface.（位于蜡烛底部，用于放置在烛台或平面上保持稳定。）"}
            }
        },
        {
            "plastic_bottle", new Dictionary<string, string>{
                {"cap", "Located at the top opening of the bottle — used for sealing and opening to pour or fill liquid.（位于瓶口顶部，用于密封或开启以倒入或注入液体。）"},
                {"body_center", "Located along the main cylindrical or rectangular body — holds the liquid contents.（位于瓶身主体部分，用于盛装液体。）"},
                {"bottom_base", "Located at the bottom of the bottle — provides stable placement on surfaces.（位于瓶底，用于在平面上稳定放置。）"}
            }
        },
        {
            "knife", new Dictionary<string, string>{
                {"blade_tip", "Located at the pointed tip of the knife blade — used for cutting or piercing.（位于刀刃尖端，用于切割或刺入。）"},
                {"blade_edge", "Located along the sharpened edge of the blade — used for slicing or chopping.（位于刀刃的锋利边，用于切片或剁切。）"},
                {"handle", "Located at the grip section — used for holding and controlling the knife safely.（位于手柄部分，用于握持并安全操作刀具。）"}
            }
        },
        {
            "waste_liquid_container", new Dictionary<string, string>{
                {"opening_rim", "Located at the top opening — used for pouring in waste liquids safely.（位于容器顶部开口，用于安全倒入废液。）"},
                {"body_center", "Located along the main body — holds the collected waste liquid.（位于容器主体部分，用于盛放收集的废液。）"},
                {"bottom_base", "Located at the bottom of the container — provides stable placement on a surface.（位于容器底部，用于在平面上稳定放置。）"}
            }
        },

        {
            "thermometer", new Dictionary<string, string> {
                { "bulb_end", "Located at the mercury (or alcohol) bulb — represents the temperature sensing end that is immersed in the substance being measured." },
                { "top_end", "Located at the opposite end of the thermometer — used for holding or reading the scale." },
                { "side_surface", "Located along the cylindrical glass body — represents the outer glass tube surface containing the liquid column and scale markings." }
            }
        },
        {
            "balance", new Dictionary<string, string> {
                { "pan_center", "Located at the geometric center of the weighing pan — represents the main load-bearing point where samples are placed for measurement." },
                { "base_center", "Located at the geometric center of the balance base — represents the stable reference point for placement on the lab bench or surface." }
            }
        },

        {
            "stopwatch", new Dictionary<string, string> {
                { "face_center", "Located at the geometric center of the dial — represents the main display area for time reading." },
                { "button_top", "Located on the upper button — represents the control button used to start, stop, or reset the timer." }
            }
        },

        { "retort_stand", new Dictionary<string, string> {
            { "base_center", "base plate center" },
            { "rod_top", "rod top" },
            { "rod_middle", "rod middle" }
        }},
        { "ground_glass_plate", new Dictionary<string, string> {
            { "top_center", "top surface center" },
            { "bottom_center", "The geometric center of the bottom plane, defining the resting or contact point with the lab bench." },
            {"edge","The plate’s surrounding boundary, oriented toward the user, typically used for grasping, or as a reference in 3D spatial modeling." }
        }},
        {
            "separatory_funnel", new Dictionary<string, string> {
                { "top_rim", "Located at the upper opening — represents the mouth of the funnel used for adding or covering liquids." },
                { "stopcock", "Located at the valve section — represents the control valve used to regulate liquid flow between the funnel and receiving vessel." },
                { "tip", "Located at the bottom tip — represents the liquid outlet where separation or drainage occurs." },
                { "side_surface", "Located along the curved glass wall — represents the funnel’s outer surface used for visualization or handling." }
            }
        },

        { "iron_clamp", new Dictionary<string, string> {
            { "clamp_center", "The center point between the two jaws of the clamp (the area that clamps the experimental equipment)" },
            { "attachment_point", "The place where the tail of the iron clamp is connected to the upright pole of the iron frame is usually fixed with screws." }
        }},
        {
            "ring_clamp", new Dictionary<string, string> {
                { "ring_center", "Located at the geometric center of the ring — represents the primary support area used to hold flasks, funnels, or wire gauze during experiments." },
                { "attachment_point", "Located at the junction where the ring connects to the support rod — represents the mounting point used to fix the clamp on the stand." }
            }
        },
        {
            "hard_paper", new Dictionary<string, string>{
                {"corner_point", "Located at one corner of the paper — often used for handling or positioning.（位于硬纸的一角，用于拿取或定位。）"},
                {"center_surface", "Located at the flat central area — used for supporting, writing, or placing samples.（位于硬纸平面中央，用于支撑、书写或放置样品。）"},
                {"edge_side", "Located along the side edges — used for alignment or cutting.（位于硬纸边缘，用于对齐或裁剪。）"}
            }
        },
        {
            "filter_paper", new Dictionary<string, string> {
                { "center", "Located at the geometric center of the filter paper — represents the main filtration area where liquid passes through." },
                { "edge", "Located along the outer rim — represents the perimeter used for handling or alignment within a funnel." }
            }
        },
        { "dropper", new Dictionary<string, string> {
            { "top_rim", "Located at the upper rubber bulb — used for squeezing to draw or release liquid." },
            { "tip", "Located at the narrow dispensing end — the point where liquid drops exit." },
            { "side_surface", "Located along the glass or plastic tube body — represents the main cylindrical surface used for alignment or gripping." }
        }},
        { "rubber_stopper", new Dictionary<string, string> {
            { "top_center", "Center of the upper surface of the rubber stopper" },
            { "bottom_center", "Center of the bottom of the rubber stopper (the end that inserts into the mouth of the test tube)" },
            { "hole_center", "Center of the catheter hole on the rubber plug" }
        }},
        { "combustion_spoon", new Dictionary<string, string> {
            { "bowl_center", "The geometric center of the combustion spoon’s bowl, where the substance to be burned or heated is placed. This point serves as the main focus for flame application or chemical reactions during experiments." },
            { "handle", "The elongated part of the spoon designed for gripping. It allows the user to safely hold, maneuver, and position the spoon without coming into direct contact with the flame or hot substances." }
        }},
        {
            "long-neck_funnel", new Dictionary<string, string> {
                { "top_rim", "Located at the upper opening — represents the mouth of the funnel used for pouring liquids or inserting filter paper." },
                { "stem_end", "Located at the end of the long narrow stem — represents the outlet through which liquid flows into another container." }
            }
        },

        { "dropper_bottle", new Dictionary<string, string> {
            { "bottom_center", "Located at the center of the bottle's base — serves as the placement or support point when the bottle stands upright on the table." },
            { "mouth", "Located at the opening of the bottle — used to insert or attach the dropper, or for sealing with a stopper." },
            { "side_surface", "Located on the bottle's side surface — represents the area where liquid can be poured or dripped out when the bottle is tilted." }
        }},
        {
            "wash_bottle", new Dictionary<string, string> {
                { "bottom_center", "Located at the center of the bottle base — represents the placement point providing stable support on the lab bench." },
                { "nozzle_tip", "Located at the tip of the bent nozzle — represents the outlet where the liquid jet is directed for washing or rinsing." },
                { "side_surface", "Located along the outer wall of the bottle — represents the main body used for holding the washing solution and for gripping during operation." }
            }
        },

        { "spatula", new Dictionary<string, string> {
            { "tip", "Located at the flat or curved sampling end — used for scooping, transferring, or stirring solid chemicals." },
            { "handle", "Located at the opposite end — serves as the holding part for user grip and control during operation." }
        }},
        {
            "scoopula", new Dictionary<string, string> {
                { "scoop_end", "Located at the curved scoop tip — represents the working end used to transfer or collect solid chemicals." },
                { "handle", "Located at the opposite straight end — represents the holding area used for grip and control during operation." }
            }
        },
        { "filter_funnel", new Dictionary<string, string> {
            { "top_rim", "funnel opening — located at the wide upper edge, where the filter paper is placed and solution is poured in." },
            { "stem_end", "tube end — located at the narrow lower end of the funnel, through which the filtered liquid flows out." },
            { "inner_cone", "inner conical surface — supports the filter paper and guides liquid flow downward." }
        }},
        { "distilling_flask", new Dictionary<string, string> {
            { "bottom_center", "heating bottom" },
            { "side_neck", "side neck" },
            { "top_mouth", "top opening" }
        }},
        { "mortar", new Dictionary<string, string> {
            { "inside_bottom", "grinding center" },
            { "rim", "rim" }
        }},
        { "water_trough", new Dictionary<string, string> {
            { "bottom_center", "Located at the geometric center of the sink bottom (usually the lowest point where it touches the tabletop or stand)" },
            { "rim", "Center point of the upper edge of the sink (center of the highest plane on the upper edge of the sink)" }
        }},
        { "matchstick", new Dictionary<string, string> {
            { "tip", "Located at the ignition head — the chemical-coated end used for lighting." },
            { "handle", "Located along the wooden stick — the part held by hand or used to control burning direction." }
        }},
        { "cotton_wool", new Dictionary<string, string> {
            { "center", "Located at the geometric center of the cotton mass — serves as the reference point for general placement or grasping." },
            { "top_surface", "Located on the upper surface of the cotton mass — used to indicate contact with flame, air exposure, or surface interaction." }
        }},
        {
            "wooden_splint", new Dictionary<string, string> {
                { "tip", "Located at the ignition end — represents the end that is ignited and used to transfer or test for flame or gas presence." },
                { "handle", "Located at the opposite end — represents the holding area kept away from the flame for safe handling." }
            }
        },
        { "tweezers", new Dictionary<string, string> {
            { "tip", "Located at the front clamping ends — used to grip, hold, or transfer small objects such as filter paper or solid samples." },
            { "handle", "Located at the rear elongated section — the part held by hand to control the tweezers’ opening and closing." }
        }},

        { "delivery_tube", new Dictionary<string, string> {
            { "end_a", "This is the port where the tubing connects to the first experimental device (such as a gas bottle, generator, or rubber stopper)." },
            { "end_b", "This is the port where the tubing exits or leads to another device (such as a gas collection bottle, a gas guide bottle, or a cannula port leading to a solution)." },
            { "bend_point", "This is the midpoint or corner where the delivery tube bends, often used to control direction or support positioning within the setup." }
         }},

        { "clay_triangle", new Dictionary<string, string> {
            { "center", "support center" },
            { "corners", "triangle corners" }
        }},
        {
            "U-tube", new Dictionary<string, string> {
                { "left_mouth", "Located at the left opening — represents one end of the tube used for adding or connecting liquids or gases." },
                { "right_mouth", "Located at the right opening — represents the opposite end of the tube used for balancing or connecting another apparatus." },
                { "bottom_center", "Located at the center of the U-shaped bend — represents the lowest point where liquid collects or the pressure reference point." }
            }
        },

        { "bulb_desiccator", new Dictionary<string, string> {
            { "bottom_center", "Located at the geometric center of the desiccator base; this is where samples or desiccant containers are placed. It also serves as the support point when the desiccator sits on a lab bench." },
            { "top_rim", "Located along the upper circular edge of the desiccator body; this represents the contact surface between the main body and the lid, forming an airtight seal." },
            { "side_surface", "Refers to the curved outer wall of the desiccator body; this area is often used for visualization, handling, or anchoring the model in simulations." }
        }},
        { "condenser", new Dictionary<string, string> {
            { "inlet", "Located at the lower side joint — cooling water inlet through which cold water enters the condenser jacket." },
            { "outlet", "Located at the upper side joint — cooling water outlet where warmed water exits." },
            { "side_neck", "Located at the side neck on the upper part of the condenser — vapor from the distillation flask enters here." },
            { "tip", "Located at the lower tip — condensate outlet where the cooled liquid drips out." }
        }},

        {
            "asbestos_mesh", new Dictionary<string, string> {
                { "center", "Located at the geometric center of the mesh — represents the main heating area where the flame heat is evenly distributed to support vessels such as beakers or flasks." },
                { "edges", "Located along the outer metal frame — represents the supporting boundaries that rest on the ring clamp or tripod stand." }
            }
        },

        { "test_tube_brush", new Dictionary<string, string> {
            { "brush_end", "Located at the tip of the brush where the bristles are concentrated. This is the part that enters the test tube and performs the actual cleaning action." },
            { "handle", "Located at the opposite end of the brush, typically made of metal or plastic, designed for manual grip and control during cleaning." }
        }},
        { "kipps_apparatus", new Dictionary<string, string> {
            { "top_inlet", "top inlet — where acid is added" },
            { "gas_outlet", "gas outlet — where generated gas is released" },
            { "bottom_valve", "bottom valve — used to control liquid discharge and gas generation" }
        }},

        { "gas_washing_bottle", new Dictionary<string, string> {
            { "inlet_tube", "Located on one side of the stopper, this tube usually extends to the bottom of the bottle. It allows the gas to enter and bubble through the washing liquid for purification or absorption." },
            { "outlet_tube", "Situated on the opposite side of the stopper, this tube extends only slightly below the bottle’s mouth. It serves as the outlet for the cleaned gas to exit the bottle." },
            { "bottom_center", "Located at the geometric center of the bottle’s base, it marks the placement point on the lab bench or serves as a 3D spatial reference for positioning the apparatus." }
        }}
    };

}
