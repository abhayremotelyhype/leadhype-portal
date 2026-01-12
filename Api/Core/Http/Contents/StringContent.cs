using System.Text;

namespace Api
{
    public class StringContent : System.Net.Http.StringContent
    {
        #region Constructors
        public StringContent(string content) : base(content)
        {
        }

        public StringContent(string content, Encoding? encoding) : base(content, encoding)
        {
        }
        #endregion

        #region Public Properties
        private string mContentType = string.Empty;
        public string ContentType
        {
            get => mContentType;
            set
            {
                if (mContentType == value)
                    return;

                mContentType = value;
                Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mContentType);
            }
        }
        #endregion
    }
}
