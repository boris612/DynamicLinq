﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PoweredSoft.DynamicLinq.Dal;
using PoweredSoft.DynamicLinq.Dal.Pocos;
using PoweredSoft.DynamicLinq;
using Microsoft.EntityFrameworkCore;
using PoweredSoft.DynamicLinq.EntityFrameworkCore;

namespace PoweredSoft.DynamicLinq.Test
{
    [TestClass]
    public class EntityFrameworkCoreTests
    {

        private BlogCoreContext GetCoreContext(string testName)
        {
            var options = new DbContextOptionsBuilder<BlogCoreContext>()
                .UseInMemoryDatabase(databaseName: testName).Options;
            return new BlogCoreContext(options);
        }


        public static void SeedForTests(BlogCoreContext context)
        {
            context.Authors.Add(new Author
            {
                FirstName = "David",
                LastName = "Lebee",
                Posts = new List<Post>()
                {
                    new Post()
                    {
                        CreateTime = DateTimeOffset.Now,
                        PublishTime = DateTimeOffset.Now,
                        Title = "New project",
                        Content = "Lots of good things coming",
                        Comments = new List<Comment>()
                        {
                            new Comment()
                            {
                                DisplayName = "John Doe",
                                Email = "john.doe@me.com",
                                CommentText = "Very interesting",
                            },
                            new Comment()
                            {
                                DisplayName = "Nice Guy",
                                Email = "nice.guy@lol.com",
                                CommentText = "Best of luck!"
                            }
                        }
                    },
                    new Post()
                    {
                        CreateTime = DateTimeOffset.Now,
                        PublishTime = null,
                        Title = "The future!",
                        Content = "Is Near"
                    }
                }
            });

            context.Authors.Add(new Author
            {
                FirstName = "Some",
                LastName = "Dude",
                Posts = new List<Post>()
                {
                    new Post() {
                        CreateTime = DateTimeOffset.Now,
                        PublishTime = DateTimeOffset.Now,
                        Title = "The One",
                        Content = "And Only"
                    },
                    new Post()
                    {
                        CreateTime = DateTimeOffset.Now,
                        PublishTime = DateTimeOffset.Now,
                        Title = "The Two",
                        Content = "And Second"
                    }
                }
            });

            context.SaveChanges();
        }

        [TestMethod]
        public void TestSimpleWhere()
        {
            var context = GetCoreContext(nameof(TestSimpleWhere));
            SeedForTests(context);

            var query = context.Authors.AsQueryable();
            query = query.Where("FirstName", ConditionOperators.Equal, "David");
            var author = query.FirstOrDefault();
            Assert.IsNotNull(author);
        }
        [TestMethod]
        public void TestWhereAnd()
        {
            var context = GetCoreContext(nameof(TestWhereAnd));
            SeedForTests(context);

            var query = context.Authors.AsQueryable();
            query = query.Query(q => q
                .Compare("FirstName", ConditionOperators.Equal, "David")
                .And("LastName", ConditionOperators.Equal, "Lebee")
            );

            var author = query.FirstOrDefault();
            Assert.IsNotNull(author);
        }

        [TestMethod]
        public void TestWhereOr()
        {
            var context = GetCoreContext(nameof(TestWhereOr));
            SeedForTests(context);

            var query = context.Authors.AsQueryable();
            query = query.Query(q => q
                .Compare("FirstName", ConditionOperators.Equal, "David")
                .Or("FirstName", ConditionOperators.Equal, "Some")
            );

            var author = query.FirstOrDefault();
            Assert.IsNotNull(author);
        }

        [TestMethod]
        public void TestGoingThroughSimpleNav()
        {
            var context = GetCoreContext(nameof(TestGoingThroughSimpleNav));
            SeedForTests(context);

            var query = context.Posts.AsQueryable();
            query = query.Include("Author").Where("Author.FirstName", ConditionOperators.Contains, "David");
            var post = query.FirstOrDefault();
            Assert.AreEqual("David", post?.Author?.FirstName);
        }

        [TestMethod]
        public void TestGoingThroughCollectionNav()
        {
            var context = GetCoreContext(nameof(TestGoingThroughCollectionNav));
            SeedForTests(context);

            var query = context.Authors.AsQueryable();
            query = query.Where("Posts.Title", ConditionOperators.Contains, "New");
            var author = query.FirstOrDefault();

            Assert.AreEqual(author?.FirstName, "David");
        }

        [TestMethod]
        public void TestGoingThrough2CollectionNav()
        {
            var context = GetCoreContext(nameof(TestGoingThrough2CollectionNav));
            SeedForTests(context);

            var query = context.Authors.AsQueryable();
            query = query.Where("Posts.Comments.Email", ConditionOperators.Contains, "@me.com");
            var author = query.FirstOrDefault();

            Assert.AreEqual(author?.FirstName, "David");
        }

        [TestMethod]
        public void TestSort()
        {
            var context = GetCoreContext(nameof(TestSort));
            SeedForTests(context);

            var query = context.Posts.AsQueryable();
            var dq = query.OrderBy("Title").ThenByDescending("Content").ToList();
            var sq = query.OrderBy(t => t.Title).ThenByDescending(t => t.Content).ToList();

            Assert.AreEqual(dq.Count, sq.Count);
            for (var i = 0; i < dq.Count; i++)
                Assert.AreEqual(dq[i].Id, sq[i].Id);
        }

        [TestMethod]
        public void TestContextTypeLessHelper()
        {
            var context = GetCoreContext(nameof(TestContextTypeLessHelper));
            SeedForTests(context);

            var queryable = context.Query(typeof(Author), q => q.Compare("FirstName", ConditionOperators.Equal, "David"));
            var result = queryable.ToObjectList();
            var first = result.FirstOrDefault() as Author;
            Assert.AreEqual(first?.FirstName, "David");
        }
    }
}
