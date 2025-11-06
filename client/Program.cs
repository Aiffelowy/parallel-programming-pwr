using System;
using System.Net.Sockets;
using System.IO;
using System.Drawing.Imaging;
using System.Drawing;

namespace ClientApp
{
    class Program
    {


        static async void Connect(String server, String message)
        {
            try
            {

                Int32 port = 42069;

                using TcpClient client = new TcpClient(server, port);

                // Translate the passed message into ASCII and store it as a Byte array.
                //Byte[] data = System.Text.Encoding.ASCII.GetBytes(message);

                // Get a client stream for reading and writing.
                NetworkStream stream = client.GetStream();

                // Send the message to the connected TcpServer.
                //stream.Write(data, 0, data.Length);

                //Console.WriteLine("Sent: {0}", message);

                while (true) {
                    //Buffer to store the response bytes.
                    byte[] len_buf = new Byte[4];


                    //// Read the first batch of the TcpServer response bytes.
                    stream.Read(len_buf);
                    uint len = BitConverter.ToUInt32(len_buf.Reverse().ToArray(), 0);
                    var buffer = new byte[len];

                    stream.Read(buffer);


                    using (Image image = Image.FromStream(new MemoryStream(buffer)))
                    {
                        for (int x = 0; x < image.Width; x++)
                        {
                            for (int y = 0; y < image.Height; y++)
                            {
                                Color pixelColor = ((Bitmap)image).GetPixel(x, y);
                                // Invert the color
                                Color invertedColor = Color.FromArgb(255 - pixelColor.R, 255 - pixelColor.G, 255 - pixelColor.B);
                                ((Bitmap)image).SetPixel(x, y, invertedColor);
                            }
                        }
                        Console.WriteLine("Processed an image.");
                        using (var ms = new MemoryStream())
                        {
                            image.Save(ms, ImageFormat.Png);
                            var awouhuiah = ms.ToArray();
                            stream.Write(BitConverter.GetBytes(buffer.Length));
                            stream.Write(awouhuiah);
                            Console.WriteLine("Sent processed image back to server.");
                        }


                    }

                }

                stream.Close();
                client.Close();
            }

            catch (ArgumentNullException e)
            {
                Console.WriteLine("ArgumentNullException: {0}", e);
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }

            Console.WriteLine("\n Press Enter to continue...");
            Console.Read();
        }

        static void Main(string[] args)
        {
            const string SERVER_IP = "127.0.0.1";
            Connect(SERVER_IP, "foo");
        }
    }
}