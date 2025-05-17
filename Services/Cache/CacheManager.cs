using System.Text.Json;
using StackExchange.Redis;

namespace E_Commers.Services.Cache
{
    public interface ICacheManager
    {
        Task<T?> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, string[]? tags = null);
        Task RemoveAsync(string key);
        Task RemoveByTagAsync(string tag);
        Task RemoveByTagsAsync(string[] tags);
        Task<bool> ExistsAsync(string key);
        Task<TimeSpan?> GetTimeToLiveAsync(string key);
        Task<string[]> GetTagsAsync(string key);
    }

    public class CacheManager : ICacheManager
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _database;
        private readonly ILogger<CacheManager> _logger;
        private const int DEFAULT_EXPIRY_MINUTES = 30;
        private const string TAG_PREFIX = "tag:";
        private const string KEY_TAGS_PREFIX = "key_tags:";

        public CacheManager(IConnectionMultiplexer redis, ILogger<CacheManager> logger)
        {
            _redis = redis;
            _database = _redis.GetDatabase();
            _logger = logger;
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            try
            {
                _logger.LogInformation($"Getting cache for key: {key}");
                var value = await _database.StringGetAsync(key);

                if (value.IsNull)
                {
                    _logger.LogWarning($"Cache miss for key: {key}");
                    return default;
                }

                _logger.LogInformation($"Cache hit for key: {key}");
				return JsonSerializer.Deserialize<T>(value.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting cache for key: {key}");
                return default;
            }
        }

        public async Task SetAsync<T>(
            string key,
            T value,
            TimeSpan? expiry = null,
            string[]? tags = null
        )
        {
            try
            {
                _logger.LogInformation($"Setting cache for key: {key}");
                var serializedValue = JsonSerializer.Serialize(value);
                var expiryTime = expiry ?? TimeSpan.FromMinutes(DEFAULT_EXPIRY_MINUTES);

                // Start a transaction
                var transaction = _database.CreateTransaction();

                // Set the main value
                transaction.StringSetAsync(key, serializedValue, expiryTime);

                if (tags != null && tags.Any())
                {
                    // Store tags for this key
                    var keyTagsSet = $"{KEY_TAGS_PREFIX}{key}";
                    transaction.SetAddAsync(keyTagsSet, tags.Select(t => (RedisValue)t).ToArray());
                    transaction.KeyExpireAsync(keyTagsSet, expiryTime);

                    // Add key to each tag's set
                    foreach (var tag in tags)
                    {
                        var tagKey = $"{TAG_PREFIX}{tag}";
                        transaction.SetAddAsync(tagKey, key);
                    }
                }

                // Execute the transaction
                var result = await transaction.ExecuteAsync();
                if (result)
                {
                    _logger.LogInformation(
                        $"Cache set successfully for key: {key} with tags: {string.Join(", ", tags ?? Array.Empty<string>())}"
                    );
                }
                else
                {
                    _logger.LogWarning($"Failed to set cache for key: {key}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error setting cache for key: {key}");
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                _logger.LogInformation($"Removing cache for key: {key}");

                // Get tags for this key
                var keyTagsSet = $"{KEY_TAGS_PREFIX}{key}";
                var tags = await _database.SetMembersAsync(keyTagsSet);

                var transaction = _database.CreateTransaction();

                // Remove the main value
                transaction.KeyDeleteAsync(key);

                // Remove key from tag sets
                foreach (var tag in tags)
                {
                    var tagKey = $"{TAG_PREFIX}{tag}";
                    transaction.SetRemoveAsync(tagKey, key);
                }

                // Remove the key's tag set
                transaction.KeyDeleteAsync(keyTagsSet);

                await transaction.ExecuteAsync();
                _logger.LogInformation($"Cache removed successfully for key: {key}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error removing cache for key: {key}");
            }
        }

        public async Task RemoveByTagAsync(string tag)
        {
            try
            {
                _logger.LogInformation($"Removing cache by tag: {tag}");
                var tagKey = $"{TAG_PREFIX}{tag}";
                var keys = await _database.SetMembersAsync(tagKey);

                if (keys.Any())
                {
                    var transaction = _database.CreateTransaction();

                    // Remove all keys associated with this tag
                    foreach (var key in keys)
                    {
                        var keyStr = key.ToString();
                        var keyTagsSet = $"{KEY_TAGS_PREFIX}{keyStr}";

                        // Remove the main value
                        transaction.KeyDeleteAsync(keyStr);
                        // Remove the key's tag set
                        transaction.KeyDeleteAsync(keyTagsSet);
                    }

                    // Remove the tag set itself
                    transaction.KeyDeleteAsync(tagKey);

                    await transaction.ExecuteAsync();
                    _logger.LogInformation($"Removed {keys.Length} cache entries for tag: {tag}");
                }
                else
                {
                    _logger.LogWarning($"No cache entries found for tag: {tag}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error removing cache by tag: {tag}");
            }
        }

        public async Task RemoveByTagsAsync(string[] tags)
        {
            try
            {
                _logger.LogInformation($"Removing cache by tags: {string.Join(", ", tags)}");

                // Get all keys that have any of these tags
                var allKeys = new HashSet<string>();
                foreach (var tag in tags)
                {
                    var tagKey = $"{TAG_PREFIX}{tag}";
                    var keys = await _database.SetMembersAsync(tagKey);
                    foreach (var key in keys)
                    {
                        allKeys.Add(key.ToString());
                    }
                }

                if (allKeys.Any())
                {
                    var transaction = _database.CreateTransaction();

                    // Remove all affected keys
                    foreach (var key in allKeys)
                    {
                        var keyTagsSet = $"{KEY_TAGS_PREFIX}{key}";

                        // Remove the main value
                        transaction.KeyDeleteAsync(key);
                        // Remove the key's tag set
                        transaction.KeyDeleteAsync(keyTagsSet);
                    }

                    // Remove all tag sets
                    foreach (var tag in tags)
                    {
                        var tagKey = $"{TAG_PREFIX}{tag}";
                        transaction.KeyDeleteAsync(tagKey);
                    }

                    await transaction.ExecuteAsync();
                    _logger.LogInformation(
                        $"Removed {allKeys.Count} cache entries for tags: {string.Join(", ", tags)}"
                    );
                }
                else
                {
                    _logger.LogWarning(
                        $"No cache entries found for tags: {string.Join(", ", tags)}"
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error removing cache by tags: {string.Join(", ", tags)}");
            }
        }

        public async Task<bool> ExistsAsync(string key)
        {
            try
            {
                _logger.LogInformation($"Checking if cache exists for key: {key}");
                return await _database.KeyExistsAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking cache existence for key: {key}");
                return false;
            }
        }

        public async Task<TimeSpan?> GetTimeToLiveAsync(string key)
        {
            try
            {
                _logger.LogInformation($"Getting TTL for key: {key}");
                return await _database.KeyTimeToLiveAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting TTL for key: {key}");
                return null;
            }
        }

        public async Task<string[]> GetTagsAsync(string key)
        {
            try
            {
                _logger.LogInformation($"Getting tags for key: {key}");
                var keyTagsSet = $"{KEY_TAGS_PREFIX}{key}";
                var tags = await _database.SetMembersAsync(keyTagsSet);
                return tags.Select(t => t.ToString()).ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting tags for key: {key}");
                return Array.Empty<string>();
            }
        }
    }
}
