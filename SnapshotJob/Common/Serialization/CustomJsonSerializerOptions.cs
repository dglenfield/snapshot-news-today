namespace Common.Serialization;

[Flags]
public enum CustomJsonSerializerOptions
{
    IgnoreNull = 0,
    WriteIndented = 1 << 0, // 1
    //CamelCase = 1 << 1, // 2
    //IgnoreDefaultValues = 1 << 2, // 4
    //IncludeFields = 1 << 3, // 8
    //StrictParsing = 1 << 2, // 16
    //AnotherOption = 1 << 3, // 32
    // Add more as needed: 64, 128, 256, 512, etc.
}
