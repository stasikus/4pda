using System;
using System.Collections.Generic;
using System.Text;

namespace WhitePrideWorldWide 
{
    class MultiPartFormDataStream : System.IO.MemoryStream
    {
        private string boundary;

        public string Boundary
        {
            get
            {
                return boundary;
            }
        }


        public MultiPartFormDataStream()
        {
            boundary = "--------" + 
                        DateTime.Now.Ticks.ToString();
        }

        public void AddFormField(string key, string value)
        {
            string str = "--"+ boundary + "\r\n" +
                        "Content-Disposition: form-data; " +
                         "name=\"" + key + "\"\r\n\r\n" + value + "\r\n";
            byte[] bytes = Encoding.UTF8.GetBytes(str);
            this.Write(bytes, 0 , bytes.Length);
        }

        public void AddFile(string field_name , string path , string type)
        {
            if (!System.IO.File.Exists(path)) throw new System.IO.FileNotFoundException();

            string str = "--" + boundary + "\r\n" +
                       "Content-Disposition: form-data; " +
                        "name=\"" + field_name + "\"; filename=\"" +
                        System.IO.Path.GetFileName(path) + "\"\r\n" +
                        "Content-Type: " + type + "\r\n\r\n";
            byte[] bytes = Encoding.UTF8.GetBytes(str);
            this.Write(bytes, 0, bytes.Length);
            bytes = System.IO.File.ReadAllBytes(path);
            this.Write(bytes, 0, bytes.Length);
            str = "\r\n";
            bytes = Encoding.UTF8.GetBytes(str);
            this.Write(bytes, 0, bytes.Length);
        }

        public void AddFileStream (System.IO.Stream fileStream , string field_name, string name, string type)
        {
            string str = "--" + boundary + "\r\n" +
                       "Content-Disposition: form-data; " +
                        "name=\"" + field_name + "\"; filename=\"" +
                        name + "\"\r\n" +
                        "Content-Type: " + type + "\r\n\r\n";
            byte[] bytes = Encoding.UTF8.GetBytes(str);
            this.Write(bytes, 0, bytes.Length);
            List<byte> lb = new List<byte>();
            int b;
            while ((b = fileStream.ReadByte()) != -1)
            {
                lb.Add((byte)b);
            }
            byte[] fileBytes = lb.ToArray();
            this.Write(fileBytes, 0, fileBytes.Length);
            str = "\r\n";
            bytes = Encoding.UTF8.GetBytes(str);
            this.Write(bytes, 0, bytes.Length);
        }


        public void SetEndOfData()
        {
           string str = "--" + boundary + "--\r\n\r\n";
           byte[] bytes = Encoding.UTF8.GetBytes(str);
           this.Write(bytes, 0, bytes.Length);
        }

        public override string ToString()
        {
            string res = string.Empty;
            this.Seek(0 , System.IO.SeekOrigin.Begin);
            byte[] bytes = new byte[1024];
            int read = this.Read(bytes , 0 , bytes.Length);
            while (read > 0)
            {
                res += Encoding.UTF8.GetString(bytes , 0 , read);
                read = this.Read(bytes, 0, bytes.Length);
            }
            return res;
            
        }

    }
}
