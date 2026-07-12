using System;

public static class AttributeDataExtensions
{
    /// <summary>
    /// 按参数名安全获取 Attribute 的值（不区分大小写）。
    /// 同时支持构造函数位置参数和命名参数。
    /// 若未找到或值为 null，返回 default(TypedConstant)。
    /// </summary>
    public static TypedConstant GetArgumentValue(this AttributeData attr, string parameterName)
    {
        // 1. 优先查找命名参数（不区分大小写）
        foreach (var named in attr.NamedArguments)
        {
            if (string.Equals(named.Key, parameterName, StringComparison.OrdinalIgnoreCase))
                return named.Value;
        }

        // 2. 回退到构造函数位置参数，通过参数名匹配索引（不区分大小写）
        var method = attr.AttributeConstructor;
        if (method != null)
        {
            var parameters = method.Parameters;
            for (int i = 0; i < parameters.Length && i < attr.ConstructorArguments.Length; i++)
            {
                if (string.Equals(parameters[i].Name, parameterName, StringComparison.OrdinalIgnoreCase))
                    return attr.ConstructorArguments[i];
            }
        }

        // 3. 未找到该参数
        return default;
    }
}