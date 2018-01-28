using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using RestSharp;
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

		public string Get(string relativeUrl, IEnumerable<KeyValuePair<string, string>> parameters) {
		    var restClient = new RestClient(serverUrl);

		    var restRequest = new RestRequest(relativeUrl, Method.POST);
		    restRequest.Timeout = Timeout;
		    restRequest.RequestFormat = DataFormat.Xml;
            foreach (var keyValuePair in parameters) {
                restRequest.AddParameter(keyValuePair.Key,
                    keyValuePair.Value);
            }

		    var resultTask = restClient.ExecuteTaskAsync(restRequest);
		    resultTask.Wait();


		    if (resultTask.Result.ResponseStatus != ResponseStatus.Completed) {
		        throw new SolrConnectionException("Timeout querying " + serverUrl);
		    } else if ((int) resultTask.Result.ResponseStatus >= 400) {
		        throw new SolrConnectionException("Error querying " + serverUrl);
		    }

		    return resultTask.Result.Content;
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
