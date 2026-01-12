namespace Api
{
    public class HttpRequestMessage : System.Net.Http.HttpRequestMessage
    {
        #region Private Fields
        private string? mAccept;
        private string? mAcceptLanguage;
        private string? mUserAgent;
        private string? mReferrer;
        private string? mOrigin;
        #endregion

        #region Indexer
        public string this[string name]
        {
            get { return Headers.GetValues(name).FirstOrDefault() ?? ""; }
            set { InsertHeader(name, value); }
        }
        #endregion

        #region Public Properties
        public string? Accept
        {
            get => mAccept;
            set
            {
                mAccept = value;

                string name = nameof(Accept);
                InsertHeader(name, value);
            }
        }

        public string? AcceptLanguage
        {
            get => mAcceptLanguage;
            set
            {
                if (mAcceptLanguage == value)
                    return;

                mAcceptLanguage = value;

                string name = nameof(AcceptLanguage);
                InsertHeader(name, value);
            }
        }

        public string? UserAgent
        {
            get => mUserAgent;
            set
            {
                mUserAgent = value;

                string name = nameof(UserAgent);
                InsertHeader(name, value);
            }
        }

        public string? Origin
        {
            get => mOrigin;
            set
            {
                mOrigin = value;

                string name = nameof(Origin);
                InsertHeader(name, value);
            }
        }

        public string? Referrer
        {
            get => mReferrer;
            set
            {
                mReferrer = value;

                string name = nameof(Referrer);
                InsertHeader(name, value);
            }
        }

        #endregion

        #region Internal Methods
        internal void InsertHeader(string key, string? value)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
                return;

            switch (key)
            {
                case "Accept":
                    Headers.Accept.Clear();
                    Headers.Accept.TryParseAdd(value);
                    break;
                case "UserAgent":
                    Headers.UserAgent.Clear();
                    Headers.UserAgent.TryParseAdd(value);
                    break;
                case "AcceptLanguage":
                    Headers.AcceptLanguage.Clear();
                    Headers.AcceptLanguage.TryParseAdd(value);
                    break;
                case "Referrer":
                    Headers.Referrer = new Uri(value);
                    break;
                default:
                    //Remove if header already exists, so we can add with updated value
                    if (Headers.Contains(key))
                        Headers.Remove(key);

                    Headers.TryAddWithoutValidation(key, value);
                    break;
            }
        }
        #endregion

    }
}
