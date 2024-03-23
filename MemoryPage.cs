namespace VirtualMemory
{
    /// <summary>
    ///  Класс, определяющий страницу, находящуюся в виртуальной памяти.
    /// </summary>
    public class MemoryPage
    {
        // Размер битовой карты в байтах.
        public const int byteNum = 512 / (sizeof(int) * 8);
        public const int pageSize = 512 + byteNum;
        public int Index = 0;
        public bool IsModified = false;
        public DateTime ModTime = DateTime.Now;
        public byte[] Bitmap;
        public byte[] Data;

        /// <summary>
        /// Страница в виртуальной памяти.
        /// </summary>
        /// <param name="index">Абсолютный номер страницы.</param>
        public MemoryPage(int index)
        {
            Index = index;
            Bitmap = new byte[byteNum];
            Data = new byte[pageSize - byteNum];
        }
    }
}
