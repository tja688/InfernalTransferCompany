using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// 契约数据生成器
/// 负责生成随机的顾客信息、契约文书和相关的游戏数据
/// </summary>
[CreateAssetMenu(fileName = "ContractDataGenerator", menuName = "Hedium/Contract Data Generator")]
public class ContractDataGenerator : ScriptableObject
{
    [Header("=== 顾客数据资源 ===")]
    [Tooltip("顾客姓名数据库")]
    public CustomerNameData nameData;
    
    [Tooltip("职业数据库")]
    public OccupationData occupationData;
    
    [Tooltip("顾客照片资源")]
    public CustomerPhotoData photoData;

    [Header("=== 契约模板 ===")]
    [Tooltip("契约描述模板")]
    public List<ContractTemplate> contractTemplates = new List<ContractTemplate>();

    [Header("=== 生成概率设置 ===")]
    [Range(0f, 1f)]
    [Tooltip("生成文书问题的基础概率")]
    public float documentIssueChance = 0.15f;
    
    [Range(0f, 1f)]
    [Tooltip("生成伪装顾客的概率")]
    public float disguiseChance = 0.08f;
    
    [Range(0f, 1f)]
    [Tooltip("生成克洛克达尔帮成员的概率")]
    public float clocardalMemberChance = 0.05f;
    
    [Range(0f, 1f)]
    [Tooltip("生成内容不匹配的概率")]
    public float contentMismatchChance = 0.12f;

    [Header("=== 难度调节 ===")]
    [Tooltip("当前难度等级")]
    public int difficultyLevel = 1;
    
    [Tooltip("难度对问题概率的影响系数")]
    public float difficultyMultiplier = 1.2f;

    /// <summary>
    /// 生成随机顾客数据
    /// </summary>
    public Customer GenerateRandomCustomer()
    {
        var customer = new Customer();
        
        // 生成基础信息
        customer.name = GenerateRandomName();
        customer.occupation = GenerateRandomOccupation();
        customer.photo = GenerateRandomPhoto();
        
        // 根据概率生成特殊状态
        float difficultyBonus = (difficultyLevel - 1) * 0.1f;
        customer.isDisguised = UnityEngine.Random.value < (disguiseChance + difficultyBonus);
        customer.isClocardalMember = UnityEngine.Random.value < (clocardalMemberChance + difficultyBonus);
        
        // 生成口头请求
        customer.spokenRequest = GenerateSpokenRequest(customer);
        
        return customer;
    }

    /// <summary>
    /// 生成随机契约文书
    /// </summary>
    public ContractDocument GenerateRandomContract(Customer customer)
    {
        var document = new ContractDocument();
        
        // 基础信息
        document.customerName = customer.name;
        document.occupation = customer.occupation;
        document.customerPhoto = customer.photo;
        document.appointmentDate = GenerateAppointmentDate();
        
        // 生成契约类型和相关信息
        document.HeContractType = GenerateRandomHeContractType();
        document.contractDescription = GenerateContractDescription(document.HeContractType);
        document.soulPercentage = GenerateRandomSoulPercentage(document.HeContractType);
        
        // 根据难度和概率生成文书问题
        ApplyDocumentIssues(document, customer);
        
        return document;
    }

    /// <summary>
    /// 生成完整的契约上下文
    /// </summary>
    public HeContractContext GenerateRandomHeContractContext()
    {
        var context = new HeContractContext();
        
        // 生成顾客和契约
        context.customer = GenerateRandomCustomer();
        context.document = GenerateRandomContract(context.customer);
        
        // 设置初始状态
        context.satisfaction = 3; // 将在manager中使用配置覆盖
        context.failCount = 0;
        context.isTerminated = false;
        
        return context;
    }

    #region 私有生成方法

    private string GenerateRandomName()
    {
        if (nameData != null && nameData.names.Count > 0)
        {
            // 根据权重选择姓名
            return WeightedRandomSelection(nameData.names, item => item.weight).name;
        }
        
        // 默认姓名列表
        string[] defaultNames = { "艾莉丝", "托马斯", "玛丽", "约翰", "凯瑟琳", "威廉", "伊莎贝拉", "亚瑟", "维多利亚", "爱德华" };
        return defaultNames[UnityEngine.Random.Range(0, defaultNames.Length)];
    }

    private string GenerateRandomOccupation()
    {
        if (occupationData != null && occupationData.occupations.Count > 0)
        {
            return WeightedRandomSelection(occupationData.occupations, item => item.weight).name;
        }
        
        // 默认职业列表
        string[] defaultOccupations = { "铁匠", "商人", "学者", "农夫", "工匠", "守卫", "医师", "吟游诗人", "船员", "织工" };
        return defaultOccupations[UnityEngine.Random.Range(0, defaultOccupations.Length)];
    }

    private Sprite GenerateRandomPhoto()
    {
        if (photoData != null && photoData.photos.Count > 0)
        {
            return WeightedRandomSelection(photoData.photos, item => item.weight).sprite;
        }
        
        return null; // 如果没有照片资源
    }

    private HeContractType GenerateRandomHeContractType()
    {
        // 根据难度调整各类型的出现概率
        var typeWeights = new Dictionary<HeContractType, float>
        {
            { HeContractType.Money, 0.3f },
            { HeContractType.Fame, 0.25f },
            { HeContractType.Skill, 0.3f },
            { HeContractType.Event, 0.15f } // 事件类型稍微稀少
        };
        
        // 难度越高，复杂类型出现概率越高
        if (difficultyLevel >= 3)
        {
            typeWeights[HeContractType.Event] += 0.1f;
            typeWeights[HeContractType.Money] -= 0.05f;
            typeWeights[HeContractType.Skill] -= 0.05f;
        }
        
        return WeightedRandomHeContractType(typeWeights);
    }

    private string GenerateContractDescription(HeContractType HeContractType)
    {
        // 从模板中查找对应的描述
        var templates = contractTemplates.FindAll(t => t.HeContractType == HeContractType);
        if (templates.Count > 0)
        {
            var selectedTemplate = templates[UnityEngine.Random.Range(0, templates.Count)];
            return selectedTemplate.descriptions[UnityEngine.Random.Range(0, selectedTemplate.descriptions.Count)];
        }
        
        // 默认描述
        return HeContractType switch
        {
            HeContractType.Money => GetRandomFromArray(new[] { "渴望获得财富", "希望财源滚滚", "想要摆脱贫困" }),
            HeContractType.Fame => GetRandomFromArray(new[] { "期望获得名望", "渴求万人敬仰", "想要名垂青史" }),
            HeContractType.Skill => GetRandomFromArray(new[] { "渴望识读文字", "希望掌握技艺", "想要获得智慧" }),
            HeContractType.Event => GetRandomFromArray(new[] { "希望改变命运", "想要扭转局面", "渴求人生转机" }),
            _ => "未知需求"
        };
    }

    private float GenerateRandomSoulPercentage(HeContractType HeContractType)
    {
        // 根据契约类型设置不同的灵魂份额范围
        return HeContractType switch
        {
            HeContractType.Money => UnityEngine.Random.Range(0.2f, 0.4f),    // 二成到四成
            HeContractType.Fame => UnityEngine.Random.Range(0.3f, 0.5f),     // 三成到五成
            HeContractType.Skill => UnityEngine.Random.Range(0.2f, 0.4f),    // 二成到四成
            HeContractType.Event => UnityEngine.Random.Range(0.4f, 0.6f),    // 四成到六成（风险更高）
            _ => UnityEngine.Random.Range(0.2f, 0.5f)
        };
    }

    private DateTime GenerateAppointmentDate()
    {
        // 大多数情况下是当天，少数情况有日期问题
        if (UnityEngine.Random.value < 0.9f)
        {
            return DateTime.Today;
        }
        else
        {
            // 生成错误日期
            return DateTime.Today.AddDays(UnityEngine.Random.Range(-3, 4));
        }
    }

    private string GenerateSpokenRequest(Customer customer)
    {
        var requestTemplates = new List<string>
        {
            "我希望获得更多的财富",
            "我想要变得更有名气",
            "我渴望学会新的技能",
            "我需要改变我的命运",
            "我想要获得力量",
            "我希望能够识读文字",
            "我想要摆脱现在的困境",
            "我渴望获得知识",
            "我希望能够富裕起来",
            "我想要在这个世界留下痕迹"
        };
        
        // 如果是伪装者，可能会有不一致的表述
        if (customer.isDisguised && UnityEngine.Random.value < 0.3f)
        {
            // 故意说一些模糊或不一致的话
            var vagueRequests = new[]
            {
                "我...我想要那个东西",
                "你知道我想要什么的",
                "按照文书上的做就行了",
                "就是普通的那种契约"
            };
            return GetRandomFromArray(vagueRequests);
        }
        
        return GetRandomFromArray(requestTemplates.ToArray());
    }

    private void ApplyDocumentIssues(ContractDocument document, Customer customer)
    {
        float difficultyBonus = (difficultyLevel - 1) * 0.05f;
        float baseChance = documentIssueChance + difficultyBonus;
        
        // 封蜡问题
        if (UnityEngine.Random.value < baseChance * 0.8f)
        {
            document.isSealed = false;
            Debug.Log("生成文书问题: 封蜡破损");
        }
        
        // 伪造问题
        if (UnityEngine.Random.value < baseChance * 0.3f)
        {
            switch (UnityEngine.Random.Range(0, 3))
            {
                case 0:
                    document.isGenuine = false;
                    break;
                case 1:
                    document.hasITCWatermark = false;
                    break;
                case 2:
                    document.isInkGenuine = false;
                    break;
            }
            Debug.Log("生成文书问题: 伪造文书");
        }
        
        // 日期问题
        if (UnityEngine.Random.value < baseChance && document.appointmentDate != DateTime.Today)
        {
            document.isDateCorrect = false;
            Debug.Log("生成文书问题: 日期不符");
        }
        
        // 身份问题
        if (customer.isDisguised)
        {
            document.isIdentityMatched = false;
            Debug.Log("生成文书问题: 身份不符");
        }
        
        // 内容匹配问题
        if (UnityEngine.Random.value < (contentMismatchChance + difficultyBonus))
        {
            document.isContentMatched = false;
            Debug.Log("生成文书问题: 内容不匹配");
        }
    }

    #endregion

    #region 工具方法

    /// <summary>
    /// 权重随机选择
    /// </summary>
    private T WeightedRandomSelection<T>(List<T> items, System.Func<T, float> weightSelector)
    {
        if (items.Count == 0) return default(T);
        
        float totalWeight = 0f;
        foreach (var item in items)
        {
            totalWeight += weightSelector(item);
        }
        
        float randomValue = UnityEngine.Random.Range(0f, totalWeight);
        float currentWeight = 0f;
        
        foreach (var item in items)
        {
            currentWeight += weightSelector(item);
            if (randomValue <= currentWeight)
            {
                return item;
            }
        }
        
        return items[items.Count - 1];
    }

    /// <summary>
    /// 权重随机选择契约类型
    /// </summary>
    private HeContractType WeightedRandomHeContractType(Dictionary<HeContractType, float> weights)
    {
        float totalWeight = 0f;
        foreach (var weight in weights.Values)
        {
            totalWeight += weight;
        }
        
        float randomValue = UnityEngine.Random.Range(0f, totalWeight);
        float currentWeight = 0f;
        
        foreach (var kvp in weights)
        {
            currentWeight += kvp.Value;
            if (randomValue <= currentWeight)
            {
                return kvp.Key;
            }
        }
        
        return HeContractType.Money; // 默认返回
    }

    /// <summary>
    /// 从数组中随机选择一个元素
    /// </summary>
    private T GetRandomFromArray<T>(T[] array)
    {
        if (array.Length == 0) return default(T);
        return array[UnityEngine.Random.Range(0, array.Length)];
    }

    #endregion

    #region 数据结构

    [System.Serializable]
    public class ContractTemplate
    {
        public HeContractType HeContractType;
        public List<string> descriptions = new List<string>();
    }

    [System.Serializable]
    public class NameEntry
    {
        public string name;
        [Range(0.1f, 10f)]
        public float weight = 1f;
        public Gender preferredGender = Gender.Any;
    }

    [System.Serializable]
    public class OccupationEntry
    {
        public string name;
        [Range(0.1f, 10f)]
        public float weight = 1f;
        public string description;
        public Color associatedColor = Color.white;
    }

    [System.Serializable]
    public class PhotoEntry
    {
        public Sprite sprite;
        [Range(0.1f, 10f)]
        public float weight = 1f;
        public Gender gender = Gender.Any;
        public string description;
    }

    public enum Gender
    {
        Male,
        Female,
        Any
    }

    #endregion

    #region 验证和调试

    /// <summary>
    /// 验证配置完整性
    /// </summary>
    [ContextMenu("Validate Configuration")]
    public void ValidateConfiguration()
    {
        int issues = 0;
        
        if (nameData == null || nameData.names.Count == 0)
        {
            Debug.LogWarning("姓名数据缺失或为空");
            issues++;
        }
        
        if (occupationData == null || occupationData.occupations.Count == 0)
        {
            Debug.LogWarning("职业数据缺失或为空");
            issues++;
        }
        
        if (photoData == null || photoData.photos.Count == 0)
        {
            Debug.LogWarning("照片数据缺失或为空");
            issues++;
        }
        
        if (contractTemplates.Count == 0)
        {
            Debug.LogWarning("契约模板为空");
            issues++;
        }
        
        if (issues == 0)
        {
            Debug.Log("配置验证通过!");
        }
        else
        {
            Debug.LogError($"发现 {issues} 个配置问题");
        }
    }

    /// <summary>
    /// 生成测试数据
    /// </summary>
    [ContextMenu("Generate Test Data")]
    public void GenerateTestData()
    {
        Debug.Log("=== 生成测试数据 ===");
        
        for (int i = 0; i < 5; i++)
        {
            var context = GenerateRandomHeContractContext();
            Debug.Log($"测试 {i + 1}: {context.customer.name} ({context.customer.occupation}) - {context.document.HeContractType} - {context.document.soulPercentage * 100:F0}%");
        }
    }

    #endregion
}

#region 数据资源类

/// <summary>
/// 顾客姓名数据库
/// </summary>
[CreateAssetMenu(fileName = "CustomerNameData", menuName = "Hedium/Data/Customer Names")]
public class CustomerNameData : ScriptableObject
{
    public List<ContractDataGenerator.NameEntry> names = new List<ContractDataGenerator.NameEntry>();
}

/// <summary>
/// 职业数据库
/// </summary>
[CreateAssetMenu(fileName = "OccupationData", menuName = "Hedium/Data/Occupations")]
public class OccupationData : ScriptableObject
{
    public List<ContractDataGenerator.OccupationEntry> occupations = new List<ContractDataGenerator.OccupationEntry>();
}

/// <summary>
/// 顾客照片数据库
/// </summary>
[CreateAssetMenu(fileName = "CustomerPhotoData", menuName = "Hedium/Data/Customer Photos")]
public class CustomerPhotoData : ScriptableObject
{
    public List<ContractDataGenerator.PhotoEntry> photos = new List<ContractDataGenerator.PhotoEntry>();
}

#endregion