using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MHBServer
{
    class Program
    {
        private static ManualResetEvent doneAccepting = new ManualResetEvent(false);

        private static byte[] smlBuf = new byte[4];  //a small buffer for recieving data like size

        static void Main(string[] args)
        {
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, 10101);

            Socket server = new Socket(SocketType.Stream, ProtocolType.Tcp);
            server.ReceiveTimeout = 10000;
            server.SendTimeout = 10000;
            server.Bind(localEndPoint);
            server.Listen(100);
            Console.WriteLine("localEP: " + server.LocalEndPoint);
            Console.WriteLine("listening for connections...");

            while (true)
            {
                doneAccepting.Reset();

                //start accepting connections
                server.BeginAccept(AcceptConnectionCallback, server);

                doneAccepting.WaitOne();
            }
        }

        private static void AcceptConnectionCallback(IAsyncResult ar)
        {
            doneAccepting.Set();  //allow accepting more connections asynchronously

            try
            {
                Socket handler = ((Socket)ar.AsyncState).EndAccept(ar);

                Console.WriteLine(handler.RemoteEndPoint + ": connected");

                //receive protocool type
                handler.Receive(smlBuf, 1, SocketFlags.None);
                MHBPROTOCOOL protcool = (MHBPROTOCOOL)smlBuf[0];
                Console.WriteLine(handler.RemoteEndPoint + ": using " + protcool.ToString() + " protocool");
                if (protcool == MHBPROTOCOOL.MHB40v1)
                {
                    //send ok
                    smlBuf[0] = (byte)MHBINFO.OK;
                    handler.Send(smlBuf, 1, SocketFlags.None);
                }
                else
                {
                    //send not ok
                    Console.WriteLine(handler.RemoteEndPoint + ": invalid protocool; close");
                    smlBuf[0] = (byte)MHBINFO.UNSUPPORTEDPROTOCOOL;
                    handler.Send(smlBuf, 1, SocketFlags.None);
                    handler.Close();
                    return;
                }

                MHFTPLib.MHLib mhl = new MHFTPLib.MHLib();
                //send public key
                handler.Send(BitConverter.GetBytes(mhl.m_MyPublicKeyBytes.Length));
                handler.Send(mhl.m_MyPublicKeyBytes);
                //receive public key
                handler.Receive(smlBuf, 4, SocketFlags.None);
                byte[] mhkey = new byte[BitConverter.ToInt32(smlBuf, 0)];
                handler.Receive(mhkey, mhkey.Length, SocketFlags.None);
                mhl.SavePeerPublicKeyBytes(mhkey);  //note, this will override other public keys if multiple connections are active

                //check login
                MHSocket mhHandler = new MHSocket(handler, mhl);
                if (mhHandler.ReceiveString() == "barrage109239pw")
                    mhHandler.Send(new byte[1] { (byte)MHBINFO.OK }, 1);
                else
                {
                    Console.WriteLine(handler.RemoteEndPoint + ": failed authentication; close");
                    mhHandler.Send(new byte[1] { (byte)MHBINFO.FAILEDAUTHENTICATION }, 1);
                    mhHandler.Close();
                    return;
                }

                //receive command
                MHBCOMMANDS cmd = (MHBCOMMANDS)mhHandler.Receive()[0];
                Console.WriteLine(handler.RemoteEndPoint + ": sent " + cmd.ToString() + " command");

                switch (cmd)
                {
                    case MHBCOMMANDS.LISTDIR:
                        ListDir(mhHandler);
                        break;
                    case MHBCOMMANDS.UPLOADSCRIPT:
                        Upload(mhHandler);
                        break;
                    case MHBCOMMANDS.DOWNLOADSCRIPT:
                        Download(mhHandler);
                        break;
                    case MHBCOMMANDS.UPLOADSCRIPTWPASSWORD:
                        UploadWithPassword(mhHandler);
                        break;
                    case MHBCOMMANDS.DELETESCRIPTWPASSWORD:
                        DeleteWithPassword(mhHandler);
                        break;
                    default:
                        mhHandler.Send(new byte[1] { (byte)MHBINFO.UNKNOWNCOMMAND }, 1);
                        break;
                }
                Console.WriteLine(mhHandler.RemoteEndPoint + ": close\n");
                mhHandler.Close();
            }
            catch (SocketException e)
            {
                Console.WriteLine("Socket failed: {0}: {1}", e.ErrorCode.ToString(), e.Message);
            }
        }

        private static void ListDir(MHSocket socket)
        {
            socket.SendString(string.Join("\n", Directory.GetDirectories(".")));
        }

        private static void Upload(MHSocket socket)
        {
            string scriptName = socket.ReceiveString();
            Console.WriteLine(socket.RemoteEndPoint + ": start uploading " + scriptName + " from client");
            if (Directory.Exists(scriptName))
            {
                Console.WriteLine(socket.RemoteEndPoint + ": " + scriptName + " already exists");
                smlBuf[0] = (byte)MHBINFO.ALREADYEXISTS;
                socket.Send(smlBuf, 1);
                return;
            }
            else
            {
                Directory.CreateDirectory(scriptName);

                smlBuf[0] = (byte)MHBINFO.OK;
                socket.Send(smlBuf, 1);
            }

            //receive password and store in meta file
            string password = socket.ReceiveString();
            HashAlgorithm hasher = SHA256.Create();
            File.WriteAllBytes(Path.Combine(scriptName, ".meta"), hasher.ComputeHash(Encoding.ASCII.GetBytes(password)));
            Console.WriteLine(socket.RemoteEndPoint + ": created hashed password for " + scriptName);

            //check if sent all files
            while (socket.Receive()[0] == (byte)MHBINFO.NEXTFILE)
            {
                string filename = socket.ReceiveString();
                Console.WriteLine(socket.RemoteEndPoint + ": uploading " + Path.Combine(scriptName, filename));

                FileStream fs = new FileStream(Path.Combine(scriptName, filename), FileMode.Create);
                socket.ReceiveFile(fs);
                fs.Close();
            }
        }

        private static void UploadWithPassword(MHSocket socket)
        {
            string scriptName = socket.ReceiveString();
            Console.WriteLine(socket.RemoteEndPoint + ": start uploading " + scriptName + " from client");

            //receive password and compare with meta file
            string password = socket.ReceiveString();

            //check if exists
            if (!Directory.Exists(scriptName))
            {
                Console.WriteLine(socket.RemoteEndPoint + ": " + scriptName + " does not exist");
                smlBuf[0] = (byte)MHBINFO.DOESNOTEXIST;
                socket.Send(smlBuf, 1);
                return;
            }

            HashAlgorithm hasher = SHA256.Create();
            byte[] hash = hasher.ComputeHash(Encoding.ASCII.GetBytes(password));
            byte[] fileHash = File.ReadAllBytes(Path.Combine(scriptName, ".meta"));
            bool match = hash.Length == fileHash.Length;
            for (int i = 0; i < hash.Length; i++)
                if (hash[i] != fileHash[i])
                    match = false;
            if (!match)
            {
                Console.WriteLine(socket.RemoteEndPoint + ": passwords don't match");
                smlBuf[0] = (byte)MHBINFO.INCORRECTPASSWORD;
                socket.Send(smlBuf, 1);
                return;
            }
            else
            {
                smlBuf[0] = (byte)MHBINFO.OK;
                socket.Send(smlBuf, 1);
            }

            //check if sent all files
            while (socket.Receive()[0] == (byte)MHBINFO.NEXTFILE)
            {
                string filename = socket.ReceiveString();
                Console.WriteLine(socket.RemoteEndPoint + ": uploading " + Path.Combine(scriptName, filename));

                FileStream fs = new FileStream(Path.Combine(scriptName, filename), FileMode.Create);
                socket.ReceiveFile(fs);
                fs.Close();
            }
        }

        private static void Download(MHSocket socket)
        {
            string scriptName = socket.ReceiveString();

            Console.WriteLine(socket.RemoteEndPoint + ": start downloading " + scriptName + " to client");
            //check ok
            if (!Directory.Exists(scriptName))
            {
                Console.WriteLine(socket.RemoteEndPoint + ": " + scriptName + " does not exist");
                smlBuf[0] = (byte)MHBINFO.DOESNOTEXIST;
                socket.Send(smlBuf, 1);
                return;
            }
            else
            {
                smlBuf[0] = (byte)MHBINFO.OK;
                socket.Send(smlBuf, 1);
            }

            //start uploading files
            string[] filenames = Directory.GetFiles(scriptName);
            for (int i = 0; i < filenames.Length; i++)
            {
                if (!MHSocket.IsAllowedFile(filenames[i]))
                    continue;

                Console.WriteLine(socket.RemoteEndPoint + ": sending " + filenames[i] + " to client");

                //send NEXTFILE info
                smlBuf[0] = (byte)MHBINFO.NEXTFILE;
                socket.Send(smlBuf, 1);

                //send filename
                socket.SendString(Path.GetFileName(filenames[i]));

                //send file data
                FileStream fs = new FileStream(filenames[i], FileMode.Open);
                socket.SendFile(fs);
                fs.Close();
            }
            //indicate end of folder
            smlBuf[0] = (byte)MHBINFO.DONE;
            socket.Send(smlBuf, 1);
        }

        private static void DeleteWithPassword(MHSocket socket)
        {
            string scriptName = socket.ReceiveString();
            string password = socket.ReceiveString();

            if (!Directory.Exists(scriptName))
            {
                Console.WriteLine(socket.RemoteEndPoint + ": " + scriptName + " does not exist");
                smlBuf[0] = (byte)MHBINFO.DOESNOTEXIST;
                socket.Send(smlBuf, 1);
                return;
            }
            else
            {
                //compare password with meta file
                HashAlgorithm hasher = SHA256.Create();
                byte[] hash = hasher.ComputeHash(Encoding.ASCII.GetBytes(password));
                byte[] fileHash = File.ReadAllBytes(Path.Combine(scriptName, ".meta"));
                bool match = hash.Length == fileHash.Length;
                for (int i = 0; i < hash.Length; i++)
                    if (hash[i] != fileHash[i])
                        match = false;
                if (!match)
                {
                    Console.WriteLine(socket.RemoteEndPoint + ": passwords don't match");
                    smlBuf[0] = (byte)MHBINFO.INCORRECTPASSWORD;
                    socket.Send(smlBuf, 1);
                    return;
                }
            }

            //delete directory
            Directory.Delete(scriptName, true);

            smlBuf[0] = (byte)MHBINFO.OK;
            socket.Send(smlBuf, 1);
        }
    }
}
