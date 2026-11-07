using System;
using System.Linq;

public class DiggablesReceiver(MuseumRoot root)
{
    private MuseumPlacer Placer => root.Building.Placer;

    private MuseumState State => root.State;

    public void AddDiggable(IpcMessageSubmitDiggable value)
    {
        if (MeshCollection.Skeletons.Keys.Contains(value.Type))
        {
            if (!root.State.CurrentSkeletonPiecesAmounts.TryGetValue(value.Id, out var amount))
            {
                return;
            }
            if (amount > 0)
            {
                return;
            }

            var currentPieces = State.CurrentSkeletonPiecesAmounts;
            currentPieces[value.Id]++;
            Placer.UpdateCurrentSkeleton(value.Id);
            if (currentPieces.All(p => p.Value > 0))
            {
                var oldSkeleton = State.CurrentSkeleton;
                State.CompleteSkeleton(root);
                Placer.ReplaceCurrentSkeleton(State.CurrentSkeleton, oldSkeleton);
            }
        }
        else
        {
            State.OtherDiggablesAmounts[value.Id]++;
            Placer.UpdateItem(value.Id);
        }
    }
}
