using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NLipsum.Core;

namespace DemoSite.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class QuotesController : ControllerBase
    {
        private readonly Random random;
        private readonly IQuotesProvider quotesProvider;

        public QuotesController(IQuotesProvider quotesProvider)
        {
            this.random = new Random();
            this.quotesProvider = quotesProvider ?? throw new ArgumentNullException(nameof(quotesProvider));
        }

        [HttpGet]
        public async Task<string> Get()
        {
            var quotes = (await this.quotesProvider.GetAll()).ToArray();
            return quotes[this.random.Next(quotes.Length)];
        }
    }

    public class QuotesProviderComposite : IQuotesProvider
    {
        private readonly IQuotesProvider[] quotesProviders;

        public QuotesProviderComposite(params IQuotesProvider[] quotesProviders)
        {
            this.quotesProviders = quotesProviders ?? throw new ArgumentNullException(nameof(quotesProviders));
        }

        public async Task<IEnumerable<string>> GetAll() =>
            (await Task.WhenAll(this.quotesProviders.Select(i => i.GetAll()))).SelectMany(i => i);
    }

    public class NLipsumQuotesProviderAdapter : IQuotesProvider
    {
        public Task<IEnumerable<string>> GetAll() =>
            Task.FromResult(new LipsumGenerator().GenerateSentences(100).AsEnumerable());
    }

    public class QuotesProviderCache : IQuotesProvider
    {
        private readonly IQuotesProvider inner;
        private string[] cache;

        public QuotesProviderCache(IQuotesProvider inner)
        {
            this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public async Task<IEnumerable<string>> GetAll()
        {
            if (this.cache == null)
                this.cache = (await this.inner.GetAll()).ToArray();

            return this.cache;
        }
    }

    public class DukeQuotesProvider : IQuotesProvider
    {
        private readonly HttpClient httpClient;

        public DukeQuotesProvider(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public async Task<IEnumerable<string>> GetAll()
        {
            var result = await this.httpClient.GetStringAsync("http://localhost:8080");
            return JsonDocument.Parse(result).RootElement
                .GetProperty("quotes").EnumerateArray().Select(i => i.GetString());
        }
    }

    public interface IQuotesProvider
    {
        Task<IEnumerable<string>> GetAll();
    }

    public class FakeQuotesProvider : IQuotesProvider
    {
        public Task<IEnumerable<string>> GetAll() =>
            Task.FromResult(new[] { "Hello, World!" }.AsEnumerable());
    }

}
