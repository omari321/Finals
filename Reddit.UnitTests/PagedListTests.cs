using Bogus;
using Microsoft.EntityFrameworkCore;
using Reddit.Models;
using Reddit.Repositories;

namespace Reddit.UnitTests
{
    public class PagedListTests
    {

        private ApplicationDbContext CreateContext(int itemsToGenerate,out List<Post> posts)
        {
            var dbName = Guid.NewGuid().ToString();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            var context = new ApplicationDbContext(options);
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
            var id = 1;
            var postFake = new Faker<Post>()
                .RuleFor(c => c.AuthorId, _ => id++)
                .RuleFor(c=>c.Title,f=>f.Commerce.Department())
                .RuleFor(c=>c.Upvotes,_=>Random.Shared.Next(100))
                .RuleFor(c => c.Downvotes, _ => Random.Shared.Next(100))
                .RuleFor(c=>c.Content,f=>f.Commerce.ProductDescription());
            posts = postFake.Generate(itemsToGenerate);
            context.Posts.AddRange(posts);

            context.SaveChanges();
            return context;
        }

        [Fact]
        public async Task Execute_CreateAsync_WithCorrectParameters_ShouldSucceed()
        {
            using var context = CreateContext(30,out var posts);
            var pagedResult =await PagedList<Post>.CreateAsync(context.Set<Post>(),1,15);

            Assert.Equal(15,pagedResult.Items.Count);
            Assert.Equal(posts[0], pagedResult.Items.First());
            Assert.True(pagedResult.HasNextPage);
            Assert.True(!pagedResult.HasPreviousPage);
            Assert.Equal(30, pagedResult.TotalCount);
        }
        [Fact]
        public async Task Execute_CreateAsync_WhenCollectionEmpty_ShouldSucceed()
        {
            using var context = CreateContext(15, out var posts);
            var pagedResult = await PagedList<Post>.CreateAsync(context.Set<Post>(),2, 15);

            Assert.Empty(pagedResult.Items);
            Assert.True(!pagedResult.HasNextPage);
            Assert.True(pagedResult.HasPreviousPage);
            Assert.Equal(15, pagedResult.TotalCount);

        }
        [Fact]
        public async Task Execute_CreateAsync_WhenPageSizeMoreThanCollectionSize_ShouldSucceed()
        {
            using var context = CreateContext(15, out var posts);
            var pagedResult = await PagedList<Post>.CreateAsync(context.Set<Post>(), 1, 30);

            Assert.Equal(15, pagedResult.Items.Count);
            Assert.True(!pagedResult.HasNextPage);
            Assert.True(!pagedResult.HasPreviousPage);
            Assert.Equal(15, pagedResult.TotalCount);

        }
        [Fact]
        public async Task Execute_CreateAsync_TotalCountMoreThanPageSize_ShouldSucceed()
        {
            using var context = CreateContext(100, out var posts);
            var pagedResult = await PagedList<Post>.CreateAsync(context.Set<Post>(), 2, 30);

            Assert.Equal(30, pagedResult.Items.Count);
            Assert.True(pagedResult.HasNextPage);
            Assert.True(pagedResult.HasPreviousPage);
            Assert.Equal(100, pagedResult.TotalCount);

        }
        [Fact]
        public async Task Execute_CreateAsync_EmptyList_ShouldSucceed()
        {
            using var context = CreateContext(0, out var _);
            var pagedResult = await PagedList<Post>.CreateAsync(context.Set<Post>(), 1, 30);

            Assert.Equal(0, pagedResult.TotalCount);
            Assert.True(!pagedResult.HasNextPage);
        }

        [Fact]
        public async Task Execute_CreateAsync_PageSizeMoreThanItems_ShouldSucceed()
        {
            using var context = CreateContext(15, out var _);
            var pagedResult = await PagedList<Post>.CreateAsync(context.Set<Post>(), 2, 30);

            Assert.Empty(pagedResult.Items);
            Assert.Equal(15, pagedResult.TotalCount);
            Assert.True(!pagedResult.HasNextPage);
        }
        [Fact]
        public async Task Execute_CreateAsync_NegativeVariables_ShouldSucceed()
        {
            using var context = CreateContext(15, out var _);
            var pagedResult = await PagedList<Post>.CreateAsync(context.Set<Post>(), 1, -15);

            Assert.Empty(pagedResult.Items);
        }
    }
}