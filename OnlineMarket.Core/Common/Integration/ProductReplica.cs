﻿namespace OnlineMarket.Core.Common.Integration;

// <sellerID-productID, versionID, Price>
// store in redis, use as a cache in cart
public sealed class ProductReplica
{
    public ProductReplica(string key, string version, float price)
    {
        this.Key = key;
        this.Price = price;
        this.Version = version;
    }

    public string Key { get; set; }

    public float Price { get; set; }

    public string Version { get; set; }

}