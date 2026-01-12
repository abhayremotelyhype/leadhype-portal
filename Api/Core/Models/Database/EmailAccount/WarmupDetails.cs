namespace Api
{
    public class WarmupDetails
    {
        /// <summary>
        /// If warmup details exists or needs to be created
        /// </summary>
        public bool WarmupExists { get; set; }

        /// <summary>
        /// Warmup Id
        /// </summary>
        public int? Id { get; set; }
        
        /// <summary>
        /// Warmup Key Identifier Tag
        /// </summary>
        public string? IdentifierTag { get; set; }

    }
}
