using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

static class MHBClient
{
    const string ServerAddress = "barragegame.ddns.net";
    private static byte[] smlBuf = new byte[4];  //a small buffer large enough to store size or command info

    private static MHSocket CreateSocket()
    {
        //get target ip address
        IPAddress ipAddress = Dns.GetHostEntry(ServerAddress).AddressList[0];
        IPEndPoint remoteEndPoint = new IPEndPoint(ipAddress, 10101);

        //create and connect client
        Socket socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        try
        {
            socket.Connect(remoteEndPoint);
        }
        catch (SocketException e)
        {
            MessageBox.Show("Failed to connect to server");
            return null;
        }
        Console.WriteLine("Connected to " + socket.RemoteEndPoint + " from " + socket.LocalEndPoint);

        //send protocool type
        smlBuf[0] = (byte)MHBPROTOCOOL.MHB40v1;
        socket.Send(smlBuf, 1, SocketFlags.None);
        //receive ok
        if (socket.Receive(smlBuf, 1, SocketFlags.None) != (byte)MHBINFO.OK)
            throw new Exception("Unexpected response from server");

        //create MHSocket
        MHFTPLib.MHLib mhl = new MHFTPLib.MHLib();

        //receive public key
        socket.Receive(smlBuf, 4, SocketFlags.None);  //key size
        byte[] buf = new byte[BitConverter.ToInt32(smlBuf, 0)];
        MHSocket.ReceiveRaw(socket, buf, buf.Length);  //key bytes
        mhl.SavePeerPublicKeyBytes(buf);

        //send public key
        socket.Send(BitConverter.GetBytes(mhl.m_MyPublicKeyBytes.Length));  //key size
        socket.Send(mhl.m_MyPublicKeyBytes);  //key bytes

        //create socket
        MHSocket mhs = new MHSocket(socket, mhl);

        //login
        mhs.SendString("barrage109239pw");
        //receive ok
        buf = mhs.Receive();
        if ((MHBINFO)buf[0] != MHBINFO.OK)
            throw new Exception("Failed to login");  //failed to login

        return mhs;
    }


    public static string ListScripts()
    {
        //connect
        MHSocket mhs = CreateSocket();
        if (mhs == null)
            return "";

        //send list dir command
        smlBuf[0] = (byte)MHBCOMMANDS.LISTDIR;
        mhs.Send(smlBuf, 1);

        //get string
        string str = mhs.ReceiveString();
        mhs.Close();

        //convert to string
        return str;
    }

    public static void Upload(string scriptFolder, string scriptName, string password)
    {
        //connect
        MHSocket mhs = CreateSocket();
        if (mhs == null)
            return;

        //send upload command
        smlBuf[0] = (byte)MHBCOMMANDS.UPLOADSCRIPT;
        mhs.Send(smlBuf, 1);
        //script name parameter
        mhs.SendString(scriptName);

        //wait for ok
        switch (mhs.Receive()[0])
        {
            case (byte)MHBINFO.OK:
                break;
            case (byte)MHBINFO.ALREADYEXISTS:
                mhs.Close();
                MessageBox.Show("Script already exists online. Please update it instead or use a different name.");
                return;
            default:
                throw new Exception("Unexpected response from server");
        }

        //password
        mhs.SendString(password);

        //start uploading files
        string[] filenames = Directory.GetFiles(Path.Combine(scriptFolder, scriptName));
        for (int i = 0; i < filenames.Length; i++)
        {
            if (!MHSocket.IsAllowedFile(filenames[i]))
                continue;

            //send NEXTFILE info
            smlBuf[0] = (byte)MHBINFO.NEXTFILE;
            mhs.Send(smlBuf, 1);

            //send filename
            mhs.SendString(Path.GetFileName(filenames[i]));

            //send file data
            FileStream fs = new FileStream(filenames[i], FileMode.Open);
            mhs.SendFile(fs);
            fs.Close();
        }
        //indicate end of folder
        smlBuf[0] = (byte)MHBINFO.DONE;
        mhs.Send(smlBuf, 1);

        mhs.Close();
    }

    public static void UploadWithPassword(string scriptFolder, string scriptName, string password)
    {
        //connect
        MHSocket mhs = CreateSocket();
        if (mhs == null)
            return;

        //send upload command
        smlBuf[0] = (byte)MHBCOMMANDS.UPLOADSCRIPTWPASSWORD;
        mhs.Send(smlBuf, 1);
        //script name parameter
        mhs.SendString(scriptName);
        //password parameter
        mhs.SendString(password);

        //wait for ok
        switch (mhs.Receive()[0])
        {
            case (byte)MHBINFO.OK:
                break;
            case (byte)MHBINFO.INCORRECTPASSWORD:
                MessageBox.Show("The password entered does not match the level password");
                mhs.Close();
                return;
            case (byte)MHBINFO.DOESNOTEXIST:
                MessageBox.Show("Targeted script does not exist. Upload the script first");
                mhs.Close();
                return;
            default:
                throw new Exception("Unexpected response from server");
        }

        //start uploading files
        string[] filenames = Directory.GetFiles(Path.Combine(scriptFolder, scriptName));
        for (int i = 0; i < filenames.Length; i++)
        {
            if (!MHSocket.IsAllowedFile(filenames[i]))
                continue;

            //send NEXTFILE info
            smlBuf[0] = (byte)MHBINFO.NEXTFILE;
            mhs.Send(smlBuf, 1);

            //send filename
            mhs.SendString(Path.GetFileName(filenames[i]));

            //send file data
            FileStream fs = new FileStream(filenames[i], FileMode.Open);
            mhs.SendFile(fs);
            fs.Close();
        }
        //indicate end of folder
        smlBuf[0] = (byte)MHBINFO.DONE;
        mhs.Send(smlBuf, 1);

        mhs.Close();
    }

    public static void Download(string scriptFolder, string scriptName)
    {
        //connect
        MHSocket mhs = CreateSocket();
        if (mhs == null)
            return;

        //send download command
        smlBuf[0] = (byte)MHBCOMMANDS.DOWNLOADSCRIPT;
        mhs.Send(smlBuf, 1);

        //send target script name
        mhs.SendString(scriptName);

        //wait for ok
        switch (mhs.Receive()[0])
        {
            case (byte)MHBINFO.OK:
                break;
            case (byte)MHBINFO.DOESNOTEXIST:
                MessageBox.Show("Targeted script does not exist");
                mhs.Close();
                return;
            default:
                throw new Exception("Unexpected response from server");
        }

        Directory.CreateDirectory(scriptName);

        //check if sent all files
        while (mhs.Receive()[0] == (byte)MHBINFO.NEXTFILE)
        {
            string filename = mhs.ReceiveString();

            FileStream fs = new FileStream(Path.Combine(scriptName, filename), FileMode.Create);
            mhs.ReceiveFile(fs);
            fs.Close();
        }

        mhs.Close();
    }

    public static void Delete(string scriptName, string password)
    {
        //connect
        MHSocket mhs = CreateSocket();
        if (mhs == null)
            return;

        //send download command
        smlBuf[0] = (byte)MHBCOMMANDS.DELETESCRIPTWPASSWORD;
        mhs.Send(smlBuf, 1);

        mhs.SendString(scriptName);
        mhs.SendString(password);
        switch (mhs.Receive()[0])
        {
            case (byte)MHBINFO.OK:
                break;
            case (byte)MHBINFO.DOESNOTEXIST:
                MessageBox.Show("Targeted script does not exist");
                break;
            case (byte)MHBINFO.INCORRECTPASSWORD:
                MessageBox.Show("The password entered does not match the level password");
                break;
            default:
                throw new Exception("Unexpected response from server");
        }
        mhs.Close();
    }
}
