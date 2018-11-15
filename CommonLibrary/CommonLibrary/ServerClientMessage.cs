using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibrary
{
    public enum MessageType { DownloadAndExe, DownloadAndExeRes, AskForFile}

    [Serializable]
    public class ServerClientMessage
    {
        private MessageType _messageType;
        private byte[] _data;
        private int _size;

        public MessageType MyMessageType
        {
            get
            {
                return this._messageType;
            }
            private set
            {
                this._messageType = value;
            }
        }

        public byte[] MyData
        {
            get
            {
                return this._data;
            }
            set
            {
                this._data = value;
            }
        }

        public int Size
        {
            get
            {
                return this._size;
            }
            private set
            {
                this._size = value;
            }
        }

        public ServerClientMessage()
        {
        }

        public ServerClientMessage(MessageType type, int size)
        {
            this._messageType = type;
            this.Size = size;
        }

        public ServerClientMessage(MessageType type, int size, byte[] data)
        {
            this.MyMessageType = type;
            this.Size = size;
            this.MyData = data;
        }

        public void DeSerialize(byte[] data)
        {
            this.MyMessageType = (MessageType)BitConverter.ToInt32(data, 0);
            this.Size = BitConverter.ToInt32(data, 4);
            this.MyData = data.Skip(8).ToArray();
        }

        public byte[] serialize()
        {
            List<byte> byteList = new List<byte>();
            byteList.AddRange(BitConverter.GetBytes(Convert.ToInt32(this.MyMessageType)));
            byteList.AddRange(BitConverter.GetBytes(this.Size));
            byteList.AddRange(this.MyData);

            return byteList.ToArray();
        }
        
    }
}
