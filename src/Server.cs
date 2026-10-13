using System.Collections.Generic;

public static class Server
{
    public static string CurrentSkeleton { get; internal set; } = "Мамонт";
    public static HashSet<string> CollectedSkeletonPieces { get; internal set; } = [];
}
