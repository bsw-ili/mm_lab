using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;   // 需要导入 Newtonsoft.Json (可用 NuGet 或 Unity Package Manager)

public class SceneLoader1 : MonoBehaviour
{
    [System.Serializable]
    public class SceneObject
    {
        public string name;
        public float[] position;
        public float[] rotation;
        public float[] scale;
        public State state;
    }

    [System.Serializable]
    public class State
    {
        public bool hasWater;
        public string waterLevel;
    }

    [System.Serializable]
    public class SceneConfig
    {
        public List<SceneObject> objects;
    }

    // 预制体映射
    public GameObject BeakerPrefab;
    public GameObject GasBottlePrefab;

    void Start()
    {
        // 假设这是 LLM 返回的 JSON（你可以从文件或 API 获取）
        string jsonConfig = @"
        {
          'objects': [
            {
              'name': 'Beaker',
              'position': [0, 1.2, 0],
              'rotation': [45, 0, 0],
              'scale': [1, 1, 1],
              'state': {
                'hasWater': false
              }
            },
            {
              'name': 'GasBottle',
              'position': [0, 0, 0],
              'rotation': [0, 0, 0],
              'scale': [1, 1, 1],
              'state': {
                'waterLevel': 'full'
              }
            }
          ]
        }";

        SceneConfig config = JsonConvert.DeserializeObject<SceneConfig>(jsonConfig);

        foreach (var obj in config.objects)
        {
            GameObject prefab = null;
            if (obj.name == "Beaker") prefab = BeakerPrefab;
            if (obj.name == "GasBottle") prefab = GasBottlePrefab;

            if (prefab != null)
            {
                GameObject instance = Instantiate(prefab);
                instance.transform.position = new Vector3(obj.position[0], obj.position[1], obj.position[2]);
                instance.transform.rotation = Quaternion.Euler(obj.rotation[0], obj.rotation[1], obj.rotation[2]);
                instance.transform.localScale = new Vector3(obj.scale[0], obj.scale[1], obj.scale[2]);

                // 根据状态调整
                if (obj.name == "Beaker")
                {
                    Transform water = instance.transform.Find("Water");
                    if (water != null) water.gameObject.SetActive(obj.state.hasWater);
                }
                else if (obj.name == "GasBottle")
                {
                    Transform water = instance.transform.Find("Water");
                    if (water != null)
                    {
                        if (obj.state.waterLevel == "full") water.localScale = new Vector3(1, 1, 1);
                        else if (obj.state.waterLevel == "empty") water.localScale = new Vector3(1, 0, 1);
                    }
                }
            }
        }
    }
}
