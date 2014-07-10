using System;
using System.Collections.Generic;
using System.Text;

namespace WhitePrideWorldWide
{
    class AntigateCaptchaResolver : BaseHttp
    {
        private string domain;
        private string key;
        private string result;
        private string file_path;
        private string file_type;
        private string captchaid;
        public int Timeout = 100 ;
        public System.IO.Stream stream;

        public string Result
        {
            get
            {
                return result;
            }
        }

        public string CaptchaId
        {
            get
            {
                return captchaid;
            }
        }


        public AntigateCaptchaResolver (string domain , string key, string fpath) : base (0 , 0 )
        {
            this.domain = domain;
            this.key = key;
            this.file_path = fpath;
            file_type = "image/pjpeg";
            if (fpath.Contains("jpg")) file_type = "image/pjpeg";
            else if (fpath.Contains("gif")) file_type = "image/gif";
            else if (fpath.Contains("png")) file_type = "image/png";
        }

        protected override void Execute()
        {
            try
            {
                Ready = false;
                HttpClient client = new HttpClient();
                MultiPartFormDataStream stream = new MultiPartFormDataStream();
                this.stream.Seek(0, System.IO.SeekOrigin.Begin);
                stream.AddFormField("method", "post");
                stream.AddFormField("key", key);
                stream.AddFormField("file", file_path);
              //  stream.AddFile("file", file_path, file_type);
                stream.AddFileStream  (this.stream , "file", "img.jpg", file_type);
                stream.SetEndOfData();
                client.SetProperty("ContentType", "multipart/form-data; boundary=" + stream.Boundary);
                string tmpstr = client.Post("http://" + domain + "/in.php", stream);
                string captcha_id = null;
                if (tmpstr.Contains("ERROR_")) { result = tmpstr; return; }
                else if (tmpstr.Contains("OK|"))
                {
                    captcha_id = tmpstr.Replace("OK|","");
                }
                if (captcha_id == null || captcha_id == string.Empty)
                {
                    result = "ERROR: bad captcha id";
                    return;
                }
                for (int i = 0; i <= Timeout; i += 5)
                {
                    System.Threading.Thread.Sleep(5000);
                    client.ClearCookies();
                    captchaid = captcha_id;
                    tmpstr = client.Get("http://"+domain+"/res.php?key="+key+"&action=get&id="+captcha_id);
                      if (tmpstr.Contains("ERROR_")) { result = tmpstr; return; }
                     else if (tmpstr.Contains("OK|"))
                     {
                       result = tmpstr.Replace("OK|","").Trim();
                       return;
                     }
                }
                result = "ERROR_TIMEOUT";
            }
                
            finally
            {
                lock (this)
                {
                    Ready = true;
                }
            }
        }
    }
}
