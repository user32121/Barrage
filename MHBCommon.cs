using MHFTPLib;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

enum MHBCOMMANDS : byte
{
    LISTDIR,  //list directory
    UPLOADSCRIPT,  //upload a script
    DOWNLOADSCRIPT,  //download a script
    UPLOADSCRIPTWPASSWORD,  //override an existing file (requires user password)
    DELETESCRIPTWPASSWORD,  //delete a script folder (requires user password)
}

enum MHBINFO : byte
{
    GENERICERROR,  //used when an unknown error occured
    OK,  //no error
    DONE,  //looping finished
    FAILEDAUTHENTICATION,  //failed to login to server
    UNKNOWNCOMMAND,  //an invalid command index was sent
    ALREADYEXISTS,  //script folder already exists
    INCORRECTPASSWORD,  //the password entered does not match the level password
    NEXTFILE,  //start streaming the next file
    CONTINUEFILE,  //wait for next packet of current file
    ENDFILE,  //file finished streaming
    DOESNOTEXIST,  //targeted script folder does not exist
    UNSUPPORTEDPROTOCOOL,  //protocool does not exist
}

enum MHBPROTOCOOL
{
    UNKONWN,
    MHB40v1 = 110,  //40 byte merkle hellman
}

class MHSocket
{
    private Socket socket;
    private MHFTPLib.MHLib encrypter;

    private static byte[] smlBuf = new byte[4];  //a small buffer for recieving data like size
    private static byte[] lgBuf = new byte[1024];  //a large buffer for sending lots of data (1 kB packet)

    public EndPoint RemoteEndPoint { get { return socket.RemoteEndPoint; } }

    public MHSocket(Socket socket, MHLib encrypter)
    {
        this.socket = socket;
        this.encrypter = encrypter;
    }

    public void Send(byte[] buffer, int size)
    {
        byte[] encrypted = encrypter.EncryptBytes(buffer, size);
        socket.Send(BitConverter.GetBytes(encrypted.Length));  //size header
        socket.Send(encrypted, encrypted.Length, SocketFlags.None);  //data
    }
    public void SendString(string text)
    {
        byte[] encrypted = encrypter.EncryptString(text);
        socket.Send(BitConverter.GetBytes(encrypted.Length));  //size header
        socket.Send(encrypted, encrypted.Length, SocketFlags.None);  //data
    }
    public void SendFile(FileStream fs)  //file stream to read from
    {
        while (fs.Position < fs.Length)
        {
            //info header
            smlBuf[0] = (byte)MHBINFO.CONTINUEFILE;
            Send(smlBuf, 1);
            
            int bytesRead = fs.Read(lgBuf, 4, 1020);  //read from stream
            byte[] ar = BitConverter.GetBytes(bytesRead);  //data header
            for (int i = 0; i < 4; i++)
                lgBuf[i] = ar[i];
            bytesRead += 4;
            Send(lgBuf, bytesRead);
        }
        //end file streaming
        smlBuf[0] = (byte)MHBINFO.ENDFILE;
        Send(smlBuf, 1);
    }

    public byte[] Receive()
    {
        socket.Receive(smlBuf, 4, SocketFlags.None);  //size header
        byte[] buf = new byte[BitConverter.ToInt32(smlBuf, 0)];
        ReceiveRaw(socket, buf, buf.Length);  //data
        return encrypter.DecryptBytes(buf, buf.Length);
    }
    public string ReceiveString()
    {
        socket.Receive(smlBuf, 4, SocketFlags.None);  //size header
        byte[] buf = new byte[BitConverter.ToInt32(smlBuf, 0)];
        ReceiveRaw(socket, buf, buf.Length);  //data
        return encrypter.DecryptString(buf, buf.Length);
    }
    public void ReceiveFile(FileStream fileStream)  //file stream to write to
    {
        while (true)
        {
            switch (Receive()[0])
            {
                case (byte)MHBINFO.CONTINUEFILE:  //check if file has ended
                    break;
                case (byte)MHBINFO.ENDFILE:
                    return;
                default:
                    Console.WriteLine("Unexpected response from server");
                    return;
            }

            byte[] bytes = Receive();
            int bytesReceived = BitConverter.ToInt32(bytes, 0);
            fileStream.Write(bytes, 4, bytesReceived);
        }
    }

    public void Close()
    {
        socket.Close();
    }

    public static void ReceiveRaw(Socket socket, byte[] buf, int bytesToRead)
    {
        int curPos = 0;
        while (bytesToRead > 0)
        {
            int bytesRead = socket.Receive(buf, curPos, bytesToRead, SocketFlags.None);
            bytesToRead -= bytesRead;
            curPos += bytesRead;
        }
    }
}
