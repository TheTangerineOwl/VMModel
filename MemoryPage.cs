namespace VirtualMemory
{
    /// <summary>
    ///  Класс, определяющий страницу, находящуюся в виртуальной памяти.
    /// </summary>
    public class MemoryPage
    {
        public const int byteNum = 512 / sizeof(int);
        public const int pageSize = 512 + byteNum;

        public int Index = 0;
        public bool IsModified = false;
        public DateTime ModTime = DateTime.Now;

        public byte[] Bitmap;
        public byte[] Data;

        public MemoryPage(int index)
        {
            Index = index;
            Bitmap = new byte[byteNum];
            Data = new byte[pageSize];
        }
    }
}
