using Microsoft.Extensions.Options;

namespace Data.Service
{
    public sealed class CacheOptions : IOptions<CacheOptions>
    {
        public TimeSpan Expiry { get; set; } = TimeSpan.FromMinutes(100);
        public TimeSpan Renewal { get; set; } = TimeSpan.FromMinutes(90);
        public bool AddPrefix { get; set; } = true;
        internal string Prefix { get; set; } = null!;
        public string Suffix { get; set; } = "IsInUse";
        public char Delimiter { get; set; } = '&';

        public CacheOptions Value => this;
    }
}
