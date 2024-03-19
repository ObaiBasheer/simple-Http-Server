using System.Net;
using System.Net.Sockets;
using System.Text;

// You can use print statements as follows for debugging, they'll be visible when running tests.
Console.WriteLine("Logs from your program will appear here!");
string[] arguments = Environment.GetCommandLineArgs();
string directoryPath = "";
if (arguments.Length > 1)
{
    directoryPath = arguments[2];

}

TcpListener server = new TcpListener(IPAddress.Any, 4221);
// Start listening for client requests.
server.Start();

while (true)
{
    // Accept client requests .
    //var socket = server.AcceptSocket(); // wait for client . Accepts a pending connection request.
    TcpClient tcpClient = await server.AcceptTcpClientAsync(); // wait for client . Accepts a pending connection request.

    //Accept more the one request as the time
    Thread thread = new Thread(() => HandleClient(tcpClient));
    thread.Start();
    Console.WriteLine("Connected!");
}

void HandleClient(TcpClient tcpClient)
{
    try
    {
        var msg = new byte[256];
        NetworkStream stream = tcpClient.GetStream();
        //var requestData = Encoding.UTF8.GetString(msg, 0, socket.Receive(msg, 0, msg.Length, SocketFlags.None));

        var request = stream.Read(msg);
        var requestData = Encoding.UTF8.GetString(msg, 0, request);
        string[] requestLines = requestData.Split(new string[] { "\r\n" }, StringSplitOptions.None);

        string[] requestParts = requestLines[0].Split(' ');
        string method = requestParts[0];
        string path = requestParts[1];

        string response;
        string data;
        if (path == "/")
        {
            response = $"HTTP/1.1 200 OK\r\n\r\n";
        }
        else if (path.StartsWith("/echo/"))
        {
            int startIndex = path.IndexOf("/echo/") + "/echo/".Length;
            var text = path[startIndex..];
            data = text;
            response =
           "HTTP/1.1 200 OK\r\n" +
           "Content-Type: text/plain\r\n" +
           $"Content-Length: {data.Length}\r\n" +
           "\r\n" +
           data;
        }
        else if (path.StartsWith("/user-agent"))
        {
            string header = "";
            for (int i = 1; i < requestLines.Length; i++)
            {
                if (requestLines[i].ToLower().Contains("user-agent"))
                {
                    header = requestLines[i].Split(":")[1].Trim();
                    break;
                }
            }
            response =
          "HTTP/1.1 200 OK\r\n" +
          "Content-Type: text/plain\r\n" +
          $"Content-Length: {header.Length}\r\n" +
          "\r\n" +
          header;
        }
        else if (path.StartsWith("/files") && method == "GET")
        {
            int startIndex = path.IndexOf("/files/") + "/files/".Length;
            var fileName = path[startIndex..];
            string filePath = Path.Combine(directoryPath, fileName);
            if (File.Exists(filePath))
            {
                string fileContent = File.ReadAllText(directoryPath + "/" + fileName);
                response =
                 "HTTP/1.1 200 OK\r\n" +
                 "Content-Type: application/octet-stream\r\n" +
                 $"Content-Length: {fileContent.Length}\r\n" +
                 "\r\n" +
                 fileContent;
            }
            else
            {
                response = "HTTP/1.1 404 Not Found\r\n\r\n";
            }
        }
        else if (path.StartsWith("/files") && method == "POST")
        {
            int startIndex = path.IndexOf("/files/") + "/files/".Length;
            var fileName = path[startIndex..];
            string filePath = Path.Combine(directoryPath, fileName);

            string fileContent = requestLines[requestLines.Length - 1];
            File.WriteAllText(filePath, fileContent);
            response = "HTTP/1.1 201 \r\n\r\n";

        }
        else
        {
            response = "HTTP/1.1 404 Not Found\r\n\r\n";
        }
        byte[] responseBytes = Encoding.ASCII.GetBytes(response);

        stream.WriteAsync(responseBytes);



    }
    catch (Exception ex)
    {
        // Log or handle your exception here.
    }
    finally
    {
        // Clean up resources, close streams and client connection.
        tcpClient.Close();
    }
}



