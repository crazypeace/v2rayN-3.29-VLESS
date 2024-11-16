using System;
using System.Net;
using System.Text;

namespace v2rayN.Base
{
    class WebClientEx : WebClient
    {
        public int Timeout { get; set; }

        public WebClientEx(int timeout = 3000)
        {
            Timeout = timeout;
            this.Encoding = Encoding.UTF8;
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            HttpWebRequest request;
            request = (HttpWebRequest)base.GetWebRequest(address);
            request.Timeout = Timeout;
            request.ReadWriteTimeout = Timeout;
            //request.AllowAutoRedirect = false;
            //request.AllowWriteStreamBuffering = true;

            request.ServicePoint.BindIPEndPointDelegate = (servicePoint, remoteEndPoint, retryCount) =>
            {
                if (remoteEndPoint.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                    return new IPEndPoint(IPAddress.IPv6Any, 0);
                else
                    return new IPEndPoint(IPAddress.Any, 0);
            };

            return request;
        }
    }
}
