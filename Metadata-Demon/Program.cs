using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Data.Common;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using MySql.Data.MySqlClient;
using Demon;
using System.Reflection.PortableExecutable;

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
                        Socket handler = listenSocket.AcceptAsync().Result;

                        NetworkStream netstream = new NetworkStream(handler);
                        
                            BinaryReader reader = new BinaryReader(netstream);

                        reader.ReadBytes(1);
                        int numberCommand = int.Parse(Encoding.UTF8.GetString(reader.ReadBytes(1)));
                        Console.WriteLine(numberCommand);
                        Console.ReadKey();
                        if (numberCommand == 1)
                            {
                                int fileNameLength = reader.ReadInt32();
                                byte[] fileNameBytes = reader.ReadBytes(fileNameLength);
                                string fileName = Encoding.UTF8.GetString(fileNameBytes);



                                int fileLength = reader.ReadInt32();
                                byte[] fileBytes = new byte[fileLength];
                                int bytesRead = 0;
                                Console.WriteLine(fileNameLength + "\n" + fileName + "\n" + fileLength + "\n");
                                
                                Task.Run(()=>RecieveFile(netstream, fileName, fileLength));
                                
                            }
                            else if(numberCommand == 2)
                            {

                            }

                        //Цветовое офрмление серверной части, для наглядности обмена данными
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.Write(DateTime.Now.ToShortTimeString() + ": ");
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("<Response Server> : ");
                        Console.WriteLine();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message + "\n" + ex.ToString());
                    Console.ReadKey();
                }
            }
        }

        static async Task<string> RecieveFile(NetworkStream netstream, string filename, int fileLength)
        {
            int bytesRead = 0;
            byte[] fileBytes = new byte[fileLength];

            string messages = "ERROR";

            try
            {
                //Считываем байты файла из потока
                while (bytesRead < fileLength)
                {
                    Console.WriteLine(bytesRead);
                    bytesRead += netstream.Read(fileBytes, bytesRead, fileLength - bytesRead);
                }
                BinaryReader reader = new BinaryReader(netstream);

                int id_messages = reader.ReadInt32();
                
                if (bytesRead == fileLength)
                {
                    MySqlConnection connection = DBUtils.GetDBConnection();
                    connection.Open();

                    string sql_command = "INSERT INTO server_chats.files (filename, filedata, id_message) VALUES (@filename, @filedata, @id_messages)";

                    MySqlCommand command = connection.CreateCommand();
                    command.CommandText = sql_command;

                    command.Parameters.AddWithValue("@filename", filename);
                    command.Parameters.AddWithValue("@filedata", fileBytes);
                    command.Parameters.AddWithValue("@id_messages", id_messages);

                    command.ExecuteNonQuery();
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return "ERROR";
            }
            finally
            {
                byte[] data = Encoding.UTF8.GetBytes("READY");

                netstream.Socket.Send(data);
                netstream.Close();
                messages = "FILERECIEVED";
            }
            return messages;
        }
    }
}