需要migration 并应用到database 建表
dotnet tool run dotnet-ef migrations add MakeProductCategoryNullable --project OnlineMarket.OrleansImpl.csproj --startup-project OnlineMarket.OrleansImpl.csproj --context SellerDbContext
dotnet tool run dotnet-ef database update --project OnlineMarket.OrleansImpl.csproj --startup-project OnlineMarket.OrleansImpl.csproj
然后测试应该可以跑通了