Migration is required and applied to the database to create a table
dotnet tool run dotnet-ef migrations add MakeProductCategoryNullable --project OnlineMarket.OrleansImpl.csproj --startup-project OnlineMarket.OrleansImpl.csproj --context SellerDbContext
dotnet tool run dotnet-ef database update --project OnlineMarket.OrleansImpl.csproj --startup-project OnlineMarket.OrleansImpl.csproj
Then the test should be able to run