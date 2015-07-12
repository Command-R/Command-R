using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;

namespace CommandR.WebApi
{
    //REF: http://bartwullems.blogspot.com/2013/03/web-api-file-upload-set-filename.html
    public class FilenameMultipartFormDataStreamProvider : MultipartFormDataStreamProvider
    {
        public FilenameMultipartFormDataStreamProvider(string path) : base(path)
        {
        }

        public override string GetLocalFileName(HttpContentHeaders headers)
        {
            return string.Format("{0}_{1}",
                Guid.NewGuid().ToString("N"),
                Path.GetFileName(headers.ContentDisposition.FileName.Replace("\"", string.Empty)));
        }
    };
}
