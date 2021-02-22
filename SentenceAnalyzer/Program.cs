using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SentenceAnalyzer
{
    class Program
    {
        string _filePath = "";
        int _threadCount = 5;
        string[] _sentences;
        List<wordFrequence> _wordList = new List<wordFrequence>();

        static void Main(string[] args)
        {
            Program program = new Program();

            Console.Write("Lütfen dosya yolunu 'C:\\fullPath\\files.txt' formatında giriniz : ");
            program._filePath = Console.ReadLine();

            Console.Write("Lütfen 'Thread' sayısını giriniz (Default: 5) : ");
            string thread = Console.ReadLine();
            program._threadCount = Convert.ToInt32(String.IsNullOrWhiteSpace(thread) ? "5" : thread);

            Thread mainThread = new Thread(new ThreadStart(program.mainWork));
            mainThread.Start();
        }


        /// <summary>
        /// İşlem ThreadPool yapısı ile daha kısa bir şekilde çözülürdü ama thread yönetimini
        /// bizim yapmamız istendiği için thread'ler ve queue'lar ayrı ayrı oluşturulup işlem yapıldı.
        /// </summary>        
        // Ana thread'in çalıştırdığı iş
        private void mainWork()
        {
            int wordCounts = 0;
            try
            {
                string paragraph = System.IO.File.ReadAllText(@_filePath);
                _sentences = getSentences(paragraph);
                Console.WriteLine("Sentence Count : " + _sentences.Length);
                int queueIndex = 0;
                List<Queue<string>> queueList = new List<Queue<string>>(_threadCount);
                List<Thread> threadList = new List<Thread>(_threadCount);

                // Thread'lerin işleyeceği queue'lar hazırlanıyor
                int loopCount = _sentences.Length > _threadCount ? _sentences.Length : _threadCount;
                for (int i = 0; i < loopCount; i++)
                {
                    if (i >= _sentences.Length)
                    {
                        queueList.Add(new Queue<string>());
                    }
                    else
                    {
                        queueIndex = i % _threadCount;
                        try
                        {
                            queueList[queueIndex].Enqueue(_sentences[i]);
                        }
                        catch (Exception)
                        {
                            queueList.Add(new Queue<string>());
                            queueList[queueIndex].Enqueue(_sentences[i]);
                        }

                        wordCounts += getWords(_sentences[i]).Length;
                    }
                }
                Console.WriteLine("Avg. Word Count : " + Convert.ToInt32(wordCounts / _sentences.Length));
                Console.WriteLine("Thread counts:");

                for (int i = 0; i < _threadCount; i++)
                {
                    queueList.Add(new Queue<string>());
                    Thread subThread = new Thread(() => this.subWork(queueList[i]));
                    threadList.Add(subThread);
                    Console.WriteLine("     ThreadId = " + subThread.ManagedThreadId + ", Count = " + queueList[i].Count);
                    subThread.Start();
                }

                // Çalışan tüm thread' lerin tamamlanması bekleniyor
                foreach (Thread thread in threadList)
                {
                    thread.Join();
                }

                sortList();

                foreach (var wordFrequence in _wordList)
                {
                    Console.WriteLine(wordFrequence.word + " : " + wordFrequence.count);
                }

                Console.WriteLine("Kapatmak için bir tuşa basınız.");
                Console.ReadLine();

            }
            catch (Exception ex)
            {
                Console.WriteLine("Beklenmeyen hata oluştu: " + ex.ToString());
                Console.ReadLine();
            }
        }

        // Alt thread'lerin çalıştırdığı iş
        private void subWork (Queue<string> queue)
        {
            while (queue.Count > 0)
            {
                string[] words = getWords(queue.Dequeue());
                for (int j = 0; j < words.Length; j++)
                {
                    int index;
                    try
                    {
                        index = _wordList.FindIndex(w => w.word == words[j]);
                    }
                    catch (Exception)
                    {
                        index = -1;
                    }

                    if (index == -1)
                    {
                        _wordList.Add(new wordFrequence() { word = words[j], count = 1 });
                    }
                    else
                    {
                        _wordList[index].count++;
                    }
                }
            }
        }

        // Harf Frekansına göre büyükten küçüğe Selection Sort algoritması ile sıralama
        private void sortList()
        {
            int n = _wordList.Count;

            for (int i = 0; i < n - 1; i++) 
            {
                int minId = i;

                for (int j = i + 1; j < n; j++)
                    if (_wordList[j].count > _wordList[minId].count)
                        minId = j; 

                wordFrequence temp = _wordList[minId]; 
                _wordList[minId] = _wordList[i]; 
                _wordList[i] = temp; 
            }
        }

        private string[] getSentences(string paragraph)
        {
            // Cümle sonuna gelebilecek noktalama işaretleri => . ? ! :
            paragraph = paragraph.Replace('.', '#').Replace('?', '#').Replace('!', '#').Replace(':', '#');
            if (paragraph.Length > 0)
            {
                if (paragraph[0] == '#')
                    paragraph = paragraph.Substring(1, paragraph.Length - 1);

                if (paragraph[paragraph.Length - 1] == '#')
                    paragraph = paragraph.Substring(0, paragraph.Length - 1);
            }
            string[] sentencesArray = paragraph.Split('#');

            return sentencesArray;
        }

        private string[] getWords(string sentence)
        {
            sentence = sentence.Replace(' ', '~').Replace("\r\n", "~");
            if (sentence.Length > 0)
            {
                if (sentence[0] == '~')
                    sentence = sentence.Substring(1, sentence.Length - 1);

                if (sentence[sentence.Length - 1] == '~')
                    sentence = sentence.Substring(0, sentence.Length - 1);
            }
            string[] wordsArray = sentence.Split('~');

            return wordsArray;
        }
    }

    class wordFrequence
    {
        public string word { get; set; }
        public int count { get; set; }
    }
}
