﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using PoweredSoft.DynamicLinq.Dal.Pocos;
using PoweredSoft.DynamicLinq.Test.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoweredSoft.DynamicLinq.Test
{
    public class Mock
    {
        public int Id { get; set; }
        public int ForeignId { get; set; }
        public decimal Total { get; set; }

        public List<MockB> Bs { get; set; } = new List<MockB>();
    }

    public class MockB
    {
        public List<string> FirstNames { get; set; }
    }

    [TestClass]
    public class SelectTests
    {
        [TestMethod]
        public void TestSelect()
        {
            var list = new List<Mock>()
            {
                new Mock{
                    Id  = 1,
                    ForeignId = 1,
                    Total = 100,
                    Bs = new List<MockB>() {
                        new MockB { FirstNames =  new List<string>{"David", "John" } }
                    }
                },
            };

            var regularSyntaxA = list
                .AsQueryable()
                .Select(t => new
                {
                    Id = t.Id,
                    FirstNames = t.Bs.SelectMany(t2 => t2.FirstNames).ToList(),
                    FirstNamesLists = t.Bs.Select(t2 => t2.FirstNames).ToList()
                });
            
            var regularSyntax = regularSyntaxA.ToList();

            var dynamicSyntax = list
                .AsQueryable()
                .Select(t =>
                {
                    t.Path("Id");
                    t.PathToList("Bs.FirstNames", "FirstNames", SelectCollectionHandling.Flatten);
                    t.PathToList("Bs.FirstNames", "FirstNamesLists", SelectCollectionHandling.LeaveAsIs);
                })
                .ToDynamicClassList();

            Assert.AreEqual(regularSyntax.Count, dynamicSyntax.Count);
            for(var i = 0; i < regularSyntax.Count; i++)
            {
                Assert.AreEqual(regularSyntax[i].Id, dynamicSyntax[i].GetDynamicPropertyValue<int>("Id"));
                QueryableAssert.AreEqual(regularSyntax[i].FirstNames.AsQueryable(), dynamicSyntax[i].GetDynamicPropertyValue<List<string>>("FirstNames").AsQueryable());


                var left = regularSyntax[i].FirstNamesLists;
                var right = dynamicSyntax[i].GetDynamicPropertyValue<List<List<string>>>("FirstNamesLists");
                Assert.AreEqual(left.Count, right.Count);
                for(var j = 0; j < left.Count; j++)
                    QueryableAssert.AreEqual(left[j].AsQueryable(), right[j].AsQueryable());
            }
        }

        [TestMethod]
        public void SelectNullChecking()
        {
            var query = TestData.Authors.AsQueryable();

            var qs = query.Select(t => new
            {
                CommentLikes = t.Posts == null ? 
                    new List<CommentLike>() :
                    t.Posts.Where(t2 => t2.Comments != null).SelectMany(t2 => t2.Comments.Where(t3 => t3.CommentLikes != null).SelectMany(t3 => t3.CommentLikes)).ToList()
            });

            var a = qs.ToList();

            var querySelect = query.Select(t =>
            {
                t.NullChecking(true);
                t.PathToList("Posts.Comments.CommentLikes", selectCollectionHandling: SelectCollectionHandling.Flatten);
            });

            var b = querySelect.ToDynamicClassList();

            Assert.AreEqual(a.Count, b.Count);
            for(var i = 0; i  < a.Count; i++)
            {
                var left = a[i];
                var right = b[i];

                var leftCommentLikes = left.CommentLikes;
                var rightCommentLikes = right.GetDynamicPropertyValue<List<CommentLike>>("CommentLikes");
                QueryableAssert.AreEqual(leftCommentLikes.AsQueryable(), rightCommentLikes.AsQueryable());
            }
        }

        [TestMethod]
        public void SelectNullChecking2()
        {
            var query = TestData.Likes.AsQueryable();

            var qs = query.Select(t => new
            {
                Post = t.Comment == null || t.Comment.Post == null ? null : t.Comment.Post,
                Texts = (t.Comment == null || t.Comment.Post == null || t.Comment.Post.Comments == null ? new List<string>() : t.Comment.Post.Comments.Select(t2 => t2.CommentText)).ToList()
            });

            var a = qs.ToList();

            var querySelect = query.Select(t =>
            {
                t.NullChecking(true);
                // this needs to be fixed.
                t.PathToList("Comment.Post.Comments.CommentText", selectCollectionHandling: SelectCollectionHandling.Flatten);
            });

            var b = querySelect.ToDynamicClassList();
        }
    }
}
