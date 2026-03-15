namespace MineMogul.ConveyorCannonTweaks
{
    internal static class ConveyorCannonTweaksRuntime
    {
        internal static ConveyorCannonTweaksRuntimeController Current { get; private set; }

        internal static void Set(ConveyorCannonTweaksRuntimeController runtime)
        {
            Current = runtime;
        }

        internal static void Clear(ConveyorCannonTweaksRuntimeController runtime)
        {
            if (ReferenceEquals(Current, runtime))
            {
                Current = null;
            }
        }
    }
}
