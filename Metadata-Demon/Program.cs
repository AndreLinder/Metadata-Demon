using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Data.Common;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace MetadataDemon
{
    class Program
    {
        static int port = 8004; // порт для приема входящих запросов

        static void Main(string[] args)
        {
            // получаем адреса для запуска сокет

            IPEndPoint ipPoint = new IPEndPoint(IPAddress.Any, port);
            Socket listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            while (true)
            {
                try
                {
                    listenSocket.Bind(ipPoint);

                    // начинаем прослушивание
                    listenSocket.Listen(100);

                    Console.WriteLine("Metadata-Demon is starting...");

                    while (true)
                    {
                        //Сообщение для ответа клиенту
                        string message = "ERROR";

                        //Номер команды и её параметры
                        string numberCommand = "";
                        string parameters = "";

                        Socket handler = listenSocket.AcceptAsync().Result;

                        using (NetworkStream netstream = new NetworkStream(handler))
                        {
                            BinaryReader reader = new BinaryReader(netstream);

                            int fileNameLength = reader.ReadInt32();
                            byte[] fileNameBytes = reader.ReadBytes(fileNameLength);
                            string fileName = Encoding.UTF8.GetString(fileNameBytes);

                            FileStream fs = new FileStream("D:\\temp\\" + fileName, FileMode.Create);

                            int fileLength = reader.ReadInt32();
                            byte[] fileBytes = new byte[fileLength];
                            int bytesRead = 0;
                            Console.WriteLine(fileNameLength + "\n" + fileName + "\n" + fileLength + "\n");
                            while (bytesRead < fileLength)
                            {
                                Console.WriteLine(bytesRead);
                                bytesRead += netstream.Read(fileBytes, bytesRead, fileLength - bytesRead);
                            }
                            fs.Write(fileBytes);
                            fs.Close();
                        }
                        
                        byte[] data = Encoding.UTF8.GetBytes("READY");
                        if (handler.Available == 0)
                        {
                            Console.WriteLine("OK");
                            handler.Send(data);
                        }
                        //RecieveFile(handler);

                        //Цветовое офрмление серверной части, для наглядности обмена данными
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.Write(DateTime.Now.ToShortTimeString() + ": ");
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("<Response Server> : " + data);
                        
                        Console.WriteLine();

                        // закрываем сокет
                        handler.Shutdown(SocketShutdown.Both);
                        handler.Close();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message + "\n" + ex.ToString());
                    Console.ReadKey();
                }
            }
        }

        static async Task<string> RecieveFile(Socket socket, string filename)
        {
            NetworkStream ns = new NetworkStream(socket);
            FileStream fs = new FileStream("D:\\temp\\"+filename, FileMode.Create);
            ns.CopyTo(fs);

            Console.WriteLine("Save file: D:\\temp\\" + filename);

            ns.Close();
            fs.Close();

            return "FILERECIEVED";
        }
    }
}