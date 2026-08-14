using Swed64;

namespace KittyMenu
{
    // Resolves entity-list slots to addresses. The list is paged: a controller
    // index and a pawn handle both resolve through the same chunked layout.
    public static class EntityList
    {
        public static IntPtr GetEntry(Swed swed, IntPtr entityList, int index)
        {
            if (entityList == IntPtr.Zero || index <= 0)
                return IntPtr.Zero;

            int chunk = (index & 0x7FFF) >> 9;
            int slot = index & 0x1FF;

            IntPtr listEntry = swed.ReadPointer(entityList, 0x8 * chunk + Offsets.ChunkEntryOffset);
            if (listEntry == IntPtr.Zero)
                return IntPtr.Zero;

            return swed.ReadPointer(listEntry, Offsets.ListEntryStride * slot);
        }
    }
}
