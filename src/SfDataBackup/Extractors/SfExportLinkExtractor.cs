using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SfDataBackup.Services;

namespace SfDataBackup.Extractors
{
    public class SfExportLinkExtractor : ISfExportLinkExtractor
    {
        private ILogger<SfExportLinkExtractor> logger;
        private ISfService service;
        private SfOptions options;

        public SfExportLinkExtractor(
            ILogger<SfExportLinkExtractor> logger,
            ISfService service,
            IOptionsSnapshot<SfOptions> optionsProvider
        )
        {
            this.logger = logger;
            this.service = service;
            this.options = optionsProvider.Value;
        }

        public async Task<SfExportLinkExtractorResult> ExtractAsync()
        {
            logger.LogInformation("Getting page source from export service page...");

            var source = await service.GetPageSourceAsync(options.ExportService.Page);

            logger.LogInformation("Extracting relative URLs from source");

            // Extract links from source
            var links = GetLinksFromSource(source);

            return new SfExportLinkExtractorResult(true, links);
        }

        public List<string> GetLinksFromSource(string source)
        {
            var links = new List<string>();

            var matches = Regex.Matches(source, options.ExportService.Regex, RegexOptions.IgnoreCase);

            // Explicit type is required
            foreach (Match match in matches)
            {
                var relativeUrlGroup = match.Groups["relurl"];
                var relativeUrl = relativeUrlGroup.Value;

                // Remove &amp; from relative URL
                relativeUrl = relativeUrl.Replace("&amp;", "&");

                logger.LogDebug("Extracted {url}", relativeUrl);

                links.Add(relativeUrl);
            }

            return links;
        }
    }

    public class SfExportLinkExtractorResult : SfResult
    {
        public IList<string> Links { get; set; }

        public SfExportLinkExtractorResult(bool success) : base(success)
        {
        }

        public SfExportLinkExtractorResult(bool success, IList<string> links) : base(success)
        {
            Links = links;
        }
    }
}