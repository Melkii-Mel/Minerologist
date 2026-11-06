public static class ObjectExtensions
{
    public static T[]? ToArrayNullable<T>(this T? item)
    {
        return item == null ? null : [item];
    }
}
