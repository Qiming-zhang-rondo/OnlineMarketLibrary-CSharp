﻿using OnlineMarket.Core.Common.Integration;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace OnlineMarket.OrleansImpl.Infra.Redis
{
    public sealed class RedisConnectionFactoryImpl : IRedisConnectionFactory
    {
        private readonly Dictionary<string, IConnectionMultiplexer> _connections = new();

        public RedisConnectionFactoryImpl(string primaryConStr, string backupConStr)
        {
            // stackexchange.github.io/StackExchange.Redis/Configuration.html

            this._connections["Primary"] = ConnectionMultiplexer.Connect(primaryConStr);
            this._connections["ReadOnlyBackup"] = ConnectionMultiplexer.Connect(backupConStr);
        }

        public IConnectionMultiplexer GetConnection(string name)
        {
            return this._connections.TryGetValue(name, out var connection) ? connection : null;
        }

        public async Task<bool> SaveProductAsync(string key, ProductReplica productCaches)
        {
            var db = this._connections["Primary"].GetDatabase();
            var value = JsonConvert.SerializeObject(productCaches);
            return await db.StringSetAsync(key, value);
        }

        public async Task<bool> UpdateProductAsync(string key, ProductReplica productCaches)
        {
            var db = this._connections["Primary"].GetDatabase();
            var value = JsonConvert.SerializeObject(productCaches);
            return await db.StringSetAsync(key, value);
        }

        public async Task<ProductReplica> GetProductAsync(string key)
        {
            var db = this._connections["ReadOnlyBackup"].GetDatabase();
            var value = await db.StringGetAsync(key);
            return value.HasValue ? JsonConvert.DeserializeObject<ProductReplica>(value) : null;
        }

    }

}

