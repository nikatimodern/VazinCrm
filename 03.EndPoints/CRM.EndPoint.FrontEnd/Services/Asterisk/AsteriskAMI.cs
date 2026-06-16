using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CRM.EndPoint.FrontEnd.Services.Asterisk;

public class AsteriskAMI {
    private TcpClient _client;
    private NetworkStream _stream;

    private readonly string _hostname;
    private readonly int _port;
    private readonly string _username;
    private readonly string _password;

    public AsteriskAMI(string hostname, int port, string username, string password) {
        _hostname = hostname;
        _port = port;
        _username = username;
        _password = password;
    }

    public async Task ConnectAsync() {
        _client = new TcpClient();
        await _client.ConnectAsync(_hostname, _port);
        _stream = _client.GetStream();

        // AMI banner
        await ReadLineAsync();

        // login
        var loginData = $"Action: Login\r\nUsername: {_username}\r\nSecret: {_password}\r\n\r\n";
        await SendAsync(loginData);
        await ReadLineAsync(); // login response
    }

    public void Close() {
        _stream?.Close();
        _client?.Close();
    }

    private async Task SendAsync(string data) {
        if (_stream == null) return;
        var bytes = Encoding.ASCII.GetBytes(data);
        await _stream.WriteAsync(bytes, 0, bytes.Length);
    }

    private async Task<string> ReadLineAsync() {
        if (_stream == null) return null;
        byte[] buffer = new byte[1000000];
        int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
        return Encoding.ASCII.GetString(buffer, 0, bytesRead);
    }

    public async Task<string> ReadRawAsync() {
        if (_stream == null) return null;
        byte[] buffer = new byte[1000000];
        int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
        if (bytesRead == 0) throw new IOException("Disconnected from AMI");
        return Encoding.ASCII.GetString(buffer, 0, bytesRead);
    }
}