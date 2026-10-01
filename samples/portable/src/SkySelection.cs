using System;

namespace GeometrySpaceRace.Presentation
{
    /// <summary>Texture shuffle bags on an independent appearance stream; bounded random access.</summary>
    public sealed class SkySelection
    {
        public const int Count = 10;
        private readonly int[] bag, previous;
        private readonly int seed, count, checkpointsPerChange;
        private long cachedBag = -1;
        public SkySelection(int seed, int count = Count, int checkpointsPerChange = 35)
        {
            if (count < 2 || count > 32) throw new ArgumentOutOfRangeException(nameof(count));
            if (checkpointsPerChange < 1) throw new ArgumentOutOfRangeException(nameof(checkpointsPerChange));
            this.seed = seed; this.count = count; this.checkpointsPerChange=checkpointsPerChange;
            bag = new int[count]; previous = new int[count];
        }

        public int ForCheckpoint(long checkpoint)
        {
            if (checkpoint < 0) throw new ArgumentOutOfRangeException(nameof(checkpoint));
            long section = checkpoint / checkpointsPerChange, index = section / count;
            if (index != cachedBag)
            {
                Shuffle(index, bag);
                if (index > 0)
                {
                    Shuffle(index - 1, previous);
                    if (bag[0] == previous[count - 1])
                    { int held = bag[0]; bag[0] = bag[1]; bag[1] = held; }
                }
                cachedBag = index;
            }
            return bag[(int)(section % count)];
        }

        private void Shuffle(long index, int[] values)
        {
            ulong state = unchecked((ulong)(uint)seed ^ (ulong)index * 0xD6E8FEB86659FD93UL ^ 0x617E285AA429B31FUL);
            for (int i = 0; i < count; i++) values[i] = i;
            for (int i = count - 1; i > 0; i--)
            {
                ulong value;
                unchecked
                {
                    value = (state += 0x9E3779B97F4A7C15UL);
                    value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
                    value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
                    value ^= value >> 31;
                }
                int selected = (int)(value % (ulong)(i + 1));
                int held = values[i]; values[i] = values[selected]; values[selected] = held;
            }
        }
    }
}
