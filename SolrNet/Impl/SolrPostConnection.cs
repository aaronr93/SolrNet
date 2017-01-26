using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using SolrNet.Exceptions;
using SolrNet.Utils;

namespace SolrNet.Impl
{
	/// <summary>
	/// Manages HTTP connection with Solr, uses POST request instead of GET in order to handle large requests
	/// </summary>
	public class PostSolrConnection : ISolrConnection
	{
		private readonly ISolrConnection conn;
		private readonly string serverUrl;

	    /// <summary>
	    /// Default timeout in milliseconds
	    /// </summary>
	    public static int Timeout { get; set; } = 10000;

		public PostSolrConnection(ISolrConnection conn, string serverUrl)
		{
			this.conn = conn;
			this.serverUrl = serverUrl;
		}

		public string Post(string relativeUrl, string s)
		{
			return conn.Post(relativeUrl, s);
		}

		public string Get(string relativeUrl, IEnumerable<KeyValuePair<string, string>> parameters)
		{
			var u = new UriBuilder(serverUrl);
			u.Path += relativeUrl;
			var request = (HttpWebRequest)WebRequest.Create(u.Uri);
			request.Method = "POST";
			request.ContentType = "application/x-www-form-urlencoded";
			var qs = string.Join("&", parameters
				.Select(kv => string.Format("{0}={1}", HttpUtility.UrlEncode(kv.Key), HttpUtility.UrlEncode(kv.Value)))
				.ToArray());
			request.ContentLength = Encoding.UTF8.GetByteCount(qs);
			request.ProtocolVersion = HttpVersion.Version11;
			request.KeepAlive = true;
			try {
			    using (var postParamsTask = request.GetRequestStreamAsync().WithTimeout(new TimeSpan(0,0,0,0, Timeout))) {
			        postParamsTask.Wait();
			        using (var postParams = postParamsTask.Result)
			        using (var sw = new StreamWriter(postParams))
			            sw.Write(qs);
			        using (var responseTask = request.GetResponseAsync()) {
                        responseTask.Wait();
			            using (var responseStream = responseTask.Result.GetResponseStream())
			            using (var sr = new StreamReader(responseStream, Encoding.UTF8, true))
			                return sr.ReadToEnd();
			        }
			    }
			}
			catch (Exception e)
			{
				throw new SolrConnectionException(e);
			}
		}

		public string PostStream(string relativeUrl, string contentType, System.IO.Stream content, IEnumerable<KeyValuePair<string, string>> getParameters) {
			return conn.PostStream(relativeUrl, contentType, content, getParameters);
		}

	}

    public static class AsyncExtensions
    {
        public static Task<T> WithTimeout<T>(this Task<T> task, TimeSpan timeout)
        {
            return Task.Factory.StartNew(() =>
            {
                var b = task.Wait((int)timeout.TotalMilliseconds);
                if (b) return task.Result;
                throw new WebException("The operation has timed out", WebExceptionStatus.Timeout);
            });
        }
    }

}
