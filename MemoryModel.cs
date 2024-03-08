using System.Text;

namespace VirtualMemory
{
    /// <summary>
    /// Класс, определяющий модель виртуальной памяти.
    /// </summary>
    public class MemoryModel : IDisposable
    {
        int _bufferSize;
        int _arraySize;
        Stream array;
        MemoryPage[] pages;

        public MemoryModel(int bufferSize = 3, int arraySize = 10000, string filename = "vm.bin")
        {
            _bufferSize = bufferSize;
            _arraySize = arraySize;
            // В случае, если отсутствует файл дампа, создает новый файл и заполняет нулями нужное количество страниц.
            if (!File.Exists(filename))
            {
                array = new FileStream(filename, FileMode.Create, FileAccess.ReadWrite);
                byte[] buffer = Encoding.Default.GetBytes("VM");
                array.Write(buffer, 0, buffer.Length);
                int bytesCount = (arraySize / MemoryPage.byteNum + 1) * MemoryPage.pageSize;
                for (int i = 0; i < bytesCount; i++)
                    array.WriteByte(0);
            }
            else
                // В случае, если файл дампа уже существует, открывает его для работы.
                array = new FileStream(filename, FileMode.Open, FileAccess.ReadWrite);
            // Пропускает сигнатуру в начале файла.
            array.Seek(2, SeekOrigin.Begin);
            pages = new MemoryPage[bufferSize];
            // Создание массива для промежуточного считывания необходимого количества страниц из файла дампа и их чтение.
            byte[] data = new byte[bufferSize * MemoryPage.pageSize];
            array.Read(data, 0, data.Length);
            for (int i = 0; i < bufferSize; i++)
            {
                MemoryPage newPage = new MemoryPage(i);
                // Копирование битовой карты из памяти для выбранной страницы в буфере.
                Buffer.BlockCopy(data, i * MemoryPage.pageSize, newPage.Bitmap, 0, MemoryPage.byteNum);
                // Копирование данных из памяти для выбранной страницы в буфере.
                Buffer.BlockCopy(data, i * MemoryPage.pageSize + MemoryPage.byteNum, newPage.Data, 0, MemoryPage.byteNum * sizeof(int));
                pages[i] = newPage;
            }

        }

        public int? FindPage(int index)     // определить и вернуть номер страницы в буфере по индексу данных в массиве
        {
            int pageIndex = index / MemoryPage.byteNum;     // определить абсолютный номер страницы
            if (pageIndex > _arraySize / MemoryPage.byteNum)
                return null;   // проверка наличия страницы в памяти

            MemoryPage? page = pages.FirstOrDefault(page => page.Index == pageIndex); // поиск страницы в буфере

            if (page == null)
            {
                MemoryPage toRemove = pages.Aggregate((x, y) => x.ModTime < y.ModTime ? x : y); // поиск самой старой страницы в буфере

                int toRemoveIndex = Array.IndexOf(pages, toRemove); // индекс страницы для замены


                if (toRemove.IsModified == true)
                    SavePage(toRemoveIndex);   // если старая страницы была изменена, сохранить ее в памяти

                MemoryPage newPage = new MemoryPage(pageIndex); // сброс статуса и установка нового времени изменения происходят по умолчанию

                array.Seek(2 + MemoryPage.pageSize * pageIndex, SeekOrigin.Begin);  // чтение страницы с нужным абсолютным индексом из памяти
                array.Read(pages[toRemoveIndex].Bitmap, 0, MemoryPage.byteNum);
                array.Read(pages[toRemoveIndex].Data, 0, MemoryPage.byteNum * sizeof(int));

                pages[toRemoveIndex] = newPage; // замена страницы в буфере

                return toRemoveIndex;   // возврат индекса страницы в буфере
            }

            return Array.IndexOf(pages, page);
        }

        public bool ReadValue(out int value, int elementIndex)  // чтение значения из буфера
        {
            int pageIndex = FindPage(elementIndex) ?? -1;   // если не удалось найти страницу, то -1
            value = 0;

            if (pageIndex == -1)
                return false;

            int onPageIndex = elementIndex % MemoryPage.byteNum;    // страничный адрес элемента массива с заданным элементом
            if (pages[pageIndex].Bitmap[onPageIndex] == 0)
                return false;    // если значение не было задано

            byte[] bytes = new byte[sizeof(int)];

            Buffer.BlockCopy(pages[pageIndex].Data, onPageIndex * sizeof(int), bytes, 0, sizeof(int));  // считывание значения
            value = BitConverter.ToInt32(bytes, 0);
            return true;    // результат завершения операции
        }

        public bool WriteValue(int value, int elementIndex)         // запись значения (по аналогии c ReadValue)
        {
            int pageIndex = FindPage(elementIndex) ?? -1;
            if (pageIndex == -1)
                return false;

            int onPageIndex = elementIndex % MemoryPage.byteNum;
            byte[] bytes = BitConverter.GetBytes(value);

            Buffer.BlockCopy(bytes, 0, pages[pageIndex].Data, onPageIndex * sizeof(int), sizeof(int));
            pages[pageIndex].Bitmap[onPageIndex] = 1;
            pages[pageIndex].IsModified = true;

            return true;
        }

        public void SavePage(int index)     // сохранение страницы в памяти
        {
            array.Seek(2 + MemoryPage.pageSize * pages[index].Index, SeekOrigin.Begin);
            array.Write(pages[index].Bitmap, 0, pages[index].Bitmap.Length);
            array.Write(pages[index].Data, 0, pages[index].Data.Length);
        }

        public int? this[int index] // перегрузка индексатора
        {
            get
            {
                if (ReadValue(out int value, index))
                    return value;  // если находит значение, то возвращает
                else
                    return null;
            }

            set
            {
                if (value != null)
                    WriteValue(value.Value, index);  // если значение задано, то записывает в буфер
                else
                    throw new ArgumentNullException("Значение не может быть null!", nameof(value));
            }
        }

        public void Dispose()   // освобождение ресурсов при окончании работы
        {
            array.Dispose();
            GC.SuppressFinalize(this);
        }

        ~MemoryModel()
        {
            array.Dispose();
        }

    }
}
