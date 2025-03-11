public static class StringNamingExtensions
{
    public static string ToFieldName(this string propertyName)
    {
        if (propertyName.Length == 0 || propertyName[0] == '_')
            return propertyName;
        return string.Concat($"_{char.ToLower(propertyName[0])}{propertyName.Substring(1)}");
    }

    public static string ToPropertyName(this string fieldName)
    {
        if (fieldName.Length == 0 || char.IsUpper(fieldName[0]))
            return fieldName;
        if (fieldName[0] == '_')
        {
            fieldName = fieldName.Substring(1);
        }
        if (fieldName.Length == 0)
            return string.Empty;
        if (fieldName.Length == 1)
            return char.ToUpper(fieldName[0]).ToString();
        return string.Concat(char.ToUpper(fieldName[0]), fieldName.Substring(1));
    }
}