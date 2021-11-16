using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using Newtonsoft.Json;
using System.Threading;
using System.Globalization;
using System.Reflection;
using System.Net.Mail;
using System.Net.NetworkInformation;
using System.Net.Http;

namespace OnlineNotify
{
    class LineNotify
    {
        private class Root
        {
            public MWHEADER MWHEADER { get; set; }
            public TRANRQ TRANRQ { get; set; }
            public TRANRS TRANRS { get; set; }
        }

        private class MWHEADER
        {
            public string MSGID { get; set; }
            public string SOURCECHANNEL { get; set; }
            public string TXNSEQ { get; set; }

            public string O360SEQ { get; set; }
            public string RETURNCODE { get; set; }
            public string RETURNDESC { get; set; }

        }
        private class TRANRQ //宣告CTEAM物件
        {
            public string account { get; set; }

            public string apiKey { get; set; }
            public string teamSn { get; set; }
            public string contentType { get; set; }
            public string textContent { get; set; }
            public string mediaContent { get; set; }
            public string fileShowName { get; set; }
            public string subject { get; set; }
        }

        private class TRANRS
        {
            public bool IsSuccess { get; set; }
            public string Description { get; set; }            
            public int ErrorCode { get; set; }
            public string BatchID { get; set; }
            public string TeamNunber { get; set; }
        }




        public void Check_Service()
        {
            //偵測錯誤曲塊 開始

            string Notify_Wrd = "MH/BH 錯誤測試!" + DateTime.Now.ToLongTimeString() ;


            //偵測錯誤曲塊 結束


            Send_Line(Notify_Wrd); //呼叫line通知
            Send_CTeam(Notify_Wrd); //呼叫Cteam通知
        }


        public static bool ValidateServerCertificate(Object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        //呼叫Line 通知
        public void Send_Line(string Error_Wrd)
        {
            try
            {
                //設定 HTTPS 連線時，不要理會憑證的有效性問題
                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(ValidateServerCertificate);

                ServicePointManager.SecurityProtocol = (SecurityProtocolType)(0xc0 | 0x300 | 0xc00);
                HttpWebRequest objHttpWebRequest = (HttpWebRequest)WebRequest.Create("https://notify-api.line.me/api/notify");
                objHttpWebRequest.ContentType = "application/x-www-form-urlencoded";
                objHttpWebRequest.Method = WebRequestMethods.Http.Post;
                objHttpWebRequest.Timeout = 50000;

                //設定Line Proxy Server Port
                string sProxy_Server = ConfigurationManager.AppSettings["sProxy_Server"];
                int sProxy_Port = int.Parse(ConfigurationManager.AppSettings["sProxy_Port"]);

                objHttpWebRequest.Proxy = new WebProxy(sProxy_Server , sProxy_Port);  //設定連線

                //if (Environment.MachineName.Substring(0, 2).ToUpper() != "PI")
                //{
                //    //objHttpWebRequest.Proxy = new WebProxy("dxwsgut", 80);  //測試
                //    objHttpWebRequest.Proxy = new WebProxy("dxsrvgw", 80);  //測試
                //}
                //else
                //{
                //    //objHttpWebRequest.Proxy = new WebProxy("pxwsgsrv", 80);  //正式
                //    objHttpWebRequest.Proxy = new WebProxy("pxsrvgw", 80);  //正式
                //}

                //設定Token
                string sLineToken = ConfigurationManager.AppSettings["sLineToken"];

                objHttpWebRequest.Headers.Add("Authorization", "Bearer " + sLineToken);
                //objHttpWebRequest.Headers.Add("Authorization", "Bearer rAMmvmxpiLuB9vo1Iq1IBC05e0Qi1JvfPDIKa7ck4eI");

                string strPostData = "message=" + Error_Wrd;

                var x = new System.Text.UTF8Encoding();

                byte[] byteContent = x.GetBytes(strPostData);

                objHttpWebRequest.ContentLength = byteContent.Length;


                using (Stream st = objHttpWebRequest.GetRequestStream())
                {
                    st.Write(byteContent, 0, byteContent.Length);
                }


                HttpWebResponse myHttpWebResponse = (HttpWebResponse)objHttpWebRequest.GetResponse();
                if (myHttpWebResponse.StatusCode == HttpStatusCode.OK)
                {

                    ((int)myHttpWebResponse.StatusCode).ToString(); //RetCode = 200

                    Stream receiveStream = myHttpWebResponse.GetResponseStream();
                    Encoding encode = Encoding.GetEncoding("utf-8");
                    using (StreamReader readStream = new StreamReader(receiveStream, encode))
                    {
                        Char[] read = new Char[256];
                        int count = readStream.Read(read, 0, 256);

                        while (count > 0)
                        {
                            // Dumps the 256 characters on a string
                            String str = new String(read, 0, count);
                            count = readStream.Read(read, 0, 256);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteMQLog(ex.Message + "\r\n" + ex.StackTrace.Trim() + "\r\n" + ex.TargetSite);
            }

        }


        //呼叫Cteam通知
        public void Send_CTeam(string Error_Wrd)
        {
            try
            {
                string sURI = ConfigurationManager.AppSettings.Get("sAPIURL"); //中台網址

                string sAP_ID = ConfigurationManager.AppSettings.Get("sAP_ID"); //APID

                string saccount = ConfigurationManager.AppSettings.Get("saccount"); //帳號
                string sapiKey = ConfigurationManager.AppSettings.Get("sapiKey"); //api_key
                string steamSn = ConfigurationManager.AppSettings.Get("steamSn"); //團隊編號
                string scontentType = ConfigurationManager.AppSettings.Get("scontentType"); //訊息內容類別 1: 文字
                string ssubject = ConfigurationManager.AppSettings.Get("ssubject") + " " + DateTime.Now.ToLongTimeString(); //訊息主旨

                string stextContent = sAP_ID + Error_Wrd; //訊息文字內容
                string mediaContent = ""; //訊息多媒體內容
                string fileShowName = ""; //檔案顯示名稱

                string Result = "";
                var Root = new Root
                {
                    MWHEADER = new MWHEADER
                    {
                        MSGID = "MIP-C-MSGALARMQ001",
                        SOURCECHANNEL = sAP_ID,
                        //SOURCECHANNEL = "MID-NT-MSG-04",
                        TXNSEQ = GenTxnSeq()
                    },
                    TRANRQ = new TRANRQ
                    {
                        account = saccount,
                        apiKey = sapiKey,
                        teamSn = steamSn,
                        contentType = scontentType,
                        textContent = stextContent,
                        mediaContent = mediaContent,
                        fileShowName = fileShowName,
                        subject = ssubject
                    }
                };

                //設定 HTTPS 連線時，不要理會憑證的有效性問題
                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(ValidateServerCertificate);
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)(0xc0 | 0x300 | 0xc00);

                //建立 HttpClient
                HttpClient client = new HttpClient();

                HttpContent content = new StringContent(JsonConvert.SerializeObject(Root));
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                var response = client.PostAsync(sURI, content).Result;//改成自己的
                response.EnsureSuccessStatusCode();//用来抛异常的

                if (response.StatusCode.ToString() == "OK")
                {
                    string res = response.Content.ReadAsStringAsync().Result.ToString();
                    //KafkaForEloanStepOneResponse KafkaResult = JsonConvert.DeserializeObject<KafkaForEloanStepOneResponse>(res);
                    //Root = JsonConvert.DeserializeObject<Root>(response.ToString());

                }
            }
            catch (Exception ex)
            {
                WriteMQLog(ex.Message + "\r\n" + ex.StackTrace.Trim() + "\r\n" + ex.TargetSite);
            }

        }



        #region WriteLog 寫入Log檔案
        private void WriteMQLog(string sMsg)
        {
            StreamWriter sWriter = null;

            string sFilePath = ConfigurationManager.AppSettings["sFilePath"];

            //資料夾不存在，建立資料夾
            if (Directory.Exists(sFilePath + @"\Log\") == false)
            {
                Directory.CreateDirectory(sFilePath + @"\Log\");
            }

            string sLogName = DateTime.Now.ToString("yyyyMMdd") + "-" + DateTime.Now.ToString("HH") + "-Log.txt";  //log記錄到小時

            sWriter = new StreamWriter(sFilePath + @"\Log\" + sLogName , true, Encoding.GetEncoding("big5"));
            sWriter.WriteLine("================" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + "============");
            sWriter.WriteLine(sMsg);
            sWriter.WriteLine("");
            sWriter.Flush();
            sWriter.Close();

        }
        #endregion


        /// <summary>
        /// 產生EAI電文交易序號，時間14碼yyyyMMddHHmmss+亂數8碼
        /// </summary>
        /// <returns></returns>
        private static string GenTxnSeq()
        {
            Random rand = new Random(Guid.NewGuid().GetHashCode());
            string strTxnSeq = DateTime.Now.ToString("yyyyMMddHHmmss").ToString() + rand.Next(0, 99999999).ToString().PadLeft(8, '0');
            return strTxnSeq;
            throw new NotImplementedException();
        }

    }
}
