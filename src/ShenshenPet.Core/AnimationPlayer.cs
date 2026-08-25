namespace ShenshenPet.Core;

public sealed class AnimationPlayer
{
    private readonly PetManifest _manifest;
    private double _elapsedMilliseconds;

    public AnimationPlayer(PetManifest manifest, string initialAnimation = "idle")
    {
        _manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
        CurrentAnimation = _manifest.ResolveAnimation(initialAnimation);
    }

    public AnimationDefinition CurrentAnimation { get; private set; }

    public int FrameIndex { get; private set; }

    public void Play(string id, bool restart = true)
    {
        var next = _manifest.ResolveAnimation(id);
        if (!restart && string.Equals(CurrentAnimation.Id, next.Id, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        CurrentAnimation = next;
        FrameIndex = 0;
        _elapsedMilliseconds = 0;
    }

    public bool Advance(TimeSpan elapsed)
    {
        if (elapsed <= TimeSpan.Zero)
        {
            return false;
        }

        var changed = false;
        _elapsedMilliseconds += elapsed.TotalMilliseconds;

        while (_elapsedMilliseconds >= CurrentAnimation.FrameDurationsMs[FrameIndex])
        {
            _elapsedMilliseconds -= CurrentAnimation.FrameDurationsMs[FrameIndex];
            if (FrameIndex + 1 < CurrentAnimation.FrameCount)
            {
                FrameIndex++;
                changed = true;
                continue;
            }

            if (CurrentAnimation.Loop)
            {
                FrameIndex = 0;
                changed = true;
                continue;
            }

            Play(CurrentAnimation.ReturnTo ?? "idle");
            return true;
        }

        return changed;
    }
}
