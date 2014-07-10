using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Security;


namespace WhitePrideWorldWide
{
    class HttpClient : ICloneable
    {
        private Dictionary<string, string> RequestProperies = new Dictionary<string, string>(); // свойства 
        public WebHeaderCollection ResponseHeaders = new WebHeaderCollection();
        public WebHeaderCollection RequestHeaders = new WebHeaderCollection();
        public Dictionary<string, string> SetCookies = new Dictionary<string, string>();

        public string sProxy ;  // прокси
        public bool UseProxy; // использовать прокси
        public bool AllowControlRedirect = true;
        public bool AllowAutoRedirect = false;
        public bool UseCookies; // использовать куки
        public Encoding encoding = Encoding.UTF8 ; // кодировка
        private HttpStatusCode responseCode ;
        private string responseUrl ;
        public int DownloadStep = 1024;
       
        
        
        
        
        public static bool IsValidProxy(string proxy)
        {
            HttpClient httpClient = new HttpClient();
            httpClient.sProxy = proxy;
            httpClient.UseProxy = true;
            httpClient.SetProperty("TimeOut", "5000");
            string sHtml = null;
            try
            {
                sHtml = httpClient.Get("http://vkontakte.ru/");
            }
            catch
            {
            }
            return (!String.IsNullOrEmpty(sHtml) && sHtml.Contains("name=\"email\""));
        }
        
        
         public static bool IsValidProxy(string proxy, int timeout )
        {
            HttpClient httpClient = new HttpClient();
            httpClient.sProxy = proxy;
            httpClient.UseProxy = true;
            httpClient.SetProperty("TimeOut", timeout.ToString() );
            string sHtml = null;
            try
            {
                sHtml = httpClient.Get("http://vkontakte.ru/");
            }
            catch
            {
            }
            return (!String.IsNullOrEmpty(sHtml) && sHtml.Contains("name=\"email\""));
        }
        
        
        
         public object Clone()
        {
            HttpClient httpClient = (HttpClient) this.MemberwiseClone();
            httpClient.cookies = new CookieCollection();
            httpClient.cookies.Add(this.cookies);
            httpClient.RequestProperies = new Dictionary<string, string>();
            httpClient.RequestHeaders = new WebHeaderCollection();
            httpClient.RequestHeaders.Add(this.RequestHeaders);
            httpClient.UseCookies = this.UseCookies;
            foreach (KeyValuePair<string, string> pair in RequestProperies)
            {
                httpClient.RequestProperies.Add(pair.Key , pair.Value);
            }
              foreach (KeyValuePair<string, string> pair in SetCookies)
            {
                httpClient.SetCookies.Add(pair.Key , pair.Value);
            }
            httpClient.encoding = this.encoding;
            return httpClient;
        }


        public HttpStatusCode ResponseCode
        {
            get
            {
                return responseCode;
            }
        }
        public string ResponseUrl
        {
            get
            {
                return responseUrl;
            }
        }


        #region Cookies
        private CookieCollection cookies = new CookieCollection(); 
        public string CookieText()
        {
            string res = String.Empty;
            foreach (KeyValuePair <string , string> cook in SetCookies)
            {
                res += cook.Key + "=" + cook.Value + "; ";
            }
            return res;
        }

        public string GetCookie(int index)
        {
            if (index >= cookies.Count) throw new IndexOutOfRangeException();   
           return cookies[index].Value;  
        }

        public string GetCookie (string key)
        {
            return cookies [key].Value; 
        }


        #endregion

        public HttpClient()
        {
            AllowControlRedirect = true;
            UseProxy  = false;
            UseCookies = true;
        }

        #region Headers
        public void SetProperty(string key, string value)
        {
            RequestProperies[key.ToLower()] = value;
        }

        public void ClearHeaders()
        {
            RequestProperies.Clear();
        }

   

       public void ParseCookies(String setCookie)
        {
            if (setCookie == null) return;
            string[] scookies = setCookie.Split(',');
            if (scookies != null)
            {
                foreach (string s in scookies)
                {
                    string[] vals = s.Split(';');

                    if (vals.Length > 1 && vals[0].Contains("="))
                    {
                        int p = vals[0].IndexOf("=");
                        string key = vals[0].Substring(0 , p ).Trim();
                        string value = vals[0].Substring(p + 1, vals[0].Length - p - 1  ).Trim();
                        SetCookies[key] = value;
                    }
                }
            }
        }

        public void AddCookies(string Cookie)
        {
            if (Cookie == null ) return;
            string[] cookies = Cookie.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            if (cookies == null) return;
            foreach (String s in cookies)
            {
                string[] values = s.Split('=');
                if (values != null && values.Length >= 2)
                {
                    SetCookies[values[0]] = values[1];
                }
            }
        }
        
        private void AddCookies()
        {
            this.RequestHeaders["Cookie"] = String.Empty;
            foreach (KeyValuePair<string, string> pair in SetCookies)
            {
                RequestHeaders["Cookie"] += pair.Key + "=" + pair.Value + "; ";
            }
        }


        private void InitRequest(HttpWebRequest request)
        {
          
           foreach (KeyValuePair<string , string>  pair in RequestProperies)
           {
               if (pair.Key == "useragent") request.UserAgent  = pair.Value;
               else if (pair.Key == "accept") request.Accept = pair.Value;
               else if (pair.Key == "referer") request.Referer = pair.Value;
               else if (pair.Key == "contenttype") request.ContentType = pair.Value;
               else if (pair.Key == "contentlength") request.ContentLength = long.Parse(pair.Value);
               else if (pair.Key == "timeout")  request.Timeout = int.Parse(pair.Value);
           }
        }
     #endregion



        #region Post and Get Methods

        private void WriteToStream(Stream src, Stream dest, long len)
        {
            byte[] buffer = new byte[DownloadStep];
            int br = src.Read(buffer, 0, buffer.Length);
            int ball = 0;
            while (br > 0)
            {
                dest.Write(buffer, 0, br);
                ball += br;
                this.DoProgress((long)ball, (long)len, String.Empty);
                br = src.Read(buffer, 0, buffer.Length);
            }
        }

        // получение данных страницы
        public string Get(string sUrl)
        {
            StartRequest();
            try
            {
                MemoryStream stream = new MemoryStream();
                _get(sUrl, stream);
                stream.Seek(0, SeekOrigin.Begin);
                StreamReader reader = new StreamReader(stream, encoding);
                return reader.ReadToEnd();
            }
            finally
            {
                EndRequest();
            }
        }

        public string Post(string sUrl, string sPostData)
        {
            StartRequest();
            try
            {
                MemoryStream stream = new MemoryStream();
                byte[] bytes = encoding.GetBytes(sPostData);
                stream.Write(bytes, 0, bytes.Length);
                stream.Seek(0, SeekOrigin.Begin);
                MemoryStream response = new MemoryStream();
                _post(sUrl, stream, response, stream.Length);
                response.Seek(0, SeekOrigin.Begin);
                StreamReader reader = new StreamReader(response, encoding);
                return reader.ReadToEnd();
            }
            finally
            {
                EndRequest();
            }
        }

        public void Get(string sUrl, Stream stream)
        {
            StartRequest();
            try
            {
                _get(sUrl, stream);
            }
            finally
            {
                EndRequest();
            }
        }

        public void Post(string sUrl, string sPostData, Stream sResponse)
        {
            StartRequest();
            try
            {
                MemoryStream stream = new MemoryStream();
                byte[] bytes = encoding.GetBytes(sPostData);
                stream.Write(bytes, 0, bytes.Length);
                stream.Seek(0, SeekOrigin.Begin);
                _post(sUrl, stream, sResponse, stream.Length);
            }
            finally
            {
                EndRequest();
            }
        }

        public string Post(string sUrl, Stream content)
        {
            StartRequest();
            try
            {
                content.Seek(0, SeekOrigin.Begin);
                MemoryStream response = new MemoryStream();
                _post(sUrl, content, response, content.Length);
                response.Seek(0, SeekOrigin.Begin);
                StreamReader reader = new StreamReader(response, encoding);
                return reader.ReadToEnd();
            }
            finally
            {
                EndRequest();
            }
        }

       
      
        private void _get(string sUrl, Stream stream)
        {
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(sUrl);
            httpWebRequest.AllowAutoRedirect = AllowAutoRedirect;
            if (sUrl.Contains("https")) SetCredencials(httpWebRequest);
            else httpWebRequest.Proxy = null;
            if (UseProxy)
            {
                SetProxy(httpWebRequest); // если используем прокси
                SetCredencials(httpWebRequest);
                if (sUrl.Contains("https")) httpWebRequest.Proxy.Credentials = CredentialCache.DefaultNetworkCredentials;
                ServicePointManager.Expect100Continue = true;
                httpWebRequest.PreAuthenticate = true;
            }
         
            
            //   SetProperty("ContentType", "");
            if (UseCookies && cookies != null)
            {   // добавляем куки в запрос
                //  httpWebRequest.CookieContainer.Add(cookies);
                AddCookies(); // добавляем куки в запрос
            }
            httpWebRequest.Headers.Add(RequestHeaders); // добавляем заголовки в запрос
            InitRequest(httpWebRequest);
          //  if (UseCookies) httpWebRequest.CookieContainer = new CookieContainer();
           
            HttpWebResponse httpWebResponse = null;
            try
            {
                httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                responseUrl = sUrl;
                long len = httpWebResponse.ContentLength;
                ResponseHeaders.Clear();
                ResponseHeaders.Add(httpWebResponse.Headers);
                responseCode = httpWebResponse.StatusCode;
                if (len == -1)
                    try
                    {
                        len = long.Parse(httpWebResponse.Headers.Get("Content-Length"));
                    }
                    catch (ArgumentNullException)
                    {
                        len = 0;
                    }

                if (UseCookies && httpWebResponse.Cookies != null)
                {
                  //  ClearCookies();
                    cookies.Add(httpWebResponse.Cookies);
                    string sCookie = httpWebResponse.Headers["Set-Cookie"];
                    ParseCookies(sCookie);
                }
                if (httpWebResponse.StatusCode == HttpStatusCode.Redirect && AllowControlRedirect && !AllowAutoRedirect)
                {
                    bool Handled = false;
                    string path = httpWebResponse.Headers["location"];
                    this.SetProperty("Referer", sUrl);
                    if (!path.Contains("http"))
                    {
                        path =  "http://" + new Uri(sUrl).Authority + path;
                    }
                    //AddCookiesInRequest();
                    DoRedirect(httpWebRequest, httpWebResponse, ref Handled, ref path);
                    if (!Handled)
                    {
                        httpWebResponse.Close();
                        httpWebResponse = null;
                        _get(path, stream);
                    }
                    return;
                }

                try
                {
                    Stream responseStream = (Stream)httpWebResponse.GetResponseStream();
                    // пишем в выходной поток
                    WriteToStream(responseStream, stream, len);
                }
                catch (ProtocolViolationException)
                {

                }
            }
            catch (WebException e)
            {
                //if (e.Response != null) WriteToStream(e.Response.GetResponseStream(), stream , 0);
                throw;
            }
            finally
            {
                if (httpWebResponse != null) httpWebResponse.Close();
            }
        }

        public static void s(string a, string b)
        {
            StreamWriter w = new StreamWriter(a, false , Encoding.GetEncoding ("windows-1251"));
            w.WriteLine(b);
            w.Close();
        }



        private void _post(string sUrl, Stream sPost, Stream sGet, long content_len)
        {
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(sUrl);
            httpWebRequest.AllowAutoRedirect = AllowAutoRedirect;
           if (sUrl.Contains("https")) SetCredencials(httpWebRequest);
            if (UseProxy)
            {
                SetProxy(httpWebRequest); // если используем прокси
                SetCredencials(httpWebRequest);
               if (sUrl.Contains("https")) httpWebRequest.Proxy.Credentials = CredentialCache.DefaultNetworkCredentials;
              // httpWebRequest.Credentials = new NetworkCredential("mycert", "hello");
              // ServicePointManager.Expect100Continue = true;
               httpWebRequest.PreAuthenticate = true;
            }
            else httpWebRequest.Proxy = null;
            
          //  if (UseCookies) httpWebRequest.CookieContainer = new CookieContainer();
            if (UseCookies && cookies != null)
            {   // добавляем куки в запрос
               // httpWebRequest.CookieContainer.Add(cookies);
                AddCookies(); // добавляем куки в запрос
            }

            httpWebRequest.Headers.Add(RequestHeaders); // добавляем заголовки в запрос
            InitRequest(httpWebRequest);

             HttpWebResponse httpWebResponse = null;
            try
            {
            httpWebRequest.Method = "POST";
            // кодируем пост запрос
            httpWebRequest.ContentLength = content_len;
            Stream QueryStream = httpWebRequest.GetRequestStream();
            WriteToStream(sPost, QueryStream, content_len);
            QueryStream.Close();

                httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                responseUrl = sUrl;
                long len = httpWebResponse.ContentLength;
                ResponseHeaders.Clear();
                ResponseHeaders.Add(httpWebResponse.Headers);
                responseCode = httpWebResponse.StatusCode;
                if (len == -1)
                    try
                    {
                        len = long.Parse(httpWebResponse.Headers.Get("Content-Length"));
                    }
                    catch (ArgumentNullException)
                    {
                        len = 0;
                    }

                if (UseCookies && httpWebResponse.Cookies != null)
                {
                   // ClearCookies();
                    cookies.Add(httpWebResponse.Cookies);
                    string sCookie = httpWebResponse.Headers["Set-Cookie"];
                    ParseCookies(sCookie);
                }
                if (httpWebResponse.StatusCode == HttpStatusCode.Redirect && AllowControlRedirect && !AllowAutoRedirect)
                {
                    bool Handled = false;
                    string path = httpWebResponse.Headers["location"];
                    this.SetProperty("Referer", sUrl);
                    if (!path.Contains("http"))
                    {
                        path = "http://" + new Uri(sUrl).Authority + path;
                    }
                   
                    DoRedirect(httpWebRequest, httpWebResponse, ref Handled, ref path);
                    
                    if (!Handled)
                    {
                        SetProperty("Referer", sUrl);
                        httpWebResponse.Close();
                        httpWebResponse = null;
                        _get(path, sGet);
                    }
                    return;
                }
                try
                {
                    Stream responseStream = (Stream)httpWebResponse.GetResponseStream();
                    // пишем в выходной поток
                    WriteToStream(responseStream, sGet, len);
                }
                catch (ProtocolViolationException)
                {

                }
            }
            catch (WebException e )
            {
              //  if (e.Response != null) WriteToStream(e.Response.GetResponseStream(), sGet, 0);
                throw;
            }
            finally
            {
                if (httpWebResponse != null) httpWebResponse.Close();
            }
        }
        #endregion

        #region ClassEvents
        public class RedirectEventArgs : EventArgs
        {
            public readonly HttpWebRequest httpWebRequest;
            public readonly HttpWebResponse httpWebResponse;
            public bool Handled;
            public string Path;

            public RedirectEventArgs (HttpWebRequest httpWebRequest , HttpWebResponse httpWebResponse, string path)
            {
                 this.httpWebRequest = httpWebRequest;
                 this.httpWebResponse = httpWebResponse;
                 Handled = false;
                 Path = path;
            }
        }

        public class DownloadEventArgs : EventArgs
        {
            public readonly long bytes_read , bytes_all ;
            public readonly string what;
            public DownloadEventArgs (long br , long ba, string what)
            {
                 bytes_read = br;
                 bytes_all = ba;
                 this.what = what;
            }
        }

       
        
        public  delegate void ProgressEventHandler (object sender, DownloadEventArgs e); // обработчик процесса загрузки
        public event ProgressEventHandler ProcessProgress;
        
        public event EventHandler RequestStart; // перед загрузкой
        public event EventHandler RequestEnd; // после загрузки

        public delegate void RedirectHandler(object sender, RedirectEventArgs e);
        public event RedirectHandler Redirect;

        protected virtual void OnRedirect(object sender , RedirectEventArgs e)
        {
            if (Redirect != null)
            {
                Redirect (this, e);
            }
        }

        private void DoRedirect(HttpWebRequest httpWebRequest, HttpWebResponse httpWebResponse, ref bool Handled, ref string path)
        {
            RedirectEventArgs e = new RedirectEventArgs(httpWebRequest, httpWebResponse, path);
            OnRedirect(this, e);
            Handled = e.Handled;
            path = e.Path;
        }


        protected virtual void OnRequestStart (EventArgs e)
        {
            if (RequestStart != null)
            {
                RequestStart (this, e);
            }
        }

        protected virtual void OnRequestEnd (EventArgs e)
        {
            if (RequestEnd != null)
            {
                RequestEnd (this, e);
            }
        }

        protected virtual void OnProcessProgress(DownloadEventArgs e)
        {
            if (ProcessProgress != null)
            {
                ProcessProgress(this, e);
            }
        }

  

        public void DoProgress(long bytes_read, long bytes_all, string what)
        {
            OnProcessProgress(new DownloadEventArgs (bytes_read,  bytes_all, what));
        }
       
        private void StartRequest()
        {
            OnRequestStart(null);
        }

        private void EndRequest()
        {
            OnRequestEnd (null);
        }
        #endregion


        #region Credencials

        private static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }


        private void SetCredencials (HttpWebRequest httpWebRequest)
        {
            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);

            store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

            X509Certificate2Collection scollection = (X509Certificate2Collection)store.Certificates.Find(X509FindType.FindByTimeValid, DateTime.Now, false);

         
                foreach (X509Certificate2 x509 in scollection)
                {
                    httpWebRequest.ClientCertificates.Add(x509);
                }
      
            httpWebRequest.Credentials = CredentialCache.DefaultCredentials;
            ServicePointManager.ServerCertificateValidationCallback  +=
 new System.Net.Security.RemoteCertificateValidationCallback (CheckValidationResult);
        }

        // установка proxy в формате host:port:user:pass или host:port
        private void SetProxy(HttpWebRequest request)
        {
            string[] proxyparams = sProxy.Split(':');
            try
            {
                WebProxy proxy = new WebProxy(proxyparams[0], int.Parse(proxyparams[1]));
                if (proxyparams.Length == 4)
                {
                    NetworkCredential nc = new NetworkCredential();
                    nc.UserName = proxyparams[2];
                    nc.Password = proxyparams[3];
                    proxy.Credentials = nc;
                }
                request.Proxy = proxy;
            }
            catch (IndexOutOfRangeException)
            {
                throw;
            }
        }

#endregion

        public void ClearCookies ()
        {
            cookies = new CookieCollection();
            RequestHeaders["Cookie"] = string.Empty;
            SetCookies.Clear();
        }
    }
}
