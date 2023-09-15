using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using WebApiDistributedSqlCache.Model;

namespace WebApiDistributedSqlCache.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly IDistributedCache _distributedCache;
        private readonly IMemoryCache _memoryCache;
        private readonly string WeatherForecastKey = "WeatherForecast";

        public WeatherForecastController(ILogger<WeatherForecastController> logger, IDistributedCache distributedCache, IMemoryCache memoryCache)
        {
            _logger = logger;
            _distributedCache = distributedCache;
            _memoryCache = memoryCache;
        }

        private static SubTypeObjects GenerateResponse2()
        {
            SubTypeObjects stobs = new SubTypeObjects();
            stobs.SubTypes = new List<SubTypeObject>();
            for (int i = 0; i < 10; i++)
            {
                var item = new SubTypeObject();
                item.Name = "Name->" + i.ToString();
                item.Type = i;
                item.SubType = i * 10;
                stobs.SubTypes.Add(item);
            }
            return stobs;
        }

        [HttpGet("getst")]
        public async Task<SubTypeObjects> GetST()
        {
            var objectFromCachex = _memoryCache.Get(WeatherForecastKey);

            //byte[] objectFromCache = await _distributedCache.GetAsync(WeatherForecastKey);

            if (objectFromCachex != null)
            {
                _logger.LogInformation("***Read from MCcache...");
                // Deserialize it
                var jsonToDeserialize = System.Text.Encoding.UTF8.GetString((byte[])objectFromCachex);

                var cachedResult = JsonSerializer.Deserialize<SubTypeObjects>(jsonToDeserialize);
                if (cachedResult != null)
                {
                    // If found, then return it
                    return cachedResult;
                }
            }
            _logger.LogInformation("***Fill MCache...");
            // If not found, then recalculate response
            var result = GenerateResponse2();

            // Serialize the response

            object obb = result;
            byte[] objectToCache = JsonSerializer.SerializeToUtf8Bytes(result);
            byte[] obbToCache = JsonSerializer.SerializeToUtf8Bytes(obb);

            AddObject("hm",obbToCache);

           // weatherForecastCollection = GetWeatherForecast();

            var cacheMemOptions = new MemoryCacheEntryOptions()
                .SetSize(1)  // Set size of current item
                .SetSlidingExpiration(TimeSpan.FromSeconds(10))
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(30));
            // Cache it
            _memoryCache.Set(WeatherForecastKey, objectToCache, cacheMemOptions);

            return result;
        }

        [HttpGet("getmem")]
        public async Task<IEnumerable<WeatherForecast>> GetMem()
        {
            IEnumerable<WeatherForecast> weatherForecastCollection = null;
            // Find cached item
            var objectFromCachex = _memoryCache.Get(WeatherForecastKey);

            //byte[] objectFromCache = await _distributedCache.GetAsync(WeatherForecastKey);

            if (objectFromCachex != null)
            {
                _logger.LogInformation("***Read from MCcache...");
                // Deserialize it
                var jsonToDeserialize = System.Text.Encoding.UTF8.GetString((byte[])objectFromCachex);

                var cachedResult = JsonSerializer.Deserialize<IEnumerable<WeatherForecast>>(jsonToDeserialize);
                if (cachedResult != null)
                {
                    // If found, then return it
                    return cachedResult;
                }
            }
            _logger.LogInformation("***Fill MCache...");
            // If not found, then recalculate response
            var result = GenerateResponse();

            // Serialize the response

            object obb = result;
            byte[] objectToCache = JsonSerializer.SerializeToUtf8Bytes(result);
            byte[] obbToCache = JsonSerializer.SerializeToUtf8Bytes(obb);

            AddObject("hm", obbToCache);

            var cacheEntryOptions = new DistributedCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromSeconds(10))
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(30));

            weatherForecastCollection = GetWeatherForecast();

            var cacheMemOptions = new MemoryCacheEntryOptions()
                .SetSize(1)  // Set size of current item
                .SetSlidingExpiration(TimeSpan.FromSeconds(10))
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(30));
            // Cache it
            _memoryCache.Set(WeatherForecastKey, objectToCache, cacheMemOptions);

            return result;
        }
        private void AddObject(string keyPrefix, object obj)//, Type type)
        {
            var nmo = nameof(AddObject);
            _logger.LogDebug($"{nmo}");

            byte[] objectToCache = JsonSerializer.SerializeToUtf8Bytes(obj);//, type);

            var cacheEntryOptions = new DistributedCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromSeconds(30))
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(60));
        }
            private static IEnumerable<WeatherForecast> GetWeatherForecast()
        {
            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            })
            .ToArray();
        }

        [HttpGet]
        public async Task<IEnumerable<WeatherForecast>> Get()
        {
            // Find cached item
            byte[] objectFromCache = await _distributedCache.GetAsync(WeatherForecastKey);

            if (objectFromCache != null)
            {
                _logger.LogInformation("***Read from DCcache...");
                // Deserialize it
                var jsonToDeserialize = System.Text.Encoding.UTF8.GetString(objectFromCache);
                var cachedResult = JsonSerializer.Deserialize<IEnumerable<WeatherForecast>>(jsonToDeserialize);
                if (cachedResult != null)
                {
                    // If found, then return it
                    return cachedResult;
                }
            }
            _logger.LogInformation("***Fill DCache...");
            // If not found, then recalculate response
            var result = GenerateResponse();

            // Serialize the response
            byte[] objectToCache = JsonSerializer.SerializeToUtf8Bytes(result);
            var cacheEntryOptions = new DistributedCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromSeconds(20))
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(30));

            // Cache it
            await _distributedCache.SetAsync(WeatherForecastKey, objectToCache, cacheEntryOptions);

            return result;
        }

        private static IEnumerable<WeatherForecast> GenerateResponse()
        {
            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            })
            .ToArray();
        }

    }
}
